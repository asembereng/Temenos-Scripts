using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Controllers;

/// <summary>
/// Controller for report generation and management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AllUsers")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportingService reportingService,
        ILogger<ReportsController> logger)
    {
        _reportingService = reportingService ?? throw new ArgumentNullException(nameof(reportingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get available reports
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetReports(CancellationToken cancellationToken = default)
    {
        try
        {
            // Return mock data for now - in a real implementation, this would fetch from database
            var reports = new[]
            {
                new 
                {
                    id = "report-001",
                    name = "Weekly Operations Summary",
                    description = "Weekly summary of SOD/EOD operations and system performance",
                    format = "pdf",
                    createdAt = DateTime.UtcNow.AddDays(-7).ToString("O"),
                    size = "2.3 MB",
                    status = "ready"
                },
                new 
                {
                    id = "report-002", 
                    name = "Performance Analytics",
                    description = "System performance metrics and trend analysis",
                    format = "excel",
                    createdAt = DateTime.UtcNow.AddDays(-3).ToString("O"),
                    size = "1.8 MB",
                    status = "ready"
                },
                new 
                {
                    id = "report-003",
                    name = "Compliance Report",
                    description = "Monthly compliance and audit report",
                    format = "pdf",
                    createdAt = DateTime.UtcNow.AddDays(-1).ToString("O"),
                    size = "3.1 MB", 
                    status = "generating"
                }
            };

            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reports");
            return StatusCode(500, new { message = "Failed to get reports", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate a new report
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<object>> GenerateReport([FromBody] object reportConfig, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            _logger.LogInformation("Generating report requested by {UserId}", userId);

            // In a real implementation, this would process the reportConfig and generate the actual report
            // For now, return a success response
            var result = new
            {
                reportId = Guid.NewGuid().ToString(),
                status = "queued",
                message = "Report generation started successfully",
                estimatedCompletion = DateTime.UtcNow.AddMinutes(5)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report");
            return StatusCode(500, new { message = "Failed to generate report", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate operations summary report
    /// </summary>
    [HttpPost("operations-summary")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<OperationsSummaryReportDto>> GenerateOperationsSummary([FromBody] ReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _reportingService.GenerateOperationsSummaryAsync(request, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate operations summary report");
            return StatusCode(500, new { message = "Failed to generate operations summary report", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate performance analytics report
    /// </summary>
    [HttpPost("performance-analytics")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<PerformanceAnalyticsReportDto>> GeneratePerformanceAnalytics([FromBody] ReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _reportingService.GeneratePerformanceAnalyticsAsync(request, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate performance analytics report");
            return StatusCode(500, new { message = "Failed to generate performance analytics report", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate compliance report
    /// </summary>
    [HttpPost("compliance")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<ComplianceReportDto>> GenerateComplianceReport([FromBody] ComplianceReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _reportingService.GenerateComplianceReportAsync(request, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate compliance report");
            return StatusCode(500, new { message = "Failed to generate compliance report", error = ex.Message });
        }
    }

    /// <summary>
    /// Download a report
    /// </summary>
    [HttpGet("{reportId}/download")]
    [Authorize(Policy = "AllUsers")]
    public async Task<ActionResult> DownloadReport(string reportId, [FromQuery] string format = "pdf", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading report {ReportId} in {Format} format", reportId, format);

            // In a real implementation, this would fetch the actual report data
            // For now, return a mock PDF file
            var mockPdfContent = System.Text.Encoding.UTF8.GetBytes($"Mock Report Content for {reportId}");
            
            var contentType = format.ToLower() switch
            {
                "pdf" => "application/pdf",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "csv" => "text/csv",
                _ => "application/octet-stream"
            };

            var fileName = $"report-{reportId}.{format}";
            
            return File(mockPdfContent, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download report {ReportId}", reportId);
            return StatusCode(500, new { message = "Failed to download report", error = ex.Message });
        }
    }

    /// <summary>
    /// Export report in specified format
    /// </summary>
    [HttpPost("{reportId}/export")]
    [Authorize(Policy = "AllUsers")]
    public async Task<ActionResult> ExportReport(string reportId, [FromBody] ExportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var exportFormat = Enum.Parse<ExportFormat>(request.Format, true);
            var reportData = await _reportingService.ExportReportAsync(reportId, exportFormat, cancellationToken);
            
            var contentType = exportFormat switch
            {
                ExportFormat.PDF => "application/pdf",
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.CSV => "text/csv",
                _ => "application/octet-stream"
            };

            var fileName = $"report-{reportId}.{exportFormat.ToString().ToLower()}";
            
            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export report {ReportId}", reportId);
            return StatusCode(500, new { message = "Failed to export report", error = ex.Message });
        }
    }

    /// <summary>
    /// Schedule a report
    /// </summary>
    [HttpPost("schedule")]
    [Authorize(Policy = "OperatorOrAdmin")]
    public async Task<ActionResult<ScheduleResultDto>> ScheduleReport([FromBody] ReportScheduleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var result = await _reportingService.ScheduleReportAsync(request, userId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule report");
            return StatusCode(500, new { message = "Failed to schedule report", error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for report export
/// </summary>
public class ExportRequest
{
    public string Format { get; set; } = "PDF";
}