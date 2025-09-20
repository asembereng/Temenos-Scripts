using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Enums;
using TemenosAlertManager.Core.Interfaces;

namespace TemenosAlertManager.Infrastructure.Services;

public class AlertService : IAlertService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AlertService> _logger;

    public AlertService(IUnitOfWork unitOfWork, ILogger<AlertService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Alert> CreateAlertAsync(ICheckResult checkResult, CancellationToken cancellationToken = default)
    {
        var fingerprint = GenerateFingerprint(checkResult);
        
        // Check if we should suppress this alert
        var severity = MapCheckStatusToSeverity(checkResult.Status);
        if (await ShouldSuppressAlertAsync(fingerprint, severity, cancellationToken))
        {
            _logger.LogDebug("Alert suppressed for fingerprint: {Fingerprint}", fingerprint);
            // Return existing alert instead of creating new one
            var existingAlert = await _unitOfWork.Alerts.GetLatestAlertByFingerprintAsync(fingerprint, cancellationToken);
            return existingAlert!; // Should not be null if suppression returned true
        }

        var alert = new Alert
        {
            Title = GenerateAlertTitle(checkResult),
            Description = GenerateAlertDescription(checkResult),
            Severity = severity,
            State = AlertState.Active,
            Domain = checkResult.Domain,
            Source = checkResult.Target,
            Fingerprint = fingerprint,
            MetricValue = checkResult.Value,
            Threshold = ExtractThreshold(checkResult),
            AdditionalData = JsonSerializer.Serialize(new
            {
                ExecutionTimeMs = checkResult.ExecutionTimeMs,
                ErrorMessage = checkResult.ErrorMessage,
                Details = checkResult.Details
            })
        };

        await _unitOfWork.Alerts.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created alert {AlertId} with fingerprint {Fingerprint}", alert.Id, fingerprint);
        
        return alert;
    }

    public async Task<bool> ShouldSuppressAlertAsync(string fingerprint, AlertSeverity severity, CancellationToken cancellationToken = default)
    {
        // Get the latest alert with this fingerprint
        var latestAlert = await _unitOfWork.Alerts.GetLatestAlertByFingerprintAsync(fingerprint, cancellationToken);
        
        if (latestAlert == null)
        {
            return false; // No previous alert, don't suppress
        }

        // If the latest alert is resolved, don't suppress
        if (latestAlert.State == AlertState.Resolved)
        {
            return false;
        }

        // Check if we're within the suppression window
        var suppressionWindow = TimeSpan.FromMinutes(latestAlert.SuppressionWindowMinutes);
        var timeSinceLastAlert = DateTime.UtcNow - latestAlert.CreatedAt;
        
        return timeSinceLastAlert <= suppressionWindow;
    }

    public async Task AcknowledgeAlertAsync(int alertId, string acknowledgedBy, string? notes = null, CancellationToken cancellationToken = default)
    {
        var alert = await _unitOfWork.Alerts.GetByIdAsync(alertId, cancellationToken);
        if (alert == null)
        {
            throw new ArgumentException($"Alert with ID {alertId} not found", nameof(alertId));
        }

        if (alert.State != AlertState.Active)
        {
            throw new InvalidOperationException($"Alert {alertId} is not in active state");
        }

        alert.State = AlertState.Acknowledged;
        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.AcknowledgedBy = acknowledgedBy;
        if (!string.IsNullOrEmpty(notes))
        {
            alert.Notes = string.IsNullOrEmpty(alert.Notes) ? notes : $"{alert.Notes}\n{notes}";
        }

        await _unitOfWork.Alerts.UpdateAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alert {AlertId} acknowledged by {User}", alertId, acknowledgedBy);
    }

    public async Task ResolveAlertAsync(int alertId, string resolvedBy, string? notes = null, CancellationToken cancellationToken = default)
    {
        var alert = await _unitOfWork.Alerts.GetByIdAsync(alertId, cancellationToken);
        if (alert == null)
        {
            throw new ArgumentException($"Alert with ID {alertId} not found", nameof(alertId));
        }

        if (alert.State == AlertState.Resolved)
        {
            throw new InvalidOperationException($"Alert {alertId} is already resolved");
        }

        alert.State = AlertState.Resolved;
        alert.ResolvedAt = DateTime.UtcNow;
        alert.ResolvedBy = resolvedBy;
        if (!string.IsNullOrEmpty(notes))
        {
            alert.Notes = string.IsNullOrEmpty(alert.Notes) ? notes : $"{alert.Notes}\n{notes}";
        }

        await _unitOfWork.Alerts.UpdateAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alert {AlertId} resolved by {User}", alertId, resolvedBy);
    }

    private static string GenerateFingerprint(ICheckResult checkResult)
    {
        // Create a unique fingerprint for this type of alert
        var fingerprintData = $"{checkResult.Domain}:{checkResult.Target}:{checkResult.Metric}";
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprintData));
        return Convert.ToHexString(hash)[..16]; // Take first 16 characters
    }

    private static AlertSeverity MapCheckStatusToSeverity(CheckStatus status)
    {
        return status switch
        {
            CheckStatus.Critical => AlertSeverity.Critical,
            CheckStatus.Warning => AlertSeverity.Warning,
            CheckStatus.Error => AlertSeverity.Critical,
            _ => AlertSeverity.Info
        };
    }

    private static string GenerateAlertTitle(ICheckResult checkResult)
    {
        return $"[{checkResult.Status.ToString().ToUpper()}][{checkResult.Domain}] {checkResult.Metric}";
    }

    private static string GenerateAlertDescription(ICheckResult checkResult)
    {
        var description = $"Check failed for {checkResult.Target}:\n" +
                         $"Metric: {checkResult.Metric}\n" +
                         $"Status: {checkResult.Status}";
        
        if (!string.IsNullOrEmpty(checkResult.Value))
        {
            description += $"\nValue: {checkResult.Value}";
        }
        
        if (!string.IsNullOrEmpty(checkResult.ErrorMessage))
        {
            description += $"\nError: {checkResult.ErrorMessage}";
        }
        
        if (!string.IsNullOrEmpty(checkResult.Details))
        {
            description += $"\nDetails: {checkResult.Details}";
        }

        return description;
    }

    private static string? ExtractThreshold(ICheckResult checkResult)
    {
        // Try to extract threshold information from details
        if (string.IsNullOrEmpty(checkResult.Details))
        {
            return null;
        }

        try
        {
            var details = JsonSerializer.Deserialize<Dictionary<string, object>>(checkResult.Details);
            if (details?.ContainsKey("threshold") == true)
            {
                return details["threshold"].ToString();
            }
        }
        catch
        {
            // Ignore JSON parsing errors
        }

        return null;
    }
}