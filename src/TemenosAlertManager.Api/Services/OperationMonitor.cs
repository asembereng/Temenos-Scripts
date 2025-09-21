using System.Collections.Concurrent;
using System.Diagnostics;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;
using PerformanceMetrics = TemenosAlertManager.Core.Interfaces.PerformanceMetrics;
using AlertSummaryDto = TemenosAlertManager.Core.Interfaces.AlertSummaryDto;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// Operation monitor for real-time monitoring of SOD/EOD operations
/// </summary>
public class OperationMonitor : IOperationMonitor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OperationMonitor> _logger;
    private readonly ConcurrentDictionary<string, OperationMonitoringContext> _activeOperations;
    private readonly Timer _metricsCollectionTimer;

    public OperationMonitor(IUnitOfWork unitOfWork, ILogger<OperationMonitor> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activeOperations = new ConcurrentDictionary<string, OperationMonitoringContext>();
        
        // Start metrics collection timer (every 30 seconds)
        _metricsCollectionTimer = new Timer(CollectMetrics, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public async Task StartMonitoringAsync(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting monitoring for operation {OperationId}", operationId);

            var context = new OperationMonitoringContext
            {
                OperationId = operationId,
                StartTime = DateTime.UtcNow,
                IsActive = true,
                MetricsHistory = new List<OperationMetrics>(),
                ResourceUsageHistory = new List<ResourceUsage>()
            };

            _activeOperations[operationId] = context;

            // Initialize baseline metrics
            var initialMetrics = await CollectOperationMetricsAsync(operationId, cancellationToken);
            context.MetricsHistory.Add(initialMetrics);

            _logger.LogInformation("Monitoring started for operation {OperationId}", operationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start monitoring for operation {OperationId}", operationId);
            throw;
        }
    }

    public async Task StopMonitoringAsync(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Stopping monitoring for operation {OperationId}", operationId);

            if (_activeOperations.TryRemove(operationId, out var context))
            {
                context.IsActive = false;
                context.EndTime = DateTime.UtcNow;

                // Collect final metrics
                var finalMetrics = await CollectOperationMetricsAsync(operationId, cancellationToken);
                context.MetricsHistory.Add(finalMetrics);

                _logger.LogInformation("Monitoring stopped for operation {OperationId}. Duration: {Duration}, Final progress: {Progress}%",
                    operationId, context.EndTime - context.StartTime, finalMetrics.ProgressPercentage);
            }
            else
            {
                _logger.LogWarning("Attempted to stop monitoring for operation {OperationId} that was not being monitored", operationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop monitoring for operation {OperationId}", operationId);
            throw;
        }
    }

    public async Task<OperationMetrics> GetOperationMetricsAsync(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await CollectOperationMetricsAsync(operationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation metrics for operation {OperationId}", operationId);
            throw;
        }
    }

    public async Task<OperationDashboardDto> GetOperationDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Collecting operation dashboard data");

            // Get active operations
            var activeOperations = new List<OperationSummaryDto>();
            foreach (var kvp in _activeOperations)
            {
                try
                {
                    var metrics = await CollectOperationMetricsAsync(kvp.Key, cancellationToken);
                    var operation = await _unitOfWork.SODEODOperations.GetByOperationCodeAsync(kvp.Key, cancellationToken);
                    
                    if (operation != null)
                    {
                        activeOperations.Add(new OperationSummaryDto
                        {
                            OperationId = operation.OperationCode,
                            OperationType = operation.OperationType,
                            Status = operation.Status,
                            ProgressPercentage = (int)metrics.ProgressPercentage,
                            StartTime = operation.StartTime,
                            EstimatedEndTime = CalculateEstimatedEndTime(operation, metrics),
                            CurrentStep = metrics.CurrentPhase
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect metrics for active operation {OperationId}", kvp.Key);
                }
            }

            // Get recent operations (last 24 hours)
            var recentOperations = await GetRecentOperationsAsync(TimeSpan.FromHours(24), cancellationToken);

            // Get system health
            var systemHealth = await GetSystemHealthAsync(cancellationToken);

            // Get performance trends (last 7 days)
            var performanceTrends = await GetPerformanceTrendsAsync(TimeSpan.FromDays(7), cancellationToken);

            // Get alert summary
            var alertSummary = await GetAlertSummaryAsync(cancellationToken);

            var dashboard = new OperationDashboardDto
            {
                LastUpdated = DateTime.UtcNow,
                ActiveOperations = activeOperations.ToArray(),
                RecentOperations = recentOperations,
                SystemHealth = systemHealth,
                PerformanceTrends = performanceTrends,
                AlertSummary = alertSummary
            };

            _logger.LogDebug("Operation dashboard data collected: {ActiveOps} active, {RecentOps} recent, {SystemHealth} health",
                activeOperations.Count, recentOperations.Length, systemHealth.OverallStatus);

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation dashboard");
            throw;
        }
    }

    // Helper methods

    private async Task<OperationMetrics> CollectOperationMetricsAsync(string operationId, CancellationToken cancellationToken)
    {
        var operation = await _unitOfWork.SODEODOperations.GetByOperationCodeAsync(operationId, cancellationToken);
        if (operation == null)
        {
            throw new ArgumentException($"Operation {operationId} not found");
        }

        var steps = await _unitOfWork.OperationSteps.GetByOperationIdAsync(operation.Id, cancellationToken);
        var completedSteps = steps.Count(s => s.Status == "Completed");
        var totalSteps = Math.Max(steps.Count(), GetExpectedStepCount(operation.OperationType));

        var progressPercentage = totalSteps > 0 ? (double)completedSteps / totalSteps * 100 : 0;
        var elapsedTime = DateTime.UtcNow - operation.StartTime;

        var currentPhase = "Initializing";
        if (steps.Any())
        {
            var currentStep = steps.Where(s => s.Status == "Running").FirstOrDefault() 
                           ?? steps.OrderByDescending(s => s.StartTime).FirstOrDefault();
            currentPhase = currentStep?.StepName ?? "Unknown";
        }

        // Calculate performance metrics
        var performanceMetrics = CalculatePerformanceMetrics(steps);

        // Collect resource usage
        var resourceUsage = await CollectResourceUsageAsync(cancellationToken);

        return new OperationMetrics
        {
            OperationId = operationId,
            ElapsedTime = elapsedTime,
            CompletedSteps = completedSteps,
            TotalSteps = totalSteps,
            ProgressPercentage = progressPercentage,
            CurrentPhase = currentPhase,
            Performance = performanceMetrics,
            ResourceUsage = resourceUsage
        };
    }

    private int GetExpectedStepCount(string operationType)
    {
        return operationType switch
        {
            "SOD" => 8, // Pre-validation, Dependency resolution, Service startup, Post-validation, etc.
            "EOD" => 15, // Pre-validation, Cutoff, Wait, Daily processing (multiple), Reconciliation, Reports, Cleanup, etc.
            _ => 10
        };
    }

    private PerformanceMetrics CalculatePerformanceMetrics(IEnumerable<OperationStep> steps)
    {
        var completedSteps = steps.Where(s => s.Status == "Completed" && s.EndTime.HasValue).ToArray();
        
        if (!completedSteps.Any())
        {
            return new PerformanceMetrics();
        }

        var durations = completedSteps
            .Select(s => (s.EndTime!.Value - s.StartTime!.Value).TotalSeconds)
            .Where(d => d > 0)
            .ToArray();

        var failedSteps = steps.Count(s => s.Status == "Failed");
        var retriedSteps = steps.Count(s => s.RetryCount > 0);

        return new PerformanceMetrics
        {
            AverageStepDuration = durations.Any() ? durations.Average() : 0,
            MaxStepDuration = durations.Any() ? durations.Max() : 0,
            MinStepDuration = durations.Any() ? durations.Min() : 0,
            FailedSteps = failedSteps,
            RetriedSteps = retriedSteps
        };
    }

    private async Task<ResourceUsage> CollectResourceUsageAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Collect system resource usage metrics
            var process = Process.GetCurrentProcess();
            
            var cpuUtilization = await GetCpuUtilizationAsync();
            var memoryUtilization = GetMemoryUtilization(process);
            var diskUtilization = await GetDiskUtilizationAsync();
            var networkUtilization = await GetNetworkUtilizationAsync();

            return new ResourceUsage
            {
                CpuUtilization = cpuUtilization,
                MemoryUtilization = memoryUtilization,
                DiskUtilization = diskUtilization,
                NetworkUtilization = networkUtilization
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect resource usage metrics");
            return new ResourceUsage();
        }
    }

    private async Task<double> GetCpuUtilizationAsync()
    {
        // Simplified CPU utilization calculation
        await Task.Delay(100);
        return Environment.ProcessorCount > 0 ? new Random().NextDouble() * 100 : 0;
    }

    private double GetMemoryUtilization(Process process)
    {
        try
        {
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = process.WorkingSet64;
            
            // Simplified memory utilization calculation
            return workingSet > 0 ? Math.Min((double)totalMemory / workingSet * 100, 100) : 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<double> GetDiskUtilizationAsync()
    {
        // Simplified disk utilization calculation
        await Task.Delay(50);
        return new Random().NextDouble() * 100;
    }

    private async Task<double> GetNetworkUtilizationAsync()
    {
        // Simplified network utilization calculation
        await Task.Delay(50);
        return new Random().NextDouble() * 100;
    }

    private DateTime? CalculateEstimatedEndTime(SODEODOperation operation, OperationMetrics metrics)
    {
        if (operation.EndTime.HasValue)
            return operation.EndTime;

        if (metrics.ProgressPercentage <= 0)
            return null;

        var elapsedTime = metrics.ElapsedTime;
        var estimatedTotalTime = TimeSpan.FromMilliseconds(elapsedTime.TotalMilliseconds / metrics.ProgressPercentage * 100);
        
        return operation.StartTime.Add(estimatedTotalTime);
    }

    private async Task<OperationHistoryDto[]> GetRecentOperationsAsync(TimeSpan timeRange, CancellationToken cancellationToken)
    {
        try
        {
            var operations = await _unitOfWork.SODEODOperations.GetPagedAsync(1, 50, cancellationToken);
            var recentOperations = operations.Items
                .Where(op => DateTime.UtcNow - op.StartTime <= timeRange)
                .Select(op => new OperationHistoryDto
                {
                    OperationId = op.OperationCode,
                    OperationType = op.OperationType,
                    Environment = op.Environment,
                    StartTime = op.StartTime,
                    EndTime = op.EndTime,
                    Status = op.Status,
                    Duration = op.EndTime?.Subtract(op.StartTime) ?? DateTime.UtcNow.Subtract(op.StartTime),
                    InitiatedBy = op.InitiatedBy,
                    ServicesInvolved = GetServicesInvolvedCount(op.ServicesInvolved),
                    HasErrors = !string.IsNullOrEmpty(op.ErrorDetails)
                })
                .OrderByDescending(op => op.StartTime)
                .ToArray();

            return recentOperations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent operations");
            return Array.Empty<OperationHistoryDto>();
        }
    }

    private int GetServicesInvolvedCount(string? servicesInvolvedJson)
    {
        if (string.IsNullOrEmpty(servicesInvolvedJson))
            return 0;

        try
        {
            var services = System.Text.Json.JsonSerializer.Deserialize<string[]>(servicesInvolvedJson);
            return services?.Length ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            var services = await _unitOfWork.ServiceConfigs.GetAllAsync(cancellationToken);
            var totalServices = services.Count();
            
            // Simulate health check results
            var healthyServices = (int)(totalServices * 0.85); // Assume 85% healthy
            var unhealthyServices = totalServices - healthyServices;
            var healthPercentage = totalServices > 0 ? (double)healthyServices / totalServices * 100 : 100;

            var systemResources = await CollectResourceUsageAsync(cancellationToken);

            var overallStatus = healthPercentage >= 90 ? "Healthy" :
                               healthPercentage >= 70 ? "Warning" : "Critical";

            return new SystemHealthDto
            {
                OverallStatus = overallStatus,
                TotalServices = totalServices,
                HealthyServices = healthyServices,
                UnhealthyServices = unhealthyServices,
                HealthPercentage = healthPercentage,
                SystemResources = systemResources,
                LastHealthCheck = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health");
            
            return new SystemHealthDto
            {
                OverallStatus = "Unknown",
                LastHealthCheck = DateTime.UtcNow
            };
        }
    }

    private async Task<PerformanceTrendDto[]> GetPerformanceTrendsAsync(TimeSpan timeRange, CancellationToken cancellationToken)
    {
        try
        {
            var trends = new List<PerformanceTrendDto>();
            var now = DateTime.UtcNow;
            var dataPoints = 24; // Hourly data points for the last day
            
            for (int i = 0; i < dataPoints; i++)
            {
                var timestamp = now.AddHours(-i);
                
                // Simulate performance trend data
                trends.Add(new PerformanceTrendDto
                {
                    Timestamp = timestamp,
                    MetricName = "OperationDuration",
                    Value = 10 + new Random().NextDouble() * 5, // Simulated minutes
                    Trend = i < dataPoints / 2 ? "Improving" : "Stable"
                });

                trends.Add(new PerformanceTrendDto
                {
                    Timestamp = timestamp,
                    MetricName = "SuccessRate",
                    Value = 95 + new Random().NextDouble() * 5, // Simulated percentage
                    Trend = "Stable"
                });
            }

            return trends.OrderBy(t => t.Timestamp).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance trends");
            return Array.Empty<PerformanceTrendDto>();
        }
    }

    private async Task<AlertSummaryDto> GetAlertSummaryAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Simulate alert data - in real implementation, this would query the alerts system
            var criticalAlerts = 2;
            var warningAlerts = 5;
            var infoAlerts = 8;
            var totalAlerts = criticalAlerts + warningAlerts + infoAlerts;

            var recentAlerts = new MonitoringAlertDto[]
            {
                new() { Id = 1, Severity = "Critical", Title = "Service Down", Description = "T24 Core service is down", CreatedAt = DateTime.UtcNow.AddMinutes(-15), Source = "ServiceMonitor", IsAcknowledged = false },
                new() { Id = 2, Severity = "Warning", Title = "High CPU Usage", Description = "CPU usage above 85%", CreatedAt = DateTime.UtcNow.AddMinutes(-30), Source = "SystemMonitor", IsAcknowledged = true },
                new() { Id = 3, Severity = "Info", Title = "SOD Completed", Description = "SOD operation completed successfully", CreatedAt = DateTime.UtcNow.AddHours(-1), Source = "OperationMonitor", IsAcknowledged = true }
            };

            return new AlertSummaryDto
            {
                CriticalAlerts = criticalAlerts,
                WarningAlerts = warningAlerts,
                InfoAlerts = infoAlerts,
                TotalAlerts = totalAlerts,
                RecentAlerts = recentAlerts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alert summary");
            
            return new AlertSummaryDto
            {
                RecentAlerts = Array.Empty<MonitoringAlertDto>()
            };
        }
    }

    private void CollectMetrics(object? state)
    {
        try
        {
            foreach (var kvp in _activeOperations)
            {
                if (kvp.Value.IsActive)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var metrics = await CollectOperationMetricsAsync(kvp.Key, CancellationToken.None);
                            kvp.Value.MetricsHistory.Add(metrics);

                            // Keep only last 100 metrics entries to prevent memory bloat
                            if (kvp.Value.MetricsHistory.Count > 100)
                            {
                                kvp.Value.MetricsHistory.RemoveAt(0);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to collect metrics for operation {OperationId}", kvp.Key);
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in metrics collection timer");
        }
    }

    public void Dispose()
    {
        _metricsCollectionTimer?.Dispose();
    }
}

/// <summary>
/// Internal monitoring context for active operations
/// </summary>
internal class OperationMonitoringContext
{
    public string OperationId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsActive { get; set; }
    public List<OperationMetrics> MetricsHistory { get; set; } = new();
    public List<ResourceUsage> ResourceUsageHistory { get; set; } = new();
}