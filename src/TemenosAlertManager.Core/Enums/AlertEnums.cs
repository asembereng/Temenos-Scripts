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