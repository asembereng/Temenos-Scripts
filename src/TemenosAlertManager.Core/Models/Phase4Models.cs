using System.ComponentModel.DataAnnotations;

namespace TemenosAlertManager.Core.Models;

// Testing Models
public class TestSuiteRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string TestSuiteName { get; set; } = string.Empty;
    public string[]? TestCategories { get; set; }
    public bool IncludeIntegrationTests { get; set; } = true;
    public bool IncludePerformanceTests { get; set; } = false;
    public bool FailFast { get; set; } = false;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class TestSuiteResultDto
{
    public string ExecutionId { get; set; } = string.Empty;
    public string TestSuiteName { get; set; } = string.Empty;
    public TestExecutionStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public decimal PassRate { get; set; }
    public TestResultDto[] TestResults { get; set; } = Array.Empty<TestResultDto>();
    public string? ErrorMessage { get; set; }
}

public class TestRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string TestName { get; set; } = string.Empty;
    public string? TestCategory { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}

public class TestResultDto
{
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TestStatus Status { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public Dictionary<string, object>? Metrics { get; set; }
}

public class TestSuiteDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Categories { get; set; } = Array.Empty<string>();
    public int TestCount { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public string[] RequiredParameters { get; set; } = Array.Empty<string>();
}

public class TestExecutionStatusDto
{
    public string ExecutionId { get; set; } = string.Empty;
    public TestExecutionStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string CurrentTest { get; set; } = string.Empty;
    public string CurrentPhase { get; set; } = string.Empty;
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public string[] CompletedTests { get; set; } = Array.Empty<string>();
    public string[] PendingTests { get; set; } = Array.Empty<string>();
}

public class TestReportRequest
{
    [Required]
    public string ExecutionId { get; set; } = string.Empty;
    public ReportFormat Format { get; set; } = ReportFormat.PDF;
    public bool IncludeDetailedResults { get; set; } = true;
    public bool IncludeMetrics { get; set; } = true;
    public bool IncludeScreenshots { get; set; } = false;
}

public class TestReportDto
{
    public string ReportId { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public ReportFormat Format { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

// Performance Testing Models
public class PerformanceTestRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string TestName { get; set; } = string.Empty;
    public PerformanceTestType TestType { get; set; }
    public int ConcurrentUsers { get; set; } = 10;
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);
    public string[] TargetEndpoints { get; set; } = Array.Empty<string>();
    public Dictionary<string, object>? TestParameters { get; set; }
    public PerformanceThresholds? Thresholds { get; set; }
}

public class PerformanceTestResultDto
{
    public string TestId { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public PerformanceTestType TestType { get; set; }
    public TestStatus Status { get; set; }
    public DateTime ExecutionTime { get; set; }
    public TimeSpan Duration { get; set; }
    public PerformanceMetrics Metrics { get; set; } = new();
    public bool PassedThresholds { get; set; }
    public string[] Issues { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

public class LoadTestRequest : PerformanceTestRequest
{
    public int RampUpUsers { get; set; } = 1;
    public TimeSpan RampUpDuration { get; set; } = TimeSpan.FromMinutes(1);
    public int SustainUsers { get; set; } = 10;
    public TimeSpan SustainDuration { get; set; } = TimeSpan.FromMinutes(5);
}

public class LoadTestResultDto : PerformanceTestResultDto
{
    public LoadTestPhaseResult[] PhaseResults { get; set; } = Array.Empty<LoadTestPhaseResult>();
    public UserLoadProfile UserLoadProfile { get; set; } = new();
}

public class StressTestRequest : PerformanceTestRequest
{
    public int MaxUsers { get; set; } = 100;
    public int UserIncrement { get; set; } = 10;
    public TimeSpan IncrementDuration { get; set; } = TimeSpan.FromMinutes(1);
    public decimal FailureThreshold { get; set; } = 0.05m; // 5%
}

public class StressTestResultDto : PerformanceTestResultDto
{
    public int BreakingPoint { get; set; }
    public StressTestPhaseResult[] PhaseResults { get; set; } = Array.Empty<StressTestPhaseResult>();
    public SystemResourceUsage PeakResourceUsage { get; set; } = new();
}

public class PerformanceBenchmarkDto
{
    public string BenchmarkName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime BaselineDate { get; set; }
    public PerformanceMetrics BaselineMetrics { get; set; } = new();
    public PerformanceMetrics CurrentMetrics { get; set; } = new();
    public decimal PerformanceChange { get; set; }
    public string[] Trends { get; set; } = Array.Empty<string>();
}

public class PerformanceComparisonRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public DateTime StartDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }
    public string[]? TestNames { get; set; }
    public PerformanceTestType[]? TestTypes { get; set; }
}

public class PerformanceComparisonDto
{
    public string ComparisonId { get; set; } = string.Empty;
    public DateRange ComparisonPeriod { get; set; } = new();
    public PerformanceComparisonResult[] Results { get; set; } = Array.Empty<PerformanceComparisonResult>();
    public PerformanceTrendAnalysis TrendAnalysis { get; set; } = new();
    public string[] Insights { get; set; } = Array.Empty<string>();
}

// Security Testing Models
public class SecurityScanRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string ScanName { get; set; } = string.Empty;
    public SecurityScanType ScanType { get; set; }
    public string[] TargetSystems { get; set; } = Array.Empty<string>();
    public string[]? ScanProfiles { get; set; }
    public bool IncludeAuthenticated { get; set; } = true;
    public SecurityScanOptions? Options { get; set; }
}

public class SecurityScanResultDto
{
    public string ScanId { get; set; } = string.Empty;
    public string ScanName { get; set; } = string.Empty;
    public SecurityScanType ScanType { get; set; }
    public SecurityScanStatus Status { get; set; }
    public DateTime ScanTime { get; set; }
    public TimeSpan Duration { get; set; }
    public SecurityIssueSummary IssueSummary { get; set; } = new();
    public SecurityIssueDto[] Issues { get; set; } = Array.Empty<SecurityIssueDto>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
    public decimal SecurityScore { get; set; }
}

public class VulnerabilityAssessmentRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    public string[] TargetSystems { get; set; } = Array.Empty<string>();
    public bool IncludeNetworkScan { get; set; } = true;
    public bool IncludeWebApplicationScan { get; set; } = true;
    public bool IncludeDatabaseScan { get; set; } = true;
    public string[]? ExcludeIpRanges { get; set; }
}

public class VulnerabilityAssessmentDto
{
    public string AssessmentId { get; set; } = string.Empty;
    public DateTime AssessmentDate { get; set; }
    public VulnerabilityReport NetworkVulnerabilities { get; set; } = new();
    public VulnerabilityReport WebApplicationVulnerabilities { get; set; } = new();
    public VulnerabilityReport DatabaseVulnerabilities { get; set; } = new();
    public string[] RecommendedActions { get; set; } = Array.Empty<string>();
    public decimal OverallRiskScore { get; set; }
}

public class PenetrationTestRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string TestName { get; set; } = string.Empty;
    public string[] TargetSystems { get; set; } = Array.Empty<string>();
    public PenetrationTestScope Scope { get; set; } = new();
    public bool IsAuthorized { get; set; } = false;
    public string? AuthorizationReference { get; set; }
}

