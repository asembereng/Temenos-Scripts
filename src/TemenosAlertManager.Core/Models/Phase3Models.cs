namespace TemenosAlertManager.Core.Models;

/// <summary>
/// Request for scheduling SOD operations
/// </summary>
public class SODScheduleRequest
{
    public string Environment { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public string[] ServicesFilter { get; set; } = Array.Empty<string>();
    public bool DryRun { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
    public string? Comments { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Request for scheduling EOD operations
/// </summary>
public class EODScheduleRequest
{
    public string Environment { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public string[] ServicesFilter { get; set; } = Array.Empty<string>();
    public DateTime? CutoffTime { get; set; }
    public bool DryRun { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
    public string? Comments { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Result of scheduling operation
/// </summary>
public class ScheduleResultDto
{
    public int ScheduleId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public DateTime NextExecutionTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Scheduled operation details
/// </summary>
public class ScheduledOperationDto
{
    public int ScheduleId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public DateTime NextExecutionTime { get; set; }
    public DateTime? LastExecutionTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string ScheduledBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

/// <summary>
/// Request for updating schedule
/// </summary>
public class ScheduleUpdateRequest
{
    public string? CronExpression { get; set; }
    public string? TimeZone { get; set; }
    public bool? IsEnabled { get; set; }
    public string? Comments { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
}

/// <summary>
/// Performance optimization recommendations
/// </summary>
public class OptimizationRecommendationsDto
{
    public string OperationType { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; }
    public OptimizationRecommendation[] Recommendations { get; set; } = Array.Empty<OptimizationRecommendation>();
    public PerformanceMetricsSummary CurrentPerformance { get; set; } = new();
    public PerformanceMetricsSummary ProjectedPerformance { get; set; } = new();
    public double EstimatedImprovementPercentage { get; set; }
}

/// <summary>
/// Individual optimization recommendation
/// </summary>
public class OptimizationRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Performance, Reliability, Cost
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // High, Medium, Low
    public string Impact { get; set; } = string.Empty;
    public string ImplementationEffort { get; set; } = string.Empty;
    public string[] AffectedServices { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsAutoApplicable { get; set; }
}

/// <summary>
/// Performance metrics summary
/// </summary>
public class PerformanceMetricsSummary
{
    public double AverageDurationMinutes { get; set; }
    public double SuccessRate { get; set; }
    public double ResourceUtilization { get; set; }
    public int ConcurrentOperations { get; set; }
    public Dictionary<string, double> DetailedMetrics { get; set; } = new();
}

/// <summary>
/// Optimization request
/// </summary>
public class OptimizationRequest
{
    public string Environment { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string[] RecommendationIds { get; set; } = Array.Empty<string>();
    public bool DryRun { get; set; } = false;
    public string? Comments { get; set; }
}

/// <summary>
/// Optimization result
/// </summary>
public class OptimizationResultDto
{
    public string OptimizationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public OptimizationOutcome[] Results { get; set; } = Array.Empty<OptimizationOutcome>();
    public DateTime AppliedAt { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
}

/// <summary>
/// Individual optimization outcome
/// </summary>
public class OptimizationOutcome
{
    public string RecommendationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Applied, Failed, Skipped
    public string Message { get; set; } = string.Empty;
    public PerformanceMetricsSummary BeforeMetrics { get; set; } = new();
    public PerformanceMetricsSummary AfterMetrics { get; set; } = new();
    public double ImprovementPercentage { get; set; }
}

/// <summary>
/// Performance baseline
/// </summary>
public class PerformanceBaselineDto
{
    public string OperationType { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public PerformanceMetricsSummary Baseline { get; set; } = new();
    public PerformanceThreshold[] Thresholds { get; set; } = Array.Empty<PerformanceThreshold>();
    public DateTime BaselineDate { get; set; }
    public int SampleSize { get; set; }
}

/// <summary>
/// Performance threshold
/// </summary>
public class PerformanceThreshold
{
    public string MetricName { get; set; } = string.Empty;
    public double WarningThreshold { get; set; }
    public double CriticalThreshold { get; set; }
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// Performance threshold update request
/// </summary>
public class PerformanceThresholdRequest
{
    public string OperationType { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public PerformanceThreshold[] Thresholds { get; set; } = Array.Empty<PerformanceThreshold>();
}

/// <summary>
/// Report request
/// </summary>
public class ReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Environment { get; set; }
    public string? OperationType { get; set; }
    public string[] IncludeMetrics { get; set; } = Array.Empty<string>();
    public string ReportFormat { get; set; } = "Summary"; // Summary, Detailed, Executive
}

/// <summary>
/// Operations summary report
/// </summary>
public class OperationsSummaryReportDto
{
    public string ReportId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
    public OperationTypeStatistics[] OperationStats { get; set; } = Array.Empty<OperationTypeStatistics>();
    public EnvironmentStatistics[] EnvironmentStats { get; set; } = Array.Empty<EnvironmentStatistics>();
    public TrendAnalysis[] Trends { get; set; } = Array.Empty<TrendAnalysis>();
    public KeyMetric[] KeyMetrics { get; set; } = Array.Empty<KeyMetric>();
}

/// <summary>
/// Operation type statistics
/// </summary>
public class OperationTypeStatistics
{
    public string OperationType { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDurationMinutes { get; set; }
    public double MinDurationMinutes { get; set; }
    public double MaxDurationMinutes { get; set; }
}

/// <summary>
/// Environment statistics
/// </summary>
public class EnvironmentStatistics
{
    public string Environment { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public double AvailabilityPercentage { get; set; }
    public double PerformanceScore { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
}

/// <summary>
/// Trend analysis
/// </summary>
public class TrendAnalysis
{
    public string MetricName { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty; // Improving, Stable, Degrading
    public double ChangePercentage { get; set; }
    public string Period { get; set; } = string.Empty;
}

/// <summary>
/// Key metric
/// </summary>
public class KeyMetric
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Good, Warning, Critical
}

/// <summary>
/// Performance analytics report
/// </summary>
public class PerformanceAnalyticsReportDto
{
    public string ReportId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
    public PerformanceMetricsSummary OverallPerformance { get; set; } = new();
    public ServicePerformanceAnalysis[] ServiceAnalysis { get; set; } = Array.Empty<ServicePerformanceAnalysis>();
    public ResourceUtilizationAnalysis ResourceAnalysis { get; set; } = new();
    public BottleneckAnalysis[] Bottlenecks { get; set; } = Array.Empty<BottleneckAnalysis>();
    public OptimizationRecommendation[] Recommendations { get; set; } = Array.Empty<OptimizationRecommendation>();
}

/// <summary>
/// Service performance analysis
/// </summary>
public class ServicePerformanceAnalysis
{
    public string ServiceName { get; set; } = string.Empty;
    public double AverageStartupTime { get; set; }
    public double ReliabilityScore { get; set; }
    public double ResourceConsumption { get; set; }
    public string[] Dependencies { get; set; } = Array.Empty<string>();
    public string PerformanceRating { get; set; } = string.Empty;
}

/// <summary>
/// Resource utilization analysis
/// </summary>
public class ResourceUtilizationAnalysis
{
    public double AverageCpuUtilization { get; set; }
    public double AverageMemoryUtilization { get; set; }
    public double AverageDiskUtilization { get; set; }
    public double AverageNetworkUtilization { get; set; }
    public ResourceTrend[] Trends { get; set; } = Array.Empty<ResourceTrend>();
}

/// <summary>
/// Resource trend
/// </summary>
public class ResourceTrend
{
    public string ResourceType { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public double[] Values { get; set; } = Array.Empty<double>();
    public DateTime[] Timestamps { get; set; } = Array.Empty<DateTime>();
}

/// <summary>
/// Bottleneck analysis
/// </summary>
public class BottleneckAnalysis
{
    public string ComponentName { get; set; } = string.Empty;
    public string BottleneckType { get; set; } = string.Empty; // CPU, Memory, Disk, Network, Dependency
    public double Severity { get; set; }
    public string Impact { get; set; } = string.Empty;
    public string[] AffectedOperations { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Compliance report request
/// </summary>
public class ComplianceReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string[] Environments { get; set; } = Array.Empty<string>();
    public string[] ComplianceFrameworks { get; set; } = Array.Empty<string>(); // SOX, Basel, GDPR, etc.
    public bool IncludeAuditTrail { get; set; } = true;
    public bool IncludeExceptions { get; set; } = true;
}

/// <summary>
/// Compliance report
/// </summary>
public class ComplianceReportDto
{
    public string ReportId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
    public ComplianceStatus OverallStatus { get; set; } = new();
    public ComplianceFrameworkStatus[] FrameworkStatus { get; set; } = Array.Empty<ComplianceFrameworkStatus>();
    public AuditTrailSummary AuditSummary { get; set; } = new();
    public ComplianceException[] Exceptions { get; set; } = Array.Empty<ComplianceException>();
}

/// <summary>
/// Compliance status
/// </summary>
public class ComplianceStatus
{
    public string Status { get; set; } = string.Empty; // Compliant, Non-Compliant, Partial
    public double ComplianceScore { get; set; }
    public int TotalChecks { get; set; }
    public int PassedChecks { get; set; }
    public int FailedChecks { get; set; }
}

/// <summary>
/// Compliance framework status
/// </summary>
public class ComplianceFrameworkStatus
{
    public string Framework { get; set; } = string.Empty;
    public ComplianceStatus Status { get; set; } = new();
    public ComplianceControl[] Controls { get; set; } = Array.Empty<ComplianceControl>();
}

/// <summary>
/// Compliance control
/// </summary>
public class ComplianceControl
{
    public string ControlId { get; set; } = string.Empty;
    public string ControlName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Evidence { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Audit trail summary
/// </summary>
public class AuditTrailSummary
{
    public int TotalAuditEvents { get; set; }
    public int UserActions { get; set; }
    public int SystemActions { get; set; }
    public int SecurityEvents { get; set; }
    public int DataAccessEvents { get; set; }
}

/// <summary>
/// Compliance exception
/// </summary>
public class ComplianceException
{
    public string ExceptionId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public string Status { get; set; } = string.Empty; // Open, Investigating, Resolved
    public string[] AffectedSystems { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Export format enumeration
/// </summary>
public enum ExportFormat
{
    PDF,
    Excel,
    CSV,
    JSON,
    XML
}

/// <summary>
/// Report schedule request
/// </summary>
public class ReportScheduleRequest
{
    public string ReportType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public ReportRequest ReportParameters { get; set; } = new();
    public ExportFormat ExportFormat { get; set; } = ExportFormat.PDF;
    public string[] Recipients { get; set; } = Array.Empty<string>();
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Disaster recovery checkpoint request
/// </summary>
public class CheckpointRequest
{
    public string Environment { get; set; } = string.Empty;
    public string CheckpointType { get; set; } = string.Empty; // Full, Incremental, Configuration
    public string[] IncludedSystems { get; set; } = Array.Empty<string>();
    public string Description { get; set; } = string.Empty;
    public bool VerifyIntegrity { get; set; } = true;
}

/// <summary>
/// Checkpoint result
/// </summary>
public class CheckpointResultDto
{
    public string CheckpointId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public string[] IncludedSystems { get; set; } = Array.Empty<string>();
    public string VerificationStatus { get; set; } = string.Empty;
}

/// <summary>
/// Restore request
/// </summary>
public class RestoreRequest
{
    public string CheckpointId { get; set; } = string.Empty;
    public string TargetEnvironment { get; set; } = string.Empty;
    public string[] SystemsToRestore { get; set; } = Array.Empty<string>();
    public bool DryRun { get; set; } = false;
    public bool StopServices { get; set; } = true;
    public string RestoreType { get; set; } = "Full"; // Full, Partial, ConfigOnly
}

/// <summary>
/// Restore result
/// </summary>
public class RestoreResultDto
{
    public string RestoreId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public RestoreStepResult[] Steps { get; set; } = Array.Empty<RestoreStepResult>();
}

/// <summary>
/// Restore step result
/// </summary>
public class RestoreStepResult
{
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Disaster recovery readiness
/// </summary>
public class DRReadinessDto
{
    public string Environment { get; set; } = string.Empty;
    public string OverallStatus { get; set; } = string.Empty; // Ready, Partial, NotReady
    public double ReadinessScore { get; set; }
    public DRComponentStatus[] ComponentStatus { get; set; } = Array.Empty<DRComponentStatus>();
    public DateTime LastValidated { get; set; }
    public string[] Issues { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// DR component status
/// </summary>
public class DRComponentStatus
{
    public string ComponentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastBackup { get; set; }
    public string BackupStatus { get; set; } = string.Empty;
    public double RecoveryTimeObjective { get; set; } // In minutes
    public double RecoveryPointObjective { get; set; } // In minutes
}

/// <summary>
/// DR test request
/// </summary>
public class DRTestRequest
{
    public string Environment { get; set; } = string.Empty;
    public string TestType { get; set; } = string.Empty; // Full, Partial, ConfigOnly
    public string[] SystemsToTest { get; set; } = Array.Empty<string>();
    public bool RestoreData { get; set; } = false;
    public string TestEnvironment { get; set; } = string.Empty;
}

/// <summary>
/// DR test result
/// </summary>
public class DRTestResultDto
{
    public string TestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DRTestStepResult[] Steps { get; set; } = Array.Empty<DRTestStepResult>();
    public DRTestMetrics Metrics { get; set; } = new();
    public string[] Issues { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// DR test step result
/// </summary>
public class DRTestStepResult
{
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// DR test metrics
/// </summary>
public class DRTestMetrics
{
    public double ActualRTO { get; set; } // Minutes
    public double ActualRPO { get; set; } // Minutes
    public double DataIntegrityScore { get; set; }
    public double SystemAvailabilityScore { get; set; }
}

/// <summary>
/// DR status
/// </summary>
public class DRStatusDto
{
    public string Environment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastBackup { get; set; }
    public DateTime LastTest { get; set; }
    public DRReadinessDto Readiness { get; set; } = new();
    public DRTestResultDto? LastTestResult { get; set; }
}

/// <summary>
/// Workflow definition
/// </summary>
public class WorkflowDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public WorkflowStep[] Steps { get; set; } = Array.Empty<WorkflowStep>();
    public Dictionary<string, object> DefaultParameters { get; set; } = new();
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Workflow step
/// </summary>
public class WorkflowStep
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Action, Condition, Loop, Parallel
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string[] DependsOn { get; set; } = Array.Empty<string>();
    public string? SuccessAction { get; set; }
    public string? FailureAction { get; set; }
    public int TimeoutMinutes { get; set; } = 30;
}

/// <summary>
/// Workflow result
/// </summary>
public class WorkflowResultDto
{
    public int WorkflowId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Workflow execution result
/// </summary>
public class WorkflowExecutionResultDto
{
    public int ExecutionId { get; set; }
    public int WorkflowId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Workflow status
/// </summary>
public class WorkflowStatusDto
{
    public int ExecutionId { get; set; }
    public int WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public WorkflowStepStatus[] StepStatus { get; set; } = Array.Empty<WorkflowStepStatus>();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Workflow step status
/// </summary>
public class WorkflowStepStatus
{
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Workflow summary
/// </summary>
public class WorkflowSummaryDto
{
    public int WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int StepCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public DateTime? LastExecutedAt { get; set; }
}