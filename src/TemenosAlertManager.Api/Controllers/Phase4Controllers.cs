using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;
using TemenosAlertManager.Core.Enums;
using AlertSummaryDto = TemenosAlertManager.Core.Interfaces.AlertSummaryDto;

namespace TemenosAlertManager.Api.Controllers;

/// <summary>
/// Controller for comprehensive testing capabilities
/// </summary>
[ApiController]
[Route("api/testing")]
[Authorize(Policy = "OperatorOrAdmin")]
public class TestingController : ControllerBase
{
    private readonly ITestingService _testingService;
    private readonly IAuditService _auditService;
    private readonly ILogger<TestingController> _logger;

    public TestingController(
        ITestingService testingService,
        IAuditService auditService,
        ILogger<TestingController> logger)
    {
        _testingService = testingService ?? throw new ArgumentNullException(nameof(testingService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Run comprehensive test suite
    /// </summary>
    [HttpPost("suites/run")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TestSuiteResultDto>> RunTestSuite([FromBody] TestSuiteRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _testingService.RunComprehensiveTestsAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunTestSuite", $"TestSuite:{request.TestSuiteName}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run test suite {TestSuiteName} in environment {Environment}",
                request.TestSuiteName, request.Environment);
            return StatusCode(500, new { message = "Failed to run test suite", error = ex.Message });
        }
    }

    /// <summary>
    /// Run individual test
    /// </summary>
    [HttpPost("tests/run")]
    public async Task<ActionResult<TestResultDto>> RunIndividualTest([FromBody] TestRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _testingService.RunIndividualTestAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunIndividualTest", $"Test:{request.TestName}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run test {TestName} in environment {Environment}",
                request.TestName, request.Environment);
            return StatusCode(500, new { message = "Failed to run test", error = ex.Message });
        }
    }

