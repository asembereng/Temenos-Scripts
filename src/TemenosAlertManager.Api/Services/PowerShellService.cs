using System.Management.Automation;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// PowerShell Service for Remote Temenos System Monitoring
/// 
/// This service orchestrates PowerShell-based monitoring of distributed Temenos environments.
/// Supports executing monitoring commands on remote hosts where T24, TPH, MQ, and SQL systems are deployed.
/// 
/// REMOTE DEPLOYMENT CAPABILITIES:
/// - Execute monitoring modules on target Temenos hosts via PowerShell remoting
/// - Handle authentication and session management for distributed systems
/// - Provide consistent error handling and logging across remote connections
/// - Support timeout and cancellation for reliable operations
/// 
/// ARCHITECTURE CONSIDERATIONS:
/// - PowerShell modules are loaded locally on Alert Manager host
/// - Actual monitoring commands execute on target Temenos hosts
/// - Results are serialized back to Alert Manager for processing
/// - Credentials and sessions are managed securely
/// </summary>
public interface IPowerShellService
{
    /// <summary>
    /// Execute a monitoring check on local or remote Temenos system
    /// </summary>
    /// <param name="moduleName">PowerShell module name (e.g., "TemenosChecks.TPH")</param>
    /// <param name="functionName">Function to execute (e.g., "Test-TphServices")</param>
    /// <param name="parameters">Parameters including target host information</param>
    /// <param name="cancellationToken">Cancellation support for long-running operations</param>
    /// <returns>Monitoring check result</returns>
    Task<ICheckResult> ExecuteCheckAsync(string moduleName, string functionName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute arbitrary PowerShell script on local or remote system
    /// </summary>
    Task<object?> ExecuteScriptAsync(string scriptPath, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute a SOD operation using PowerShell
    /// </summary>
    Task<object?> ExecuteSODAsync(string environment, string[] services, bool dryRun, string operationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute an EOD operation using PowerShell
    /// </summary>
    Task<object?> ExecuteEODAsync(string environment, string[] services, bool dryRun, DateTime? cutoffTime, string operationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute a service management action using PowerShell
    /// </summary>
    Task<object?> ExecuteServiceActionAsync(string action, string serviceName, string host, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of PowerShell service with remote deployment support
/// </summary>
public class PowerShellService : IPowerShellService
{
    private readonly ILogger<PowerShellService> _logger;
    private readonly string _moduleBasePath;

    public PowerShellService(ILogger<PowerShellService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Load PowerShell module path from configuration
        // Modules are stored locally on Alert Manager host but execute remotely
        _moduleBasePath = configuration["PowerShell:ModuleBasePath"] ?? "/scripts/PowerShell/Modules";
        
        _logger.LogInformation("PowerShell Service initialized with module path: {ModuleBasePath}", _moduleBasePath);
    }

    /// <summary>
    /// Execute a monitoring check on local or remote Temenos system
    /// 
    /// This method coordinates the execution of PowerShell-based monitoring checks across distributed
    /// Temenos environments. The monitoring modules handle remote execution internally using PowerShell remoting.
    /// 
    /// REMOTE EXECUTION FLOW:
    /// 1. Load monitoring module on local Alert Manager host
    /// 2. Module functions handle remote connections to target Temenos systems
    /// 3. Monitoring commands execute on target hosts via PowerShell remoting
    /// 4. Results are returned to Alert Manager for processing and alerting
    /// 
    /// PARAMETER EXPECTATIONS:
    /// - ServerName/Host: Target Temenos system hostname or IP
    /// - Credential: Authentication for remote systems (optional)
    /// - Module-specific parameters: Thresholds, service names, etc.
    /// 
    /// ERROR HANDLING:
    /// - Network connectivity issues to remote hosts
    /// - Authentication failures on target systems
    /// - PowerShell remoting configuration problems
    /// - Module-specific execution errors
    /// </summary>
    public async Task<ICheckResult> ExecuteCheckAsync(string moduleName, string functionName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract target information for logging
            var targetHost = parameters.ContainsKey("ServerName") ? parameters["ServerName"]?.ToString() : 
                           parameters.ContainsKey("Host") ? parameters["Host"]?.ToString() : "localhost";
            
            _logger.LogDebug("Executing PowerShell check: {ModuleName}.{FunctionName} on target: {TargetHost}", 
                moduleName, functionName, targetHost);

            using var powerShell = PowerShell.Create();
            
            // STEP 1: Import monitoring module on Alert Manager host
            // Module contains functions that handle remote connections internally
            var modulePath = Path.Combine(_moduleBasePath, moduleName, $"{moduleName}.psm1");
            
            if (!File.Exists(modulePath))
            {
                var error = $"PowerShell module not found: {modulePath}";
                _logger.LogError(error);
                return new CheckResult(MonitoringDomain.Host, targetHost ?? "unknown", functionName, 
                    CheckStatus.Error, null, null, error, 0);
            }
            
            _logger.LogDebug("Loading PowerShell module: {ModulePath}", modulePath);
            powerShell.AddCommand("Import-Module").AddParameter("Name", modulePath);
            await powerShell.InvokeAsync();

            // STEP 2: Prepare function call with parameters
            // Parameters include target host information for remote execution
            powerShell.Commands.Clear();
            powerShell.AddCommand(functionName);

            // Add all parameters - monitoring modules will handle remote execution
            foreach (var parameter in parameters)
            {
                _logger.LogDebug("Adding parameter: {Key} = {Value}", parameter.Key, 
                    parameter.Key.ToLower().Contains("credential") ? "[CREDENTIAL]" : parameter.Value);
                powerShell.AddParameter(parameter.Key, parameter.Value);
            }

            // STEP 3: Execute monitoring function
            // Function handles remote connection and execution internally
            _logger.LogDebug("Executing monitoring function: {FunctionName}", functionName);
            var results = await powerShell.InvokeAsync();
            
            // STEP 4: Handle execution results and errors
            if (powerShell.HadErrors)
            {
                var errors = string.Join("; ", powerShell.Streams.Error.Select(e => e.ToString()));
                _logger.LogError("PowerShell execution failed for {ModuleName}.{FunctionName} on {TargetHost}: {Errors}", 
                    moduleName, functionName, targetHost, errors);
                
                return new CheckResult(MonitoringDomain.Host, "PowerShell", functionName, CheckStatus.Error, null, null, errors, 0);
            }

            if (results.Count > 0)
            {
                var result = results[0];
                // Assume the PowerShell function returns a properly formatted check result
                return ConvertToCheckResult(result);
            }

            return new CheckResult(MonitoringDomain.Host, "PowerShell", functionName, CheckStatus.Success, "No results", null, null, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute PowerShell check {ModuleName}.{FunctionName}", moduleName, functionName);
            return new CheckResult(MonitoringDomain.Host, "PowerShell", functionName, CheckStatus.Error, null, null, ex.Message, 0);
        }
    }

    public async Task<object?> ExecuteScriptAsync(string scriptPath, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executing PowerShell script: {ScriptPath}", scriptPath);

            using var powerShell = PowerShell.Create();
            powerShell.AddScript(File.ReadAllText(scriptPath));

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    powerShell.AddParameter(parameter.Key, parameter.Value);
                }
            }

            var results = await powerShell.InvokeAsync();
            
            if (powerShell.HadErrors)
            {
                var errors = string.Join("; ", powerShell.Streams.Error.Select(e => e.ToString()));
                _logger.LogError("PowerShell script execution failed: {Errors}", errors);
                throw new InvalidOperationException($"PowerShell script execution failed: {errors}");
            }

            return results.Count > 0 ? results[0]?.BaseObject : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute PowerShell script {ScriptPath}", scriptPath);
            throw;
        }
    }

    private static ICheckResult ConvertToCheckResult(PSObject psObject)
    {
        // Convert PSObject to ICheckResult - assuming the PowerShell function returns the correct structure
        var properties = psObject.Properties;
        
        var domain = (MonitoringDomain)Enum.Parse(typeof(MonitoringDomain), properties["Domain"]?.Value?.ToString() ?? "Host");
        var target = properties["Target"]?.Value?.ToString() ?? "Unknown";
        var metric = properties["Metric"]?.Value?.ToString() ?? "Unknown";
        var status = (CheckStatus)Enum.Parse(typeof(CheckStatus), properties["Status"]?.Value?.ToString() ?? "Error");
        var value = properties["Value"]?.Value?.ToString();
        var details = properties["Details"]?.Value?.ToString();
        var errorMessage = properties["ErrorMessage"]?.Value?.ToString();
        var executionTimeMs = Convert.ToDouble(properties["ExecutionTimeMs"]?.Value ?? 0);

        return new CheckResult(domain, target, metric, status, value, details, errorMessage, executionTimeMs);
    }

    /// <summary>
    /// Execute a SOD operation using PowerShell
    /// </summary>
    public async Task<object?> ExecuteSODAsync(string environment, string[] services, bool dryRun, string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting SOD operation for environment: {Environment}, OperationId: {OperationId}", environment, operationId);

            var parameters = new Dictionary<string, object>
            {
                ["Environment"] = environment,
                ["Services"] = services,
                ["DryRun"] = dryRun,
                ["OperationId"] = operationId
            };

            return await ExecuteCheckAsync("TemenosChecks.SOD", "Start-TemenosSOD", parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute SOD operation for environment: {Environment}", environment);
            throw;
        }
    }

    /// <summary>
    /// Execute an EOD operation using PowerShell
    /// </summary>
    public async Task<object?> ExecuteEODAsync(string environment, string[] services, bool dryRun, DateTime? cutoffTime, string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting EOD operation for environment: {Environment}, OperationId: {OperationId}", environment, operationId);

            var parameters = new Dictionary<string, object>
            {
                ["Environment"] = environment,
                ["Services"] = services,
                ["DryRun"] = dryRun,
                ["OperationId"] = operationId
            };

            if (cutoffTime.HasValue)
            {
                parameters["CutoffTime"] = cutoffTime.Value;
            }

            return await ExecuteCheckAsync("TemenosChecks.SOD", "Start-TemenosEOD", parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute EOD operation for environment: {Environment}", environment);
            throw;
        }
    }

    /// <summary>
    /// Execute a service management action using PowerShell
    /// </summary>
    public async Task<object?> ExecuteServiceActionAsync(string action, string serviceName, string host, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing service action: {Action} on service: {ServiceName} at host: {Host}", action, serviceName, host);

            var actionParameters = new Dictionary<string, object>
            {
                ["ServiceName"] = serviceName,
                ["ComputerName"] = host,
                ["Action"] = action
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    actionParameters[param.Key] = param.Value;
                }
            }

            var functionName = action.ToLower() switch
            {
                "start" => "Start-TemenosService",
                "stop" => "Stop-TemenosService", 
                "restart" => "Restart-TemenosService",
                "healthcheck" => "Test-TemenosServiceHealth",
                _ => throw new ArgumentException($"Unsupported service action: {action}")
            };

            return await ExecuteCheckAsync("TemenosChecks.ServiceManagement", functionName, actionParameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute service action: {Action} on service: {ServiceName}", action, serviceName);
            throw;
        }
    }
}

// Simple implementation of ICheckResult for PowerShell results
public class CheckResult : ICheckResult
{
    public CheckResult(MonitoringDomain domain, string target, string metric, CheckStatus status, 
        string? value, string? details, string? errorMessage, double? executionTimeMs)
    {
        Domain = domain;
        Target = target;
        Metric = metric;
        Status = status;
        Value = value;
        Details = details;
        ErrorMessage = errorMessage;
        ExecutionTimeMs = executionTimeMs;
    }

    public MonitoringDomain Domain { get; }
    public string Target { get; }
    public string Metric { get; }
    public string? Value { get; }
    public CheckStatus Status { get; }
    public string? Details { get; }
    public string? ErrorMessage { get; }
    public double? ExecutionTimeMs { get; }
}