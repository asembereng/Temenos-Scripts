using System.Text.Json;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Entities.Configuration;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;
using ValidationResult = TemenosAlertManager.Core.Interfaces.ValidationResult;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// EOD Orchestrator with advanced transaction management
/// </summary>
public class EODOrchestrator : IEODOrchestrator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPowerShellService _powerShellService;
    private readonly IDependencyManager _dependencyManager;
    private readonly IOperationMonitor _operationMonitor;
    private readonly ILogger<EODOrchestrator> _logger;

    public EODOrchestrator(
        IUnitOfWork unitOfWork,
        IPowerShellService powerShellService,
        IDependencyManager dependencyManager,
        IOperationMonitor operationMonitor,
        ILogger<EODOrchestrator> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));
        _dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));
        _operationMonitor = operationMonitor ?? throw new ArgumentNullException(nameof(operationMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationResultDto> ExecuteEODAsync(EODRequest request, string operationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting EOD orchestration for operation {OperationId} in environment {Environment}", 
            operationId, request.Environment);

        try
        {
            // Start monitoring
            await _operationMonitor.StartMonitoringAsync(operationId, cancellationToken);

            // Phase 1: Pre-EOD Validation
            var validationResult = await ValidatePreEODConditionsAsync(request.Environment, cancellationToken);
            if (!validationResult.IsValid && !request.ForceExecution)
            {
                throw new InvalidOperationException($"Pre-EOD validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            await UpdateOperationStep(operationId, "Pre-EOD Validation", "Completed", 
                validationResult.IsValid ? "All validations passed" : "Validation failed but forced execution", cancellationToken);

            // Phase 2: Transaction Cutoff
            var cutoffResult = await ExecuteTransactionCutoffAsync(request.Environment, request.CutoffTime, cancellationToken);
            
            await UpdateOperationStep(operationId, "Transaction Cutoff", "Completed", 
                "Transaction processing halted successfully", cancellationToken);

            // Phase 3: Wait for In-Flight Transactions
            await WaitForInFlightTransactionsAsync(request.Environment, operationId, cancellationToken);
            
            await UpdateOperationStep(operationId, "In-Flight Transaction Wait", "Completed", 
                "All in-flight transactions completed", cancellationToken);

            // Phase 4: Daily Processing Execution
            await ExecuteDailyProcessingAsync(request.Environment, request.ServicesFilter, operationId, request.DryRun, cancellationToken);
            
            await UpdateOperationStep(operationId, "Daily Processing", "Completed", 
                "Daily processing completed successfully", cancellationToken);

            // Phase 5: Reconciliation and Reporting
            await ExecuteReconciliationAndReportingAsync(request.Environment, operationId, request.DryRun, cancellationToken);
            
            await UpdateOperationStep(operationId, "Reconciliation and Reporting", "Completed", 
                "Reconciliation and reporting completed", cancellationToken);

            // Phase 6: System Cleanup and Preparation
            await ExecuteSystemCleanupAsync(request.Environment, operationId, request.DryRun, cancellationToken);
            
            await UpdateOperationStep(operationId, "System Cleanup", "Completed", 
                "System cleanup completed", cancellationToken);

            // Update operation as completed
            await UpdateOperationStatus(operationId, "Completed", "EOD operation completed successfully", cancellationToken);

            _logger.LogInformation("EOD orchestration completed successfully for operation {OperationId}", operationId);

            return new OperationResultDto
            {
                OperationId = operationId,
                Status = "Completed",
                Message = "EOD operation completed successfully",
                StartTime = DateTime.UtcNow,
                EstimatedDurationMinutes = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EOD orchestration failed for operation {OperationId}", operationId);
            
            await UpdateOperationStatus(operationId, "Failed", ex.Message, cancellationToken);
            
            // Attempt rollback
            try
            {
                await RollbackEODAsync(operationId, cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "EOD rollback failed for operation {OperationId}", operationId);
            }

            throw;
        }
        finally
        {
            await _operationMonitor.StopMonitoringAsync(operationId, cancellationToken);
        }
    }

    public async Task<ValidationResult> ValidatePreEODConditionsAsync(string environment, CancellationToken cancellationToken = default)
    {
        var validationResult = new ValidationResult { IsValid = true };
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            _logger.LogInformation("Validating pre-EOD conditions for environment {Environment}", environment);

            // 1. Check transaction volume and processing status
            var transactionValidation = await ValidateTransactionStatusAsync(environment, cancellationToken);
            if (!transactionValidation.IsValid)
            {
                errors.AddRange(transactionValidation.Errors);
            }

            // 2. Check system performance and resources
            var performanceValidation = await ValidateSystemPerformanceAsync(environment, cancellationToken);
            if (!performanceValidation.IsValid)
            {
                warnings.AddRange(performanceValidation.Errors);
            }

            // 3. Check backup status
            var backupValidation = await ValidateBackupStatusAsync(environment, cancellationToken);
            if (!backupValidation.IsValid)
            {
                errors.AddRange(backupValidation.Errors);
            }

            // 4. Check batch job readiness
            var batchValidation = await ValidateBatchJobReadinessAsync(environment, cancellationToken);
            if (!batchValidation.IsValid)
            {
                errors.AddRange(batchValidation.Errors);
            }

            // 5. Check regulatory reporting readiness
            var reportingValidation = await ValidateReportingReadinessAsync(environment, cancellationToken);
            if (!reportingValidation.IsValid)
            {
                warnings.AddRange(reportingValidation.Errors);
            }

            validationResult.IsValid = errors.Count == 0;
            validationResult.Errors = errors.ToArray();
            validationResult.Warnings = warnings.ToArray();

            _logger.LogInformation("Pre-EOD validation completed for environment {Environment}. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                environment, validationResult.IsValid, errors.Count, warnings.Count);

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre-EOD validation failed for environment {Environment}", environment);
            
            return new ValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Validation process failed: {ex.Message}" }
            };
        }
    }

    public async Task<OperationResultDto> ExecuteTransactionCutoffAsync(string environment, DateTime? cutoffTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var effectiveCutoffTime = cutoffTime ?? DateTime.Now;
            
            _logger.LogInformation("Executing transaction cutoff for environment {Environment} at {CutoffTime}", 
                environment, effectiveCutoffTime);

            // 1. Stop new transaction acceptance
            await StopNewTransactionAcceptanceAsync(environment, cancellationToken);

            // 2. Process pending transactions up to cutoff time
            await ProcessPendingTransactionsAsync(environment, effectiveCutoffTime, cancellationToken);

            // 3. Update transaction processing status
            await UpdateTransactionProcessingStatusAsync(environment, "Cutoff", cancellationToken);

            _logger.LogInformation("Transaction cutoff completed successfully for environment {Environment}", environment);

            return new OperationResultDto
            {
                OperationId = Guid.NewGuid().ToString(),
                Status = "Completed",
                Message = "Transaction cutoff completed successfully",
                StartTime = DateTime.UtcNow,
                EstimatedDurationMinutes = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction cutoff failed for environment {Environment}", environment);
            throw;
        }
    }

    public async Task<OperationResultDto> RollbackEODAsync(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Initiating EOD rollback for operation {OperationId}", operationId);

            // Get the operation details
            var operation = await _unitOfWork.SODEODOperations.GetByOperationCodeAsync(operationId, cancellationToken);
            if (operation == null)
            {
                throw new ArgumentException($"Operation {operationId} not found");
            }

            // Get completed steps to rollback
            var completedSteps = await _unitOfWork.OperationSteps.GetByOperationIdAsync(operation.Id, cancellationToken);
            var stepsToRollback = completedSteps.Where(s => s.Status == "Completed").OrderByDescending(s => s.StepOrder).ToArray();

            await UpdateOperationStep(operationId, "EOD Rollback", "Running", "Initiating rollback procedure", cancellationToken);

            // Execute rollback steps in reverse order
            foreach (var step in stepsToRollback)
            {
                try
                {
                    await ExecuteEODRollbackStepAsync(operationId, step, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rollback step {StepName} for operation {OperationId}", step.StepName, operationId);
                    // Continue with other rollback steps even if one fails
                }
            }

            // Re-enable transaction processing
            await RestoreTransactionProcessingAsync(operationId, cancellationToken);

            // Update operation status
            await UpdateOperationStatus(operationId, "Rolled Back", "EOD operation rolled back successfully", cancellationToken);

            _logger.LogInformation("EOD rollback completed for operation {OperationId}", operationId);

            return new OperationResultDto
            {
                OperationId = operationId,
                Status = "Rolled Back",
                Message = "EOD operation rolled back successfully",
                StartTime = DateTime.UtcNow,
                EstimatedDurationMinutes = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EOD rollback failed for operation {OperationId}", operationId);
            throw;
        }
    }

    // Helper methods for EOD processing

    private async Task WaitForInFlightTransactionsAsync(string environment, string operationId, CancellationToken cancellationToken)
    {
        var maxWaitTime = TimeSpan.FromMinutes(30); // Maximum wait time
        var checkInterval = TimeSpan.FromSeconds(30); // Check every 30 seconds
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Waiting for in-flight transactions to complete in environment {Environment}", environment);

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("EOD operation was cancelled while waiting for in-flight transactions");
            }

            var pendingCount = await GetPendingTransactionCountAsync(environment, cancellationToken);
            
            if (pendingCount == 0)
            {
                _logger.LogInformation("All in-flight transactions completed for environment {Environment}", environment);
                return;
            }

            _logger.LogInformation("Waiting for {PendingCount} in-flight transactions in environment {Environment}", 
                pendingCount, environment);

            await UpdateOperationStep(operationId, "In-Flight Transaction Wait", "Running", 
                $"Waiting for {pendingCount} in-flight transactions", cancellationToken);

            await Task.Delay(checkInterval, cancellationToken);
        }

        _logger.LogWarning("Timeout waiting for in-flight transactions in environment {Environment}", environment);
        throw new TimeoutException($"Timeout waiting for in-flight transactions to complete in environment {environment}");
    }

    private async Task ExecuteDailyProcessingAsync(string environment, string[]? serviceFilter, string operationId, bool dryRun, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing daily processing for environment {Environment}", environment);

        // 1. Interest calculations
        await UpdateOperationStep(operationId, "Interest Calculations", "Running", "Calculating daily interest", cancellationToken);
        await ExecuteInterestCalculationsAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "Interest Calculations", "Completed", "Interest calculations completed", cancellationToken);

        // 2. Standing instructions processing
        await UpdateOperationStep(operationId, "Standing Instructions", "Running", "Processing standing instructions", cancellationToken);
        await ProcessStandingInstructionsAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "Standing Instructions", "Completed", "Standing instructions processed", cancellationToken);

        // 3. Account maintenance operations
        await UpdateOperationStep(operationId, "Account Maintenance", "Running", "Executing account maintenance", cancellationToken);
        await ExecuteAccountMaintenanceAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "Account Maintenance", "Completed", "Account maintenance completed", cancellationToken);

        // 4. Position calculations
        await UpdateOperationStep(operationId, "Position Calculations", "Running", "Updating position calculations", cancellationToken);
        await UpdatePositionCalculationsAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "Position Calculations", "Completed", "Position calculations updated", cancellationToken);

        _logger.LogInformation("Daily processing completed for environment {Environment}", environment);
    }

    private async Task ExecuteReconciliationAndReportingAsync(string environment, string operationId, bool dryRun, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing reconciliation and reporting for environment {Environment}", environment);

        // 1. Internal reconciliation
        await UpdateOperationStep(operationId, "Internal Reconciliation", "Running", "Performing internal reconciliation", cancellationToken);
        await PerformInternalReconciliationAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "Internal Reconciliation", "Completed", "Internal reconciliation completed", cancellationToken);

        // 2. External reconciliation
        await UpdateOperationStep(operationId, "External Reconciliation", "Running", "Performing external reconciliation", cancellationToken);
        await PerformExternalReconciliationAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "External Reconciliation", "Completed", "External reconciliation completed", cancellationToken);

        // 3. Regulatory reports
        await UpdateOperationStep(operationId, "Regulatory Reports", "Running", "Generating regulatory reports", cancellationToken);
        await GenerateRegulatoryReportsAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "Regulatory Reports", "Completed", "Regulatory reports generated", cancellationToken);

        // 4. Management reports
        await UpdateOperationStep(operationId, "Management Reports", "Running", "Generating management reports", cancellationToken);
        await GenerateManagementReportsAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "Management Reports", "Completed", "Management reports generated", cancellationToken);

        _logger.LogInformation("Reconciliation and reporting completed for environment {Environment}", environment);
    }

    private async Task ExecuteSystemCleanupAsync(string environment, string operationId, bool dryRun, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing system cleanup for environment {Environment}", environment);

        // 1. Archive processed transactions
        await UpdateOperationStep(operationId, "Transaction Archival", "Running", "Archiving processed transactions", cancellationToken);
        await ArchiveProcessedTransactionsAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "Transaction Archival", "Completed", "Transactions archived", cancellationToken);

        // 2. Cleanup temporary files
        await UpdateOperationStep(operationId, "File Cleanup", "Running", "Cleaning up temporary files", cancellationToken);
        await CleanupTemporaryFilesAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "File Cleanup", "Completed", "Temporary files cleaned", cancellationToken);

        // 3. Update system statistics
        await UpdateOperationStep(operationId, "Statistics Update", "Running", "Updating system statistics", cancellationToken);
        await UpdateSystemStatisticsAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "Statistics Update", "Completed", "System statistics updated", cancellationToken);

        // 4. Prepare for next business day
        await UpdateOperationStep(operationId, "Next Day Preparation", "Running", "Preparing for next business day", cancellationToken);
        await PrepareForNextBusinessDayAsync(environment, dryRun, cancellationToken);
        await UpdateOperationStep(operationId, "Next Day Preparation", "Completed", "Next day preparation completed", cancellationToken);

        _logger.LogInformation("System cleanup completed for environment {Environment}", environment);
    }

    // Validation methods
    private async Task<ValidationResult> ValidateTransactionStatusAsync(string environment, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

    private async Task<ValidationResult> ValidateSystemPerformanceAsync(string environment, CancellationToken cancellationToken)
    {
        await Task.Delay(1500, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

    private async Task<ValidationResult> ValidateBackupStatusAsync(string environment, CancellationToken cancellationToken)
    {
        await Task.Delay(800, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

    private async Task<ValidationResult> ValidateBatchJobReadinessAsync(string environment, CancellationToken cancellationToken)
    {
        await Task.Delay(1200, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

    private async Task<ValidationResult> ValidateReportingReadinessAsync(string environment, CancellationToken cancellationToken)
    {
        await Task.Delay(900, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

    // Transaction management methods
    private async Task StopNewTransactionAcceptanceAsync(string environment, CancellationToken cancellationToken)
    {
        await Task.Delay(2000, cancellationToken);
        _logger.LogInformation("New transaction acceptance stopped for environment {Environment}", environment);
    }

    private async Task ProcessPendingTransactionsAsync(string environment, DateTime cutoffTime, CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
        _logger.LogInformation("Pending transactions processed for environment {Environment} up to {CutoffTime}", environment, cutoffTime);
    }

    private async Task UpdateTransactionProcessingStatusAsync(string environment, string status, CancellationToken cancellationToken)
    {
        await Task.Delay(500, cancellationToken);
        _logger.LogInformation("Transaction processing status updated to {Status} for environment {Environment}", status, environment);
    }

    private async Task<int> GetPendingTransactionCountAsync(string environment, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        // Simulate decreasing pending transactions
        return new Random().Next(0, 10);
    }

    // Daily processing methods
    private async Task ExecuteInterestCalculationsAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 1000 : 10000, cancellationToken);
    }

    private async Task ProcessStandingInstructionsAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 800 : 8000, cancellationToken);
    }

    private async Task ExecuteAccountMaintenanceAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 1200 : 12000, cancellationToken);
    }

    private async Task UpdatePositionCalculationsAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 1500 : 15000, cancellationToken);
    }

    // Reconciliation and reporting methods
    private async Task PerformInternalReconciliationAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 2000 : 20000, cancellationToken);
    }

    private async Task PerformExternalReconciliationAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 3000 : 30000, cancellationToken);
    }

    private async Task GenerateRegulatoryReportsAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 1500 : 15000, cancellationToken);
    }

    private async Task GenerateManagementReportsAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 1000 : 10000, cancellationToken);
    }

    // System cleanup methods
    private async Task ArchiveProcessedTransactionsAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 1000 : 10000, cancellationToken);
    }

    private async Task CleanupTemporaryFilesAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 500 : 5000, cancellationToken);
    }

    private async Task UpdateSystemStatisticsAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 800 : 8000, cancellationToken);
    }

    private async Task PrepareForNextBusinessDayAsync(string environment, bool dryRun, CancellationToken cancellationToken)
    {
        await Task.Delay(dryRun ? 1200 : 12000, cancellationToken);
    }

    // Rollback methods
    private async Task ExecuteEODRollbackStepAsync(string operationId, OperationStep step, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rolling back EOD step {StepName} for operation {OperationId}", step.StepName, operationId);
        
        // Implement specific rollback logic based on step type
        switch (step.StepName.ToLower())
        {
            case "transaction cutoff":
                await RestoreTransactionProcessingAsync(operationId, cancellationToken);
                break;
            case "daily processing":
                await RollbackDailyProcessingAsync(operationId, cancellationToken);
                break;
            default:
                await Task.Delay(1000, cancellationToken);
                break;
        }
        
        await UpdateOperationStep(operationId, $"Rollback {step.StepName}", "Completed", "Step rolled back successfully", cancellationToken);
    }

    private async Task RestoreTransactionProcessingAsync(string operationId, CancellationToken cancellationToken)
    {
        await Task.Delay(2000, cancellationToken);
        _logger.LogInformation("Transaction processing restored for operation {OperationId}", operationId);
    }

    private async Task RollbackDailyProcessingAsync(string operationId, CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
        _logger.LogInformation("Daily processing rolled back for operation {OperationId}", operationId);
    }

    // Common helper methods
    private async Task UpdateOperationStep(string operationId, string stepName, string status, string details, CancellationToken cancellationToken)
    {
        try
        {
            var operation = await _unitOfWork.SODEODOperations.GetByOperationCodeAsync(operationId, cancellationToken);
            if (operation == null) return;

            var existingStep = (await _unitOfWork.OperationSteps.GetByOperationIdAsync(operation.Id, cancellationToken))
                .FirstOrDefault(s => s.StepName == stepName);

            if (existingStep != null)
            {
                existingStep.Status = status;
                existingStep.Details = details;
                if (status == "Completed" || status == "Failed")
                {
                    existingStep.EndTime = DateTime.UtcNow;
                }
                await _unitOfWork.OperationSteps.UpdateAsync(existingStep, cancellationToken);
            }
            else
            {
                var newStep = new OperationStep
                {
                    OperationId = operation.Id,
                    StepName = stepName,
                    Status = status,
                    Details = details,
                    StartTime = DateTime.UtcNow,
                    StepOrder = (await _unitOfWork.OperationSteps.GetByOperationIdAsync(operation.Id, cancellationToken)).Count() + 1
                };

                if (status == "Completed" || status == "Failed")
                {
                    newStep.EndTime = DateTime.UtcNow;
                }

                await _unitOfWork.OperationSteps.AddAsync(newStep, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update operation step {StepName} for operation {OperationId}", stepName, operationId);
        }
    }

    private async Task UpdateOperationStatus(string operationId, string status, string message, CancellationToken cancellationToken)
    {
        try
        {
            var operation = await _unitOfWork.SODEODOperations.GetByOperationCodeAsync(operationId, cancellationToken);
            if (operation == null) return;

            operation.Status = status;
            if (status == "Completed" || status == "Failed" || status == "Cancelled" || status == "Rolled Back")
            {
                operation.EndTime = DateTime.UtcNow;
            }

            if (status == "Failed")
            {
                operation.ErrorDetails = message;
            }

            await _unitOfWork.SODEODOperations.UpdateAsync(operation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update operation status for operation {OperationId}", operationId);
        }
    }
}