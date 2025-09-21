using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Controllers;

/// <summary>
/// Controller for managing Temenos SOD/EOD operations
/// </summary>
[Route("api/temenos/operations")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class TemenosOperationsController : ControllerBase
{
    private readonly ITemenosOperationService _temenosOperationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<TemenosOperationsController> _logger;

    public TemenosOperationsController(
        ITemenosOperationService temenosOperationService,
        IAuditService auditService,
        ILogger<TemenosOperationsController> logger)
    {
        _temenosOperationService = temenosOperationService ?? throw new ArgumentNullException(nameof(temenosOperationService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initiate Start of Day operation
    /// </summary>
    /// <param name="request">SOD operation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result with tracking information</returns>
    [HttpPost("sod")]
    public async Task<ActionResult<OperationResultDto>> StartSOD([FromBody] SODRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _logger.LogInformation("SOD operation requested by {UserName} for environment {Environment}", userName, request.Environment);

            // Audit the request
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                "SOD_START_REQUESTED", 
                $"Environment:{request.Environment}", 
                request, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            var result = await _temenosOperationService.StartSODAsync(request, userName, cancellationToken);

            // Audit the result
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                "SOD_START_INITIATED", 
                $"OperationId:{result.OperationId}", 
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

            _logger.LogError(ex, "Failed to start SOD operation for environment {Environment}", request.Environment);

            await _auditService.LogFailureAsync(
                userName, 
                userName, 
                "SOD_START_FAILED", 
                $"Environment:{request.Environment}", 
                ex.Message, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            return StatusCode(500, new { Message = "Failed to start SOD operation", Error = ex.Message });
        }
    }

    /// <summary>
    /// Initiate End of Day operation
    /// </summary>
    /// <param name="request">EOD operation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result with tracking information</returns>
    [HttpPost("eod")]
    public async Task<ActionResult<OperationResultDto>> StartEOD([FromBody] EODRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _logger.LogInformation("EOD operation requested by {UserName} for environment {Environment}", userName, request.Environment);

            // Audit the request
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                "EOD_START_REQUESTED", 
                $"Environment:{request.Environment}", 
                request, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            var result = await _temenosOperationService.StartEODAsync(request, userName, cancellationToken);

            // Audit the result
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                "EOD_START_INITIATED", 
                $"OperationId:{result.OperationId}", 
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

            _logger.LogError(ex, "Failed to start EOD operation for environment {Environment}", request.Environment);

            await _auditService.LogFailureAsync(
                userName, 
                userName, 
                "EOD_START_FAILED", 
                $"Environment:{request.Environment}", 
                ex.Message, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            return StatusCode(500, new { Message = "Failed to start EOD operation", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get status of specific operation
    /// </summary>
    /// <param name="operationId">Unique operation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed operation status</returns>
    [HttpGet("{operationId}")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<OperationStatusDto>> GetOperationStatus(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _temenosOperationService.GetOperationStatusAsync(operationId, cancellationToken);

            if (result == null)
            {
                return NotFound(new { Message = $"Operation {operationId} not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for operation {OperationId}", operationId);
            return StatusCode(500, new { Message = "Failed to get operation status", Error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel running operation
    /// </summary>
    /// <param name="operationId">Unique operation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancellation result</returns>
    [HttpPost("{operationId}/cancel")]
    public async Task<ActionResult<OperationResultDto>> CancelOperation(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _logger.LogInformation("Operation cancellation requested by {UserName} for operation {OperationId}", userName, operationId);

            // Audit the request
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                "OPERATION_CANCEL_REQUESTED", 
                $"OperationId:{operationId}", 
                null, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            var result = await _temenosOperationService.CancelOperationAsync(operationId, userName, cancellationToken);

            // Audit the result
            await _auditService.LogEventAsync(
                userName, 
                userName, 
                "OPERATION_CANCELLED", 
                $"OperationId:{operationId}", 
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

            _logger.LogError(ex, "Failed to cancel operation {OperationId}", operationId);

            await _auditService.LogFailureAsync(
                userName, 
                userName, 
                "OPERATION_CANCEL_FAILED", 
                $"OperationId:{operationId}", 
                ex.Message, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            return StatusCode(500, new { Message = "Failed to cancel operation", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get all recent operations
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of operations</returns>
    [HttpGet]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<PagedResult<OperationSummaryDto>>> GetOperations(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _temenosOperationService.GetOperationsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operations list");
            return StatusCode(500, new { Message = "Failed to get operations", Error = ex.Message });
        }
    }
}