using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Core.Models;

public class CheckResultDto
{
    public MonitoringDomain Domain { get; set; }
    public string Target { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string? Value { get; set; }
    public CheckStatus Status { get; set; }
    public string? Details { get; set; }
    public string? ErrorMessage { get; set; }
    public double? ExecutionTimeMs { get; set; }
    public DateTime CheckedAt { get; set; }
}

public class AlertDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public AlertState State { get; set; }
    public MonitoringDomain Domain { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? MetricValue { get; set; }
    public string? Threshold { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public string? Notes { get; set; }
}

public class HealthSummaryDto
{
    public string Domain { get; set; } = string.Empty;
    public CheckStatus OverallStatus { get; set; }
    public int ActiveAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public DateTime LastChecked { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class DashboardDto
{
    public List<HealthSummaryDto> DomainSummaries { get; set; } = new();
    public List<AlertDto> RecentAlerts { get; set; } = new();
    public List<CheckResultDto> RecentChecks { get; set; } = new();
    public Dictionary<string, object> SystemMetrics { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class AlertRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public MonitoringDomain Domain { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? MetricValue { get; set; }
    public string? Threshold { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

public class AcknowledgeAlertRequest
{
    public string Notes { get; set; } = string.Empty;
}

public class ResolveAlertRequest
{
    public string Notes { get; set; } = string.Empty;
}