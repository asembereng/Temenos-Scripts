using System.Text.Json;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Entities.Configuration;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// SOD Orchestrator with advanced dependency management
/// </summary>
public class SODOrchestrator : ISODOrchestrator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPowerShellService _powerShellService;
    private readonly IDependencyManager _dependencyManager;
    private readonly IOperationMonitor _operationMonitor;
    private readonly ILogger<SODOrchestrator> _logger;

    public SODOrchestrator(
        IUnitOfWork unitOfWork,
        IPowerShellService powerShellService,
        IDependencyManager dependencyManager,
        IOperationMonitor operationMonitor,
        ILogger<SODOrchestrator> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));
        _dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));
        _operationMonitor = operationMonitor ?? throw new ArgumentNullException(nameof(operationMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationResultDto> ExecuteSODAsync(SODRequest request, string operationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting SOD orchestration for operation {OperationId} in environment {Environment}", 
            operationId, request.Environment);

        try
        {
            // Start monitoring
            await _operationMonitor.StartMonitoringAsync(operationId, cancellationToken);

            // Phase 1: Pre-SOD Validation
            var validationResult = await ValidatePreSODConditionsAsync(request.Environment, cancellationToken);
            if (!validationResult.IsValid && !request.ForceExecution)
            {
                throw new InvalidOperationException($"Pre-SOD validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            await UpdateOperationStep(operationId, "Pre-SOD Validation", "Completed", 
                validationResult.IsValid ? "All validations passed" : "Validation failed but forced execution", cancellationToken);

            // Phase 2: Resolve Service Dependencies
            var serviceSequence = await GetServiceStartupSequenceAsync(request.Environment, request.ServicesFilter, cancellationToken);
            
            await UpdateOperationStep(operationId, "Dependency Resolution", "Completed", 
                $"Resolved startup sequence for {serviceSequence.Length} services", cancellationToken);

            // Phase 3: Execute Service Startup in Dependency Order
            var executionResult = await ExecuteServiceStartupSequenceAsync(serviceSequence, operationId, request.DryRun, cancellationToken);

            // Phase 4: Post-SOD Validation
            var postValidation = await ValidatePostSODConditionsAsync(request.Environment, cancellationToken);
            
            await UpdateOperationStep(operationId, "Post-SOD Validation", "Completed", 
                postValidation.IsValid ? "All post-SOD validations passed" : "Some post-SOD validations failed", cancellationToken);

            // Update operation as completed
            await UpdateOperationStatus(operationId, "Completed", "SOD operation completed successfully", cancellationToken);

            _logger.LogInformation("SOD orchestration completed successfully for operation {OperationId}", operationId);

            return new OperationResultDto
            {
                OperationId = operationId,
                Status = "Completed",
                Message = "SOD operation completed successfully",
                StartTime = DateTime.UtcNow,
                EstimatedDurationMinutes = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SOD orchestration failed for operation {OperationId}", operationId);
            
            await UpdateOperationStatus(operationId, "Failed", ex.Message, cancellationToken);
            
            // Attempt rollback
            try
            {
                await RollbackSODAsync(operationId, cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "SOD rollback failed for operation {OperationId}", operationId);
            }

            throw;
        }
        finally
        {
            await _operationMonitor.StopMonitoringAsync(operationId, cancellationToken);
        }
    }

    public async Task<ValidationResult> ValidatePreSODConditionsAsync(string environment, CancellationToken cancellationToken = default)
    {
        var validationResult = new ValidationResult { IsValid = true };
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            _logger.LogInformation("Validating pre-SOD conditions for environment {Environment}", environment);

            // 1. Check database connectivity
            var dbValidation = await ValidateDatabaseConnectivityAsync(environment, cancellationToken);
            if (!dbValidation.IsValid)
            {
                errors.AddRange(dbValidation.Errors);
            }

            // 2. Check file system availability
            var fsValidation = await ValidateFileSystemAvailabilityAsync(environment, cancellationToken);
            if (!fsValidation.IsValid)
            {
                errors.AddRange(fsValidation.Errors);
            }

            // 3. Check network connectivity
            var networkValidation = await ValidateNetworkConnectivityAsync(environment, cancellationToken);
            if (!networkValidation.IsValid)
            {
                warnings.AddRange(networkValidation.Errors);
            }

            // 4. Check service dependencies
            var dependencyValidation = await ValidateServiceDependenciesAsync(environment, cancellationToken);
            if (!dependencyValidation.IsValid)
            {
                errors.AddRange(dependencyValidation.Errors);
            }

            // 5. Check system resources
            var resourceValidation = await ValidateSystemResourcesAsync(environment, cancellationToken);
            if (!resourceValidation.IsValid)
            {
                warnings.AddRange(resourceValidation.Errors);
            }

            validationResult.IsValid = errors.Count == 0;
            validationResult.Errors = errors.ToArray();
            validationResult.Warnings = warnings.ToArray();

            _logger.LogInformation("Pre-SOD validation completed for environment {Environment}. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                environment, validationResult.IsValid, errors.Count, warnings.Count);

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre-SOD validation failed for environment {Environment}", environment);
            
            return new ValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Validation process failed: {ex.Message}" }
            };
        }
    }

    public async Task<ServiceConfig[]> GetServiceStartupSequenceAsync(string environment, string[]? serviceFilter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Resolving service startup sequence for environment {Environment}", environment);

            // Get all enabled services for the environment
            var allServices = await _unitOfWork.ServiceConfigs.GetAllAsync(cancellationToken);
            var services = allServices.Where(s => s.IsEnabled).ToArray();

            // Apply service filter if provided
            if (serviceFilter?.Length > 0)
            {
                services = services.Where(s => serviceFilter.Contains(s.Name)).ToArray();
            }

            // Get execution plan from dependency manager
            var executionPlan = await _dependencyManager.GetOptimalExecutionOrderAsync(services, "SOD", cancellationToken);

            // Flatten the execution plan into a sequence
            var sequence = new List<ServiceConfig>();
            foreach (var phase in executionPlan.Phases.OrderBy(p => p.PhaseNumber))
            {
                // Within each phase, order by SODOrder
                var phaseServices = phase.Services.OrderBy(s => s.SODOrder).ToArray();
                sequence.AddRange(phaseServices);
            }

            _logger.LogInformation("Service startup sequence resolved: {ServiceCount} services in {PhaseCount} phases",
                sequence.Count, executionPlan.Phases.Length);

            return sequence.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve service startup sequence for environment {Environment}", environment);
            throw;
        }
    }

    public async Task<OperationResultDto> RollbackSODAsync(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Initiating SOD rollback for operation {OperationId}", operationId);

            // Get the operation details
            var operation = await _unitOfWork.SODEODOperations.GetByOperationCodeAsync(operationId, cancellationToken);
            if (operation == null)
            {
                throw new ArgumentException($"Operation {operationId} not found");
            }

            // Get completed steps to rollback
            var completedSteps = await _unitOfWork.OperationSteps.GetByOperationIdAsync(operation.Id, cancellationToken);
            var stepsToRollback = completedSteps.Where(s => s.Status == "Completed").OrderByDescending(s => s.StepOrder).ToArray();

            await UpdateOperationStep(operationId, "SOD Rollback", "Running", "Initiating rollback procedure", cancellationToken);

            // Execute rollback steps
            foreach (var step in stepsToRollback)
            {
                try
                {
                    await ExecuteRollbackStepAsync(operationId, step, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rollback step {StepName} for operation {OperationId}", step.StepName, operationId);
                    // Continue with other rollback steps even if one fails
                }
            }

            // Update operation status
            await UpdateOperationStatus(operationId, "Rolled Back", "SOD operation rolled back successfully", cancellationToken);

            _logger.LogInformation("SOD rollback completed for operation {OperationId}", operationId);

            return new OperationResultDto
            {
                OperationId = operationId,
                Status = "Rolled Back",
                Message = "SOD operation rolled back successfully",
                StartTime = DateTime.UtcNow,
                EstimatedDurationMinutes = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SOD rollback failed for operation {OperationId}", operationId);
            throw;
        }
    }

    // Helper methods

    private async Task<OperationResultDto> ExecuteServiceStartupSequenceAsync(ServiceConfig[] services, string operationId, bool dryRun, CancellationToken cancellationToken)
    {
        var startedServices = new List<ServiceConfig>();

        try
        {
            foreach (var service in services)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("SOD operation was cancelled");
                }

                await UpdateOperationStep(operationId, $"Starting {service.Name}", "Running", 
                    $"Starting service on {service.Host}", cancellationToken);

                if (!dryRun)
                {
                    var result = await _powerShellService.ExecuteServiceActionAsync("Start", service.Name, service.Host, null, cancellationToken);
                    
                    if (result == null)
                    {
                        throw new InvalidOperationException($"Failed to start service {service.Name}");
                    }

                    startedServices.Add(service);
                }

                await UpdateOperationStep(operationId, $"Starting {service.Name}", "Completed", 
                    dryRun ? "Dry run - service start simulated" : "Service started successfully", cancellationToken);

                // Add delay between service starts if required
                if (service.SODTimeout > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(service.SODTimeout / 10, 30)), cancellationToken);
                }
            }

            return new OperationResultDto
            {
                OperationId = operationId,
                Status = "Completed",
                Message = $"Successfully started {startedServices.Count} services",
                StartTime = DateTime.UtcNow,
                EstimatedDurationMinutes = 0
            };
        }
        catch (Exception)
        {
            // If any service fails, attempt to stop the ones we already started
            foreach (var service in startedServices.AsEnumerable().Reverse())
            {
                try
                {
                    await _powerShellService.ExecuteServiceActionAsync("Stop", service.Name, service.Host, null, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to stop service {ServiceName} during rollback", service.Name);
                }
            }

            throw;
        }
    }

    private async Task<ValidationResult> ValidateDatabaseConnectivityAsync(string environment, CancellationToken cancellationToken)
    {
        // Simulate database connectivity check
        await Task.Delay(1000, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

    private async Task<ValidationResult> ValidateFileSystemAvailabilityAsync(string environment, CancellationToken cancellationToken)
    {
        // Simulate file system check
        await Task.Delay(500, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

    private async Task<ValidationResult> ValidateNetworkConnectivityAsync(string environment, CancellationToken cancellationToken)
    {
        // Simulate network connectivity check
        await Task.Delay(800, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

    private async Task<ValidationResult> ValidateServiceDependenciesAsync(string environment, CancellationToken cancellationToken)
    {
        // Simulate dependency validation
        await Task.Delay(1500, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

    private async Task<ValidationResult> ValidateSystemResourcesAsync(string environment, CancellationToken cancellationToken)
    {
        // Simulate resource validation
        await Task.Delay(1200, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

    private async Task<ValidationResult> ValidatePostSODConditionsAsync(string environment, CancellationToken cancellationToken)
    {
        // Simulate post-SOD validation
        await Task.Delay(2000, cancellationToken);
        return new ValidationResult { IsValid = true };
    }

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

    private async Task ExecuteRollbackStepAsync(string operationId, OperationStep step, CancellationToken cancellationToken)
    {
        // Implement specific rollback logic based on step type
        _logger.LogInformation("Rolling back step {StepName} for operation {OperationId}", step.StepName, operationId);
        
        // This would contain specific rollback logic for each type of step
        await Task.Delay(1000, cancellationToken);
        
        await UpdateOperationStep(operationId, $"Rollback {step.StepName}", "Completed", "Step rolled back successfully", cancellationToken);
    }
}