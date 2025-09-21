using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Core.Interfaces;

/// <summary>
/// Interface for scheduling SOD/EOD operations
/// </summary>
public interface IOperationScheduler
{
    /// <summary>
    /// Schedule a SOD operation
    /// </summary>
    Task<ScheduleResultDto> ScheduleSODAsync(SODScheduleRequest request, string scheduledBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Schedule an EOD operation
    /// </summary>
    Task<ScheduleResultDto> ScheduleEODAsync(EODScheduleRequest request, string scheduledBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get scheduled operations
    /// </summary>
    Task<ScheduledOperationDto[]> GetScheduledOperationsAsync(string? environment = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a scheduled operation
    /// </summary>
    Task<bool> CancelScheduledOperationAsync(int scheduleId, string cancelledBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update schedule configuration
    /// </summary>
    Task<bool> UpdateScheduleAsync(int scheduleId, ScheduleUpdateRequest request, string updatedBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for performance optimization and monitoring
/// </summary>
public interface IPerformanceOptimizer
{
    /// <summary>
    /// Analyze operation performance and suggest optimizations
    /// </summary>
    Task<OptimizationRecommendationsDto> AnalyzePerformanceAsync(string operationType, string? environment = null, int dayRange = 30, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Apply performance optimizations
    /// </summary>
    Task<OptimizationResultDto> ApplyOptimizationsAsync(OptimizationRequest request, string appliedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get performance baselines
    /// </summary>
    Task<PerformanceBaselineDto> GetPerformanceBaselineAsync(string operationType, string environment, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update performance thresholds
    /// </summary>
    Task<bool> UpdatePerformanceThresholdsAsync(PerformanceThresholdRequest request, string updatedBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for reporting and analytics
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Generate operations summary report
    /// </summary>
    Task<OperationsSummaryReportDto> GenerateOperationsSummaryAsync(ReportRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate performance analytics report
    /// </summary>
    Task<PerformanceAnalyticsReportDto> GeneratePerformanceAnalyticsAsync(ReportRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate compliance report
    /// </summary>
    Task<ComplianceReportDto> GenerateComplianceReportAsync(ComplianceReportRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Export report to various formats
    /// </summary>
    Task<byte[]> ExportReportAsync(string reportId, ExportFormat format, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Schedule automated report generation
    /// </summary>
    Task<ScheduleResultDto> ScheduleReportAsync(ReportScheduleRequest request, string scheduledBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for disaster recovery procedures
/// </summary>
public interface IDisasterRecoveryService
{
    /// <summary>
    /// Create disaster recovery checkpoint
    /// </summary>
    Task<CheckpointResultDto> CreateCheckpointAsync(CheckpointRequest request, string createdBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Restore from disaster recovery checkpoint
    /// </summary>
    Task<RestoreResultDto> RestoreFromCheckpointAsync(RestoreRequest request, string restoredBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate disaster recovery readiness
    /// </summary>
    Task<DRReadinessDto> ValidateDRReadinessAsync(string environment, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute disaster recovery test
    /// </summary>
    Task<DRTestResultDto> ExecuteDRTestAsync(DRTestRequest request, string executedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get disaster recovery status
    /// </summary>
    Task<DRStatusDto> GetDRStatusAsync(string environment, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for automation engine
/// </summary>
public interface IAutomationEngine
{
    /// <summary>
    /// Create automation workflow
    /// </summary>
    Task<WorkflowResultDto> CreateWorkflowAsync(WorkflowDefinition workflow, string createdBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute automation workflow
    /// </summary>
    Task<WorkflowExecutionResultDto> ExecuteWorkflowAsync(int workflowId, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get workflow execution status
    /// </summary>
    Task<WorkflowStatusDto> GetWorkflowStatusAsync(int executionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// List available workflows
    /// </summary>
    Task<WorkflowSummaryDto[]> GetWorkflowsAsync(string? category = null, bool activeOnly = true, CancellationToken cancellationToken = default);
}