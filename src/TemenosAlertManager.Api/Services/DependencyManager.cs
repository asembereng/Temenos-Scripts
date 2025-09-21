using System.Text.Json;
using TemenosAlertManager.Core.Entities.Configuration;
using TemenosAlertManager.Core.Interfaces;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// Dependency manager for handling service dependencies and execution ordering
/// </summary>
public class DependencyManager : IDependencyManager
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DependencyManager> _logger;

    public DependencyManager(IUnitOfWork unitOfWork, ILogger<DependencyManager> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ServiceDependencyGraph> ResolveServiceDependenciesAsync(string environment, string operationType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Resolving service dependencies for environment {Environment} and operation type {OperationType}", 
                environment, operationType);

            var services = await _unitOfWork.ServiceConfigs.GetAllAsync(cancellationToken);
            var filteredServices = services.Where(s => s.IsEnabled).ToArray();

            var nodes = new List<ServiceNode>();
            var dependencies = new List<ServiceDependency>();

            // Create service nodes
            foreach (var service in filteredServices)
            {
                var node = new ServiceNode
                {
                    ServiceId = service.Id,
                    ServiceName = service.Name,
                    ServiceType = service.Type.ToString(),
                    IsCritical = operationType == "SOD" ? service.IsCriticalForSOD : service.IsCriticalForEOD,
                    EstimatedDuration = TimeSpan.FromSeconds(operationType == "SOD" ? service.SODTimeout : service.EODTimeout),
                    DependencyLevel = 0 // Will be calculated later
                };
                nodes.Add(node);
            }

            // Parse and create dependencies
            foreach (var service in filteredServices)
            {
                var dependencyJson = operationType == "SOD" ? service.SODDependencies : service.EODDependencies;
                
                if (!string.IsNullOrEmpty(dependencyJson))
                {
                    try
                    {
                        var dependencyIds = JsonSerializer.Deserialize<int[]>(dependencyJson);
                        if (dependencyIds != null)
                        {
                            foreach (var dependencyId in dependencyIds)
                            {
                                var dependentService = filteredServices.FirstOrDefault(s => s.Id == dependencyId);
                                if (dependentService != null)
                                {
                                    dependencies.Add(new ServiceDependency
                                    {
                                        FromServiceId = service.Id,
                                        ToServiceId = dependencyId,
                                        DependencyType = "Hard", // Default to hard dependency
                                        Condition = "Running"
                                    });
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse dependencies for service {ServiceName}", service.Name);
                    }
                }
            }

            // Calculate dependency levels
            CalculateDependencyLevels(nodes, dependencies);

            // Check for circular dependencies
            var hasCircularDependencies = DetectCircularDependencies(nodes, dependencies);

            var graph = new ServiceDependencyGraph
            {
                Nodes = nodes.ToArray(),
                Dependencies = dependencies.ToArray(),
                MaxDepth = nodes.Any() ? nodes.Max(n => n.DependencyLevel) : 0,
                HasCircularDependencies = hasCircularDependencies
            };

            _logger.LogInformation("Service dependency graph resolved: {NodeCount} nodes, {DependencyCount} dependencies, Max depth: {MaxDepth}, Circular: {HasCircular}",
                nodes.Count, dependencies.Count, graph.MaxDepth, hasCircularDependencies);

            return graph;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve service dependencies for environment {Environment}", environment);
            throw;
        }
    }

    public async Task<ValidationResult> ValidateDependencyConstraintsAsync(ServiceConfig[] services, string operationType, CancellationToken cancellationToken = default)
    {
        var validationResult = new ValidationResult { IsValid = true };
        var errors = new List<string>();

        try
        {
            _logger.LogInformation("Validating dependency constraints for {ServiceCount} services and operation type {OperationType}", 
                services.Length, operationType);

            // Build dependency graph for validation
            var graph = await ResolveServiceDependenciesAsync("", operationType, cancellationToken);

            // Check for circular dependencies
            if (graph.HasCircularDependencies)
            {
                errors.Add("Circular dependencies detected in service configuration");
            }

            // Validate each service's dependencies
            foreach (var service in services)
            {
                var dependencyJson = operationType == "SOD" ? service.SODDependencies : service.EODDependencies;
                
                if (!string.IsNullOrEmpty(dependencyJson))
                {
                    try
                    {
                        var dependencyIds = JsonSerializer.Deserialize<int[]>(dependencyJson);
                        if (dependencyIds != null)
                        {
                            foreach (var dependencyId in dependencyIds)
                            {
                                var dependentService = services.FirstOrDefault(s => s.Id == dependencyId);
                                if (dependentService == null)
                                {
                                    errors.Add($"Service {service.Name} depends on service ID {dependencyId} which is not found or not enabled");
                                }
                                else if (!dependentService.IsEnabled)
                                {
                                    errors.Add($"Service {service.Name} depends on service {dependentService.Name} which is disabled");
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        errors.Add($"Invalid dependency configuration for service {service.Name}");
                    }
                }
            }

            // Check for orphaned critical services
            var criticalServices = services.Where(s => 
                operationType == "SOD" ? s.IsCriticalForSOD : s.IsCriticalForEOD).ToArray();

            foreach (var criticalService in criticalServices)
            {
                var node = graph.Nodes.FirstOrDefault(n => n.ServiceId == criticalService.Id);
                if (node != null && node.DependencyLevel > 3)
                {
                    errors.Add($"Critical service {criticalService.Name} has very deep dependency chain (level {node.DependencyLevel})");
                }
            }

            validationResult.IsValid = errors.Count == 0;
            validationResult.Errors = errors.ToArray();

            _logger.LogInformation("Dependency constraint validation completed. Valid: {IsValid}, Errors: {ErrorCount}",
                validationResult.IsValid, errors.Count);

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate dependency constraints");
            
            return new ValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Dependency validation failed: {ex.Message}" }
            };
        }
    }

    public async Task<ServiceExecutionPlan> GetOptimalExecutionOrderAsync(ServiceConfig[] services, string operationType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating optimal execution order for {ServiceCount} services and operation type {OperationType}", 
                services.Length, operationType);

            var graph = await ResolveServiceDependenciesAsync("", operationType, cancellationToken);

            if (graph.HasCircularDependencies)
            {
                throw new InvalidOperationException("Cannot create execution plan due to circular dependencies");
            }

            var phases = new List<ExecutionPhase>();
            var remainingNodes = graph.Nodes.Where(n => services.Any(s => s.Id == n.ServiceId)).ToList();

            var phaseNumber = 1;

            while (remainingNodes.Any())
            {
                // Find nodes that have no dependencies or all dependencies are already processed
                var readyNodes = remainingNodes.Where(node =>
                {
                    var nodeDependencies = graph.Dependencies.Where(d => d.FromServiceId == node.ServiceId);
                    return !nodeDependencies.Any() || nodeDependencies.All(dep =>
                        !remainingNodes.Any(rn => rn.ServiceId == dep.ToServiceId));
                }).ToArray();

                if (!readyNodes.Any())
                {
                    // Handle deadlock by processing remaining nodes with warning
                    _logger.LogWarning("Dependency deadlock detected, processing remaining {NodeCount} nodes in single phase", remainingNodes.Count);
                    readyNodes = remainingNodes.ToArray();
                }

                // Group services that can run in parallel
                var phaseServices = readyNodes.Select(node => services.First(s => s.Id == node.ServiceId)).ToArray();

                // Calculate phase duration (max of all service durations in the phase)
                var phaseDuration = readyNodes.Any() 
                    ? TimeSpan.FromSeconds(readyNodes.Max(n => n.EstimatedDuration.TotalSeconds))
                    : TimeSpan.Zero;

                var phase = new ExecutionPhase
                {
                    PhaseNumber = phaseNumber,
                    PhaseName = $"Phase {phaseNumber}",
                    Services = phaseServices,
                    CanExecuteInParallel = DetermineParallelExecution(phaseServices),
                    EstimatedDuration = phaseDuration
                };

                phases.Add(phase);

                // Remove processed nodes
                foreach (var node in readyNodes)
                {
                    remainingNodes.Remove(node);
                }

                phaseNumber++;

                // Safety check to prevent infinite loops
                if (phaseNumber > 50)
                {
                    _logger.LogError("Execution plan creation exceeded maximum phases, breaking loop");
                    break;
                }
            }

            var executionPlan = new ServiceExecutionPlan
            {
                Phases = phases.ToArray(),
                EstimatedTotalDuration = CalculateTotalDuration(phases),
                TotalServices = services.Length,
                CriticalServices = services.Count(s => operationType == "SOD" ? s.IsCriticalForSOD : s.IsCriticalForEOD)
            };

            _logger.LogInformation("Execution plan created: {PhaseCount} phases, {TotalDuration} estimated duration, {CriticalServices} critical services",
                phases.Count, executionPlan.EstimatedTotalDuration, executionPlan.CriticalServices);

            return executionPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create optimal execution order");
            throw;
        }
    }

    // Helper methods

    private void CalculateDependencyLevels(List<ServiceNode> nodes, List<ServiceDependency> dependencies)
    {
        var maxIterations = nodes.Count * 2; // Prevent infinite loops
        var iteration = 0;
        bool changed;

        do
        {
            changed = false;
            iteration++;

            foreach (var node in nodes)
            {
                var dependentNodes = dependencies
                    .Where(d => d.FromServiceId == node.ServiceId)
                    .Select(d => nodes.FirstOrDefault(n => n.ServiceId == d.ToServiceId))
                    .Where(n => n != null)
                    .ToArray();

                if (dependentNodes.Any())
                {
                    var maxDependentLevel = dependentNodes.Max(n => n.DependencyLevel);
                    var newLevel = maxDependentLevel + 1;

                    if (newLevel != node.DependencyLevel)
                    {
                        node.DependencyLevel = newLevel;
                        changed = true;
                    }
                }
            }
        } while (changed && iteration < maxIterations);

        if (iteration >= maxIterations)
        {
            _logger.LogWarning("Dependency level calculation may not have converged after {Iterations} iterations", iteration);
        }
    }

    private bool DetectCircularDependencies(List<ServiceNode> nodes, List<ServiceDependency> dependencies)
    {
        var visited = new HashSet<int>();
        var recursionStack = new HashSet<int>();

        foreach (var node in nodes)
        {
            if (!visited.Contains(node.ServiceId))
            {
                if (HasCircularDependencyRecursive(node.ServiceId, dependencies, visited, recursionStack))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasCircularDependencyRecursive(int serviceId, List<ServiceDependency> dependencies, HashSet<int> visited, HashSet<int> recursionStack)
    {
        visited.Add(serviceId);
        recursionStack.Add(serviceId);

        var serviceDependencies = dependencies.Where(d => d.FromServiceId == serviceId);

        foreach (var dependency in serviceDependencies)
        {
            if (!visited.Contains(dependency.ToServiceId))
            {
                if (HasCircularDependencyRecursive(dependency.ToServiceId, dependencies, visited, recursionStack))
                {
                    return true;
                }
            }
            else if (recursionStack.Contains(dependency.ToServiceId))
            {
                return true; // Circular dependency found
            }
        }

        recursionStack.Remove(serviceId);
        return false;
    }

    private bool DetermineParallelExecution(ServiceConfig[] services)
    {
        // Services can execute in parallel if all allow it and none require manual confirmation
        return services.All(s => s.AllowParallelExecution && !s.RequiresManualConfirmation);
    }

    private TimeSpan CalculateTotalDuration(List<ExecutionPhase> phases)
    {
        var totalDuration = TimeSpan.Zero;

        foreach (var phase in phases)
        {
            if (phase.CanExecuteInParallel)
            {
                // For parallel execution, add the maximum duration in the phase
                totalDuration = totalDuration.Add(phase.EstimatedDuration);
            }
            else
            {
                // For sequential execution, add the sum of all service durations
                var sequentialDuration = TimeSpan.FromSeconds(
                    phase.Services.Sum(s => s.SODTimeout > 0 ? s.SODTimeout : s.EODTimeout));
                totalDuration = totalDuration.Add(sequentialDuration);
            }
        }

        return totalDuration;
    }
}