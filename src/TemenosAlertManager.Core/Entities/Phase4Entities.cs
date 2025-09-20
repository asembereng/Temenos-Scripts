using System.ComponentModel.DataAnnotations;

namespace TemenosAlertManager.Core.Entities;

/// <summary>
/// Entity for tracking test executions
/// </summary>
public class TestExecution
{
    public int Id { get; set; }
    public string ExecutionId { get; set; } = string.Empty;
    public string TestSuiteName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public TestExecutionStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TestResults { get; set; } // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity for performance test results
/// </summary>
public class PerformanceTestResult
{
    public int Id { get; set; }
    public string TestId { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public PerformanceTestType TestType { get; set; }
    public DateTime ExecutionTime { get; set; }
    public TimeSpan Duration { get; set; }
    public decimal ResponseTime { get; set; }
    public decimal Throughput { get; set; }
    public decimal CpuUtilization { get; set; }
    public decimal MemoryUtilization { get; set; }
    public int ConcurrentUsers { get; set; }
    public string? Metrics { get; set; } // JSON
    public bool PassedThresholds { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity for security scan results
/// </summary>
public class SecurityScanResult
{
    public int Id { get; set; }
    public string ScanId { get; set; } = string.Empty;
    public string ScanName { get; set; } = string.Empty;
    public SecurityScanType ScanType { get; set; }
    public string Environment { get; set; } = string.Empty;
    public DateTime ScanTime { get; set; }
    public SecurityScanStatus Status { get; set; }
    public int CriticalIssues { get; set; }
    public int HighIssues { get; set; }
    public int MediumIssues { get; set; }
    public int LowIssues { get; set; }
    public string? Issues { get; set; } // JSON
    public string? Recommendations { get; set; } // JSON
    public string ExecutedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity for deployment tracking
/// </summary>
public class Deployment
{
    public int Id { get; set; }
    public string DeploymentId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DeploymentType DeploymentType { get; set; }
    public DeploymentStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public string? DeploymentPlan { get; set; } // JSON
    public string? ExecutionLog { get; set; }
    public string? RollbackPlan { get; set; } // JSON
    public bool IsRollback { get; set; }
    public string? ParentDeploymentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity for production incidents
/// </summary>
public class ProductionIncident
{
    public int Id { get; set; }
    public string IncidentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string AffectedServices { get; set; } = string.Empty; // JSON array
    public DateTime ReportedTime { get; set; }
    public DateTime? AcknowledgedTime { get; set; }
    public DateTime? ResolvedTime { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? ActionsTaken { get; set; } // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity for maintenance windows
/// </summary>
public class MaintenanceWindow
{
    public int Id { get; set; }
    public string WindowId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public MaintenanceType MaintenanceType { get; set; }
    public MaintenanceStatus Status { get; set; }
    public string AffectedServices { get; set; } = string.Empty; // JSON array
    public string ScheduledBy { get; set; } = string.Empty;
    public string? NotificationRecipients { get; set; } // JSON array
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity for quality gates
/// </summary>
public class QualityGate
{
    public int Id { get; set; }
    public string GateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public QualityGateType GateType { get; set; }
    public string Criteria { get; set; } = string.Empty; // JSON
    public bool IsPassed { get; set; }
    public DateTime ExecutionTime { get; set; }
    public string? Results { get; set; } // JSON
    public string? FailureReasons { get; set; } // JSON
    public string ExecutedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Enums for Phase 4 entities
public enum TestExecutionStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum PerformanceTestType
{
    Load,
    Stress,
    Volume,
    Endurance,
    Spike
}

public enum SecurityScanType
{
    Vulnerability,
    Penetration,
    Compliance,
    CodeSecurity,
    Infrastructure
}

public enum SecurityScanStatus
{
    Scheduled,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum DeploymentType
{
    Feature,
    Hotfix,
    Rollback,
    Emergency,
    Scheduled
}

public enum DeploymentStatus
{
    Planned,
    InProgress,
    Completed,
    Failed,
    RolledBack,
    Cancelled
}

public enum IncidentSeverity
{
    Low,
    Medium,
    High,
    Critical,
    Emergency
}

public enum IncidentStatus
{
    Open,
    Acknowledged,
    InProgress,
    Resolved,
    Closed
}

public enum MaintenanceType
{
    Planned,
    Emergency,
    Preventive,
    Corrective
}

public enum MaintenanceStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    Extended
}

public enum QualityGateType
{
    CodeQuality,
    TestCoverage,
    Security,
    Performance,
    Compliance
}