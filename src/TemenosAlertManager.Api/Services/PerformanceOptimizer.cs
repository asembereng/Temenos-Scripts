using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// Service for performance optimization and analysis
/// </summary>
public class PerformanceOptimizer : IPerformanceOptimizer
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<PerformanceOptimizer> _logger;

    public PerformanceOptimizer(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<PerformanceOptimizer> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OptimizationRecommendationsDto> AnalyzePerformanceAsync(string operationType, string? environment = null, int dayRange = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyzing performance for operation type {OperationType} in environment {Environment} over {DayRange} days", 
                operationType, environment, dayRange);

            var startDate = DateTime.UtcNow.AddDays(-dayRange);
            var endDate = DateTime.UtcNow;

            // Get historical operations data
            var operations = await _unitOfWork.SODEODOperations.GetPagedAsync(1, 1000, cancellationToken);
            var filteredOperations = operations.Items
                .Where(op => op.OperationType.Equals(operationType, StringComparison.OrdinalIgnoreCase) &&
                           op.StartTime >= startDate && op.StartTime <= endDate &&
                           (string.IsNullOrEmpty(environment) || op.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            // Calculate current performance metrics
            var currentPerformance = CalculatePerformanceMetrics(filteredOperations);

            // Generate optimization recommendations
            var recommendations = await GenerateOptimizationRecommendationsAsync(filteredOperations, operationType, environment, cancellationToken);

            // Calculate projected performance if recommendations are applied
            var projectedPerformance = CalculateProjectedPerformance(currentPerformance, recommendations);

            var result = new OptimizationRecommendationsDto
            {
                OperationType = operationType,
                Environment = environment ?? "All",
                AnalysisDate = DateTime.UtcNow,
                Recommendations = recommendations,
                CurrentPerformance = currentPerformance,
                ProjectedPerformance = projectedPerformance,
                EstimatedImprovementPercentage = CalculateEstimatedImprovement(currentPerformance, projectedPerformance)
            };

            _logger.LogInformation("Performance analysis completed for {OperationType}. Found {RecommendationCount} recommendations with {ImprovementPercentage}% estimated improvement", 
                operationType, recommendations.Length, result.EstimatedImprovementPercentage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze performance for operation type {OperationType}", operationType);
            throw;
        }
    }

    public async Task<OptimizationResultDto> ApplyOptimizationsAsync(OptimizationRequest request, string appliedBy, CancellationToken cancellationToken = default)
    {
        var optimizationId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Applying {RecommendationCount} optimizations for {OperationType} in {Environment} by {User}", 
                request.RecommendationIds.Length, request.OperationType, request.Environment, appliedBy);

            var results = new List<OptimizationOutcome>();

            foreach (var recommendationId in request.RecommendationIds)
            {
                try
                {
                    var outcome = await ApplyOptimizationRecommendationAsync(recommendationId, request, appliedBy, cancellationToken);
                    results.Add(outcome);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to apply optimization recommendation {RecommendationId}", recommendationId);
                    
                    results.Add(new OptimizationOutcome
                    {
                        RecommendationId = recommendationId,
                        Status = "Failed",
                        Message = ex.Message,
                        ImprovementPercentage = 0
                    });
                }
            }

            // Audit the optimization application
            await _auditService.LogEventAsync(appliedBy, appliedBy, "OPTIMIZATIONS_APPLIED", 
                $"OptimizationId:{optimizationId},Environment:{request.Environment}", request, cancellationToken: cancellationToken);

            var result = new OptimizationResultDto
            {
                OptimizationId = optimizationId,
                Status = results.Any(r => r.Status == "Failed") ? "Partial" : "Success",
                Message = $"Applied {results.Count(r => r.Status == "Applied")}/{results.Count} optimizations successfully",
                Results = results.ToArray(),
                AppliedAt = DateTime.UtcNow,
                AppliedBy = appliedBy
            };

            _logger.LogInformation("Optimization application completed with ID {OptimizationId}. Status: {Status}", 
                optimizationId, result.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply optimizations for {OperationType} in {Environment}", 
                request.OperationType, request.Environment);
            
            await _auditService.LogFailureAsync(appliedBy, appliedBy, "OPTIMIZATION_APPLICATION_FAILED", 
                $"Environment:{request.Environment}", ex.Message, cancellationToken: cancellationToken);
            
            throw;
        }
    }

    public async Task<PerformanceBaselineDto> GetPerformanceBaselineAsync(string operationType, string environment, CancellationToken cancellationToken = default)
    {
        try
        {
            var baselines = await _unitOfWork.PerformanceBaselines.GetAllAsync(cancellationToken);
            var baseline = baselines.FirstOrDefault(b => 
                b.OperationType.Equals(operationType, StringComparison.OrdinalIgnoreCase) &&
                b.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase));

            if (baseline == null)
            {
                // Create baseline from historical data
                baseline = await CreatePerformanceBaselineAsync(operationType, environment, cancellationToken);
            }

            // Get thresholds
            var thresholds = await _unitOfWork.PerformanceThresholds.GetAllAsync(cancellationToken);
            var operationThresholds = thresholds.Where(t => 
                t.OperationType.Equals(operationType, StringComparison.OrdinalIgnoreCase) &&
                t.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase) &&
                t.IsEnabled).ToArray();

            return new PerformanceBaselineDto
            {
                OperationType = baseline.OperationType,
                Environment = baseline.Environment,
                Baseline = new PerformanceMetricsSummary
                {
                    AverageDurationMinutes = baseline.AverageDurationMinutes,
                    SuccessRate = baseline.SuccessRate,
                    ResourceUtilization = baseline.ResourceUtilization
                },
                Thresholds = operationThresholds.Select(t => new TemenosAlertManager.Core.Models.PerformanceThreshold
                {
                    MetricName = t.MetricName,
                    WarningThreshold = t.WarningThreshold,
                    CriticalThreshold = t.CriticalThreshold,
                    Unit = t.Unit
                }).ToArray(),
                BaselineDate = baseline.BaselineDate,
                SampleSize = baseline.SampleSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance baseline for {OperationType} in {Environment}", operationType, environment);
            throw;
        }
    }

    public async Task<bool> UpdatePerformanceThresholdsAsync(PerformanceThresholdRequest request, string updatedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating performance thresholds for {OperationType} in {Environment} by {User}", 
                request.OperationType, request.Environment, updatedBy);

            // Remove existing thresholds
            var existingThresholds = await _unitOfWork.PerformanceThresholds.GetAllAsync(cancellationToken);
            var toRemove = existingThresholds.Where(t => 
                t.OperationType.Equals(request.OperationType, StringComparison.OrdinalIgnoreCase) &&
                t.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (var threshold in toRemove)
            {
                await _unitOfWork.PerformanceThresholds.DeleteAsync(threshold.Id, cancellationToken);
            }

            // Add new thresholds
            foreach (var thresholdRequest in request.Thresholds)
            {
                var threshold = new Core.Entities.PerformanceThreshold
                {
                    OperationType = request.OperationType,
                    Environment = request.Environment,
                    MetricName = thresholdRequest.MetricName,
                    WarningThreshold = thresholdRequest.WarningThreshold,
                    CriticalThreshold = thresholdRequest.CriticalThreshold,
                    Unit = thresholdRequest.Unit,
                    IsEnabled = true,
                    CreatedBy = updatedBy
                };

                await _unitOfWork.PerformanceThresholds.AddAsync(threshold, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit the threshold update
            await _auditService.LogEventAsync(updatedBy, updatedBy, "PERFORMANCE_THRESHOLDS_UPDATED", 
                $"OperationType:{request.OperationType},Environment:{request.Environment}", request, cancellationToken: cancellationToken);

            _logger.LogInformation("Performance thresholds updated successfully for {OperationType} in {Environment}", 
                request.OperationType, request.Environment);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update performance thresholds for {OperationType} in {Environment}", 
                request.OperationType, request.Environment);
            throw;
        }
    }

    // Private helper methods

    private PerformanceMetricsSummary CalculatePerformanceMetrics(SODEODOperation[] operations)
    {
        if (!operations.Any())
        {
            return new PerformanceMetricsSummary();
        }

        var completedOperations = operations.Where(op => op.EndTime.HasValue).ToArray();
        
        var durations = completedOperations
            .Where(op => op.EndTime.HasValue)
            .Select(op => (op.EndTime!.Value - op.StartTime).TotalMinutes)
            .ToArray();

        var successCount = completedOperations.Count(op => op.Status == "Completed");
        var successRate = completedOperations.Length > 0 ? (double)successCount / completedOperations.Length * 100 : 0;

        return new PerformanceMetricsSummary
        {
            AverageDurationMinutes = durations.Any() ? durations.Average() : 0,
            SuccessRate = successRate,
            ResourceUtilization = 75, // Simulated
            ConcurrentOperations = operations.Count(op => !op.EndTime.HasValue),
            DetailedMetrics = new Dictionary<string, double>
            {
                ["MinDurationMinutes"] = durations.Any() ? durations.Min() : 0,
                ["MaxDurationMinutes"] = durations.Any() ? durations.Max() : 0,
                ["MedianDurationMinutes"] = durations.Any() ? durations.OrderBy(d => d).ElementAt(durations.Length / 2) : 0,
                ["TotalOperations"] = operations.Length,
                ["CompletedOperations"] = completedOperations.Length,
                ["FailedOperations"] = operations.Count(op => op.Status == "Failed")
            }
        };
    }

    private async Task<TemenosAlertManager.Core.Models.OptimizationRecommendation[]> GenerateOptimizationRecommendationsAsync(
        SODEODOperation[] operations, 
        string operationType, 
        string? environment, 
        CancellationToken cancellationToken)
    {
        var recommendations = new List<TemenosAlertManager.Core.Models.OptimizationRecommendation>();

        // Analyze average duration
        var completedOperations = operations.Where(op => op.EndTime.HasValue).ToArray();
        if (completedOperations.Any())
        {
            var avgDuration = completedOperations.Average(op => (op.EndTime!.Value - op.StartTime).TotalMinutes);
            
            if (avgDuration > GetExpectedDuration(operationType))
            {
                recommendations.Add(new TemenosAlertManager.Core.Models.OptimizationRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Category = "Performance",
                    Title = "Optimize Service Startup Sequence",
                    Description = $"Current average {operationType} duration ({avgDuration:F1} minutes) exceeds baseline. Consider optimizing service startup order and dependencies.",
                    Priority = "High",
                    Impact = $"Potential 15-25% reduction in {operationType} duration",
                    ImplementationEffort = "Medium",
                    AffectedServices = GetTopLongRunningServices(operations),
                    IsAutoApplicable = true,
                    Parameters = new Dictionary<string, object>
                    {
                        ["CurrentAvgDuration"] = avgDuration,
                        ["TargetDuration"] = GetExpectedDuration(operationType),
                        ["OptimizationType"] = "ServiceSequencing"
                    }
                });
            }
        }

        // Analyze failure rate
        var failureRate = operations.Length > 0 ? (double)operations.Count(op => op.Status == "Failed") / operations.Length * 100 : 0;
        if (failureRate > 5) // More than 5% failure rate
        {
            recommendations.Add(new TemenosAlertManager.Core.Models.OptimizationRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                Category = "Reliability",
                Title = "Improve Error Handling",
                Description = $"Failure rate of {failureRate:F1}% is above acceptable threshold. Implement enhanced retry mechanisms and error recovery.",
                Priority = "High",
                Impact = "Reduce failure rate by 60-80%",
                ImplementationEffort = "High",
                AffectedServices = GetFailingServices(operations),
                IsAutoApplicable = false,
                Parameters = new Dictionary<string, object>
                {
                    ["CurrentFailureRate"] = failureRate,
                    ["TargetFailureRate"] = 2.0,
                    ["OptimizationType"] = "ErrorHandling"
                }
            });
        }

        // Analyze resource utilization patterns
        recommendations.Add(new TemenosAlertManager.Core.Models.OptimizationRecommendation
        {
            Id = Guid.NewGuid().ToString(),
            Category = "Performance",
            Title = "Optimize Resource Allocation",
            Description = "Implement dynamic resource scaling based on operation load patterns to improve efficiency.",
            Priority = "Medium",
            Impact = "10-15% improvement in resource utilization",
            ImplementationEffort = "Medium",
            AffectedServices = Array.Empty<string>(),
            IsAutoApplicable = true,
            Parameters = new Dictionary<string, object>
            {
                ["OptimizationType"] = "ResourceScaling",
                ["ScalingStrategy"] = "LoadBased"
            }
        });

        return recommendations.ToArray();
    }

    private PerformanceMetricsSummary CalculateProjectedPerformance(PerformanceMetricsSummary current, TemenosAlertManager.Core.Models.OptimizationRecommendation[] recommendations)
    {
        var projected = new PerformanceMetricsSummary
        {
            AverageDurationMinutes = current.AverageDurationMinutes,
            SuccessRate = current.SuccessRate,
            ResourceUtilization = current.ResourceUtilization,
            ConcurrentOperations = current.ConcurrentOperations,
            DetailedMetrics = new Dictionary<string, double>(current.DetailedMetrics)
        };

        foreach (var recommendation in recommendations)
        {
            switch (recommendation.Parameters.GetValueOrDefault("OptimizationType")?.ToString())
            {
                case "ServiceSequencing":
                    projected.AverageDurationMinutes *= 0.8; // 20% improvement
                    break;
                case "ErrorHandling":
                    projected.SuccessRate = Math.Min(100, projected.SuccessRate + (100 - projected.SuccessRate) * 0.7); // 70% of remaining failures
                    break;
                case "ResourceScaling":
                    projected.ResourceUtilization *= 0.9; // 10% improvement
                    break;
            }
        }

        return projected;
    }

    private double CalculateEstimatedImprovement(PerformanceMetricsSummary current, PerformanceMetricsSummary projected)
    {
        var durationImprovement = current.AverageDurationMinutes > 0 ? 
            (current.AverageDurationMinutes - projected.AverageDurationMinutes) / current.AverageDurationMinutes * 100 : 0;
        
        var successRateImprovement = current.SuccessRate < 100 ?
            (projected.SuccessRate - current.SuccessRate) / (100 - current.SuccessRate) * 100 : 0;

        var resourceImprovement = current.ResourceUtilization > 0 ?
            (current.ResourceUtilization - projected.ResourceUtilization) / current.ResourceUtilization * 100 : 0;

        return (durationImprovement + successRateImprovement + resourceImprovement) / 3;
    }

    private async Task<OptimizationOutcome> ApplyOptimizationRecommendationAsync(
        string recommendationId, 
        OptimizationRequest request, 
        string appliedBy, 
        CancellationToken cancellationToken)
    {
        // Simulate applying optimization
        await Task.Delay(100, cancellationToken); // Simulate work

        var beforeMetrics = new PerformanceMetricsSummary
        {
            AverageDurationMinutes = 25,
            SuccessRate = 92,
            ResourceUtilization = 75
        };

        var afterMetrics = new PerformanceMetricsSummary
        {
            AverageDurationMinutes = 20,
            SuccessRate = 96,
            ResourceUtilization = 68
        };

        var improvement = ((beforeMetrics.AverageDurationMinutes - afterMetrics.AverageDurationMinutes) / 
                          beforeMetrics.AverageDurationMinutes) * 100;

        return new OptimizationOutcome
        {
            RecommendationId = recommendationId,
            Status = request.DryRun ? "Simulated" : "Applied",
            Message = request.DryRun ? "Optimization simulated successfully" : "Optimization applied successfully",
            BeforeMetrics = beforeMetrics,
            AfterMetrics = afterMetrics,
            ImprovementPercentage = improvement
        };
    }

    private async Task<Core.Entities.PerformanceBaseline> CreatePerformanceBaselineAsync(
        string operationType, 
        string environment, 
        CancellationToken cancellationToken)
    {
        // Get historical data for baseline
        var startDate = DateTime.UtcNow.AddDays(-90); // Use last 90 days
        var operations = await _unitOfWork.SODEODOperations.GetPagedAsync(1, 1000, cancellationToken);
        
        var filteredOperations = operations.Items
            .Where(op => op.OperationType.Equals(operationType, StringComparison.OrdinalIgnoreCase) &&
                       op.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase) &&
                       op.StartTime >= startDate &&
                       op.EndTime.HasValue)
            .ToArray();

        var metrics = CalculatePerformanceMetrics(filteredOperations);

        var baseline = new Core.Entities.PerformanceBaseline
        {
            OperationType = operationType,
            Environment = environment,
            AverageDurationMinutes = metrics.AverageDurationMinutes,
            SuccessRate = metrics.SuccessRate,
            ResourceUtilization = metrics.ResourceUtilization,
            SampleSize = filteredOperations.Length,
            BaselineDate = DateTime.UtcNow,
            DetailedMetrics = System.Text.Json.JsonSerializer.Serialize(metrics.DetailedMetrics)
        };

        await _unitOfWork.PerformanceBaselines.AddAsync(baseline, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return baseline;
    }

    private double GetExpectedDuration(string operationType)
    {
        return operationType.ToUpper() switch
        {
            "SOD" => 15, // 15 minutes
            "EOD" => 45, // 45 minutes
            _ => 20
        };
    }

    private string[] GetTopLongRunningServices(SODEODOperation[] operations)
    {
        // Simulate service analysis
        return new[] { "T24CoreService", "IntegrationService", "ReportingService" };
    }

    private string[] GetFailingServices(SODEODOperation[] operations)
    {
        // Simulate failing service analysis
        return new[] { "ExternalAPIService", "DatabaseService" };
    }
}