public class PenetrationTestResultDto
{
    public string TestId { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public TimeSpan Duration { get; set; }
    public PenetrationTestFinding[] Findings { get; set; } = Array.Empty<PenetrationTestFinding>();
    public string[] ExploitedVulnerabilities { get; set; } = Array.Empty<string>();
    public string[] RecommendedRemediation { get; set; } = Array.Empty<string>();
    public decimal SecurityPosture { get; set; }
}

public class ComplianceTestRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string ComplianceFramework { get; set; } = string.Empty; // PCI-DSS, SOX, GDPR, etc.
    public string[] ControlsToTest { get; set; } = Array.Empty<string>();
    public bool GenerateEvidencePackage { get; set; } = true;
}

public class ComplianceTestResultDto
{
    public string TestId { get; set; } = string.Empty;
    public string ComplianceFramework { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public ComplianceControlResult[] ControlResults { get; set; } = Array.Empty<ComplianceControlResult>();
    public decimal ComplianceScore { get; set; }
    public string[] NonCompliantControls { get; set; } = Array.Empty<string>();
    public string[] RecommendedActions { get; set; } = Array.Empty<string>();
    public string? EvidencePackageUrl { get; set; }
}

public class SecurityIssueDto
{
    public string IssueId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SecuritySeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string AffectedSystem { get; set; } = string.Empty;
    public string[] Recommendations { get; set; } = Array.Empty<string>();
    public DateTime DiscoveredDate { get; set; }
    public SecurityIssueStatus Status { get; set; }
}

// Production Deployment Models
public class DeploymentRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string Version { get; set; } = string.Empty;
    public DeploymentType DeploymentType { get; set; }
    public string[] Components { get; set; } = Array.Empty<string>();
    public bool RequireApproval { get; set; } = true;
    public DateTime? ScheduledTime { get; set; }
    public string? Comments { get; set; }
    public DeploymentOptions? Options { get; set; }
}

