using TemenosAlertManager.Core.Entities.Configuration;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Core.Interfaces;

/// <summary>
/// Interface for SOD orchestration with advanced dependency management
/// </summary>
public interface ISODOrchestrator
{
    /// <summary>
    /// Execute SOD operation with dependency management
    /// </summary>
    Task<OperationResultDto> ExecuteSODAsync(SODRequest request, string operationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate pre-SOD conditions
    /// </summary>
    Task<ValidationResult> ValidatePreSODConditionsAsync(string environment, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get service startup sequence based on dependencies
    /// </summary>
    Task<ServiceConfig[]> GetServiceStartupSequenceAsync(string environment, string[]? serviceFilter = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rollback SOD operation if failure occurs
    /// </summary>
    Task<OperationResultDto> RollbackSODAsync(string operationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for EOD orchestration with transaction management
/// </summary>
public interface IEODOrchestrator
{
    /// <summary>
    /// Execute EOD operation with transaction management
    /// </summary>
    Task<OperationResultDto> ExecuteEODAsync(EODRequest request, string operationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate pre-EOD conditions
    /// </summary>
    Task<ValidationResult> ValidatePreEODConditionsAsync(string environment, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute transaction cutoff
    /// </summary>
    Task<OperationResultDto> ExecuteTransactionCutoffAsync(string environment, DateTime? cutoffTime, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rollback EOD operation if failure occurs
    /// </summary>
    Task<OperationResultDto> RollbackEODAsync(string operationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for monitoring SOD/EOD operations
/// </summary>
public interface IOperationMonitor
{
    /// <summary>
    /// Start monitoring an operation
    /// </summary>
    Task StartMonitoringAsync(string operationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop monitoring an operation
    /// </summary>
    Task StopMonitoringAsync(string operationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get real-time operation metrics
    /// </summary>
    Task<OperationMetrics> GetOperationMetricsAsync(string operationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get operation dashboard data
    /// </summary>
    Task<OperationDashboardDto> GetOperationDashboardAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for dependency management
/// </summary>
public interface IDependencyManager
{
    /// <summary>
    /// Resolve service dependencies
    /// </summary>
    Task<ServiceDependencyGraph> ResolveServiceDependenciesAsync(string environment, string operationType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate dependency constraints
    /// </summary>
    Task<ValidationResult> ValidateDependencyConstraintsAsync(ServiceConfig[] services, string operationType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get optimal execution order
    /// </summary>
    Task<ServiceExecutionPlan> GetOptimalExecutionOrderAsync(ServiceConfig[] services, string operationType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validation result for pre-condition checks
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string[] Errors { get; set; } = Array.Empty<string>();
    public string[] Warnings { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
}

/// <summary>
/// Operation metrics for monitoring
/// </summary>
public class OperationMetrics
{
    public string OperationId { get; set; } = string.Empty;
    public TimeSpan ElapsedTime { get; set; }
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public double ProgressPercentage { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public PerformanceMetrics Performance { get; set; } = new();
    public ResourceUsage ResourceUsage { get; set; } = new();
}

/// <summary>
/// Performance metrics
/// </summary>
public class PerformanceMetrics
{
    public double AverageStepDuration { get; set; }
    public double MaxStepDuration { get; set; }
    public double MinStepDuration { get; set; }
    public int FailedSteps { get; set; }
    public int RetriedSteps { get; set; }
}

/// <summary>
/// Resource usage metrics
/// </summary>
public class ResourceUsage
{
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public double DiskUtilization { get; set; }
    public double NetworkUtilization { get; set; }
}

/// <summary>
/// Service dependency graph
/// </summary>
public class ServiceDependencyGraph
{
    public ServiceNode[] Nodes { get; set; } = Array.Empty<ServiceNode>();
    public ServiceDependency[] Dependencies { get; set; } = Array.Empty<ServiceDependency>();
    public int MaxDepth { get; set; }
    public bool HasCircularDependencies { get; set; }
}

/// <summary>
/// Service node in dependency graph
/// </summary>
public class ServiceNode
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public int DependencyLevel { get; set; }
    public bool IsCritical { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
}

/// <summary>
/// Service dependency relationship
/// </summary>
public class ServiceDependency
{
    public int FromServiceId { get; set; }
    public int ToServiceId { get; set; }
    public string DependencyType { get; set; } = string.Empty; // Hard, Soft, Optional
    public string Condition { get; set; } = string.Empty;
}

/// <summary>
/// Service execution plan
/// </summary>
public class ServiceExecutionPlan
{
    public ExecutionPhase[] Phases { get; set; } = Array.Empty<ExecutionPhase>();
    public TimeSpan EstimatedTotalDuration { get; set; }
    public int TotalServices { get; set; }
    public int CriticalServices { get; set; }
}

/// <summary>
/// Execution phase with parallel services
/// </summary>
public class ExecutionPhase
{
    public int PhaseNumber { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public ServiceConfig[] Services { get; set; } = Array.Empty<ServiceConfig>();
    public bool CanExecuteInParallel { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
}

/// <summary>
/// Operation dashboard data
/// </summary>
public class OperationDashboardDto
{
    public DateTime LastUpdated { get; set; }
    public OperationSummaryDto[] ActiveOperations { get; set; } = Array.Empty<OperationSummaryDto>();
    public OperationHistoryDto[] RecentOperations { get; set; } = Array.Empty<OperationHistoryDto>();
    public SystemHealthDto SystemHealth { get; set; } = new();
    public PerformanceTrendDto[] PerformanceTrends { get; set; } = Array.Empty<PerformanceTrendDto>();
    public AlertSummaryDto AlertSummary { get; set; } = new();
}

/// <summary>
/// Operation history data
/// </summary>
public class OperationHistoryDto
{
    public string OperationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public int ServicesInvolved { get; set; }
    public bool HasErrors { get; set; }
}

/// <summary>
/// System health data
/// </summary>
public class SystemHealthDto
{
    public string OverallStatus { get; set; } = string.Empty;
    public int TotalServices { get; set; }
    public int HealthyServices { get; set; }
    public int UnhealthyServices { get; set; }
    public double HealthPercentage { get; set; }
    public ResourceUsage SystemResources { get; set; } = new();
    public DateTime LastHealthCheck { get; set; }
}

/// <summary>
/// Performance trend data
/// </summary>
public class PerformanceTrendDto
{
    public DateTime Timestamp { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Trend { get; set; } = string.Empty; // Improving, Stable, Degrading
}

/// <summary>
/// Alert summary data
/// </summary>
public class AlertSummaryDto
{
    public int CriticalAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public int InfoAlerts { get; set; }
    public int TotalAlerts { get; set; }
    public MonitoringAlertDto[] RecentAlerts { get; set; } = Array.Empty<MonitoringAlertDto>();
}

/// <summary>
/// Alert data for monitoring dashboards
/// </summary>
public class MonitoringAlertDto
{
    public int Id { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsAcknowledged { get; set; }
}