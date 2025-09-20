using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Api.Services;

public interface IMonitoringService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<ICheckResult> RunCheckAsync(string domain, string target, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckResultDto>> GetRecentChecksAsync(string? domain = null, int limit = 100, CancellationToken cancellationToken = default);
    Task<HealthSummaryDto> GetDomainHealthAsync(string domain, CancellationToken cancellationToken = default);
}

public class MonitoringService : IMonitoringService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPowerShellService _powerShellService;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(IUnitOfWork unitOfWork, IPowerShellService powerShellService, ILogger<MonitoringService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating dashboard data");

            var dashboard = new DashboardDto();

            // Get domain summaries
            var domains = Enum.GetValues<MonitoringDomain>();
            foreach (var domain in domains)
            {
                var healthSummary = await GetDomainHealthAsync(domain.ToString(), cancellationToken);
                dashboard.DomainSummaries.Add(healthSummary);
            }

            // Get recent alerts
            var recentAlerts = await _unitOfWork.Alerts.GetActiveAlertsAsync(cancellationToken);
            dashboard.RecentAlerts = recentAlerts.Take(10).Select(MapToAlertDto).ToList();

            // Get recent checks
            var recentChecks = await _unitOfWork.CheckResults.GetLatestResultsByDomainAsync("All", 20, cancellationToken);
            dashboard.RecentChecks = recentChecks.Select(MapToCheckResultDto).ToList();

            // Add system metrics
            dashboard.SystemMetrics = new Dictionary<string, object>
            {
                ["TotalAlerts"] = await _unitOfWork.Alerts.CountAsync(a => a.State == AlertState.Active, cancellationToken),
                ["CriticalAlerts"] = await _unitOfWork.Alerts.CountAsync(a => a.State == AlertState.Active && a.Severity == AlertSeverity.Critical, cancellationToken),
                ["LastRefresh"] = DateTime.UtcNow
            };

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate dashboard data");
            throw;
        }
    }

    public async Task<ICheckResult> RunCheckAsync(string domain, string target, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Running check for domain {Domain}, target {Target}", domain, target);

            // Map domain to PowerShell module
            var moduleName = GetModuleNameForDomain(domain);
            var functionName = GetDefaultFunctionForDomain(domain);

            var checkParameters = parameters ?? new Dictionary<string, object>();
            if (!checkParameters.ContainsKey("InstanceName") && !checkParameters.ContainsKey("Target"))
            {
                checkParameters["Target"] = target;
            }

            // Execute the PowerShell check
            var result = await _powerShellService.ExecuteCheckAsync(moduleName, functionName, checkParameters, cancellationToken);

            // Store the result
            var checkResult = new TemenosAlertManager.Core.Entities.CheckResult
            {
                Domain = result.Domain,
                Target = result.Target,
                Metric = result.Metric,
                Status = result.Status,
                Value = result.Value,
                Details = result.Details,
                ErrorMessage = result.ErrorMessage,
                ExecutionTimeMs = result.ExecutionTimeMs,
                CheckedAt = DateTime.UtcNow
            };

            await _unitOfWork.CheckResults.AddAsync(checkResult, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run check for domain {Domain}, target {Target}", domain, target);
            throw;
        }
    }

    public async Task<IEnumerable<CheckResultDto>> GetRecentChecksAsync(string? domain = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = string.IsNullOrEmpty(domain) 
                ? await _unitOfWork.CheckResults.GetAllAsync(cancellationToken)
                : await _unitOfWork.CheckResults.GetLatestResultsByDomainAsync(domain, limit, cancellationToken);

            return results.Take(limit).Select(MapToCheckResultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent checks for domain {Domain}", domain);
            throw;
        }
    }

    public async Task<HealthSummaryDto> GetDomainHealthAsync(string domain, CancellationToken cancellationToken = default)
    {
        try
        {
            var recentChecks = await _unitOfWork.CheckResults.GetLatestResultsByDomainAsync(domain, 50, cancellationToken);
            var activeAlerts = await _unitOfWork.Alerts.FindAsync(a => a.Domain.ToString() == domain && a.State == AlertState.Active, cancellationToken);

            var overallStatus = DetermineOverallStatus(recentChecks, activeAlerts);
            var lastChecked = recentChecks.Any() ? recentChecks.Max(c => c.CheckedAt) : DateTime.MinValue;

            return new HealthSummaryDto
            {
                Domain = domain,
                OverallStatus = overallStatus,
                ActiveAlerts = activeAlerts.Count(),
                CriticalAlerts = activeAlerts.Count(a => a.Severity == AlertSeverity.Critical),
                WarningAlerts = activeAlerts.Count(a => a.Severity == AlertSeverity.Warning),
                LastChecked = lastChecked,
                Metrics = new Dictionary<string, object>
                {
                    ["TotalChecks"] = recentChecks.Count(),
                    ["SuccessfulChecks"] = recentChecks.Count(c => c.Status == CheckStatus.Success),
                    ["FailedChecks"] = recentChecks.Count(c => c.Status == CheckStatus.Critical || c.Status == CheckStatus.Error)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health summary for domain {Domain}", domain);
            throw;
        }
    }

    private static string GetModuleNameForDomain(string domain)
    {
        return domain.ToUpperInvariant() switch
        {
            "TPH" => "TemenosChecks.TPH",
            "T24" => "TemenosChecks.T24",
            "MQ" => "TemenosChecks.MQ",
            "MSSQL" => "TemenosChecks.Sql",
            "SQL" => "TemenosChecks.Sql",
            _ => "TemenosChecks.Common"
        };
    }

    private static string GetDefaultFunctionForDomain(string domain)
    {
        return domain.ToUpperInvariant() switch
        {
            "MSSQL" or "SQL" => "Test-SqlServerAvailability",
            "MQ" => "Test-MqConnectivity",
            "TPH" => "Test-TphServices",
            "T24" => "Test-T24Services",
            _ => "Test-ServiceStatus"
        };
    }

    private static CheckStatus DetermineOverallStatus(IEnumerable<TemenosAlertManager.Core.Entities.CheckResult> checks, IEnumerable<TemenosAlertManager.Core.Entities.Alert> alerts)
    {
        if (alerts.Any(a => a.Severity == AlertSeverity.Critical))
            return CheckStatus.Critical;

        if (alerts.Any(a => a.Severity == AlertSeverity.Warning))
            return CheckStatus.Warning;

        var recentChecks = checks.Take(10);
        if (recentChecks.Any(c => c.Status == CheckStatus.Critical || c.Status == CheckStatus.Error))
            return CheckStatus.Critical;

        if (recentChecks.Any(c => c.Status == CheckStatus.Warning))
            return CheckStatus.Warning;

        return CheckStatus.Success;
    }

    private static AlertDto MapToAlertDto(TemenosAlertManager.Core.Entities.Alert alert)
    {
        return new AlertDto
        {
            Id = alert.Id,
            Title = alert.Title,
            Description = alert.Description,
            Severity = alert.Severity,
            State = alert.State,
            Domain = alert.Domain,
            Source = alert.Source,
            MetricValue = alert.MetricValue,
            Threshold = alert.Threshold,
            CreatedAt = alert.CreatedAt,
            AcknowledgedAt = alert.AcknowledgedAt,
            AcknowledgedBy = alert.AcknowledgedBy,
            ResolvedAt = alert.ResolvedAt,
            ResolvedBy = alert.ResolvedBy,
            Notes = alert.Notes
        };
    }

    private static CheckResultDto MapToCheckResultDto(TemenosAlertManager.Core.Entities.CheckResult checkResult)
    {
        return new CheckResultDto
        {
            Domain = checkResult.Domain,
            Target = checkResult.Target,
            Metric = checkResult.Metric,
            Status = checkResult.Status,
            Value = checkResult.Value,
            Details = checkResult.Details,
            ErrorMessage = checkResult.ErrorMessage,
            ExecutionTimeMs = checkResult.ExecutionTimeMs,
            CheckedAt = checkResult.CheckedAt
        };
    }
}