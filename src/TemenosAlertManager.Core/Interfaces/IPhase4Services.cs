using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Core.Interfaces;

/// <summary>
/// Interface for comprehensive testing service
/// </summary>
public interface ITestingService
{
    Task<TestSuiteResultDto> RunComprehensiveTestsAsync(TestSuiteRequest request, string userId, CancellationToken cancellationToken = default);
    Task<TestResultDto> RunIndividualTestAsync(TestRequest request, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TestSuiteDto>> GetTestSuitesAsync(string environment, CancellationToken cancellationToken = default);
    Task<TestExecutionStatusDto> GetTestExecutionStatusAsync(string executionId, CancellationToken cancellationToken = default);
    Task<TestReportDto> GenerateTestReportAsync(TestReportRequest request, CancellationToken cancellationToken = default);
    Task<bool> CancelTestExecutionAsync(string executionId, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for performance testing and validation
/// </summary>
public interface IPerformanceTestingService
{
    Task<PerformanceTestResultDto> RunPerformanceTestAsync(PerformanceTestRequest request, string userId, CancellationToken cancellationToken = default);
    Task<LoadTestResultDto> RunLoadTestAsync(LoadTestRequest request, string userId, CancellationToken cancellationToken = default);
    Task<StressTestResultDto> RunStressTestAsync(StressTestRequest request, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PerformanceBenchmarkDto>> GetPerformanceBenchmarksAsync(string environment, CancellationToken cancellationToken = default);
    Task<PerformanceComparisonDto> ComparePerformanceAsync(PerformanceComparisonRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for security testing and validation
/// </summary>
public interface ISecurityTestingService
{
    Task<SecurityScanResultDto> RunSecurityScanAsync(SecurityScanRequest request, string userId, CancellationToken cancellationToken = default);
    Task<VulnerabilityAssessmentDto> RunVulnerabilityAssessmentAsync(VulnerabilityAssessmentRequest request, string userId, CancellationToken cancellationToken = default);
    Task<PenetrationTestResultDto> RunPenetrationTestAsync(PenetrationTestRequest request, string userId, CancellationToken cancellationToken = default);
    Task<ComplianceTestResultDto> RunComplianceTestAsync(ComplianceTestRequest request, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityIssueDto>> GetSecurityIssuesAsync(string environment, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for production deployment and monitoring
/// </summary>
public interface IProductionDeploymentService
{
    Task<DeploymentResultDto> InitiateDeploymentAsync(DeploymentRequest request, string userId, CancellationToken cancellationToken = default);
    Task<DeploymentStatusDto> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default);
    Task<DeploymentValidationDto> ValidateDeploymentAsync(DeploymentValidationRequest request, CancellationToken cancellationToken = default);
    Task<RollbackResultDto> RollbackDeploymentAsync(RollbackRequest request, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeploymentHistoryDto>> GetDeploymentHistoryAsync(string environment, CancellationToken cancellationToken = default);
    Task<HealthCheckResultDto> RunPostDeploymentHealthCheckAsync(string deploymentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for production monitoring and alerting
/// </summary>
public interface IProductionMonitoringService
{
    Task<ProductionHealthDto> GetProductionHealthAsync(string environment, CancellationToken cancellationToken = default);
    Task<AlertSummaryDto> GetActiveAlertsAsync(string environment, CancellationToken cancellationToken = default);
    Task<SystemMetricsDto> GetSystemMetricsAsync(string environment, CancellationToken cancellationToken = default);
    Task<IncidentDto> CreateIncidentAsync(IncidentRequest request, string userId, CancellationToken cancellationToken = default);
    Task<IncidentDto> UpdateIncidentAsync(string incidentId, IncidentUpdateRequest request, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<IncidentDto>> GetActiveIncidentsAsync(string environment, CancellationToken cancellationToken = default);
    Task<MaintenanceWindowDto> ScheduleMaintenanceAsync(MaintenanceWindowRequest request, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for quality assurance and validation
/// </summary>
public interface IQualityAssuranceService
{
    Task<QualityGateResultDto> RunQualityGatesAsync(QualityGateRequest request, CancellationToken cancellationToken = default);
    Task<CodeQualityResultDto> AnalyzeCodeQualityAsync(CodeQualityRequest request, CancellationToken cancellationToken = default);
    Task<TestCoverageReportDto> GenerateTestCoverageReportAsync(TestCoverageRequest request, CancellationToken cancellationToken = default);
    Task<RegressionTestResultDto> RunRegressionTestsAsync(RegressionTestRequest request, string userId, CancellationToken cancellationToken = default);
    Task<AcceptanceTestResultDto> RunAcceptanceTestsAsync(AcceptanceTestRequest request, string userId, CancellationToken cancellationToken = default);
}