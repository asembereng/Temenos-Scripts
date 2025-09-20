using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AllUsers")]
public class AlertsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAlertService _alertService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        IUnitOfWork unitOfWork,
        IAlertService alertService,
        IAuditService auditService,
        ILogger<AlertsController> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all active alerts
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetActiveAlerts(CancellationToken cancellationToken = default)
    {
        try
        {
            var alerts = await _unitOfWork.Alerts.GetActiveAlertsAsync(cancellationToken);
            var alertDtos = alerts.Select(MapToAlertDto);

            return Ok(alertDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active alerts");
            return StatusCode(500, "Internal server error while retrieving active alerts");
        }
    }

    /// <summary>
    /// Get alert by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AlertDto>> GetAlert(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await _unitOfWork.Alerts.GetByIdAsync(id, cancellationToken);
            if (alert == null)
            {
                return NotFound($"Alert with ID {id} not found");
            }

            return Ok(MapToAlertDto(alert));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alert {AlertId}", id);
            return StatusCode(500, "Internal server error while retrieving alert");
        }
    }

    /// <summary>
    /// Get alerts with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetAlerts(
        [FromQuery] AlertState? state = null,
        [FromQuery] AlertSeverity? severity = null,
        [FromQuery] MonitoringDomain? domain = null,
        [FromQuery] DateTime? since = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alerts = await _unitOfWork.Alerts.GetAllAsync(cancellationToken);

            // Apply filters
            if (state.HasValue)
            {
                alerts = alerts.Where(a => a.State == state.Value);
            }

            if (severity.HasValue)
            {
                alerts = alerts.Where(a => a.Severity == severity.Value);
            }

            if (domain.HasValue)
            {
                alerts = alerts.Where(a => a.Domain == domain.Value);
            }

            if (since.HasValue)
            {
                alerts = alerts.Where(a => a.CreatedAt >= since.Value);
            }

            // Apply pagination
            var totalCount = alerts.Count();
            var pagedAlerts = alerts
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToAlertDto);

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(pagedAlerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alerts with filters");
            return StatusCode(500, "Internal server error while retrieving alerts");
        }
    }

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult> AcknowledgeAlert(int id, [FromBody] AcknowledgeAlertRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var userId = User.Identity?.Name ?? "Unknown";

            await _alertService.AcknowledgeAlertAsync(id, userName, request.Notes, cancellationToken);

            await _auditService.LogEventAsync(
                userId,
                userName,
                "AcknowledgeAlert",
                $"Alert/{id}",
                new { AlertId = id, Notes = request.Notes },
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString(),
                cancellationToken);

            _logger.LogInformation("Alert {AlertId} acknowledged by {User}", id, userName);

            return Ok(new { Message = "Alert acknowledged successfully", AlertId = id });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge alert {AlertId}", id);
            return StatusCode(500, "Internal server error while acknowledging alert");
        }
    }

    /// <summary>
    /// Resolve an alert
    /// </summary>
    [HttpPost("{id}/resolve")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult> ResolveAlert(int id, [FromBody] ResolveAlertRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var userId = User.Identity?.Name ?? "Unknown";

            await _alertService.ResolveAlertAsync(id, userName, request.Notes, cancellationToken);

            await _auditService.LogEventAsync(
                userId,
                userName,
                "ResolveAlert",
                $"Alert/{id}",
                new { AlertId = id, Notes = request.Notes },
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString(),
                cancellationToken);

            _logger.LogInformation("Alert {AlertId} resolved by {User}", id, userName);

            return Ok(new { Message = "Alert resolved successfully", AlertId = id });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve alert {AlertId}", id);
            return StatusCode(500, "Internal server error while resolving alert");
        }
    }

    /// <summary>
    /// Create a manual alert
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<AlertDto>> CreateAlert([FromBody] AlertRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var userId = User.Identity?.Name ?? "Unknown";

            // Create a mock check result from the alert request
            var checkResult = new MockCheckResult(
                request.Domain,
                request.Source,
                "ManualAlert",
                request.Severity == AlertSeverity.Critical ? CheckStatus.Critical : CheckStatus.Warning,
                request.MetricValue,
                System.Text.Json.JsonSerializer.Serialize(request.AdditionalData),
                null,
                0);

            var alert = await _alertService.CreateAlertAsync(checkResult, cancellationToken);

            // Override with custom title and description
            alert.Title = request.Title;
            alert.Description = request.Description;
            alert.Threshold = request.Threshold;

            await _unitOfWork.Alerts.UpdateAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogEventAsync(
                userId,
                userName,
                "CreateAlert",
                $"Alert/{alert.Id}",
                request,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString(),
                cancellationToken);

            _logger.LogInformation("Manual alert {AlertId} created by {User}", alert.Id, userName);

            return CreatedAtAction(nameof(GetAlert), new { id = alert.Id }, MapToAlertDto(alert));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create manual alert");
            return StatusCode(500, "Internal server error while creating alert");
        }
    }

    /// <summary>
    /// Get alert statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetAlertStatistics(
        [FromQuery] DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sinceDate = since ?? DateTime.UtcNow.AddDays(-7);

            var allAlerts = await _unitOfWork.Alerts.FindAsync(a => a.CreatedAt >= sinceDate, cancellationToken);

            var statistics = new
            {
                TotalAlerts = allAlerts.Count(),
                ActiveAlerts = allAlerts.Count(a => a.State == AlertState.Active),
                AcknowledgedAlerts = allAlerts.Count(a => a.State == AlertState.Acknowledged),
                ResolvedAlerts = allAlerts.Count(a => a.State == AlertState.Resolved),
                CriticalAlerts = allAlerts.Count(a => a.Severity == AlertSeverity.Critical),
                WarningAlerts = allAlerts.Count(a => a.Severity == AlertSeverity.Warning),
                InfoAlerts = allAlerts.Count(a => a.Severity == AlertSeverity.Info),
                ByDomain = allAlerts.GroupBy(a => a.Domain).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                RecentTrend = allAlerts
                    .Where(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    .GroupBy(a => a.CreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count()),
                Period = new
                {
                    Since = sinceDate,
                    Until = DateTime.UtcNow
                }
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alert statistics");
            return StatusCode(500, "Internal server error while retrieving alert statistics");
        }
    }

    private static AlertDto MapToAlertDto(TemenosAlertManager.Core.Entities.Alert alert)
    {
        return new AlertDto
        {
            Id = alert.Id,
            Title = alert.Title,
            Description = alert.Description,
            Severity = alert.Severity,
            State = alert.State,
            Domain = alert.Domain,
            Source = alert.Source,
            MetricValue = alert.MetricValue,
            Threshold = alert.Threshold,
            CreatedAt = alert.CreatedAt,
            AcknowledgedAt = alert.AcknowledgedAt,
            AcknowledgedBy = alert.AcknowledgedBy,
            ResolvedAt = alert.ResolvedAt,
            ResolvedBy = alert.ResolvedBy,
            Notes = alert.Notes
        };
    }

    // Helper class for creating manual alerts
    private class MockCheckResult : ICheckResult
    {
        public MockCheckResult(MonitoringDomain domain, string target, string metric, CheckStatus status, string? value, string? details, string? errorMessage, double? executionTimeMs)
        {
            Domain = domain;
            Target = target;
            Metric = metric;
            Status = status;
            Value = value;
            Details = details;
            ErrorMessage = errorMessage;
            ExecutionTimeMs = executionTimeMs;
        }

        public MonitoringDomain Domain { get; }
        public string Target { get; }
        public string Metric { get; }
        public string? Value { get; }
        public CheckStatus Status { get; }
        public string? Details { get; }
        public string? ErrorMessage { get; }
        public double? ExecutionTimeMs { get; }
    }
}