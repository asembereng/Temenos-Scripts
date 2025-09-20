using System.Text;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// Service for generating comprehensive reports and analytics
/// </summary>
public class ReportingService : IReportingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<ReportingService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationsSummaryReportDto> GenerateOperationsSummaryAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating operations summary report for period {StartDate} to {EndDate}", 
                request.StartDate, request.EndDate);

            var reportId = Guid.NewGuid().ToString();

            // Get operations data
            var operations = await _unitOfWork.SODEODOperations.GetPagedAsync(1, 10000, cancellationToken);
            var filteredOperations = operations.Items
                .Where(op => op.StartTime >= request.StartDate && op.StartTime <= request.EndDate)
                .ToArray();

            if (!string.IsNullOrEmpty(request.Environment))
            {
                filteredOperations = filteredOperations
                    .Where(op => op.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (!string.IsNullOrEmpty(request.OperationType))
            {
                filteredOperations = filteredOperations
                    .Where(op => op.OperationType.Equals(request.OperationType, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            // Generate operation type statistics
            var operationStats = GenerateOperationTypeStatistics(filteredOperations);

            // Generate environment statistics
            var environmentStats = GenerateEnvironmentStatistics(filteredOperations);

            // Generate trend analysis
            var trends = GenerateTrendAnalysis(filteredOperations, request.StartDate, request.EndDate);

            // Generate key metrics
            var keyMetrics = GenerateKeyMetrics(filteredOperations);

            var report = new OperationsSummaryReportDto
            {
                ReportId = reportId,
                GeneratedAt = DateTime.UtcNow,
                ReportPeriodStart = request.StartDate,
                ReportPeriodEnd = request.EndDate,
                OperationStats = operationStats,
                EnvironmentStats = environmentStats,
                Trends = trends,
                KeyMetrics = keyMetrics
            };

            // Store report
            await StoreGeneratedReportAsync(report, "Operations", request, cancellationToken);

            _logger.LogInformation("Operations summary report {ReportId} generated successfully with {OperationCount} operations", 
                reportId, filteredOperations.Length);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate operations summary report");
            throw;
        }
    }

    public async Task<PerformanceAnalyticsReportDto> GeneratePerformanceAnalyticsAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating performance analytics report for period {StartDate} to {EndDate}", 
                request.StartDate, request.EndDate);

            var reportId = Guid.NewGuid().ToString();

            // Get operations data
            var operations = await _unitOfWork.SODEODOperations.GetPagedAsync(1, 10000, cancellationToken);
            var filteredOperations = operations.Items
                .Where(op => op.StartTime >= request.StartDate && op.StartTime <= request.EndDate)
                .ToArray();

            if (!string.IsNullOrEmpty(request.Environment))
            {
                filteredOperations = filteredOperations
                    .Where(op => op.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            // Calculate overall performance
            var overallPerformance = CalculateOverallPerformance(filteredOperations);

            // Generate service performance analysis
            var serviceAnalysis = await GenerateServicePerformanceAnalysisAsync(filteredOperations, cancellationToken);

            // Generate resource utilization analysis
            var resourceAnalysis = GenerateResourceUtilizationAnalysis(filteredOperations);

            // Identify bottlenecks
            var bottlenecks = IdentifyBottlenecks(filteredOperations, serviceAnalysis);

            // Generate recommendations
            var recommendations = GeneratePerformanceRecommendations(bottlenecks, overallPerformance);

            var report = new PerformanceAnalyticsReportDto
            {
                ReportId = reportId,
                GeneratedAt = DateTime.UtcNow,
                ReportPeriodStart = request.StartDate,
                ReportPeriodEnd = request.EndDate,
                OverallPerformance = overallPerformance,
                ServiceAnalysis = serviceAnalysis,
                ResourceAnalysis = resourceAnalysis,
                Bottlenecks = bottlenecks,
                Recommendations = recommendations
            };

            // Store report
            await StoreGeneratedReportAsync(report, "Performance", request, cancellationToken);

            _logger.LogInformation("Performance analytics report {ReportId} generated successfully", reportId);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate performance analytics report");
            throw;
        }
    }

    public async Task<ComplianceReportDto> GenerateComplianceReportAsync(ComplianceReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating compliance report for period {StartDate} to {EndDate}", 
                request.StartDate, request.EndDate);

            var reportId = Guid.NewGuid().ToString();

            // Get audit events for compliance analysis
            var auditEvents = await _unitOfWork.AuditEvents.GetAllAsync(cancellationToken);
            var filteredAuditEvents = auditEvents
                .Where(ae => ae.EventTime >= request.StartDate && ae.EventTime <= request.EndDate)
                .ToArray();

            // Calculate overall compliance status
            var overallStatus = CalculateOverallComplianceStatus(filteredAuditEvents, request);

            // Generate framework-specific compliance status
            var frameworkStatus = GenerateFrameworkComplianceStatus(filteredAuditEvents, request);

            // Generate audit trail summary
            var auditSummary = GenerateAuditTrailSummary(filteredAuditEvents);

            // Identify compliance exceptions
            var exceptions = IdentifyComplianceExceptions(filteredAuditEvents, request);

            var report = new ComplianceReportDto
            {
                ReportId = reportId,
                GeneratedAt = DateTime.UtcNow,
                ReportPeriodStart = request.StartDate,
                ReportPeriodEnd = request.EndDate,
                OverallStatus = overallStatus,
                FrameworkStatus = frameworkStatus,
                AuditSummary = auditSummary,
                Exceptions = exceptions
            };

            // Store report
            await StoreGeneratedReportAsync(report, "Compliance", request, cancellationToken);

            _logger.LogInformation("Compliance report {ReportId} generated successfully with {ComplianceScore}% compliance score", 
                reportId, overallStatus.ComplianceScore);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate compliance report");
            throw;
        }
    }

    public async Task<byte[]> ExportReportAsync(string reportId, ExportFormat format, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting report {ReportId} in {Format} format", reportId, format);

            // Get stored report
            var reports = await _unitOfWork.GeneratedReports.GetAllAsync(cancellationToken);
            var report = reports.FirstOrDefault(r => r.ReportId == reportId);

            if (report == null)
            {
                throw new ArgumentException($"Report {reportId} not found");
            }

            var reportData = report.ReportData;
            if (string.IsNullOrEmpty(reportData))
            {
                throw new InvalidOperationException($"Report {reportId} has no data");
            }

            return format switch
            {
                ExportFormat.PDF => await ExportToPdfAsync(reportData, report.ReportType, cancellationToken),
                ExportFormat.Excel => await ExportToExcelAsync(reportData, report.ReportType, cancellationToken),
                ExportFormat.CSV => await ExportToCsvAsync(reportData, report.ReportType, cancellationToken),
                ExportFormat.JSON => Encoding.UTF8.GetBytes(reportData),
                ExportFormat.XML => await ExportToXmlAsync(reportData, report.ReportType, cancellationToken),
                _ => throw new ArgumentException($"Unsupported export format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export report {ReportId} in {Format} format", reportId, format);
            throw;
        }
    }

    public async Task<ScheduleResultDto> ScheduleReportAsync(ReportScheduleRequest request, string scheduledBy, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scheduling {ReportType} report by {User}", request.ReportType, scheduledBy);

            // Create scheduled operation for report generation
            var scheduledOperation = new ScheduledOperation
            {
                OperationType = $"REPORT_{request.ReportType.ToUpper()}",
                Environment = request.ReportParameters.Environment ?? "All",
                CronExpression = request.CronExpression,
                TimeZone = "UTC",
                IsEnabled = request.IsEnabled,
                Comments = $"Scheduled {request.ReportType} report generation",
                Configuration = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["ReportType"] = request.ReportType,
                    ["ReportParameters"] = request.ReportParameters,
                    ["ExportFormat"] = request.ExportFormat.ToString(),
                    ["Recipients"] = request.Recipients
                }),
                ScheduledBy = scheduledBy,
                NextExecutionTime = GetNextExecutionTime(request.CronExpression),
                Status = request.IsEnabled ? "Active" : "Paused"
            };

            await _unitOfWork.ScheduledOperations.AddAsync(scheduledOperation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit the scheduling
            await _auditService.LogEventAsync(scheduledBy, scheduledBy, "REPORT_SCHEDULED", 
                $"ScheduleId:{scheduledOperation.Id},ReportType:{request.ReportType}", request, cancellationToken: cancellationToken);

            _logger.LogInformation("Report scheduled successfully with ID {ScheduleId}", scheduledOperation.Id);

            return new ScheduleResultDto
            {
                ScheduleId = scheduledOperation.Id,
                OperationType = scheduledOperation.OperationType,
                Environment = scheduledOperation.Environment,
                CronExpression = request.CronExpression,
                NextExecutionTime = scheduledOperation.NextExecutionTime ?? DateTime.MinValue,
                Status = scheduledOperation.Status,
                Message = $"{request.ReportType} report scheduled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule {ReportType} report", request.ReportType);
            throw;
        }
    }

    // Private helper methods

    private OperationTypeStatistics[] GenerateOperationTypeStatistics(SODEODOperation[] operations)
    {
        return operations
            .GroupBy(op => op.OperationType)
            .Select(group => new OperationTypeStatistics
            {
                OperationType = group.Key,
                TotalOperations = group.Count(),
                SuccessfulOperations = group.Count(op => op.Status == "Completed"),
                FailedOperations = group.Count(op => op.Status == "Failed"),
                SuccessRate = group.Count() > 0 ? (double)group.Count(op => op.Status == "Completed") / group.Count() * 100 : 0,
                AverageDurationMinutes = group.Where(op => op.EndTime.HasValue).Any() ? 
                    group.Where(op => op.EndTime.HasValue).Average(op => (op.EndTime!.Value - op.StartTime).TotalMinutes) : 0,
                MinDurationMinutes = group.Where(op => op.EndTime.HasValue).Any() ? 
                    group.Where(op => op.EndTime.HasValue).Min(op => (op.EndTime!.Value - op.StartTime).TotalMinutes) : 0,
                MaxDurationMinutes = group.Where(op => op.EndTime.HasValue).Any() ? 
                    group.Where(op => op.EndTime.HasValue).Max(op => (op.EndTime!.Value - op.StartTime).TotalMinutes) : 0
            }).ToArray();
    }

    private EnvironmentStatistics[] GenerateEnvironmentStatistics(SODEODOperation[] operations)
    {
        return operations
            .GroupBy(op => op.Environment)
            .Select(group => new EnvironmentStatistics
            {
                Environment = group.Key,
                TotalOperations = group.Count(),
                AvailabilityPercentage = group.Count() > 0 ? (double)group.Count(op => op.Status == "Completed") / group.Count() * 100 : 0,
                PerformanceScore = CalculateEnvironmentPerformanceScore(group.ToArray()),
                HealthStatus = GetEnvironmentHealthStatus(group.ToArray())
            }).ToArray();
    }

    private TrendAnalysis[] GenerateTrendAnalysis(SODEODOperation[] operations, DateTime startDate, DateTime endDate)
    {
        var trends = new List<TrendAnalysis>();

        // Duration trend
        var periodDays = (endDate - startDate).Days;
        if (periodDays >= 7)
        {
            var firstHalf = operations.Where(op => op.StartTime < startDate.AddDays(periodDays / 2.0)).ToArray();
            var secondHalf = operations.Where(op => op.StartTime >= startDate.AddDays(periodDays / 2.0)).ToArray();

            var firstHalfAvgDuration = firstHalf.Where(op => op.EndTime.HasValue).Any() ?
                firstHalf.Where(op => op.EndTime.HasValue).Average(op => (op.EndTime!.Value - op.StartTime).TotalMinutes) : 0;
            var secondHalfAvgDuration = secondHalf.Where(op => op.EndTime.HasValue).Any() ?
                secondHalf.Where(op => op.EndTime.HasValue).Average(op => (op.EndTime!.Value - op.StartTime).TotalMinutes) : 0;

            var durationChange = firstHalfAvgDuration > 0 ? (secondHalfAvgDuration - firstHalfAvgDuration) / firstHalfAvgDuration * 100 : 0;

            trends.Add(new TrendAnalysis
            {
                MetricName = "Average Duration",
                Trend = durationChange < -5 ? "Improving" : durationChange > 5 ? "Degrading" : "Stable",
                ChangePercentage = Math.Abs(durationChange),
                Period = $"{periodDays} days"
            });
        }

        return trends.ToArray();
    }

    private KeyMetric[] GenerateKeyMetrics(SODEODOperation[] operations)
    {
        var metrics = new List<KeyMetric>();

        // Overall success rate
        var successRate = operations.Length > 0 ? (double)operations.Count(op => op.Status == "Completed") / operations.Length * 100 : 0;
        metrics.Add(new KeyMetric
        {
            Name = "Overall Success Rate",
            Value = successRate,
            Unit = "%",
            Status = successRate >= 95 ? "Good" : successRate >= 90 ? "Warning" : "Critical"
        });

        // Average duration
        var avgDuration = operations.Where(op => op.EndTime.HasValue).Any() ?
            operations.Where(op => op.EndTime.HasValue).Average(op => (op.EndTime!.Value - op.StartTime).TotalMinutes) : 0;
        metrics.Add(new KeyMetric
        {
            Name = "Average Duration",
            Value = avgDuration,
            Unit = "minutes",
            Status = avgDuration <= 30 ? "Good" : avgDuration <= 60 ? "Warning" : "Critical"
        });

        return metrics.ToArray();
    }

    private PerformanceMetricsSummary CalculateOverallPerformance(SODEODOperation[] operations)
    {
        if (!operations.Any())
        {
            return new PerformanceMetricsSummary();
        }

        var completedOperations = operations.Where(op => op.EndTime.HasValue).ToArray();
        var durations = completedOperations.Select(op => (op.EndTime!.Value - op.StartTime).TotalMinutes).ToArray();

        return new PerformanceMetricsSummary
        {
            AverageDurationMinutes = durations.Any() ? durations.Average() : 0,
            SuccessRate = operations.Length > 0 ? (double)operations.Count(op => op.Status == "Completed") / operations.Length * 100 : 0,
            ResourceUtilization = 75, // Simulated
            ConcurrentOperations = operations.Count(op => !op.EndTime.HasValue)
        };
    }

    private async Task<ServicePerformanceAnalysis[]> GenerateServicePerformanceAnalysisAsync(SODEODOperation[] operations, CancellationToken cancellationToken)
    {
        // Get service configurations to analyze
        var services = await _unitOfWork.ServiceConfigs.GetAllAsync(cancellationToken);
        
        return services.Take(5).Select(service => new ServicePerformanceAnalysis
        {
            ServiceName = service.Name,
            AverageStartupTime = 30 + new Random().NextDouble() * 60, // Simulated
            ReliabilityScore = 85 + new Random().NextDouble() * 15, // Simulated
            ResourceConsumption = 50 + new Random().NextDouble() * 40, // Simulated
            Dependencies = new[] { "Database", "Network" }, // Simulated
            PerformanceRating = "Good"
        }).ToArray();
    }

    private ResourceUtilizationAnalysis GenerateResourceUtilizationAnalysis(SODEODOperation[] operations)
    {
        // Simulated resource analysis
        return new ResourceUtilizationAnalysis
        {
            AverageCpuUtilization = 65,
            AverageMemoryUtilization = 72,
            AverageDiskUtilization = 45,
            AverageNetworkUtilization = 38,
            Trends = new[]
            {
                new ResourceTrend
                {
                    ResourceType = "CPU",
                    Trend = "Stable",
                    Values = new[] { 65.0, 67.0, 64.0, 66.0, 65.0 },
                    Timestamps = Enumerable.Range(0, 5).Select(i => DateTime.UtcNow.AddHours(-i)).ToArray()
                }
            }
        };
    }

    private BottleneckAnalysis[] IdentifyBottlenecks(SODEODOperation[] operations, ServicePerformanceAnalysis[] serviceAnalysis)
    {
        var bottlenecks = new List<BottleneckAnalysis>();

        // Identify slow services
        var slowServices = serviceAnalysis.Where(s => s.AverageStartupTime > 60).ToArray();
        foreach (var service in slowServices)
        {
            bottlenecks.Add(new BottleneckAnalysis
            {
                ComponentName = service.ServiceName,
                BottleneckType = "Performance",
                Severity = service.AverageStartupTime > 120 ? 8.0 : 6.0,
                Impact = $"Delays operation start by {service.AverageStartupTime:F0} seconds",
                AffectedOperations = new[] { "SOD", "EOD" },
                Recommendations = new[] { "Optimize startup sequence", "Review resource allocation" }
            });
        }

        return bottlenecks.ToArray();
    }

    private TemenosAlertManager.Core.Models.OptimizationRecommendation[] GeneratePerformanceRecommendations(BottleneckAnalysis[] bottlenecks, PerformanceMetricsSummary performance)
    {
        var recommendations = new List<TemenosAlertManager.Core.Models.OptimizationRecommendation>();

        if (performance.SuccessRate < 95)
        {
            recommendations.Add(new TemenosAlertManager.Core.Models.OptimizationRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                Category = "Reliability",
                Title = "Improve Success Rate",
                Description = $"Current success rate of {performance.SuccessRate:F1}% is below target. Implement enhanced error handling.",
                Priority = "High",
                Impact = "Increase success rate by 5-10%",
                ImplementationEffort = "Medium",
                IsAutoApplicable = false
            });
        }

        return recommendations.ToArray();
    }

    private ComplianceStatus CalculateOverallComplianceStatus(AuditEvent[] auditEvents, ComplianceReportRequest request)
    {
        // Simulate compliance calculation
        var totalChecks = 100;
        var passedChecks = 92;
        var failedChecks = totalChecks - passedChecks;

        return new ComplianceStatus
        {
            Status = passedChecks >= 95 ? "Compliant" : passedChecks >= 80 ? "Partial" : "Non-Compliant",
            ComplianceScore = (double)passedChecks / totalChecks * 100,
            TotalChecks = totalChecks,
            PassedChecks = passedChecks,
            FailedChecks = failedChecks
        };
    }

    private ComplianceFrameworkStatus[] GenerateFrameworkComplianceStatus(AuditEvent[] auditEvents, ComplianceReportRequest request)
    {
        return request.ComplianceFrameworks.Select(framework => new ComplianceFrameworkStatus
        {
            Framework = framework,
            Status = new ComplianceStatus
            {
                Status = "Compliant",
                ComplianceScore = 95,
                TotalChecks = 25,
                PassedChecks = 24,
                FailedChecks = 1
            },
            Controls = new[]
            {
                new ComplianceControl
                {
                    ControlId = $"{framework}_001",
                    ControlName = "Access Control",
                    Status = "Passed",
                    Description = "User access controls are properly implemented",
                    Evidence = new[] { "Audit logs", "User permissions report" }
                }
            }
        }).ToArray();
    }

    private AuditTrailSummary GenerateAuditTrailSummary(AuditEvent[] auditEvents)
    {
        return new AuditTrailSummary
        {
            TotalAuditEvents = auditEvents.Length,
            UserActions = auditEvents.Count(ae => ae.Action.Contains("USER")),
            SystemActions = auditEvents.Count(ae => ae.Action.Contains("SYSTEM")),
            SecurityEvents = auditEvents.Count(ae => ae.Action.Contains("AUTH") || ae.Action.Contains("LOGIN")),
            DataAccessEvents = auditEvents.Count(ae => ae.Action.Contains("DATA") || ae.Action.Contains("READ"))
        };
    }

    private ComplianceException[] IdentifyComplianceExceptions(AuditEvent[] auditEvents, ComplianceReportRequest request)
    {
        // Simulate exception identification
        return new[]
        {
            new ComplianceException
            {
                ExceptionId = Guid.NewGuid().ToString(),
                Type = "Access Violation",
                Description = "Unauthorized access attempt detected",
                Severity = "Medium",
                DetectedAt = DateTime.UtcNow.AddDays(-1),
                Status = "Investigating",
                AffectedSystems = new[] { "T24 Core" }
            }
        };
    }

    private double CalculateEnvironmentPerformanceScore(SODEODOperation[] operations)
    {
        var successRate = operations.Length > 0 ? (double)operations.Count(op => op.Status == "Completed") / operations.Length : 1.0;
        var avgDuration = operations.Where(op => op.EndTime.HasValue).Any() ?
            operations.Where(op => op.EndTime.HasValue).Average(op => (op.EndTime!.Value - op.StartTime).TotalMinutes) : 0;
        
        var durationScore = Math.Max(0, 100 - avgDuration); // Lower duration = higher score
        return (successRate * 100 + durationScore) / 2;
    }

    private string GetEnvironmentHealthStatus(SODEODOperation[] operations)
    {
        var successRate = operations.Length > 0 ? (double)operations.Count(op => op.Status == "Completed") / operations.Length * 100 : 100;
        return successRate >= 95 ? "Healthy" : successRate >= 85 ? "Warning" : "Critical";
    }

    private async Task StoreGeneratedReportAsync(object reportData, string reportType, object request, CancellationToken cancellationToken)
    {
        var reportJson = System.Text.Json.JsonSerializer.Serialize(reportData);
        var parametersJson = System.Text.Json.JsonSerializer.Serialize(request);

        var reportEntity = new GeneratedReport
        {
            ReportId = GetReportId(reportData),
            ReportType = reportType,
            ReportPeriodStart = GetReportStartDate(request),
            ReportPeriodEnd = GetReportEndDate(request),
            Environment = GetReportEnvironment(request),
            Parameters = parametersJson,
            ReportData = reportJson,
            Status = "Generated",
            GeneratedBy = "SYSTEM",
            ExpiresAt = DateTime.UtcNow.AddDays(90) // Keep for 90 days
        };

        await _unitOfWork.GeneratedReports.AddAsync(reportEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<byte[]> ExportToPdfAsync(string reportData, string reportType, CancellationToken cancellationToken)
    {
        // Simulate PDF generation
        await Task.Delay(100, cancellationToken);
        return Encoding.UTF8.GetBytes($"PDF Report: {reportType}\n{reportData}");
    }

    private async Task<byte[]> ExportToExcelAsync(string reportData, string reportType, CancellationToken cancellationToken)
    {
        // Simulate Excel generation
        await Task.Delay(100, cancellationToken);
        return Encoding.UTF8.GetBytes($"Excel Report: {reportType}\n{reportData}");
    }

    private async Task<byte[]> ExportToCsvAsync(string reportData, string reportType, CancellationToken cancellationToken)
    {
        // Simulate CSV generation
        await Task.Delay(100, cancellationToken);
        return Encoding.UTF8.GetBytes($"CSV Report: {reportType}\n{reportData}");
    }

    private async Task<byte[]> ExportToXmlAsync(string reportData, string reportType, CancellationToken cancellationToken)
    {
        // Simulate XML generation
        await Task.Delay(100, cancellationToken);
        return Encoding.UTF8.GetBytes($"<report type=\"{reportType}\">{reportData}</report>");
    }

    private DateTime GetNextExecutionTime(string cronExpression)
    {
        // Simple implementation - would use proper cron parser in real scenario
        return DateTime.UtcNow.AddDays(1);
    }

    private string GetReportId(object reportData)
    {
        var property = reportData.GetType().GetProperty("ReportId");
        return property?.GetValue(reportData)?.ToString() ?? Guid.NewGuid().ToString();
    }

    private DateTime GetReportStartDate(object request)
    {
        var property = request.GetType().GetProperty("StartDate");
        return property?.GetValue(request) as DateTime? ?? DateTime.UtcNow.AddDays(-30);
    }

    private DateTime GetReportEndDate(object request)
    {
        var property = request.GetType().GetProperty("EndDate");
        return property?.GetValue(request) as DateTime? ?? DateTime.UtcNow;
    }

    private string? GetReportEnvironment(object request)
    {
        var property = request.GetType().GetProperty("Environment");
        return property?.GetValue(request)?.ToString();
    }
}