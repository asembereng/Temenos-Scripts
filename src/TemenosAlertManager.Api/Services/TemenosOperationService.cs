using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// Enhanced service for managing Temenos SOD/EOD operations with orchestration
/// </summary>
public class TemenosOperationService : ITemenosOperationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISODOrchestrator _sodOrchestrator;
    private readonly IEODOrchestrator _eodOrchestrator;
    private readonly IAuditService _auditService;
    private readonly ILogger<TemenosOperationService> _logger;

    public TemenosOperationService(
        IUnitOfWork unitOfWork,
        ISODOrchestrator sodOrchestrator,
        IEODOrchestrator eodOrchestrator,
        IAuditService auditService,
        ILogger<TemenosOperationService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sodOrchestrator = sodOrchestrator ?? throw new ArgumentNullException(nameof(sodOrchestrator));
        _eodOrchestrator = eodOrchestrator ?? throw new ArgumentNullException(nameof(eodOrchestrator));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Start a Start of Day operation using enhanced orchestration
    /// </summary>
    public async Task<OperationResultDto> StartSODAsync(SODRequest request, string initiatedBy, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting enhanced SOD operation {OperationId} for environment {Environment} by {User}", 
                operationId, request.Environment, initiatedBy);

            // Create operation record
            var operation = new SODEODOperation
            {
                OperationType = "SOD",
                OperationCode = operationId,
                BusinessDate = DateTime.Today,
                Environment = request.Environment,
                StartTime = startTime,
                Status = "Initiated",
                InitiatedBy = initiatedBy,
                InitiationMethod = "Manual",
                ServicesInvolved = request.ServicesFilter?.Length > 0 ? 
                    System.Text.Json.JsonSerializer.Serialize(request.ServicesFilter) : null
            };

            await _unitOfWork.SODEODOperations.AddAsync(operation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit the operation initiation
            await _auditService.LogEventAsync(initiatedBy, initiatedBy, "SOD_OPERATION_INITIATED", 
                $"OperationId:{operationId},Environment:{request.Environment}", request, cancellationToken: cancellationToken);

            // Execute SOD using the orchestrator asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await _sodOrchestrator.ExecuteSODAsync(request, operationId, cancellationToken);
                    
                    await _auditService.LogEventAsync(initiatedBy, initiatedBy, "SOD_OPERATION_COMPLETED", 
                        $"OperationId:{operationId}", null, cancellationToken: cancellationToken);
                    
                    _logger.LogInformation("Enhanced SOD operation {OperationId} completed successfully", operationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Enhanced SOD operation {OperationId} failed", operationId);
                    
                    await _auditService.LogFailureAsync(initiatedBy, initiatedBy, "SOD_OPERATION_FAILED", 
                        $"OperationId:{operationId}", ex.Message, cancellationToken: cancellationToken);
                }
            }, cancellationToken);

            return new OperationResultDto
            {
                OperationId = operationId,
                Status = "Initiated",
                Message = "Enhanced SOD operation has been initiated with orchestration",
                StartTime = startTime,
                EstimatedDurationMinutes = 15
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate enhanced SOD operation for environment {Environment}", request.Environment);
            
            await _auditService.LogFailureAsync(initiatedBy, initiatedBy, "SOD_OPERATION_INIT_FAILED", 
                $"Environment:{request.Environment}", ex.Message, cancellationToken: cancellationToken);
            
            throw;
        }
    }

    /// <summary>
    /// Start an End of Day operation using enhanced orchestration
    /// </summary>
    public async Task<OperationResultDto> StartEODAsync(EODRequest request, string initiatedBy, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting enhanced EOD operation {OperationId} for environment {Environment} by {User}", 
                operationId, request.Environment, initiatedBy);

            // Create operation record
            var operation = new SODEODOperation
            {
                OperationType = "EOD",
                OperationCode = operationId,
                BusinessDate = DateTime.Today,
                Environment = request.Environment,
                StartTime = startTime,
                Status = "Initiated",
                InitiatedBy = initiatedBy,
                InitiationMethod = "Manual",
                ServicesInvolved = request.ServicesFilter?.Length > 0 ? 
                    System.Text.Json.JsonSerializer.Serialize(request.ServicesFilter) : null
            };

            await _unitOfWork.SODEODOperations.AddAsync(operation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit the operation initiation
            await _auditService.LogEventAsync(initiatedBy, initiatedBy, "EOD_OPERATION_INITIATED", 
                $"OperationId:{operationId},Environment:{request.Environment}", request, cancellationToken: cancellationToken);

            // Execute EOD using the orchestrator asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await _eodOrchestrator.ExecuteEODAsync(request, operationId, cancellationToken);
                    
                    await _auditService.LogEventAsync(initiatedBy, initiatedBy, "EOD_OPERATION_COMPLETED", 
                        $"OperationId:{operationId}", null, cancellationToken: cancellationToken);
                    
                    _logger.LogInformation("Enhanced EOD operation {OperationId} completed successfully", operationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Enhanced EOD operation {OperationId} failed", operationId);
                    
                    await _auditService.LogFailureAsync(initiatedBy, initiatedBy, "EOD_OPERATION_FAILED", 
                        $"OperationId:{operationId}", ex.Message, cancellationToken: cancellationToken);
                }
            }, cancellationToken);

            return new OperationResultDto
            {
                OperationId = operationId,
                Status = "Initiated",
                Message = "Enhanced EOD operation has been initiated with orchestration",
                StartTime = startTime,
                EstimatedDurationMinutes = 75
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate enhanced EOD operation for environment {Environment}", request.Environment);
            
            await _auditService.LogFailureAsync(initiatedBy, initiatedBy, "EOD_OPERATION_INIT_FAILED", 
                $"Environment:{request.Environment}", ex.Message, cancellationToken: cancellationToken);
            
            throw;
        }
    }

    /// <summary>
    /// Get status of a specific operation
    /// </summary>
    public async Task<OperationStatusDto> GetOperationStatusAsync(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var operation = await _unitOfWork.SODEODOperations.GetByOperationCodeAsync(operationId, cancellationToken);

            if (operation == null)
            {
                throw new ArgumentException($"Operation {operationId} not found");
            }

            // Calculate progress percentage
            var progressPercentage = CalculateProgress(operation);

            // Get current step
            var currentStep = GetCurrentStep(operation);

            // Get operation steps
            var steps = await GetOperationStepsAsync(operation.Id, cancellationToken);

            return new OperationStatusDto
            {
                OperationId = operation.OperationCode,
                OperationType = operation.OperationType,
                Status = operation.Status,
                StartTime = operation.StartTime,
                EndTime = operation.EndTime,
                ProgressPercentage = progressPercentage,
                CurrentStep = currentStep,
                Steps = steps,
                ErrorMessage = operation.ErrorDetails
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for operation {OperationId}", operationId);
            throw;
        }
    }

    /// <summary>
    /// Cancel a running operation
    /// </summary>
    public async Task<OperationResultDto> CancelOperationAsync(string operationId, string cancelledBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var operation = await _unitOfWork.SODEODOperations.GetByOperationCodeAsync(operationId, cancellationToken);

            if (operation == null)
            {
                throw new ArgumentException($"Operation {operationId} not found");
            }

            if (operation.Status != "Running" && operation.Status != "Initiated")
            {
                throw new InvalidOperationException($"Operation {operationId} is not in a cancellable state. Current status: {operation.Status}");
            }

            // Update operation status
            operation.Status = "Cancelled";
            operation.EndTime = DateTime.UtcNow;
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Operation {OperationId} cancelled by {User}", operationId, cancelledBy);

            return new OperationResultDto
            {
                OperationId = operationId,
                Status = "Cancelled",
                Message = "Operation has been cancelled successfully",
                StartTime = operation.StartTime,
                EstimatedDurationMinutes = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel operation {OperationId}", operationId);
            throw;
        }
    }

    /// <summary>
    /// Get list of recent operations
    /// </summary>
    public async Task<PagedResult<OperationSummaryDto>> GetOperationsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var operations = await _unitOfWork.SODEODOperations.GetPagedAsync(page, pageSize, cancellationToken);

            var operationSummaries = operations.Items.Select(op => new OperationSummaryDto
            {
                OperationId = op.OperationCode,
                OperationType = op.OperationType,
                Status = op.Status,
                ProgressPercentage = CalculateProgress(op),
                StartTime = op.StartTime,
                EstimatedEndTime = CalculateEstimatedEndTime(op),
                CurrentStep = GetCurrentStep(op)
            }).ToArray();

            return new PagedResult<OperationSummaryDto>
            {
                Items = operationSummaries,
                TotalCount = operations.TotalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operations list");
            throw;
        }
    }

    // Helper methods

    private static int CalculateProgress(SODEODOperation operation)
    {
        return operation.Status switch
        {
            "Initiated" => 5,
            "Running" => 50,
            "Completed" => 100,
            "Failed" => 0,
            "Cancelled" => 0,
            _ => 0
        };
    }

    private static string GetCurrentStep(SODEODOperation operation)
    {
        if (operation.Status == "Completed")
            return "Completed";
        if (operation.Status == "Failed")
            return "Failed";
        if (operation.Status == "Cancelled")
            return "Cancelled";

        return operation.OperationType == "SOD" ? "SOD Processing" : "EOD Processing";
    }

    private static DateTime? CalculateEstimatedEndTime(SODEODOperation operation)
    {
        if (operation.EndTime.HasValue)
            return operation.EndTime;

        if (operation.Status != "Running")
            return null;

        var estimatedDuration = operation.OperationType == "SOD" ? 15 : 75; // minutes
        return operation.StartTime.AddMinutes(estimatedDuration);
    }

    private async Task<OperationStepDto[]> GetOperationStepsAsync(int operationId, CancellationToken cancellationToken)
    {
        try
        {
            var steps = await _unitOfWork.OperationSteps.GetByOperationIdAsync(operationId, cancellationToken);

            return steps.Select(step => new OperationStepDto
            {
                Name = step.StepName,
                Status = step.Status,
                StartTime = step.StartTime,
                EndTime = step.EndTime,
                Details = step.Details,
                ErrorMessage = step.ErrorMessage
            }).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get operation steps for operation {OperationId}", operationId);
            return Array.Empty<OperationStepDto>();
        }
    }
}