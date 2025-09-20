using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemenosAlertManager.Api.Services;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AllUsers")]
public class HealthController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IMonitoringService monitoringService, ILogger<HealthController> logger)
    {
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get overall system health dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboard = await _monitoringService.GetDashboardAsync(cancellationToken);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard data");
            return StatusCode(500, "Internal server error while retrieving dashboard data");
        }
    }

    /// <summary>
    /// Get health summary for a specific domain
    /// </summary>
    [HttpGet("summary/{domain}")]
    public async Task<ActionResult<HealthSummaryDto>> GetDomainHealth(string domain, CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _monitoringService.GetDomainHealthAsync(domain, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health summary for domain {Domain}", domain);
            return StatusCode(500, $"Internal server error while retrieving health summary for {domain}");
        }
    }

    /// <summary>
    /// Get recent check results
    /// </summary>
    [HttpGet("checks")]
    public async Task<ActionResult<IEnumerable<CheckResultDto>>> GetRecentChecks(
        [FromQuery] string? domain = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var checks = await _monitoringService.GetRecentChecksAsync(domain, limit, cancellationToken);
            return Ok(checks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent checks for domain {Domain}", domain);
            return StatusCode(500, "Internal server error while retrieving check results");
        }
    }

    /// <summary>
    /// Run a specific check manually
    /// </summary>
    [HttpPost("checks/run")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<CheckResultDto>> RunCheck(
        [FromBody] RunCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _monitoringService.RunCheckAsync(request.Domain, request.Target, request.Parameters, cancellationToken);
            
            var checkResultDto = new CheckResultDto
            {
                Domain = result.Domain,
                Target = result.Target,
                Metric = result.Metric,
                Status = result.Status,
                Value = result.Value,
                Details = result.Details,
                ErrorMessage = result.ErrorMessage,
                ExecutionTimeMs = result.ExecutionTimeMs,
                CheckedAt = DateTime.UtcNow
            };

            return Ok(checkResultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run check for domain {Domain}, target {Target}", request.Domain, request.Target);
            return StatusCode(500, "Internal server error while running check");
        }
    }
}

public class RunCheckRequest
{
    public string Domain { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}