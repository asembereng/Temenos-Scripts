using Hangfire;
using TemenosAlertManager.Api.Services;
using TemenosAlertManager.Core.Interfaces;

namespace TemenosAlertManager.Api.BackgroundServices;

public class MonitoringSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<MonitoringSchedulerService> _logger;
    private readonly IRecurringJobManager _recurringJobManager;
    private bool _jobsScheduled = false;

    public MonitoringSchedulerService(
        IServiceScopeFactory serviceScopeFactory, 
        ILogger<MonitoringSchedulerService> logger,
        IRecurringJobManager recurringJobManager)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monitoring Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_jobsScheduled)
                {
                    await ScheduleMonitoringJobsAsync();
                    _jobsScheduled = true;
                }

                // Check for configuration changes every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                await UpdateJobSchedulesAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Monitoring Scheduler Service cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Monitoring Scheduler Service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Monitoring Scheduler Service stopped");
    }

    private async Task ScheduleMonitoringJobsAsync()
    {
        try
        {
            _logger.LogInformation("Scheduling monitoring jobs");

            // Schedule SQL Server monitoring jobs
            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "sql-server-availability",
                service => service.RunSqlServerAvailabilityChecksAsync(),
                "*/5 * * * *"); // Every 5 minutes

            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "sql-server-blocking",
                service => service.RunSqlServerBlockingChecksAsync(),
                "*/2 * * * *"); // Every 2 minutes

            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "sql-server-performance",
                service => service.RunSqlServerPerformanceChecksAsync(),
                "*/10 * * * *"); // Every 10 minutes

            // Schedule TPH monitoring jobs (when implemented)
            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "tph-services",
                service => service.RunTphServiceChecksAsync(),
                "*/3 * * * *"); // Every 3 minutes

            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "tph-queues",
                service => service.RunTphQueueChecksAsync(),
                "*/1 * * * *"); // Every minute

            // Schedule T24 monitoring jobs (when implemented)
            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "t24-services",
                service => service.RunT24ServiceChecksAsync(),
                "*/5 * * * *"); // Every 5 minutes

            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "t24-cob",
                service => service.RunT24COBChecksAsync(),
                "0 */1 * * *"); // Every hour

            // Schedule MQ monitoring jobs (when implemented)
            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "mq-connectivity",
                service => service.RunMqConnectivityChecksAsync(),
                "*/2 * * * *"); // Every 2 minutes

            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "mq-queues",
                service => service.RunMqQueueChecksAsync(),
                "*/1 * * * *"); // Every minute

            // Schedule cleanup jobs
            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "cleanup-old-results",
                service => service.CleanupOldCheckResultsAsync(),
                "0 2 * * *"); // Daily at 2 AM

            _recurringJobManager.AddOrUpdate<IMonitoringJobService>(
                "cleanup-old-audit-logs",
                service => service.CleanupOldAuditLogsAsync(),
                "0 3 * * 0"); // Weekly on Sunday at 3 AM

            _logger.LogInformation("Successfully scheduled all monitoring jobs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule monitoring jobs");
            throw;
        }
    }

    private async Task UpdateJobSchedulesAsync()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Check for any configuration changes that might affect scheduling
            // This could be extended to read schedule configurations from the database
            
            // For now, we'll keep the default schedules
            
            _logger.LogDebug("Job schedules updated based on current configuration");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update job schedules from configuration");
        }
    }
}

// Interface for monitoring job service
public interface IMonitoringJobService
{
    Task RunSqlServerAvailabilityChecksAsync();
    Task RunSqlServerBlockingChecksAsync();
    Task RunSqlServerPerformanceChecksAsync();
    Task RunTphServiceChecksAsync();
    Task RunTphQueueChecksAsync();
    Task RunT24ServiceChecksAsync();
    Task RunT24COBChecksAsync();
    Task RunMqConnectivityChecksAsync();
    Task RunMqQueueChecksAsync();
    Task CleanupOldCheckResultsAsync();
    Task CleanupOldAuditLogsAsync();
}

