namespace TemenosAlertManager.Core.Enums;

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public enum AlertState
{
    Active = 0,
    Acknowledged = 1,
    Resolved = 2,
    Suppressed = 3
}

public enum MonitoringDomain
{
    TPH = 0,
    T24 = 1,
    MQ = 2,
    MSSQL = 3,
    Host = 4,
    JVM = 5
}

public enum CheckStatus
{
    Success = 0,
    Warning = 1,
    Critical = 2,
    Error = 3
}

public enum UserRole
{
    Viewer = 0,
    Operator = 1,
    Admin = 2
}

public enum AlertChannel
{
    Email = 0,
    Slack = 1,
    Teams = 2,
    SMS = 3
}

public enum AlertDeliveryStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Retrying = 3
}

// Phase 4 enums for testing and deployment
public enum TestExecutionStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public enum PerformanceTestType
{
    Load = 0,
    Stress = 1,
    Volume = 2,
    Spike = 3,
    Endurance = 4
}

public enum SecurityScanType
{
    StaticAnalysis = 0,
    DynamicAnalysis = 1,
    DependencyCheck = 2,
    ContainerScan = 3,
    PenetrationTest = 4
}

public enum SecurityScanStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

public enum DeploymentType
{
    BlueGreen = 0,
    Canary = 1,
    RollingUpdate = 2,
    Recreate = 3
}

public enum DeploymentStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    RolledBack = 4
}

public enum MaintenanceType
{
    Planned = 0,
    Emergency = 1,
    Preventive = 2
}

public enum MaintenanceStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum QualityGateType
{
    CodeCoverage = 0,
    SecurityScan = 1,
    PerformanceTest = 2,
    UnitTests = 3,
    IntegrationTests = 4
}

public enum QualityGateStatus
{
    Pending = 0,
    Passed = 1,
    Failed = 2,
    Skipped = 3
}

public enum ReportFormat
{
    PDF = 0,
    Excel = 1,
    CSV = 2,
    JSON = 3,
    XML = 4
}

public enum AlertStatus
{
    New = 0,
    Active = 1,
    Acknowledged = 2,
    Resolved = 3,
    Closed = 4
}

public enum IncidentSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum IncidentStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Closed = 3
}

public enum DateRange
{
    Today = 0,
    Yesterday = 1,
    LastWeek = 2,
    LastMonth = 3,
    LastQuarter = 4,
    LastYear = 5,
    Custom = 6
}

public enum CodeIssueSeverity
{
    Info = 0,
    Minor = 1,
    Major = 2,
    Critical = 3,
    Blocker = 4
}

public enum RegressionTestStatus
{
    Passed = 0,
    Failed = 1,
    PassedWithWarnings = 2,
    Error = 3
}

public enum AcceptanceTestStatus
{
    Accepted = 0,
    Rejected = 1,
    ConditionallyAccepted = 2,
    PendingReview = 3
}