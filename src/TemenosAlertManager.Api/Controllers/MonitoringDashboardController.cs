using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Controllers;

/// <summary>
/// Controller for SOD/EOD operation monitoring and dashboards
/// </summary>
[Route("api/monitoring")]
[ApiController]
[Authorize(Policy = "OperatorOrAdmin")]
public class MonitoringDashboardController : ControllerBase
{
    private readonly IOperationMonitor _operationMonitor;
    private readonly IDependencyManager _dependencyManager;
    private readonly ILogger<MonitoringDashboardController> _logger;

    public MonitoringDashboardController(
        IOperationMonitor operationMonitor,
        IDependencyManager dependencyManager,
        ILogger<MonitoringDashboardController> logger)
    {
        _operationMonitor = operationMonitor ?? throw new ArgumentNullException(nameof(operationMonitor));
        _dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get comprehensive operation dashboard
    /// </summary>
    /// <returns>Dashboard with active operations, system health, and trends</returns>
    [HttpGet("dashboard")]
    public async Task<ActionResult<OperationDashboardDto>> GetOperationDashboard(CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboard = await _operationMonitor.GetOperationDashboardAsync(cancellationToken);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation dashboard");
            return StatusCode(500, new { Message = "Failed to get dashboard data", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get real-time metrics for a specific operation
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <returns>Real-time operation metrics</returns>
    [HttpGet("operations/{operationId}/metrics")]
    public async Task<ActionResult<OperationMetrics>> GetOperationMetrics(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _operationMonitor.GetOperationMetricsAsync(operationId, cancellationToken);
            return Ok(metrics);
        }
        catch (ArgumentException)
        {
            return NotFound(new { Message = $"Operation {operationId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for operation {OperationId}", operationId);
            return StatusCode(500, new { Message = "Failed to get operation metrics", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get service dependency graph for visualization
    /// </summary>
    /// <param name="environment">Environment name</param>
    /// <param name="operationType">Operation type (SOD or EOD)</param>
    /// <returns>Service dependency graph</returns>
    [HttpGet("dependencies")]
    public async Task<ActionResult<ServiceDependencyGraph>> GetServiceDependencies(
        [FromQuery] string environment, 
        [FromQuery] string operationType = "SOD", 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(environment))
            {
                return BadRequest(new { Message = "Environment parameter is required" });
            }

            if (!new[] { "SOD", "EOD" }.Contains(operationType.ToUpper()))
            {
                return BadRequest(new { Message = "OperationType must be either SOD or EOD" });
            }

            var dependencyGraph = await _dependencyManager.ResolveServiceDependenciesAsync(environment, operationType.ToUpper(), cancellationToken);
            return Ok(dependencyGraph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service dependencies for environment {Environment} and operation type {OperationType}", environment, operationType);
            return StatusCode(500, new { Message = "Failed to get service dependencies", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get optimal execution plan for services
    /// </summary>
    /// <param name="environment">Environment name</param>
    /// <param name="operationType">Operation type (SOD or EOD)</param>
    /// <param name="serviceIds">Optional service ID filter</param>
    /// <returns>Service execution plan with phases</returns>
    [HttpPost("execution-plan")]
    public async Task<ActionResult<ServiceExecutionPlan>> GetExecutionPlan(
        [FromBody] ExecutionPlanRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Environment))
            {
                return BadRequest(new { Message = "Environment is required" });
            }

            if (!new[] { "SOD", "EOD" }.Contains(request.OperationType.ToUpper()))
            {
                return BadRequest(new { Message = "OperationType must be either SOD or EOD" });
            }

            // This would need to be implemented to get the actual services
            // For now, return a simulated execution plan
            var executionPlan = new ServiceExecutionPlan
            {
                Phases = new[]
                {
                    new ExecutionPhase
                    {
                        PhaseNumber = 1,
                        PhaseName = "Database Services",
                        Services = Array.Empty<Core.Entities.Configuration.ServiceConfig>(),
                        CanExecuteInParallel = false,
                        EstimatedDuration = TimeSpan.FromMinutes(5)
                    },
                    new ExecutionPhase
                    {
                        PhaseNumber = 2,
                        PhaseName = "Core Application Services",
                        Services = Array.Empty<Core.Entities.Configuration.ServiceConfig>(),
                        CanExecuteInParallel = true,
                        EstimatedDuration = TimeSpan.FromMinutes(10)
                    },
                    new ExecutionPhase
                    {
                        PhaseNumber = 3,
                        PhaseName = "Integration Services",
                        Services = Array.Empty<Core.Entities.Configuration.ServiceConfig>(),
                        CanExecuteInParallel = true,
                        EstimatedDuration = TimeSpan.FromMinutes(3)
                    }
                },
                EstimatedTotalDuration = TimeSpan.FromMinutes(18),
                TotalServices = 15,
                CriticalServices = 8
            };

            return Ok(executionPlan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution plan for environment {Environment}", request.Environment);
            return StatusCode(500, new { Message = "Failed to get execution plan", Error = ex.Message });
        }
    }

    /// <summary>
    /// Validate service dependencies and constraints
    /// </summary>
    /// <param name="request">Validation request</param>
    /// <returns>Validation result with errors and warnings</returns>
    [HttpPost("validate-dependencies")]
    public async Task<ActionResult<ValidationResult>> ValidateDependencies(
        [FromBody] DependencyValidationRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.OperationType))
            {
                return BadRequest(new { Message = "OperationType is required" });
            }

            if (!new[] { "SOD", "EOD" }.Contains(request.OperationType.ToUpper()))
            {
                return BadRequest(new { Message = "OperationType must be either SOD or EOD" });
            }

            // This would need to get actual services based on request
            // For now, return a simulated validation result
            var validationResult = new ValidationResult
            {
                IsValid = true,
                Errors = Array.Empty<string>(),
                Warnings = new[] { "Some non-critical dependencies are missing" },
                ValidationDetails = new Dictionary<string, object>
                {
                    ["TotalServices"] = request.ServiceIds?.Length ?? 0,
                    ["ValidatedAt"] = DateTime.UtcNow
                }
            };

            return Ok(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate dependencies for operation type {OperationType}", request.OperationType);
            return StatusCode(500, new { Message = "Failed to validate dependencies", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get historical performance trends
    /// </summary>
    /// <param name="timeRange">Time range in hours (default: 24)</param>
    /// <param name="metricName">Specific metric name to filter (optional)</param>
    /// <returns>Performance trend data</returns>
    [HttpGet("performance-trends")]
    public async Task<ActionResult<PerformanceTrendDto[]>> GetPerformanceTrends(
        [FromQuery] int timeRange = 24,
        [FromQuery] string? metricName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (timeRange <= 0 || timeRange > 168) // Max 1 week
            {
                return BadRequest(new { Message = "TimeRange must be between 1 and 168 hours" });
            }

            // Simulate performance trend data
            var trends = new List<PerformanceTrendDto>();
            var now = DateTime.UtcNow;

            for (int i = 0; i < timeRange; i++)
            {
                var timestamp = now.AddHours(-i);
                
                if (string.IsNullOrEmpty(metricName) || metricName.Equals("OperationDuration", StringComparison.OrdinalIgnoreCase))
                {
                    trends.Add(new PerformanceTrendDto
                    {
                        Timestamp = timestamp,
                        MetricName = "OperationDuration",
                        Value = 10 + new Random().NextDouble() * 5,
                        Trend = "Stable"
                    });
                }

                if (string.IsNullOrEmpty(metricName) || metricName.Equals("SuccessRate", StringComparison.OrdinalIgnoreCase))
                {
                    trends.Add(new PerformanceTrendDto
                    {
                        Timestamp = timestamp,
                        MetricName = "SuccessRate",
                        Value = 95 + new Random().NextDouble() * 5,
                        Trend = "Improving"
                    });
                }

                if (string.IsNullOrEmpty(metricName) || metricName.Equals("ResourceUtilization", StringComparison.OrdinalIgnoreCase))
                {
                    trends.Add(new PerformanceTrendDto
                    {
                        Timestamp = timestamp,
                        MetricName = "ResourceUtilization",
                        Value = 60 + new Random().NextDouble() * 30,
                        Trend = "Stable"
                    });
                }
            }

            var result = trends.OrderBy(t => t.Timestamp).ToArray();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance trends");
            return StatusCode(500, new { Message = "Failed to get performance trends", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get system health overview
    /// </summary>
    /// <returns>Current system health status</returns>
    [HttpGet("system-health")]
    public async Task<ActionResult<SystemHealthDto>> GetSystemHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboard = await _operationMonitor.GetOperationDashboardAsync(cancellationToken);
            return Ok(dashboard.SystemHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health");
            return StatusCode(500, new { Message = "Failed to get system health", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get alert summary
    /// </summary>
    /// <returns>Current alert summary</returns>
    [HttpGet("alerts")]
    public async Task<ActionResult<AlertSummaryDto>> GetAlertSummary(CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboard = await _operationMonitor.GetOperationDashboardAsync(cancellationToken);
            return Ok(dashboard.AlertSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alert summary");
            return StatusCode(500, new { Message = "Failed to get alert summary", Error = ex.Message });
        }
    }
}

/// <summary>
/// Request DTO for execution plan
/// </summary>
public class ExecutionPlanRequest
{
    /// <summary>
    /// Environment name
    /// </summary>
    public string Environment { get; set; } = string.Empty;
    
    /// <summary>
    /// Operation type (SOD or EOD)
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional service IDs to include in the plan
    /// </summary>
    public int[]? ServiceIds { get; set; }
}

/// <summary>
/// Request DTO for dependency validation
/// </summary>
public class DependencyValidationRequest
{
    /// <summary>
    /// Operation type (SOD or EOD)
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Service IDs to validate
    /// </summary>
    public int[]? ServiceIds { get; set; }
}