using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Entities.Configuration;
using TemenosAlertManager.Core.Enums;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// Service for managing individual Temenos service operations
/// </summary>
public class ServiceManagementService : IServiceManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPowerShellService _powerShellService;
    private readonly ILogger<ServiceManagementService> _logger;

    public ServiceManagementService(
        IUnitOfWork unitOfWork,
        IPowerShellService powerShellService,
        ILogger<ServiceManagementService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Start a service
    /// </summary>
    public async Task<ServiceActionResultDto> StartServiceAsync(int serviceId, string initiatedBy, CancellationToken cancellationToken = default)
    {
        return await ExecuteServiceActionAsync(serviceId, "Start", initiatedBy, cancellationToken);
    }

    /// <summary>
    /// Stop a service
    /// </summary>
    public async Task<ServiceActionResultDto> StopServiceAsync(int serviceId, string initiatedBy, CancellationToken cancellationToken = default)
    {
        return await ExecuteServiceActionAsync(serviceId, "Stop", initiatedBy, cancellationToken);
    }

    /// <summary>
    /// Restart a service
    /// </summary>
    public async Task<ServiceActionResultDto> RestartServiceAsync(int serviceId, string initiatedBy, CancellationToken cancellationToken = default)
    {
        return await ExecuteServiceActionAsync(serviceId, "Restart", initiatedBy, cancellationToken);
    }

    /// <summary>
    /// Get comprehensive status of all services
    /// </summary>
    public async Task<ServiceStatusSummaryDto> GetServicesStatusAsync(string? domain = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting services status summary for domain: {Domain}", domain ?? "All");

            var serviceConfigs = await _unitOfWork.ServiceConfigs.GetAllAsync(cancellationToken);

            if (!string.IsNullOrEmpty(domain))
            {
                if (Enum.TryParse<MonitoringDomain>(domain, true, out var domainEnum))
                {
                    serviceConfigs = serviceConfigs.Where(sc => sc.Type == domainEnum).ToList();
                }
                else
                {
                    throw new ArgumentException($"Invalid domain: {domain}");
                }
            }

            var services = new List<ServiceStatusDto>();
            var domainGroups = new Dictionary<string, DomainStatusDto>();

            foreach (var serviceConfig in serviceConfigs)
            {
                var serviceStatus = await GetServiceStatusAsync(serviceConfig, cancellationToken);
                services.Add(serviceStatus);

                // Group by domain
                var domainName = serviceConfig.Type.ToString();
                if (!domainGroups.ContainsKey(domainName))
                {
                    domainGroups[domainName] = new DomainStatusDto
                    {
                        Domain = domainName,
                        TotalServices = 0,
                        HealthyServices = 0,
                        UnhealthyServices = 0
                    };
                }

                domainGroups[domainName].TotalServices++;
                if (serviceStatus.Status == "Running")
                {
                    domainGroups[domainName].HealthyServices++;
                }
                else
                {
                    domainGroups[domainName].UnhealthyServices++;
                }
            }

            // Calculate health percentages
            foreach (var domainStatus in domainGroups.Values)
            {
                domainStatus.HealthPercentage = domainStatus.TotalServices > 0 
                    ? (double)domainStatus.HealthyServices / domainStatus.TotalServices * 100 
                    : 0;
            }

            // Get active operations
            var activeSOD = await GetActiveOperationAsync("SOD", cancellationToken);
            var activeEOD = await GetActiveOperationAsync("EOD", cancellationToken);

            return new ServiceStatusSummaryDto
            {
                LastUpdated = DateTime.UtcNow,
                Services = services.ToArray(),
                DomainStatus = domainGroups.Values.ToArray(),
                ActiveSODOperation = activeSOD,
                ActiveEODOperation = activeEOD
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get services status summary");
            throw;
        }
    }

    /// <summary>
    /// Get action history for a specific service
    /// </summary>
    public async Task<PagedResult<ServiceActionDto>> GetServiceActionsAsync(int serviceId, PagingDto paging, CancellationToken cancellationToken = default)
    {
        try
        {
            var actions = await _unitOfWork.ServiceActions.GetByServiceIdPagedAsync(serviceId, paging.Page, paging.PageSize, cancellationToken);

            var actionDtos = actions.Items.Select(action => new ServiceActionDto
            {
                Id = action.Id,
                Action = action.Action,
                InitiatedBy = action.InitiatedBy,
                StartTime = action.StartTime,
                EndTime = action.EndTime,
                Status = action.Status,
                Result = action.Result,
                ErrorMessage = action.ErrorMessage
            }).ToArray();

            return new PagedResult<ServiceActionDto>
            {
                Items = actionDtos,
                TotalCount = actions.TotalCount,
                Page = paging.Page,
                PageSize = paging.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service actions for service {ServiceId}", serviceId);
            throw;
        }
    }

    // Helper methods

    private async Task<ServiceActionResultDto> ExecuteServiceActionAsync(int serviceId, string action, string initiatedBy, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        ServiceConfig? serviceConfig = null;
        ServiceAction? serviceAction = null;

        try
        {
            _logger.LogInformation("Executing {Action} action for service {ServiceId} by {User}", action, serviceId, initiatedBy);

            // Get service configuration
            serviceConfig = await _unitOfWork.ServiceConfigs.GetByIdAsync(serviceId, cancellationToken);
            if (serviceConfig == null)
            {
                throw new ArgumentException($"Service with ID {serviceId} not found");
            }

            // Create service action record
            serviceAction = new ServiceAction
            {
                ServiceConfigId = serviceId,
                Action = action,
                InitiatedBy = initiatedBy,
                StartTime = startTime,
                Status = "Running"
            };

            await _unitOfWork.ServiceActions.AddAsync(serviceAction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Execute PowerShell action
            var parameters = new Dictionary<string, object>
            {
                ["TimeoutSeconds"] = action == "Restart" ? 600 : 300
            };

            var result = await _powerShellService.ExecuteServiceActionAsync(
                action, 
                serviceConfig.Name, 
                serviceConfig.Host, 
                parameters, 
                cancellationToken);

            // Update service action with result
            serviceAction.Status = "Completed";
            serviceAction.EndTime = DateTime.UtcNow;
            serviceAction.Result = result?.ToString();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("{Action} action completed successfully for service {ServiceName} in {Duration}ms", 
                action, serviceConfig.Name, duration);

            return new ServiceActionResultDto
            {
                ServiceId = serviceId,
                ServiceName = serviceConfig.Name,
                Action = action,
                Status = "Completed",
                Message = $"{action} action completed successfully",
                StartTime = startTime,
                DurationMs = (int)duration
            };
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;

            _logger.LogError(ex, "Failed to execute {Action} action for service {ServiceId}", action, serviceId);

            // Update service action with error
            if (serviceAction != null)
            {
                serviceAction.Status = "Failed";
                serviceAction.EndTime = endTime;
                serviceAction.ErrorMessage = ex.Message;
                
                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Failed to save service action failure for service {ServiceId}", serviceId);
                }
            }

            return new ServiceActionResultDto
            {
                ServiceId = serviceId,
                ServiceName = serviceConfig?.Name ?? "Unknown",
                Action = action,
                Status = "Failed",
                Message = $"{action} action failed: {ex.Message}",
                StartTime = startTime,
                DurationMs = (int)duration
            };
        }
    }

    private async Task<ServiceStatusDto> GetServiceStatusAsync(ServiceConfig serviceConfig, CancellationToken cancellationToken)
    {
        try
        {
            // Execute health check
            var healthResult = await _powerShellService.ExecuteServiceActionAsync(
                "HealthCheck", 
                serviceConfig.Name, 
                serviceConfig.Host, 
                null, 
                cancellationToken);

            var status = "Unknown";
            var lastChecked = DateTime.UtcNow;

            // Parse health check result
            if (healthResult != null)
            {
                // Simplified status determination
                status = "Running"; // Default to running if health check succeeds
            }

            return new ServiceStatusDto
            {
                Id = serviceConfig.Id,
                Name = serviceConfig.Name,
                Host = serviceConfig.Host,
                Type = serviceConfig.Type.ToString(),
                Status = status,
                LastChecked = lastChecked,
                CanStart = true, // This would be determined by user permissions
                CanStop = true,
                CanRestart = true,
                IsCriticalForSOD = serviceConfig.IsCriticalForSOD,
                IsCriticalForEOD = serviceConfig.IsCriticalForEOD
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get status for service {ServiceName} on {Host}", 
                serviceConfig.Name, serviceConfig.Host);

            return new ServiceStatusDto
            {
                Id = serviceConfig.Id,
                Name = serviceConfig.Name,
                Host = serviceConfig.Host,
                Type = serviceConfig.Type.ToString(),
                Status = "Error",
                LastChecked = DateTime.UtcNow,
                CanStart = true,
                CanStop = true,
                CanRestart = true,
                IsCriticalForSOD = serviceConfig.IsCriticalForSOD,
                IsCriticalForEOD = serviceConfig.IsCriticalForEOD
            };
        }
    }

    private async Task<OperationSummaryDto?> GetActiveOperationAsync(string operationType, CancellationToken cancellationToken)
    {
        try
        {
            var activeOperation = await _unitOfWork.SODEODOperations.GetActiveByTypeAsync(operationType, cancellationToken);

            if (activeOperation == null)
                return null;

            var progressPercentage = activeOperation.Status switch
            {
                "Initiated" => 5,
                "Running" => 50,
                "Completed" => 100,
                _ => 0
            };

            var estimatedDuration = operationType == "SOD" ? 15 : 75; // minutes
            var estimatedEndTime = activeOperation.StartTime.AddMinutes(estimatedDuration);

            return new OperationSummaryDto
            {
                OperationId = activeOperation.OperationCode,
                OperationType = activeOperation.OperationType,
                Status = activeOperation.Status,
                ProgressPercentage = progressPercentage,
                StartTime = activeOperation.StartTime,
                EstimatedEndTime = estimatedEndTime,
                CurrentStep = operationType == "SOD" ? "SOD Processing" : "EOD Processing"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active {OperationType} operation", operationType);
            return null;
        }
    }
}