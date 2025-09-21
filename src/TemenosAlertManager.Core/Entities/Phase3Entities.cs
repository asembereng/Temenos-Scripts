using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TemenosAlertManager.Core.Entities;

/// <summary>
/// Scheduled operation entity for automation
/// </summary>
[Table("ScheduledOperations")]
public class ScheduledOperation : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string OperationType { get; set; } = string.Empty; // SOD, EOD
    
    [Required]
    [MaxLength(100)]
    public string Environment { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string CronExpression { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string TimeZone { get; set; } = "UTC";
    
    public string? ServicesFilter { get; set; } // JSON array
    
    public bool DryRun { get; set; } = false;
    
    public bool IsEnabled { get; set; } = true;
    
    public string? Comments { get; set; }
    
    public string? Configuration { get; set; } // JSON object
    
    [Required]
    [MaxLength(100)]
    public string ScheduledBy { get; set; } = string.Empty;
    
    public DateTime? NextExecutionTime { get; set; }
    
    public DateTime? LastExecutionTime { get; set; }
    
    public int ExecutionCount { get; set; } = 0;
    
    public int SuccessCount { get; set; } = 0;
    
    public int FailureCount { get; set; } = 0;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Paused, Completed, Failed
}

/// <summary>
/// Performance baseline entity
/// </summary>
[Table("PerformanceBaselines")]
public class PerformanceBaseline : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string OperationType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Environment { get; set; } = string.Empty;
    
    public double AverageDurationMinutes { get; set; }
    
    public double SuccessRate { get; set; }
    
    public double ResourceUtilization { get; set; }
    
    public int SampleSize { get; set; }
    
    public DateTime BaselineDate { get; set; }
    
    public string? DetailedMetrics { get; set; } // JSON object
}

/// <summary>
/// Performance threshold entity
/// </summary>
[Table("PerformanceThresholds")]
public class PerformanceThreshold : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string OperationType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Environment { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string MetricName { get; set; } = string.Empty;
    
    public double WarningThreshold { get; set; }
    
    public double CriticalThreshold { get; set; }
    
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Generated report entity
/// </summary>
[Table("GeneratedReports")]
public class GeneratedReport : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string ReportId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ReportType { get; set; } = string.Empty; // Operations, Performance, Compliance
    
    public DateTime ReportPeriodStart { get; set; }
    
    public DateTime ReportPeriodEnd { get; set; }
    
    [MaxLength(100)]
    public string? Environment { get; set; }
    
    [MaxLength(50)]
    public string? OperationType { get; set; }
    
    public string? Parameters { get; set; } // JSON object
    
    public string? ReportData { get; set; } // JSON object
    
    [MaxLength(50)]
    public string Status { get; set; } = "Generated"; // Generated, Delivered, Archived
    
    [Required]
    [MaxLength(100)]
    public string GeneratedBy { get; set; } = string.Empty;
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    public long? FileSizeBytes { get; set; }
    
    [MaxLength(500)]
    public string? FilePath { get; set; }
}

/// <summary>
/// Disaster recovery checkpoint entity
/// </summary>
[Table("DRCheckpoints")]
public class DRCheckpoint : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string CheckpointId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Environment { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string CheckpointType { get; set; } = string.Empty; // Full, Incremental, Configuration
    
    public string? IncludedSystems { get; set; } // JSON array
    
    public string Description { get; set; } = string.Empty;
    
    public long SizeBytes { get; set; }
    
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Created"; // Created, Verified, Corrupted, Expired
    
    [MaxLength(50)]
    public string VerificationStatus { get; set; } = "Pending"; // Pending, Passed, Failed
    
    public DateTime? ExpiresAt { get; set; }
    
    public DateTime? LastVerifiedAt { get; set; }
    
    public string? Metadata { get; set; } // JSON object
}

/// <summary>
/// Disaster recovery test entity
/// </summary>
[Table("DRTests")]
public class DRTest : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string TestId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Environment { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string TestEnvironment { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string TestType { get; set; } = string.Empty; // Full, Partial, ConfigOnly
    
    public string? SystemsToTest { get; set; } // JSON array
    
    public bool RestoreData { get; set; } = false;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Running"; // Running, Completed, Failed, Cancelled
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    public string? TestResults { get; set; } // JSON object
    
    public double? ActualRTO { get; set; } // Minutes
    
    public double? ActualRPO { get; set; } // Minutes
    
    public double? DataIntegrityScore { get; set; }
    
    public double? SystemAvailabilityScore { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ExecutedBy { get; set; } = string.Empty;
    
    public string? Issues { get; set; } // JSON array
    
    public string? Recommendations { get; set; } // JSON array
}

/// <summary>
/// Automation workflow entity
/// </summary>
[Table("AutomationWorkflows")]
public class AutomationWorkflow : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Version { get; set; } = "1.0";
    
    public string WorkflowDefinition { get; set; } = string.Empty; // JSON object
    
    public string? DefaultParameters { get; set; } // JSON object
    
    public string? Tags { get; set; } // JSON array
    
    public bool IsEnabled { get; set; } = true;
    
    public int ExecutionCount { get; set; } = 0;
    
    public DateTime? LastExecutedAt { get; set; }
}

/// <summary>
/// Workflow execution entity
/// </summary>
[Table("WorkflowExecutions")]
public class WorkflowExecution : BaseEntity
{
    public int WorkflowId { get; set; }
    
    [ForeignKey("WorkflowId")]
    public virtual AutomationWorkflow Workflow { get; set; } = null!;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Running"; // Running, Completed, Failed, Cancelled
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    public string? Parameters { get; set; } // JSON object
    
    public string? StepResults { get; set; } // JSON array
    
    public double ProgressPercentage { get; set; } = 0;
    
    [MaxLength(100)]
    public string? CurrentStep { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ExecutedBy { get; set; } = string.Empty;
}

/// <summary>
/// Optimization recommendation entity
/// </summary>
[Table("OptimizationRecommendations")]
public class OptimizationRecommendation : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string RecommendationId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string OperationType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Environment { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty; // Performance, Reliability, Cost
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Priority { get; set; } = string.Empty; // High, Medium, Low
    
    public string Impact { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ImplementationEffort { get; set; } = string.Empty;
    
    public string? AffectedServices { get; set; } // JSON array
    
    public string? Parameters { get; set; } // JSON object
    
    public bool IsAutoApplicable { get; set; } = false;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Applied, Dismissed, Expired
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? AppliedAt { get; set; }
    
    [MaxLength(100)]
    public string? AppliedBy { get; set; }
    
    public double? EstimatedImprovementPercentage { get; set; }
    
    public double? ActualImprovementPercentage { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
}