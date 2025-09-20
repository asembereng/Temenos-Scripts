using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Enums;
using TemenosAlertManager.Core.Models;

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

/// <summary>
/// Service for managing Temenos SOD/EOD operations
/// </summary>
public interface ITemenosOperationService
{
    /// <summary>
    /// Start a Start of Day operation
    /// </summary>
    Task<OperationResultDto> StartSODAsync(SODRequest request, string initiatedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Start an End of Day operation
    /// </summary>
    Task<OperationResultDto> StartEODAsync(EODRequest request, string initiatedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get status of a specific operation
    /// </summary>
    Task<OperationStatusDto> GetOperationStatusAsync(string operationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a running operation
    /// </summary>
    Task<OperationResultDto> CancelOperationAsync(string operationId, string cancelledBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get list of recent operations
    /// </summary>
    Task<PagedResult<OperationSummaryDto>> GetOperationsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing individual service actions
/// </summary>
public interface IServiceManagementService
{
    /// <summary>
    /// Start a service
    /// </summary>
    Task<ServiceActionResultDto> StartServiceAsync(int serviceId, string initiatedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop a service
    /// </summary>
    Task<ServiceActionResultDto> StopServiceAsync(int serviceId, string initiatedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Restart a service
    /// </summary>
    Task<ServiceActionResultDto> RestartServiceAsync(int serviceId, string initiatedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get comprehensive status of all services
    /// </summary>
    Task<ServiceStatusSummaryDto> GetServicesStatusAsync(string? domain = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get action history for a specific service
    /// </summary>
    Task<PagedResult<ServiceActionDto>> GetServiceActionsAsync(int serviceId, PagingDto paging, CancellationToken cancellationToken = default);
}