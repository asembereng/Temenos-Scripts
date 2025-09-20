using System.Management.Automation;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Api.Services;

public interface IPowerShellService
{
    Task<ICheckResult> ExecuteCheckAsync(string moduleName, string functionName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    Task<object?> ExecuteScriptAsync(string scriptPath, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
}

public class PowerShellService : IPowerShellService
{
    private readonly ILogger<PowerShellService> _logger;
    private readonly string _moduleBasePath;

    public PowerShellService(ILogger<PowerShellService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _moduleBasePath = configuration["PowerShell:ModuleBasePath"] ?? "/scripts/PowerShell/Modules";
    }

    public async Task<ICheckResult> ExecuteCheckAsync(string moduleName, string functionName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executing PowerShell check: {ModuleName}.{FunctionName}", moduleName, functionName);

            using var powerShell = PowerShell.Create();
            
            // Import the module
            var modulePath = Path.Combine(_moduleBasePath, moduleName, $"{moduleName}.psm1");
            powerShell.AddCommand("Import-Module").AddParameter("Name", modulePath);
            await powerShell.InvokeAsync();

            // Clear any previous commands and add the function call
            powerShell.Commands.Clear();
            powerShell.AddCommand(functionName);

            // Add parameters
            foreach (var parameter in parameters)
            {
                powerShell.AddParameter(parameter.Key, parameter.Value);
            }

            var results = await powerShell.InvokeAsync();
            
            if (powerShell.HadErrors)
            {
                var errors = string.Join("; ", powerShell.Streams.Error.Select(e => e.ToString()));
                _logger.LogError("PowerShell execution failed: {Errors}", errors);
                
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