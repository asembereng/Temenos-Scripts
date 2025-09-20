using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Controllers;

/// <summary>
/// Controller for managing individual Temenos service operations
/// </summary>
[Route("api/services")]
[ApiController]
[Authorize(Policy = "OperatorOrAdmin")]
public class ServiceManagementController : ControllerBase
{
    private readonly IServiceManagementService _serviceManagementService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ServiceManagementController> _logger;

    public ServiceManagementController(
        IServiceManagementService serviceManagementService,
        IAuditService auditService,
        ILogger<ServiceManagementController> logger)
    {
        _serviceManagementService = serviceManagementService ?? throw new ArgumentNullException(nameof(serviceManagementService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Start a specific service
    /// </summary>
    /// <param name="serviceId">ID of the service to start</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Service action result</returns>
    [HttpPost("{serviceId}/start")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ServiceActionResultDto>> StartService(int serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _logger.LogInformation("Service start requested by {UserName} for service ID {ServiceId}", userName, serviceId);

            // Audit the request
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                "SERVICE_START_REQUESTED", 
                $"ServiceId:{serviceId}", 
                null, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            var result = await _serviceManagementService.StartServiceAsync(serviceId, userName, cancellationToken);

            // Audit the result
            var auditAction = result.Status == "Completed" ? "SERVICE_STARTED" : "SERVICE_START_FAILED";
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                auditAction, 
                $"ServiceId:{serviceId},ServiceName:{result.ServiceName}", 
                result, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _logger.LogError(ex, "Failed to start service {ServiceId}", serviceId);

            await _auditService.LogFailureAsync(
                userName, 
                userName, 
                "SERVICE_START_ERROR", 
                $"ServiceId:{serviceId}", 
                ex.Message, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            return StatusCode(500, new { Message = "Failed to start service", Error = ex.Message });
        }
    }

    /// <summary>
    /// Stop a specific service
    /// </summary>
    /// <param name="serviceId">ID of the service to stop</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Service action result</returns>
    [HttpPost("{serviceId}/stop")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ServiceActionResultDto>> StopService(int serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _logger.LogInformation("Service stop requested by {UserName} for service ID {ServiceId}", userName, serviceId);

            // Audit the request
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                "SERVICE_STOP_REQUESTED", 
                $"ServiceId:{serviceId}", 
                null, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            var result = await _serviceManagementService.StopServiceAsync(serviceId, userName, cancellationToken);

            // Audit the result
            var auditAction = result.Status == "Completed" ? "SERVICE_STOPPED" : "SERVICE_STOP_FAILED";
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                auditAction, 
                $"ServiceId:{serviceId},ServiceName:{result.ServiceName}", 
                result, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _logger.LogError(ex, "Failed to stop service {ServiceId}", serviceId);

            await _auditService.LogFailureAsync(
                userName, 
                userName, 
                "SERVICE_STOP_ERROR", 
                $"ServiceId:{serviceId}", 
                ex.Message, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            return StatusCode(500, new { Message = "Failed to stop service", Error = ex.Message });
        }
    }

    /// <summary>
    /// Restart a specific service
    /// </summary>
    /// <param name="serviceId">ID of the service to restart</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Service action result</returns>
    [HttpPost("{serviceId}/restart")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ServiceActionResultDto>> RestartService(int serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _logger.LogInformation("Service restart requested by {UserName} for service ID {ServiceId}", userName, serviceId);

            // Audit the request
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                "SERVICE_RESTART_REQUESTED", 
                $"ServiceId:{serviceId}", 
                null, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            var result = await _serviceManagementService.RestartServiceAsync(serviceId, userName, cancellationToken);

            // Audit the result
            var auditAction = result.Status == "Completed" ? "SERVICE_RESTARTED" : "SERVICE_RESTART_FAILED";
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                auditAction, 
                $"ServiceId:{serviceId},ServiceName:{result.ServiceName}", 
                result, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _logger.LogError(ex, "Failed to restart service {ServiceId}", serviceId);

            await _auditService.LogFailureAsync(
                userName, 
                userName, 
                "SERVICE_RESTART_ERROR", 
                $"ServiceId:{serviceId}", 
                ex.Message, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            return StatusCode(500, new { Message = "Failed to restart service", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get comprehensive status of all services
    /// </summary>
    /// <param name="domain">Optional domain filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Service status summary</returns>
    [HttpGet("status")]
    public async Task<ActionResult<ServiceStatusSummaryDto>> GetServicesStatus(
        [FromQuery] string? domain = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _serviceManagementService.GetServicesStatusAsync(domain, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get services status");
            return StatusCode(500, new { Message = "Failed to get services status", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get service action history
    /// </summary>
    /// <param name="serviceId">ID of the service</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated service action history</returns>
    [HttpGet("{serviceId}/actions")]
    public async Task<ActionResult<PagedResult<ServiceActionDto>>> GetServiceActions(
        int serviceId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var paging = new PagingDto { Page = page, PageSize = pageSize };
            var result = await _serviceManagementService.GetServiceActionsAsync(serviceId, paging, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service actions for service {ServiceId}", serviceId);
            return StatusCode(500, new { Message = "Failed to get service actions", Error = ex.Message });
        }
    }
}