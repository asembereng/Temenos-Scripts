using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Core.Interfaces;

public interface ICheckResult
{
    MonitoringDomain Domain { get; }
    string Target { get; }
    string Metric { get; }
    string? Value { get; }
    CheckStatus Status { get; }
    string? Details { get; }
    string? ErrorMessage { get; }
    double? ExecutionTimeMs { get; }
}

public interface IMonitoringCheck
{
    Task<ICheckResult> ExecuteAsync(string target, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    MonitoringDomain Domain { get; }
    string Name { get; }
    string Description { get; }
}

public interface IAlertService
{
    Task<Alert> CreateAlertAsync(ICheckResult checkResult, CancellationToken cancellationToken = default);
    Task<bool> ShouldSuppressAlertAsync(string fingerprint, AlertSeverity severity, CancellationToken cancellationToken = default);
    Task AcknowledgeAlertAsync(int alertId, string acknowledgedBy, string? notes = null, CancellationToken cancellationToken = default);
    Task ResolveAlertAsync(int alertId, string resolvedBy, string? notes = null, CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task<bool> SendEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
    Task<bool> SendAlertEmailAsync(Alert alert, string recipient, CancellationToken cancellationToken = default);
}

public interface IConfigurationService
{
    Task<T?> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetConfigValueAsync<T>(string key, T value, bool encrypt = false, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetConfigurationByCategoryAsync(string category, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task LogEventAsync(string userId, string userName, string action, string resource, object? payload = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task LogFailureAsync(string userId, string userName, string action, string resource, string errorMessage, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
}