    /// <summary>
    /// Get available test suites
    /// </summary>
    [HttpGet("suites")]
    public async Task<ActionResult<IEnumerable<TestSuiteDto>>> GetTestSuites([FromQuery] string environment)
    {
        try
        {
            var testSuites = await _testingService.GetTestSuitesAsync(environment);
            return Ok(testSuites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get test suites for environment {Environment}", environment);
            return StatusCode(500, new { message = "Failed to get test suites", error = ex.Message });
        }
    }

    /// <summary>
    /// Get test execution status
    /// </summary>
    [HttpGet("executions/{executionId}/status")]
    public async Task<ActionResult<TestExecutionStatusDto>> GetTestExecutionStatus(string executionId)
    {
        try
        {
            var status = await _testingService.GetTestExecutionStatusAsync(executionId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get test execution status for {ExecutionId}", executionId);
            return StatusCode(500, new { message = "Failed to get test execution status", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate test report
    /// </summary>
    [HttpPost("reports/generate")]
    public async Task<ActionResult<TestReportDto>> GenerateTestReport([FromBody] TestReportRequest request)
    {
        try
        {
            var report = await _testingService.GenerateTestReportAsync(request);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate test report for execution {ExecutionId}", request.ExecutionId);
            return StatusCode(500, new { message = "Failed to generate test report", error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel test execution
    /// </summary>
    [HttpPost("executions/{executionId}/cancel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<bool>> CancelTestExecution(string executionId)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _testingService.CancelTestExecutionAsync(executionId, userId);

            await _auditService.LogEventAsync(
                userId, userId, "CancelTestExecution", $"ExecutionId:{executionId}",
                null, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel test execution {ExecutionId}", executionId);
            return StatusCode(500, new { message = "Failed to cancel test execution", error = ex.Message });
        }
    }
}

/// <summary>
/// Controller for performance testing capabilities
/// </summary>
[ApiController]
[Route("api/performance-testing")]
[Authorize(Policy = "OperatorOrAdmin")]
public class PerformanceTestingController : ControllerBase
{
    private readonly IPerformanceTestingService _performanceTestingService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PerformanceTestingController> _logger;

    public PerformanceTestingController(
        IPerformanceTestingService performanceTestingService,
        IAuditService auditService,
        ILogger<PerformanceTestingController> logger)
    {
        _performanceTestingService = performanceTestingService ?? throw new ArgumentNullException(nameof(performanceTestingService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Run performance test
    /// </summary>
    [HttpPost("run")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PerformanceTestResultDto>> RunPerformanceTest([FromBody] PerformanceTestRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _performanceTestingService.RunPerformanceTestAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunPerformanceTest", $"Test:{request.TestName}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run performance test {TestName} in environment {Environment}",
                request.TestName, request.Environment);
            return StatusCode(500, new { message = "Failed to run performance test", error = ex.Message });
        }
    }

    /// <summary>
    /// Run load test
    /// </summary>
    [HttpPost("load-test")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<LoadTestResultDto>> RunLoadTest([FromBody] LoadTestRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _performanceTestingService.RunLoadTestAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunLoadTest", $"Test:{request.TestName}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run load test {TestName} in environment {Environment}",
                request.TestName, request.Environment);
            return StatusCode(500, new { message = "Failed to run load test", error = ex.Message });
        }
    }

    /// <summary>
    /// Run stress test
    /// </summary>
    [HttpPost("stress-test")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<StressTestResultDto>> RunStressTest([FromBody] StressTestRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _performanceTestingService.RunStressTestAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunStressTest", $"Test:{request.TestName}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run stress test {TestName} in environment {Environment}",
                request.TestName, request.Environment);
            return StatusCode(500, new { message = "Failed to run stress test", error = ex.Message });
        }
    }

    /// <summary>
    /// Get performance benchmarks
    /// </summary>
    [HttpGet("benchmarks")]
    public async Task<ActionResult<IEnumerable<PerformanceBenchmarkDto>>> GetPerformanceBenchmarks([FromQuery] string environment)
    {
        try
        {
            var benchmarks = await _performanceTestingService.GetPerformanceBenchmarksAsync(environment);
            return Ok(benchmarks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance benchmarks for environment {Environment}", environment);
            return StatusCode(500, new { message = "Failed to get performance benchmarks", error = ex.Message });
        }
    }

    /// <summary>
    /// Compare performance between time periods
    /// </summary>
    [HttpPost("compare")]
    public async Task<ActionResult<PerformanceComparisonDto>> ComparePerformance([FromBody] PerformanceComparisonRequest request)
    {
        try
        {
            var comparison = await _performanceTestingService.ComparePerformanceAsync(request);
            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare performance for environment {Environment}", request.Environment);
            return StatusCode(500, new { message = "Failed to compare performance", error = ex.Message });
        }
    }
}

/// <summary>
/// Controller for security testing capabilities
/// </summary>
[ApiController]
[Route("api/security-testing")]
[Authorize(Policy = "AdminOnly")]
public class SecurityTestingController : ControllerBase
{
    private readonly ISecurityTestingService _securityTestingService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SecurityTestingController> _logger;

    public SecurityTestingController(
        ISecurityTestingService securityTestingService,
        IAuditService auditService,
        ILogger<SecurityTestingController> logger)
    {
        _securityTestingService = securityTestingService ?? throw new ArgumentNullException(nameof(securityTestingService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Run security scan
    /// </summary>
    [HttpPost("scan")]
    public async Task<ActionResult<SecurityScanResultDto>> RunSecurityScan([FromBody] SecurityScanRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _securityTestingService.RunSecurityScanAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunSecurityScan", $"Scan:{request.ScanName}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run security scan {ScanName} in environment {Environment}",
                request.ScanName, request.Environment);
            return StatusCode(500, new { message = "Failed to run security scan", error = ex.Message });
        }
    }

    /// <summary>
    /// Run vulnerability assessment
    /// </summary>
    [HttpPost("vulnerability-assessment")]
    public async Task<ActionResult<VulnerabilityAssessmentDto>> RunVulnerabilityAssessment([FromBody] VulnerabilityAssessmentRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _securityTestingService.RunVulnerabilityAssessmentAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunVulnerabilityAssessment", $"Environment:{request.Environment}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run vulnerability assessment in environment {Environment}", request.Environment);
            return StatusCode(500, new { message = "Failed to run vulnerability assessment", error = ex.Message });
        }
    }

    /// <summary>
    /// Run penetration test
    /// </summary>
    [HttpPost("penetration-test")]
    public async Task<ActionResult<PenetrationTestResultDto>> RunPenetrationTest([FromBody] PenetrationTestRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _securityTestingService.RunPenetrationTestAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunPenetrationTest", $"Test:{request.TestName}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run penetration test {TestName} in environment {Environment}",
                request.TestName, request.Environment);
            return StatusCode(500, new { message = "Failed to run penetration test", error = ex.Message });
        }
    }

    /// <summary>
    /// Run compliance test
    /// </summary>
    [HttpPost("compliance-test")]
    public async Task<ActionResult<ComplianceTestResultDto>> RunComplianceTest([FromBody] ComplianceTestRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _securityTestingService.RunComplianceTestAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunComplianceTest", $"Framework:{request.ComplianceFramework}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run compliance test for framework {ComplianceFramework} in environment {Environment}",
                request.ComplianceFramework, request.Environment);
            return StatusCode(500, new { message = "Failed to run compliance test", error = ex.Message });
        }
    }

    /// <summary>
    /// Get security issues
    /// </summary>
    [HttpGet("issues")]
    public async Task<ActionResult<IEnumerable<SecurityIssueDto>>> GetSecurityIssues([FromQuery] string environment)
    {
        try
        {
            var issues = await _securityTestingService.GetSecurityIssuesAsync(environment);
            return Ok(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security issues for environment {Environment}", environment);
            return StatusCode(500, new { message = "Failed to get security issues", error = ex.Message });
        }
    }
}

/// <summary>
/// Controller for production deployment capabilities
/// </summary>
[ApiController]
[Route("api/production-deployment")]
[Authorize(Policy = "AdminOnly")]
public class ProductionDeploymentController : ControllerBase
{
    private readonly IProductionDeploymentService _deploymentService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProductionDeploymentController> _logger;

    public ProductionDeploymentController(
        IProductionDeploymentService deploymentService,
        IAuditService auditService,
        ILogger<ProductionDeploymentController> logger)
    {
        _deploymentService = deploymentService ?? throw new ArgumentNullException(nameof(deploymentService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initiate production deployment
    /// </summary>
    [HttpPost("deploy")]
    public async Task<ActionResult<DeploymentResultDto>> InitiateDeployment([FromBody] DeploymentRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _deploymentService.InitiateDeploymentAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "InitiateDeployment", $"Version:{request.Version}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate deployment of version {Version} to environment {Environment}",
                request.Version, request.Environment);
            return StatusCode(500, new { message = "Failed to initiate deployment", error = ex.Message });
        }
    }

    /// <summary>
    /// Get deployment status
    /// </summary>
    [HttpGet("deployments/{deploymentId}/status")]
    public async Task<ActionResult<DeploymentStatusDto>> GetDeploymentStatus(string deploymentId)
    {
        try
        {
            var status = await _deploymentService.GetDeploymentStatusAsync(deploymentId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get deployment status for {DeploymentId}", deploymentId);
            return StatusCode(500, new { message = "Failed to get deployment status", error = ex.Message });
        }
    }

    /// <summary>
    /// Validate deployment readiness
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<DeploymentValidationDto>> ValidateDeployment([FromBody] DeploymentValidationRequest request)
    {
        try
        {
            var validation = await _deploymentService.ValidateDeploymentAsync(request);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate deployment for version {Version} in environment {Environment}",
                request.Version, request.Environment);
            return StatusCode(500, new { message = "Failed to validate deployment", error = ex.Message });
        }
    }

    /// <summary>
    /// Rollback deployment
    /// </summary>
    [HttpPost("rollback")]
    public async Task<ActionResult<RollbackResultDto>> RollbackDeployment([FromBody] RollbackRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _deploymentService.RollbackDeploymentAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RollbackDeployment", $"DeploymentId:{request.DeploymentId}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback deployment {DeploymentId}", request.DeploymentId);
            return StatusCode(500, new { message = "Failed to rollback deployment", error = ex.Message });
        }
    }

    /// <summary>
    /// Get deployment history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<DeploymentHistoryDto>>> GetDeploymentHistory([FromQuery] string environment)
    {
        try
        {
            var history = await _deploymentService.GetDeploymentHistoryAsync(environment);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get deployment history for environment {Environment}", environment);
            return StatusCode(500, new { message = "Failed to get deployment history", error = ex.Message });
        }
    }

    /// <summary>
    /// Run post-deployment health check
    /// </summary>
    [HttpPost("deployments/{deploymentId}/health-check")]
    public async Task<ActionResult<HealthCheckResultDto>> RunPostDeploymentHealthCheck(string deploymentId)
    {
        try
        {
            var result = await _deploymentService.RunPostDeploymentHealthCheckAsync(deploymentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run post-deployment health check for {DeploymentId}", deploymentId);
            return StatusCode(500, new { message = "Failed to run post-deployment health check", error = ex.Message });
        }
    }
}

/// <summary>
/// Controller for production monitoring capabilities
/// </summary>
[ApiController]
[Route("api/production-monitoring")]
[Authorize(Policy = "OperatorOrAdmin")]
public class ProductionMonitoringController : ControllerBase
{
    private readonly IProductionMonitoringService _monitoringService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProductionMonitoringController> _logger;

    public ProductionMonitoringController(
        IProductionMonitoringService monitoringService,
        IAuditService auditService,
        ILogger<ProductionMonitoringController> logger)
    {
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get production health status
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<ProductionHealthDto>> GetProductionHealth([FromQuery] string environment)
    {
        try
        {
            var health = await _monitoringService.GetProductionHealthAsync(environment);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get production health for environment {Environment}", environment);
            return StatusCode(500, new { message = "Failed to get production health", error = ex.Message });
        }
    }

    /// <summary>
    /// Get active alerts
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<AlertSummaryDto>> GetActiveAlerts([FromQuery] string environment)
    {
        try
        {
            var alerts = await _monitoringService.GetActiveAlertsAsync(environment);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active alerts for environment {Environment}", environment);
            return StatusCode(500, new { message = "Failed to get active alerts", error = ex.Message });
        }
    }

    /// <summary>
    /// Get system metrics
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<SystemMetricsDto>> GetSystemMetrics([FromQuery] string environment)
    {
        try
        {
            var metrics = await _monitoringService.GetSystemMetricsAsync(environment);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system metrics for environment {Environment}", environment);
            return StatusCode(500, new { message = "Failed to get system metrics", error = ex.Message });
        }
    }

    /// <summary>
    /// Create incident
    /// </summary>
    [HttpPost("incidents")]
    public async Task<ActionResult<IncidentDto>> CreateIncident([FromBody] IncidentRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var incident = await _monitoringService.CreateIncidentAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "CreateIncident", $"Incident:{incident.IncidentId}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return CreatedAtAction(nameof(GetIncident), new { incidentId = incident.IncidentId }, incident);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create incident for environment {Environment}", request.Environment);
            return StatusCode(500, new { message = "Failed to create incident", error = ex.Message });
        }
    }

    /// <summary>
    /// Update incident
    /// </summary>
    [HttpPut("incidents/{incidentId}")]
    public async Task<ActionResult<IncidentDto>> UpdateIncident(string incidentId, [FromBody] IncidentUpdateRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var incident = await _monitoringService.UpdateIncidentAsync(incidentId, request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "UpdateIncident", $"Incident:{incidentId}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(incident);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update incident {IncidentId}", incidentId);
            return StatusCode(500, new { message = "Failed to update incident", error = ex.Message });
        }
    }

    /// <summary>
    /// Get incident details
    /// </summary>
    [HttpGet("incidents/{incidentId}")]
    public async Task<ActionResult<IncidentDto>> GetIncident(string incidentId)
    {
        try
        {
            var incidents = await _monitoringService.GetActiveIncidentsAsync("");
            var incident = incidents.FirstOrDefault(i => i.IncidentId == incidentId);
            
            if (incident == null)
                return NotFound(new { message = "Incident not found" });

            return Ok(incident);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get incident {IncidentId}", incidentId);
            return StatusCode(500, new { message = "Failed to get incident", error = ex.Message });
        }
    }

    /// <summary>
    /// Get active incidents
    /// </summary>
    [HttpGet("incidents")]
    public async Task<ActionResult<IEnumerable<IncidentDto>>> GetActiveIncidents([FromQuery] string environment)
    {
        try
        {
            var incidents = await _monitoringService.GetActiveIncidentsAsync(environment);
            return Ok(incidents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active incidents for environment {Environment}", environment);
            return StatusCode(500, new { message = "Failed to get active incidents", error = ex.Message });
        }
    }

    /// <summary>
    /// Schedule maintenance window
    /// </summary>
    [HttpPost("maintenance")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<MaintenanceWindowDto>> ScheduleMaintenance([FromBody] MaintenanceWindowRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var maintenance = await _monitoringService.ScheduleMaintenanceAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "ScheduleMaintenance", $"Maintenance:{maintenance.WindowId}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return CreatedAtAction(nameof(GetMaintenance), new { windowId = maintenance.WindowId }, maintenance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule maintenance for environment {Environment}", request.Environment);
            return StatusCode(500, new { message = "Failed to schedule maintenance", error = ex.Message });
        }
    }

    /// <summary>
    /// Get maintenance window details
    /// </summary>
    [HttpGet("maintenance/{windowId}")]
    public async Task<ActionResult<MaintenanceWindowDto>> GetMaintenance(string windowId)
    {
        try
        {
            // This would typically fetch from a service, simplified for example
            var maintenance = new MaintenanceWindowDto
            {
                WindowId = windowId,
                Title = "Sample Maintenance",
                Status = MaintenanceStatus.Scheduled
            };
            return Ok(maintenance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get maintenance window {WindowId}", windowId);
            return StatusCode(500, new { message = "Failed to get maintenance window", error = ex.Message });
        }
    }
}

/// <summary>
/// Controller for quality assurance capabilities
/// </summary>
[ApiController]
[Route("api/quality-assurance")]
[Authorize(Policy = "OperatorOrAdmin")]
public class QualityAssuranceController : ControllerBase
{
    private readonly IQualityAssuranceService _qaService;
    private readonly IAuditService _auditService;
    private readonly ILogger<QualityAssuranceController> _logger;

    public QualityAssuranceController(
        IQualityAssuranceService qaService,
        IAuditService auditService,
        ILogger<QualityAssuranceController> logger)
    {
        _qaService = qaService ?? throw new ArgumentNullException(nameof(qaService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Run quality gates
    /// </summary>
    [HttpPost("quality-gates")]
    public async Task<ActionResult<QualityGateResultDto>> RunQualityGates([FromBody] QualityGateRequest request)
    {
        try
        {
            var result = await _qaService.RunQualityGatesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run quality gates for version {Version} in environment {Environment}",
                request.Version, request.Environment);
            return StatusCode(500, new { message = "Failed to run quality gates", error = ex.Message });
        }
    }

    /// <summary>
    /// Analyze code quality
    /// </summary>
    [HttpPost("code-quality")]
    public async Task<ActionResult<CodeQualityResultDto>> AnalyzeCodeQuality([FromBody] CodeQualityRequest request)
    {
        try
        {
            var result = await _qaService.AnalyzeCodeQualityAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze code quality for version {Version} in environment {Environment}",
                request.Version, request.Environment);
            return StatusCode(500, new { message = "Failed to analyze code quality", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate test coverage report
    /// </summary>
    [HttpPost("test-coverage")]
    public async Task<ActionResult<TestCoverageReportDto>> GenerateTestCoverageReport([FromBody] TestCoverageRequest request)
    {
        try
        {
            var result = await _qaService.GenerateTestCoverageReportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate test coverage report for version {Version} in environment {Environment}",
                request.Version, request.Environment);
            return StatusCode(500, new { message = "Failed to generate test coverage report", error = ex.Message });
        }
    }

    /// <summary>
    /// Run regression tests
    /// </summary>
    [HttpPost("regression-tests")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RegressionTestResultDto>> RunRegressionTests([FromBody] RegressionTestRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _qaService.RunRegressionTestsAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunRegressionTests", $"Baseline:{request.BaselineVersion} Current:{request.CurrentVersion}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run regression tests for baseline {BaselineVersion} vs current {CurrentVersion} in environment {Environment}",
                request.BaselineVersion, request.CurrentVersion, request.Environment);
            return StatusCode(500, new { message = "Failed to run regression tests", error = ex.Message });
        }
    }

    /// <summary>
    /// Run acceptance tests
    /// </summary>
    [HttpPost("acceptance-tests")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<AcceptanceTestResultDto>> RunAcceptanceTests([FromBody] AcceptanceTestRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _qaService.RunAcceptanceTestsAsync(request, userId);

            await _auditService.LogEventAsync(
                userId, userId, "RunAcceptanceTests", $"Version:{request.Version}",
                request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run acceptance tests for version {Version} in environment {Environment}",
                request.Version, request.Environment);
            return StatusCode(500, new { message = "Failed to run acceptance tests", error = ex.Message });
        }
    }
}