// Implementation of monitoring job service
public class MonitoringJobService : IMonitoringJobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMonitoringService _monitoringService;
    private readonly IAlertService _alertService;
    private readonly IPowerShellService _powerShellService;
    private readonly ILogger<MonitoringJobService> _logger;

    public MonitoringJobService(
        IUnitOfWork unitOfWork,
        IMonitoringService monitoringService,
        IAlertService alertService,
        IPowerShellService powerShellService,
        ILogger<MonitoringJobService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunSqlServerAvailabilityChecksAsync()
    {
        try
        {
            _logger.LogDebug("Running SQL Server availability checks");
            
            var sqlTargets = await _unitOfWork.Configuration.GetSqlTargetConfigsAsync();
            
            foreach (var target in sqlTargets)
            {
                try
                {
                    var result = await _powerShellService.ExecuteCheckAsync(
                        "TemenosChecks.Sql",
                        "Test-SqlServerAvailability",
                        new Dictionary<string, object>
                        {
                            ["InstanceName"] = target.InstanceName,
                            ["DatabaseName"] = target.DatabaseName ?? "master"
                        });

                    await ProcessCheckResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check SQL Server availability for {Instance}", target.InstanceName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run SQL Server availability checks");
        }
    }

    public async Task RunSqlServerBlockingChecksAsync()
    {
        try
        {
            _logger.LogDebug("Running SQL Server blocking checks");
            
            var sqlTargets = await _unitOfWork.Configuration.GetSqlTargetConfigsAsync();
            
            foreach (var target in sqlTargets)
            {
                try
                {
                    var thresholds = ParseThresholds(target.Thresholds);
                    
                    var result = await _powerShellService.ExecuteCheckAsync(
                        "TemenosChecks.Sql",
                        "Get-SqlBlockingSessions",
                        new Dictionary<string, object>
                        {
                            ["InstanceName"] = target.InstanceName,
                            ["Thresholds"] = thresholds
                        });

                    await ProcessCheckResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check SQL Server blocking for {Instance}", target.InstanceName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run SQL Server blocking checks");
        }
    }

    public async Task RunSqlServerPerformanceChecksAsync()
    {
        try
        {
            _logger.LogDebug("Running SQL Server performance checks");
            
            var sqlTargets = await _unitOfWork.Configuration.GetSqlTargetConfigsAsync();
            
            foreach (var target in sqlTargets)
            {
                try
                {
                    // Run TempDB usage check
                    var tempDbResult = await _powerShellService.ExecuteCheckAsync(
                        "TemenosChecks.Sql",
                        "Get-SqlTempDbUsage",
                        new Dictionary<string, object>
                        {
                            ["InstanceName"] = target.InstanceName,
                            ["Thresholds"] = ParseThresholds(target.Thresholds)
                        });

                    await ProcessCheckResult(tempDbResult);

                    // Run long-running queries check
                    var longQueryResult = await _powerShellService.ExecuteCheckAsync(
                        "TemenosChecks.Sql",
                        "Get-SqlLongRunningQueries",
                        new Dictionary<string, object>
                        {
                            ["InstanceName"] = target.InstanceName,
                            ["Thresholds"] = ParseThresholds(target.Thresholds)
                        });

                    await ProcessCheckResult(longQueryResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check SQL Server performance for {Instance}", target.InstanceName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run SQL Server performance checks");
        }
    }

    public async Task RunTphServiceChecksAsync()
    {
        try
        {
            _logger.LogDebug("Running TPH service checks");
            
            var serviceConfigs = await _unitOfWork.Configuration.GetServiceConfigsAsync();
            var tphServices = serviceConfigs.Where(s => s.Type == TemenosAlertManager.Core.Enums.MonitoringDomain.TPH);
            
            foreach (var service in tphServices)
            {
                try
                {
                    var result = await _powerShellService.ExecuteCheckAsync(
                        "TemenosChecks.TPH",
                        "Test-TphServices",
                        new Dictionary<string, object>
                        {
                            ["ServerName"] = service.Host,
                            ["ServiceNames"] = new[] { service.Name }
                        });

                    await ProcessCheckResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check TPH service {Service} on {Host}", service.Name, service.Host);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run TPH service checks");
        }
    }

    public async Task RunTphQueueChecksAsync()
    {
        try
        {
            _logger.LogDebug("Running TPH queue checks");
            
            var queueConfigs = await _unitOfWork.Configuration.GetQueueConfigsAsync();
            
            foreach (var queue in queueConfigs)
            {
                try
                {
                    var result = await _powerShellService.ExecuteCheckAsync(
                        "TemenosChecks.TPH",
                        "Get-TphQueueDepth",
                        new Dictionary<string, object>
                        {
                            ["QueueManager"] = queue.QueueManager,
                            ["QueueName"] = queue.Name,
                            ["Thresholds"] = new Dictionary<string, object>
                            {
                                ["Warning"] = queue.WarningDepth,
                                ["Critical"] = queue.CriticalDepth
                            }
                        });

                    await ProcessCheckResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check TPH queue {Queue}", queue.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run TPH queue checks");
        }
    }

    public async Task RunT24ServiceChecksAsync()
    {
        try
        {
            _logger.LogDebug("Running T24 service checks");
            // Implementation for T24 service checks
            // Similar pattern to TPH service checks
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run T24 service checks");
        }
    }

    public async Task RunT24COBChecksAsync()
    {
        try
        {
            _logger.LogDebug("Running T24 COB checks");
            // Implementation for T24 COB checks
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run T24 COB checks");
        }
    }

    public async Task RunMqConnectivityChecksAsync()
    {
        try
        {
            _logger.LogDebug("Running MQ connectivity checks");
            // Implementation for MQ connectivity checks
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run MQ connectivity checks");
        }
    }

    public async Task RunMqQueueChecksAsync()
    {
        try
        {
            _logger.LogDebug("Running MQ queue checks");
            // Implementation for MQ queue checks
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run MQ queue checks");
        }
    }

    public async Task CleanupOldCheckResultsAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up old check results");
            
            var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep 30 days
            var oldResults = await _unitOfWork.CheckResults.FindAsync(cr => cr.CreatedAt < cutoffDate);
            
            foreach (var result in oldResults)
            {
                await _unitOfWork.CheckResults.DeleteAsync(result.Id);
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} old check results", oldResults.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old check results");
        }
    }

    public async Task CleanupOldAuditLogsAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up old audit logs");
            
            var cutoffDate = DateTime.UtcNow.AddDays(-90); // Keep 90 days for compliance
            var oldLogs = await _unitOfWork.AuditEvents.FindAsync(ae => ae.CreatedAt < cutoffDate);
            
            foreach (var log in oldLogs)
            {
                await _unitOfWork.AuditEvents.DeleteAsync(log.Id);
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} old audit logs", oldLogs.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old audit logs");
        }
    }

    private async Task ProcessCheckResult(ICheckResult result)
    {
        try
        {
            // Store the check result
            var checkResult = new TemenosAlertManager.Core.Entities.CheckResult
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

            await _unitOfWork.CheckResults.AddAsync(checkResult);

            // Create alert if needed
            if (result.Status == TemenosAlertManager.Core.Enums.CheckStatus.Critical || 
                result.Status == TemenosAlertManager.Core.Enums.CheckStatus.Warning)
            {
                var alert = await _alertService.CreateAlertAsync(result);
                
                if (alert != null)
                {
                    // Queue email notification
                    await QueueAlertEmailAsync(alert);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process check result for {Domain}.{Target}.{Metric}", 
                result.Domain, result.Target, result.Metric);
        }
    }

    private async Task QueueAlertEmailAsync(TemenosAlertManager.Core.Entities.Alert alert)
    {
        try
        {
            // Get email recipients based on alert severity and domain
            var recipients = await GetAlertRecipientsAsync(alert);
            
            foreach (var recipient in recipients)
            {
                var emailOutbox = new TemenosAlertManager.Core.Entities.AlertOutbox
                {
                    AlertId = alert.Id,
                    Channel = TemenosAlertManager.Core.Enums.AlertChannel.Email,
                    Recipient = recipient,
                    Subject = $"[{alert.Severity.ToString().ToUpper()}][{alert.Domain}] {alert.Title}",
                    Payload = GenerateAlertEmailBody(alert),
                    Status = TemenosAlertManager.Core.Enums.AlertDeliveryStatus.Pending
                };

                await _unitOfWork.AlertOutbox.AddAsync(emailOutbox);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue alert email for alert {AlertId}", alert.Id);
        }
    }

    private async Task<List<string>> GetAlertRecipientsAsync(TemenosAlertManager.Core.Entities.Alert alert)
    {
        // TODO: Implement proper recipient resolution based on alert configuration
        // For now, return a default list
        return new List<string> { "operations@company.local", "admin@company.local" };
    }

    private string GenerateAlertEmailBody(TemenosAlertManager.Core.Entities.Alert alert)
    {
        return $@"
<h2>Alert Details</h2>
<p><strong>Title:</strong> {alert.Title}</p>
<p><strong>Description:</strong> {alert.Description}</p>
<p><strong>Severity:</strong> {alert.Severity}</p>
<p><strong>Domain:</strong> {alert.Domain}</p>
<p><strong>Source:</strong> {alert.Source}</p>
<p><strong>Created:</strong> {alert.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
{(string.IsNullOrEmpty(alert.MetricValue) ? "" : $"<p><strong>Current Value:</strong> {alert.MetricValue}</p>")}
{(string.IsNullOrEmpty(alert.Threshold) ? "" : $"<p><strong>Threshold:</strong> {alert.Threshold}</p>")}
";
    }

    private Dictionary<string, object> ParseThresholds(string? thresholdsJson)
    {
        if (string.IsNullOrEmpty(thresholdsJson))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(thresholdsJson) 
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}