public class DeploymentResultDto
{
    public string DeploymentId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DeploymentStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DeploymentStep[] Steps { get; set; } = Array.Empty<DeploymentStep>();
    public string? ErrorMessage { get; set; }
    public HealthCheckResultDto? PostDeploymentHealth { get; set; }
}

public class DeploymentStatusDto
{
    public string DeploymentId { get; set; } = string.Empty;
    public DeploymentStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public DeploymentStep[] CompletedSteps { get; set; } = Array.Empty<DeploymentStep>();
    public DeploymentStep[] PendingSteps { get; set; } = Array.Empty<DeploymentStep>();
}

public class DeploymentValidationRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string Version { get; set; } = string.Empty;
    public bool ValidatePrerequisites { get; set; } = true;
    public bool ValidateCompatibility { get; set; } = true;
    public bool ValidateResources { get; set; } = true;
}

public class DeploymentValidationDto
{
    public string ValidationId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public ValidationResult[] ValidationResults { get; set; } = Array.Empty<ValidationResult>();
    public string[] Warnings { get; set; } = Array.Empty<string>();
    public string[] BlockingIssues { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

public class RollbackRequest
{
    [Required]
    public string DeploymentId { get; set; } = string.Empty;
    [Required]
    public string TargetVersion { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public bool ForceRollback { get; set; } = false;
}

public class RollbackResultDto
{
    public string RollbackId { get; set; } = string.Empty;
    public string OriginalDeploymentId { get; set; } = string.Empty;
    public string TargetVersion { get; set; } = string.Empty;
    public DeploymentStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public RollbackStep[] Steps { get; set; } = Array.Empty<RollbackStep>();
    public string? ErrorMessage { get; set; }
}

public class DeploymentHistoryDto
{
    public string DeploymentId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DeploymentType DeploymentType { get; set; }
    public DeploymentStatus Status { get; set; }
    public DateTime DeploymentTime { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool IsRollback { get; set; }
    public string? Comments { get; set; }
}

public class HealthCheckResultDto
{
    public string CheckId { get; set; } = string.Empty;
    public DateTime CheckTime { get; set; }
    public HealthCheckStatus OverallStatus { get; set; }
    public ComponentHealthResult[] ComponentResults { get; set; } = Array.Empty<ComponentHealthResult>();
    public string[] Issues { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

// Supporting Models and Enums
public enum TestStatus
{
    Passed,
    Failed,
    Skipped,
    Error
}

public enum TestExecutionStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum SecuritySeverity
{
    Informational,
    Low,
    Medium,
    High,
    Critical
}

public enum SecurityIssueStatus
{
    Open,
    InProgress,
    Resolved,
    Accepted,
    Closed
}

public enum HealthCheckStatus
{
    Healthy,
    Warning,
    Critical,
    Unknown
}

// Supporting Classes
public class PerformanceThresholds
{
    public decimal MaxResponseTime { get; set; } = 1000; // ms
    public decimal MinThroughput { get; set; } = 100; // requests/sec
    public decimal MaxErrorRate { get; set; } = 0.01m; // 1%
    public decimal MaxCpuUtilization { get; set; } = 0.8m; // 80%
    public decimal MaxMemoryUtilization { get; set; } = 0.8m; // 80%
}

public class PerformanceMetrics
{
    public decimal ResponseTime { get; set; }
    public decimal Throughput { get; set; }
    public decimal ErrorRate { get; set; }
    public decimal CpuUtilization { get; set; }
    public decimal MemoryUtilization { get; set; }
    public decimal DiskUtilization { get; set; }
    public decimal NetworkUtilization { get; set; }
}

public class LoadTestPhaseResult
{
    public string Phase { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int UserCount { get; set; }
    public PerformanceMetrics Metrics { get; set; } = new();
}

public class StressTestPhaseResult
{
    public int UserCount { get; set; }
    public PerformanceMetrics Metrics { get; set; } = new();
    public bool SystemStable { get; set; }
    public string[] Errors { get; set; } = Array.Empty<string>();
}

public class UserLoadProfile
{
    public int PeakUsers { get; set; }
    public DateTime PeakTime { get; set; }
    public LoadPattern[] LoadPatterns { get; set; } = Array.Empty<LoadPattern>();
}

public class LoadPattern
{
    public TimeSpan TimeOffset { get; set; }
    public int UserCount { get; set; }
    public string Activity { get; set; } = string.Empty;
}

public class SystemResourceUsage
{
    public decimal CpuUtilization { get; set; }
    public decimal MemoryUtilization { get; set; }
    public decimal DiskUtilization { get; set; }
    public decimal NetworkUtilization { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PerformanceComparisonResult
{
    public string TestName { get; set; } = string.Empty;
    public PerformanceMetrics BaselineMetrics { get; set; } = new();
    public PerformanceMetrics CurrentMetrics { get; set; } = new();
    public decimal PerformanceChange { get; set; }
    public string Trend { get; set; } = string.Empty;
}

public class PerformanceTrendAnalysis
{
    public string TrendDirection { get; set; } = string.Empty;
    public decimal TrendMagnitude { get; set; }
    public string[] IdentifiedPatterns { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

public class SecurityScanOptions
{
    public string[] IncludeChecks { get; set; } = Array.Empty<string>();
    public string[] ExcludeChecks { get; set; } = Array.Empty<string>();
    public int ScanIntensity { get; set; } = 3; // 1-5 scale
    public TimeSpan MaxScanDuration { get; set; } = TimeSpan.FromHours(2);
}

public class SecurityIssueSummary
{
    public int CriticalIssues { get; set; }
    public int HighIssues { get; set; }
    public int MediumIssues { get; set; }
    public int LowIssues { get; set; }
    public int InformationalIssues { get; set; }
    public int TotalIssues { get; set; }
}

public class VulnerabilityReport
{
    public string ScanType { get; set; } = string.Empty;
    public int VulnerabilityCount { get; set; }
    public VulnerabilityItem[] Vulnerabilities { get; set; } = Array.Empty<VulnerabilityItem>();
    public decimal RiskScore { get; set; }
}

public class VulnerabilityItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public SecuritySeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string[] AffectedSystems { get; set; } = Array.Empty<string>();
    public string[] Remediation { get; set; } = Array.Empty<string>();
}

public class PenetrationTestScope
{
    public string[] TargetNetworks { get; set; } = Array.Empty<string>();
    public string[] TargetApplications { get; set; } = Array.Empty<string>();
    public string[] ExcludedSystems { get; set; } = Array.Empty<string>();
    public bool SocialEngineering { get; set; } = false;
    public bool PhysicalSecurity { get; set; } = false;
}

public class PenetrationTestFinding
{
    public string FindingId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public SecuritySeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ExploitDetails { get; set; } = string.Empty;
    public string[] ImpactDescription { get; set; } = Array.Empty<string>();
    public string[] Remediation { get; set; } = Array.Empty<string>();
}

public class ComplianceControlResult
{
    public string ControlId { get; set; } = string.Empty;
    public string ControlName { get; set; } = string.Empty;
    public bool IsCompliant { get; set; }
    public string[] Evidence { get; set; } = Array.Empty<string>();
    public string[] Gaps { get; set; } = Array.Empty<string>();
    public string[] RequiredActions { get; set; } = Array.Empty<string>();
}

public class DeploymentOptions
{
    public bool BlueGreenDeployment { get; set; } = false;
    public bool CanaryDeployment { get; set; } = false;
    public int CanaryPercentage { get; set; } = 10;
    public bool RollingDeployment { get; set; } = true;
    public int MaxUnavailableInstances { get; set; } = 1;
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool AutoRollbackOnFailure { get; set; } = true;
}

public class DeploymentStep
{
    public string StepName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeploymentStepStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string[] SubSteps { get; set; } = Array.Empty<string>();
}

public class RollbackStep
{
    public string StepName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RollbackStepStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ValidationResult
{
    public string ValidationType { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public string[] Details { get; set; } = Array.Empty<string>();
}

public class ComponentHealthResult
{
    public string ComponentName { get; set; } = string.Empty;
    public HealthCheckStatus Status { get; set; }
    public string[] Metrics { get; set; } = Array.Empty<string>();
    public string? ErrorMessage { get; set; }
    public DateTime LastChecked { get; set; }
}

public enum DeploymentStepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

public enum RollbackStepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

// Production Monitoring Models
public class ProductionHealthDto
{
    public string Environment { get; set; } = string.Empty;
    public HealthCheckStatus OverallStatus { get; set; }
    public DateTime LastUpdated { get; set; }
    public ServiceHealthStatus[] ServiceStatuses { get; set; } = Array.Empty<ServiceHealthStatus>();
    public SystemMetricsDto SystemMetrics { get; set; } = new();
    public string[] ActiveAlerts { get; set; } = Array.Empty<string>();
    public string[] RecentIncidents { get; set; } = Array.Empty<string>();
}

public class ServiceHealthStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public HealthCheckStatus Status { get; set; }
    public DateTime LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metrics { get; set; }
}

public class AlertSummaryDto
{
    public string Environment { get; set; } = string.Empty;
    public int TotalActiveAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighAlerts { get; set; }
    public int MediumAlerts { get; set; }
    public int LowAlerts { get; set; }
    public AlertDto[] RecentAlerts { get; set; } = Array.Empty<AlertDto>();
    public DateTime LastUpdated { get; set; }
}

public class AlertDto
{
    public string AlertId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime TriggeredTime { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedTime { get; set; }
    public AlertStatus Status { get; set; }
}

public class SystemMetricsDto
{
    public decimal CpuUtilization { get; set; }
    public decimal MemoryUtilization { get; set; }
    public decimal DiskUtilization { get; set; }
    public decimal NetworkUtilization { get; set; }
    public int ActiveConnections { get; set; }
    public decimal ResponseTime { get; set; }
    public decimal Throughput { get; set; }
    public decimal ErrorRate { get; set; }
    public DateTime Timestamp { get; set; }
}

public class IncidentRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; }
    [Required]
    public string Environment { get; set; } = string.Empty;
    public string[] AffectedServices { get; set; } = Array.Empty<string>();
    public string? AssignedTo { get; set; }
}

public class IncidentDto
{
    public string IncidentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string[] AffectedServices { get; set; } = Array.Empty<string>();
    public DateTime ReportedTime { get; set; }
    public DateTime? AcknowledgedTime { get; set; }
    public DateTime? ResolvedTime { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? ResolutionNotes { get; set; }
    public IncidentAction[] ActionsTaken { get; set; } = Array.Empty<IncidentAction>();
}

public class IncidentUpdateRequest
{
    public IncidentStatus? Status { get; set; }
    public string? AssignedTo { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? UpdateNotes { get; set; }
}

public class IncidentAction
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class MaintenanceWindowRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public DateTime StartTime { get; set; }
    [Required]
    public DateTime EndTime { get; set; }
    public MaintenanceType MaintenanceType { get; set; }
    public string[] AffectedServices { get; set; } = Array.Empty<string>();
    public string[] NotificationRecipients { get; set; } = Array.Empty<string>();
}

public class MaintenanceWindowDto
{
    public string WindowId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public MaintenanceType MaintenanceType { get; set; }
    public MaintenanceStatus Status { get; set; }
    public string[] AffectedServices { get; set; } = Array.Empty<string>();
    public string ScheduledBy { get; set; } = string.Empty;
    public string[] NotificationRecipients { get; set; } = Array.Empty<string>();
}

// Quality Assurance Models
public class QualityGateRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string Version { get; set; } = string.Empty;
    public QualityGateType[] GateTypes { get; set; } = Array.Empty<QualityGateType>();
    public Dictionary<string, object>? Criteria { get; set; }
}

public class QualityGateResultDto
{
    public string GateId { get; set; } = string.Empty;
    public bool IsPassed { get; set; }
    public DateTime ExecutionTime { get; set; }
    public QualityGateResult[] Results { get; set; } = Array.Empty<QualityGateResult>();
    public string[] FailureReasons { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

public class QualityGateResult
{
    public QualityGateType GateType { get; set; }
    public bool IsPassed { get; set; }
    public decimal Score { get; set; }
    public decimal Threshold { get; set; }
    public string[] Details { get; set; } = Array.Empty<string>();
}

public class CodeQualityRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string Version { get; set; } = string.Empty;
    public string[] AnalysisRules { get; set; } = Array.Empty<string>();
    public bool IncludeDuplication { get; set; } = true;
    public bool IncludeComplexity { get; set; } = true;
    public bool IncludeCoverage { get; set; } = true;
}

public class CodeQualityResultDto
{
    public string AnalysisId { get; set; } = string.Empty;
    public DateTime AnalysisTime { get; set; }
    public decimal OverallScore { get; set; }
    public CodeQualityMetrics Metrics { get; set; } = new();
    public CodeIssue[] Issues { get; set; } = Array.Empty<CodeIssue>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

public class CodeQualityMetrics
{
    public int LinesOfCode { get; set; }
    public decimal TestCoverage { get; set; }
    public decimal CodeDuplication { get; set; }
    public decimal CyclomaticComplexity { get; set; }
    public int CodeSmells { get; set; }
    public int Bugs { get; set; }
    public int Vulnerabilities { get; set; }
}

public class CodeIssue
{
    public string IssueId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public CodeIssueSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Rule { get; set; } = string.Empty;
}

public class TestCoverageRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string Version { get; set; } = string.Empty;
    public string[] TestSuites { get; set; } = Array.Empty<string>();
    public bool IncludeBranchCoverage { get; set; } = true;
    public bool IncludeLineCoverage { get; set; } = true;
}

public class TestCoverageReportDto
{
    public string ReportId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public TestCoverageMetrics OverallCoverage { get; set; } = new();
    public ModuleCoverageResult[] ModuleCoverage { get; set; } = Array.Empty<ModuleCoverageResult>();
    public string[] UncoveredAreas { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

public class TestCoverageMetrics
{
    public decimal LineCoverage { get; set; }
    public decimal BranchCoverage { get; set; }
    public decimal FunctionCoverage { get; set; }
    public int TotalLines { get; set; }
    public int CoveredLines { get; set; }
    public int TotalBranches { get; set; }
    public int CoveredBranches { get; set; }
}

public class ModuleCoverageResult
{
    public string ModuleName { get; set; } = string.Empty;
    public TestCoverageMetrics Coverage { get; set; } = new();
    public string[] UncoveredMethods { get; set; } = Array.Empty<string>();
}

public class RegressionTestRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string BaselineVersion { get; set; } = string.Empty;
    [Required]
    public string CurrentVersion { get; set; } = string.Empty;
    public string[] TestSuites { get; set; } = Array.Empty<string>();
    public bool IncludePerformanceRegression { get; set; } = true;
}

public class RegressionTestResultDto
{
    public string TestId { get; set; } = string.Empty;
    public DateTime TestTime { get; set; }
    public RegressionTestStatus Status { get; set; }
    public RegressionResult[] FunctionalRegressions { get; set; } = Array.Empty<RegressionResult>();
    public PerformanceRegressionResult[] PerformanceRegressions { get; set; } = Array.Empty<PerformanceRegressionResult>();
    public string[] NewFailures { get; set; } = Array.Empty<string>();
    public string[] FixedIssues { get; set; } = Array.Empty<string>();
}

public class RegressionResult
{
    public string TestName { get; set; } = string.Empty;
    public TestStatus BaselineResult { get; set; }
    public TestStatus CurrentResult { get; set; }
    public bool IsRegression { get; set; }
    public string? RegresssionDetails { get; set; }
}

public class PerformanceRegressionResult
{
    public string MetricName { get; set; } = string.Empty;
    public decimal BaselineValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal PercentageChange { get; set; }
    public bool IsRegression { get; set; }
    public decimal Threshold { get; set; }
}

public class AcceptanceTestRequest
{
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string Version { get; set; } = string.Empty;
    public string[] AcceptanceCriteria { get; set; } = Array.Empty<string>();
    public string[] TestScenarios { get; set; } = Array.Empty<string>();
    public bool IncludeUserAcceptanceTests { get; set; } = true;
}

public class AcceptanceTestResultDto
{
    public string TestId { get; set; } = string.Empty;
    public DateTime TestTime { get; set; }
    public AcceptanceTestStatus Status { get; set; }
    public AcceptanceCriteriaResult[] CriteriaResults { get; set; } = Array.Empty<AcceptanceCriteriaResult>();
    public UserAcceptanceTestResult[] UserAcceptanceResults { get; set; } = Array.Empty<UserAcceptanceTestResult>();
    public string[] PassedCriteria { get; set; } = Array.Empty<string>();
    public string[] FailedCriteria { get; set; } = Array.Empty<string>();
}

public class AcceptanceCriteriaResult
{
    public string CriteriaId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMet { get; set; }
    public string[] Evidence { get; set; } = Array.Empty<string>();
    public string? FailureReason { get; set; }
}

public class UserAcceptanceTestResult
{
    public string TestScenario { get; set; } = string.Empty;
    public string TestDescription { get; set; } = string.Empty;
    public bool IsAccepted { get; set; }
    public string TestedBy { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string? Comments { get; set; }
}

// Additional Enums
public enum AlertSeverity
{
    Informational,
    Low,
    Medium,
    High,
    Critical
}

public enum AlertStatus
{
    Active,
    Acknowledged,
    Resolved,
    Suppressed
}

public enum CodeIssueSeverity
{
    Info,
    Minor,
    Major,
    Critical,
    Blocker
}

public enum RegressionTestStatus
{
    Passed,
    Failed,
    PassedWithWarnings,
    Error
}

public enum AcceptanceTestStatus
{
    Accepted,
    Rejected,
    ConditionallyAccepted,
    PendingReview
}