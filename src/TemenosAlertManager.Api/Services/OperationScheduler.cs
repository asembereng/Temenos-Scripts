using NCrontab;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// Service for scheduling SOD/EOD operations with cron-based automation
/// </summary>
public class OperationScheduler : IOperationScheduler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITemenosOperationService _temenosOperationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<OperationScheduler> _logger;

    public OperationScheduler(
        IUnitOfWork unitOfWork,
        ITemenosOperationService temenosOperationService,
        IAuditService auditService,
        ILogger<OperationScheduler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _temenosOperationService = temenosOperationService ?? throw new ArgumentNullException(nameof(temenosOperationService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ScheduleResultDto> ScheduleSODAsync(SODScheduleRequest request, string scheduledBy, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scheduling SOD operation for environment {Environment} by {User}", request.Environment, scheduledBy);

            // Validate cron expression
            if (!IsValidCronExpression(request.CronExpression))
            {
                throw new ArgumentException($"Invalid cron expression: {request.CronExpression}");
            }

            // Calculate next execution time
            var nextExecution = GetNextExecutionTime(request.CronExpression, request.TimeZone);

            // Create scheduled operation
            var scheduledOperation = new ScheduledOperation
            {
                OperationType = "SOD",
                Environment = request.Environment,
                CronExpression = request.CronExpression,
                TimeZone = request.TimeZone,
                ServicesFilter = request.ServicesFilter?.Length > 0 ? 
                    System.Text.Json.JsonSerializer.Serialize(request.ServicesFilter) : null,
                DryRun = request.DryRun,
                IsEnabled = request.IsEnabled,
                Comments = request.Comments,
                Configuration = request.Configuration?.Count > 0 ? 
                    System.Text.Json.JsonSerializer.Serialize(request.Configuration) : null,
                ScheduledBy = scheduledBy,
                NextExecutionTime = nextExecution,
                Status = request.IsEnabled ? "Active" : "Paused"
            };

            await _unitOfWork.ScheduledOperations.AddAsync(scheduledOperation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit the scheduling
            await _auditService.LogEventAsync(scheduledBy, scheduledBy, "SOD_OPERATION_SCHEDULED", 
                $"ScheduleId:{scheduledOperation.Id},Environment:{request.Environment}", request, cancellationToken: cancellationToken);

            _logger.LogInformation("SOD operation scheduled successfully with ID {ScheduleId} for environment {Environment}", 
                scheduledOperation.Id, request.Environment);

            return new ScheduleResultDto
            {
                ScheduleId = scheduledOperation.Id,
                OperationType = "SOD",
                Environment = request.Environment,
                CronExpression = request.CronExpression,
                NextExecutionTime = nextExecution,
                Status = scheduledOperation.Status,
                Message = "SOD operation scheduled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule SOD operation for environment {Environment}", request.Environment);
            
            await _auditService.LogFailureAsync(scheduledBy, scheduledBy, "SOD_SCHEDULE_FAILED", 
                $"Environment:{request.Environment}", ex.Message, cancellationToken: cancellationToken);
            
            throw;
        }
    }

    public async Task<ScheduleResultDto> ScheduleEODAsync(EODScheduleRequest request, string scheduledBy, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scheduling EOD operation for environment {Environment} by {User}", request.Environment, scheduledBy);

            // Validate cron expression
            if (!IsValidCronExpression(request.CronExpression))
            {
                throw new ArgumentException($"Invalid cron expression: {request.CronExpression}");
            }

            // Calculate next execution time
            var nextExecution = GetNextExecutionTime(request.CronExpression, request.TimeZone);

            // Create scheduled operation
            var scheduledOperation = new ScheduledOperation
            {
                OperationType = "EOD",
                Environment = request.Environment,
                CronExpression = request.CronExpression,
                TimeZone = request.TimeZone,
                ServicesFilter = request.ServicesFilter?.Length > 0 ? 
                    System.Text.Json.JsonSerializer.Serialize(request.ServicesFilter) : null,
                DryRun = request.DryRun,
                IsEnabled = request.IsEnabled,
                Comments = request.Comments,
                Configuration = request.Configuration?.Count > 0 ? 
                    System.Text.Json.JsonSerializer.Serialize(request.Configuration) : null,
                ScheduledBy = scheduledBy,
                NextExecutionTime = nextExecution,
                Status = request.IsEnabled ? "Active" : "Paused"
            };

            await _unitOfWork.ScheduledOperations.AddAsync(scheduledOperation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit the scheduling
            await _auditService.LogEventAsync(scheduledBy, scheduledBy, "EOD_OPERATION_SCHEDULED", 
                $"ScheduleId:{scheduledOperation.Id},Environment:{request.Environment}", request, cancellationToken: cancellationToken);

            _logger.LogInformation("EOD operation scheduled successfully with ID {ScheduleId} for environment {Environment}", 
                scheduledOperation.Id, request.Environment);

            return new ScheduleResultDto
            {
                ScheduleId = scheduledOperation.Id,
                OperationType = "EOD",
                Environment = request.Environment,
                CronExpression = request.CronExpression,
                NextExecutionTime = nextExecution,
                Status = scheduledOperation.Status,
                Message = "EOD operation scheduled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule EOD operation for environment {Environment}", request.Environment);
            
            await _auditService.LogFailureAsync(scheduledBy, scheduledBy, "EOD_SCHEDULE_FAILED", 
                $"Environment:{request.Environment}", ex.Message, cancellationToken: cancellationToken);
            
            throw;
        }
    }

    public async Task<ScheduledOperationDto[]> GetScheduledOperationsAsync(string? environment = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var operations = await _unitOfWork.ScheduledOperations.GetAllAsync(cancellationToken);
            
            if (!string.IsNullOrEmpty(environment))
            {
                operations = operations.Where(op => op.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase));
            }

            var result = operations.Select(op => new ScheduledOperationDto
            {
                ScheduleId = op.Id,
                OperationType = op.OperationType,
                Environment = op.Environment,
                CronExpression = op.CronExpression,
                TimeZone = op.TimeZone,
                NextExecutionTime = op.NextExecutionTime ?? DateTime.MinValue,
                LastExecutionTime = op.LastExecutionTime,
                Status = op.Status,
                IsEnabled = op.IsEnabled,
                ScheduledBy = op.ScheduledBy,
                CreatedAt = op.CreatedAt,
                ExecutionCount = op.ExecutionCount,
                SuccessCount = op.SuccessCount,
                FailureCount = op.FailureCount
            }).OrderBy(op => op.NextExecutionTime).ToArray();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scheduled operations for environment {Environment}", environment);
            throw;
        }
    }

    public async Task<bool> CancelScheduledOperationAsync(int scheduleId, string cancelledBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduledOperation = await _unitOfWork.ScheduledOperations.GetByIdAsync(scheduleId, cancellationToken);
            if (scheduledOperation == null)
            {
                _logger.LogWarning("Attempted to cancel non-existent scheduled operation {ScheduleId}", scheduleId);
                return false;
            }

            scheduledOperation.IsEnabled = false;
            scheduledOperation.Status = "Cancelled";

            await _unitOfWork.ScheduledOperations.UpdateAsync(scheduledOperation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit the cancellation
            await _auditService.LogEventAsync(cancelledBy, cancelledBy, "OPERATION_SCHEDULE_CANCELLED", 
                $"ScheduleId:{scheduleId},OperationType:{scheduledOperation.OperationType}", null, cancellationToken: cancellationToken);

            _logger.LogInformation("Scheduled operation {ScheduleId} cancelled by {User}", scheduleId, cancelledBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel scheduled operation {ScheduleId}", scheduleId);
            throw;
        }
    }

    public async Task<bool> UpdateScheduleAsync(int scheduleId, ScheduleUpdateRequest request, string updatedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduledOperation = await _unitOfWork.ScheduledOperations.GetByIdAsync(scheduleId, cancellationToken);
            if (scheduledOperation == null)
            {
                _logger.LogWarning("Attempted to update non-existent scheduled operation {ScheduleId}", scheduleId);
                return false;
            }

            var hasChanges = false;

            if (!string.IsNullOrEmpty(request.CronExpression) && request.CronExpression != scheduledOperation.CronExpression)
            {
                if (!IsValidCronExpression(request.CronExpression))
                {
                    throw new ArgumentException($"Invalid cron expression: {request.CronExpression}");
                }
                
                scheduledOperation.CronExpression = request.CronExpression;
                scheduledOperation.NextExecutionTime = GetNextExecutionTime(request.CronExpression, request.TimeZone ?? scheduledOperation.TimeZone);
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.TimeZone) && request.TimeZone != scheduledOperation.TimeZone)
            {
                scheduledOperation.TimeZone = request.TimeZone;
                scheduledOperation.NextExecutionTime = GetNextExecutionTime(scheduledOperation.CronExpression, request.TimeZone);
                hasChanges = true;
            }

            if (request.IsEnabled.HasValue && request.IsEnabled.Value != scheduledOperation.IsEnabled)
            {
                scheduledOperation.IsEnabled = request.IsEnabled.Value;
                scheduledOperation.Status = request.IsEnabled.Value ? "Active" : "Paused";
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.Comments))
            {
                scheduledOperation.Comments = request.Comments;
                hasChanges = true;
            }

            if (request.Configuration?.Count > 0)
            {
                scheduledOperation.Configuration = System.Text.Json.JsonSerializer.Serialize(request.Configuration);
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _unitOfWork.ScheduledOperations.UpdateAsync(scheduledOperation, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Audit the update
                await _auditService.LogEventAsync(updatedBy, updatedBy, "OPERATION_SCHEDULE_UPDATED", 
                    $"ScheduleId:{scheduleId},OperationType:{scheduledOperation.OperationType}", request, cancellationToken: cancellationToken);

                _logger.LogInformation("Scheduled operation {ScheduleId} updated by {User}", scheduleId, updatedBy);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update scheduled operation {ScheduleId}", scheduleId);
            throw;
        }
    }

    /// <summary>
    /// Execute scheduled operations - called by background service
    /// </summary>
    public async Task ExecuteScheduledOperationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var operations = await _unitOfWork.ScheduledOperations.GetAllAsync(cancellationToken);
            
            var dueOperations = operations.Where(op => 
                op.IsEnabled && 
                op.Status == "Active" && 
                op.NextExecutionTime.HasValue && 
                op.NextExecutionTime.Value <= now).ToArray();

            _logger.LogInformation("Found {Count} scheduled operations due for execution", dueOperations.Length);

            foreach (var operation in dueOperations)
            {
                try
                {
                    await ExecuteScheduledOperationAsync(operation, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute scheduled operation {ScheduleId}", operation.Id);
                    
                    operation.FailureCount++;
                    operation.Status = "Failed";
                    await _unitOfWork.ScheduledOperations.UpdateAsync(operation, cancellationToken);
                }
            }

            if (dueOperations.Length > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scheduled operations");
        }
    }

    private async Task ExecuteScheduledOperationAsync(ScheduledOperation operation, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing scheduled {OperationType} operation {ScheduleId} for environment {Environment}", 
            operation.OperationType, operation.Id, operation.Environment);

        try
        {
            // Update execution tracking
            operation.ExecutionCount++;
            operation.LastExecutionTime = DateTime.UtcNow;
            operation.NextExecutionTime = GetNextExecutionTime(operation.CronExpression, operation.TimeZone);

            await _unitOfWork.ScheduledOperations.UpdateAsync(operation, cancellationToken);

            // Parse configuration
            var servicesFilter = !string.IsNullOrEmpty(operation.ServicesFilter) ? 
                System.Text.Json.JsonSerializer.Deserialize<string[]>(operation.ServicesFilter) : Array.Empty<string>();

            // Execute the operation
            if (operation.OperationType == "SOD")
            {
                var sodRequest = new SODRequest
                {
                    Environment = operation.Environment,
                    ServicesFilter = servicesFilter,
                    DryRun = operation.DryRun,
                    ForceExecution = false,
                    Comments = $"Scheduled execution (Schedule ID: {operation.Id})"
                };

                await _temenosOperationService.StartSODAsync(sodRequest, "SYSTEM_SCHEDULER", cancellationToken);
            }
            else if (operation.OperationType == "EOD")
            {
                var eodRequest = new EODRequest
                {
                    Environment = operation.Environment,
                    ServicesFilter = servicesFilter,
                    DryRun = operation.DryRun,
                    ForceExecution = false,
                    Comments = $"Scheduled execution (Schedule ID: {operation.Id})"
                };

                await _temenosOperationService.StartEODAsync(eodRequest, "SYSTEM_SCHEDULER", cancellationToken);
            }

            // Update success count
            operation.SuccessCount++;
            
            _logger.LogInformation("Scheduled {OperationType} operation {ScheduleId} executed successfully", 
                operation.OperationType, operation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute scheduled {OperationType} operation {ScheduleId}", 
                operation.OperationType, operation.Id);
            
            operation.FailureCount++;
            throw;
        }
    }

    private bool IsValidCronExpression(string cronExpression)
    {
        try
        {
            CrontabSchedule.Parse(cronExpression);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private DateTime GetNextExecutionTime(string cronExpression, string timeZone)
    {
        try
        {
            var schedule = CrontabSchedule.Parse(cronExpression);
            var baseTime = DateTime.UtcNow;
            
            // Convert to target timezone if specified
            if (!string.IsNullOrEmpty(timeZone) && timeZone != "UTC")
            {
                try
                {
                    var targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                    baseTime = TimeZoneInfo.ConvertTimeFromUtc(baseTime, targetTimeZone);
                }
                catch (TimeZoneNotFoundException)
                {
                    _logger.LogWarning("Invalid timezone {TimeZone}, using UTC", timeZone);
                }
            }

            var nextOccurrence = schedule.GetNextOccurrence(baseTime);
            
            // Convert back to UTC for storage
            if (!string.IsNullOrEmpty(timeZone) && timeZone != "UTC")
            {
                try
                {
                    var targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                    nextOccurrence = TimeZoneInfo.ConvertTimeToUtc(nextOccurrence, targetTimeZone);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Already in UTC
                }
            }

            return nextOccurrence;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate next execution time for cron expression {CronExpression}", cronExpression);
            return DateTime.UtcNow.AddDays(1); // Default to 1 day from now
        }
    }
}