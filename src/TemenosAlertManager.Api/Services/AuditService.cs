using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Interfaces;

namespace TemenosAlertManager.Api.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogEventAsync(string userId, string userName, string action, string resource, object? payload = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = new AuditEvent
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Resource = resource,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = true,
                EventTime = DateTime.UtcNow
            };

            if (payload != null)
            {
                var payloadJson = JsonSerializer.Serialize(payload);
                auditEvent.PayloadHash = ComputeHash(payloadJson);
                auditEvent.Details = payloadJson;
            }

            await _unitOfWork.AuditEvents.AddAsync(auditEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Audit event logged: {Action} on {Resource} by {UserName}", action, resource, userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event for user {UserName}, action {Action}", userName, action);
            // Don't throw - audit logging failure shouldn't break the main operation
        }
    }

    public async Task LogFailureAsync(string userId, string userName, string action, string resource, string errorMessage, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = new AuditEvent
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Resource = resource,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = false,
                ErrorMessage = errorMessage,
                EventTime = DateTime.UtcNow
            };

            await _unitOfWork.AuditEvents.AddAsync(auditEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Audit failure logged: {Action} on {Resource} by {UserName} - {ErrorMessage}", 
                action, resource, userName, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit failure for user {UserName}, action {Action}", userName, action);
            // Don't throw - audit logging failure shouldn't break the main operation
        }
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }
}