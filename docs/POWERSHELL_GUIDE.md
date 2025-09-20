# PowerShell Module Development Guide

## Overview

The Temenos Alert Manager uses PowerShell modules to perform monitoring checks across different domains (TPH, T24, MQ, SQL Server). This modular approach provides flexibility, security, and maintainability.

## Module Architecture

### Standard Module Structure
```
TemenosChecks.{Domain}/
├── TemenosChecks.{Domain}.psd1    # Module manifest
├── TemenosChecks.{Domain}.psm1    # Main module file
├── Functions/                     # Individual function files (optional)
│   ├── Test-{Feature}.ps1
│   └── Get-{Feature}Status.ps1
├── Tests/                         # Pester tests
│   └── TemenosChecks.{Domain}.Tests.ps1
└── README.md                      # Module documentation
```

### Module Dependencies
All monitoring modules should depend on:
- `TemenosChecks.Common` - Base utilities and result formatting
- Domain-specific modules (e.g., `SqlServer` for SQL monitoring)

## Creating a New Module

### 1. Module Manifest (.psd1)
```powershell
@{
    RootModule = 'TemenosChecks.NewDomain.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'UNIQUE-GUID-HERE'
    Author = 'Temenos Alert Manager'
    CompanyName = 'Central Bank'
    Copyright = '(c) 2024 Central Bank. All rights reserved.'
    Description = 'Monitoring checks for New Domain'
    PowerShellVersion = '7.0'
    RequiredModules = @('TemenosChecks.Common')
    
    FunctionsToExport = @(
        'Test-NewDomainConnectivity',
        'Get-NewDomainStatus',
        'Test-NewDomainPerformance'
    )
    
    PrivateData = @{
        PSData = @{
            Tags = @('Temenos', 'Monitoring', 'Banking', 'NewDomain')
            ReleaseNotes = 'Initial release of New Domain monitoring module'
        }
    }
}
```

### 2. Module Implementation (.psm1)
```powershell
# Import required modules
using module TemenosChecks.Common

# Function to test connectivity
function Test-NewDomainConnectivity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Target,
        
        [int]$Port = 1234,
        
        [hashtable]$Thresholds = @{}
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Testing connectivity to $Target`:$Port" -Source 'NewDomainCheck'
        
        # Perform the actual check
        $isConnected = Test-NetworkConnectivity -TargetHost $Target -Port $Port
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        if ($isConnected) {
            $status = [CheckStatus]::Success
            $value = "Connected"
        } else {
            $status = [CheckStatus]::Critical
            $value = "Not Connected"
        }
        
        return New-CheckResult -Domain ([MonitoringDomain]::Host) -Target $Target -Metric 'Connectivity' -Status $status -Value $value -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "Connectivity test failed: $errorMessage" -Level 'Error' -Source 'NewDomainCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::Host) -Target $Target -Metric 'Connectivity' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Export functions
Export-ModuleMember -Function @(
    'Test-NewDomainConnectivity'
)
```

## Function Development Standards

### Function Naming Convention
- **Test-{Feature}**: Connectivity and availability checks
- **Get-{Feature}Status**: Status information retrieval
- **Get-{Feature}Metrics**: Performance metrics collection
- **Test-{Feature}Performance**: Performance validation

### Parameter Standards
All monitoring functions should accept:
```powershell
param(
    [Parameter(Mandatory)]
    [string]$Target,              # The target system/server/service to check
    
    [PSCredential]$Credential,   # Optional credentials for authentication
    
    [hashtable]$Thresholds = @{}, # Configurable thresholds for warnings/errors
    
    [int]$TimeoutSeconds = 30     # Timeout for the operation
)
```

### Error Handling Pattern
```powershell
$startTime = Get-Date

try {
    Write-CheckLog -Message "Starting check for $Target" -Source 'ModuleName'
    
    # Perform check logic here
    $result = Invoke-SomeOperation
    
    $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
    
    # Determine status based on thresholds
    $status = if ($result.Value -gt $Thresholds.Critical) {
        [CheckStatus]::Critical
    } elseif ($result.Value -gt $Thresholds.Warning) {
        [CheckStatus]::Warning
    } else {
        [CheckStatus]::Success
    }
    
    return New-CheckResult -Domain $Domain -Target $Target -Metric $MetricName -Status $status -Value $result.Value -ExecutionTimeMs $executionTime
}
catch {
    $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
    $errorMessage = $_.Exception.Message
    
    Write-CheckLog -Message "Check failed: $errorMessage" -Level 'Error' -Source 'ModuleName'
    
    return New-CheckResult -Domain $Domain -Target $Target -Metric $MetricName -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
}
```

### Result Object Standard
Always return results using the `New-CheckResult` function:
```powershell
return New-CheckResult -Domain ([MonitoringDomain]::YourDomain) -Target $Target -Metric $MetricName -Status $Status -Value $Value -Details $Details -ExecutionTimeMs $ExecutionTime
```

## Specific Module Examples

### TPH (Payment Hub) Module
```powershell
# TemenosChecks.TPH.psm1
using module TemenosChecks.Common

function Test-TphServices {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServerName,
        
        [string[]]$ServiceNames = @('TPHService', 'TPHListener'),
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        $results = @()
        
        foreach ($serviceName in $ServiceNames) {
            $serviceStatus = Get-ServiceStatus -ServiceName $serviceName -ComputerName $ServerName
            
            $status = if ($serviceStatus.Status -eq 'Running') {
                [CheckStatus]::Success
            } else {
                [CheckStatus]::Critical
            }
            
            $results += New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target $ServerName -Metric "Service:$serviceName" -Status $status -Value $serviceStatus.Status
        }
        
        return $results
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target $ServerName -Metric 'Services' -Status ([CheckStatus]::Error) -ErrorMessage $_.Exception.Message -ExecutionTimeMs $executionTime
    }
}

function Get-TphQueueDepth {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$QueueManager,
        
        [Parameter(Mandatory)]
        [string]$QueueName,
        
        [hashtable]$Thresholds = @{ Warning = 1000; Critical = 5000 }
    )
    
    $startTime = Get-Date
    
    try {
        # Example implementation - adjust based on actual TPH queue monitoring
        $queueDepth = Get-MQQueueDepth -QueueManager $QueueManager -QueueName $QueueName
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $status = if ($queueDepth -gt $Thresholds.Critical) {
            [CheckStatus]::Critical
        } elseif ($queueDepth -gt $Thresholds.Warning) {
            [CheckStatus]::Warning
        } else {
            [CheckStatus]::Success
        }
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target "$QueueManager`:$QueueName" -Metric 'QueueDepth' -Status $status -Value $queueDepth -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target "$QueueManager`:$QueueName" -Metric 'QueueDepth' -Status ([CheckStatus]::Error) -ErrorMessage $_.Exception.Message -ExecutionTimeMs $executionTime
    }
}

Export-ModuleMember -Function @('Test-TphServices', 'Get-TphQueueDepth')
```

### T24 Module
```powershell
# TemenosChecks.T24.psm1
using module TemenosChecks.Common

function Test-T24Services {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServerName,
        
        [string[]]$ServiceNames = @('T24Server', 'T24Agent', 'TAFJAgent'),
        
        [PSCredential]$Credential
    )
    
    # Similar implementation to TPH services
}

function Get-T24COBStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$T24Server,
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        # Check COB (Close of Business) status
        # This would typically involve connecting to T24 and checking batch job status
        
        $cobStatus = "COMPLETED" # Example - implement actual T24 COB check
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $status = switch ($cobStatus) {
            "COMPLETED" { [CheckStatus]::Success }
            "RUNNING" { [CheckStatus]::Success }
            "FAILED" { [CheckStatus]::Critical }
            default { [CheckStatus]::Warning }
        }
        
        return New-CheckResult -Domain ([MonitoringDomain]::T24) -Target $T24Server -Metric 'COBStatus' -Status $status -Value $cobStatus -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        return New-CheckResult -Domain ([MonitoringDomain]::T24) -Target $T24Server -Metric 'COBStatus' -Status ([CheckStatus]::Error) -ErrorMessage $_.Exception.Message -ExecutionTimeMs $executionTime
    }
}

Export-ModuleMember -Function @('Test-T24Services', 'Get-T24COBStatus')
```

### MQ Module
```powershell
# TemenosChecks.MQ.psm1
using module TemenosChecks.Common

function Test-MqConnectivity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$QueueManager,
        
        [Parameter(Mandatory)]
        [string]$ServerName,
        
        [int]$Port = 1414,
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        # Test MQ connectivity
        $isConnected = Test-NetworkConnectivity -TargetHost $ServerName -Port $Port
        
        if ($isConnected) {
            # Further test MQ-specific connectivity
            # This would involve using IBM MQ PowerShell cmdlets or REST API
            $mqStatus = Test-MQConnection -QueueManager $QueueManager -Server $ServerName
        }
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $status = if ($isConnected -and $mqStatus) {
            [CheckStatus]::Success
        } else {
            [CheckStatus]::Critical
        }
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$ServerName`:$QueueManager" -Metric 'Connectivity' -Status $status -Value ($isConnected ? "Connected" : "Disconnected") -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$ServerName`:$QueueManager" -Metric 'Connectivity' -Status ([CheckStatus]::Error) -ErrorMessage $_.Exception.Message -ExecutionTimeMs $executionTime
    }
}

function Get-MqQueueDepth {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$QueueManager,
        
        [Parameter(Mandatory)]
        [string]$QueueName,
        
        [hashtable]$Thresholds = @{ Warning = 1000; Critical = 5000 }
    )
    
    $startTime = Get-Date
    
    try {
        # Get queue depth using MQ commands or API
        # This would typically use runmqsc or MQ REST API
        $queueDepth = Get-MQQueueAttribute -QueueManager $QueueManager -QueueName $QueueName -Attribute CURDEPTH
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $status = if ($queueDepth -gt $Thresholds.Critical) {
            [CheckStatus]::Critical
        } elseif ($queueDepth -gt $Thresholds.Warning) {
            [CheckStatus]::Warning
        } else {
            [CheckStatus]::Success
        }
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$QueueManager`:$QueueName" -Metric 'QueueDepth' -Status $status -Value $queueDepth -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$QueueManager`:$QueueName" -Metric 'QueueDepth' -Status ([CheckStatus]::Error) -ErrorMessage $_.Exception.Message -ExecutionTimeMs $executionTime
    }
}

Export-ModuleMember -Function @('Test-MqConnectivity', 'Get-MqQueueDepth')
```

## Testing

### Unit Testing with Pester
```powershell
# TemenosChecks.NewDomain.Tests.ps1
Describe "TemenosChecks.NewDomain" {
    BeforeAll {
        Import-Module "TemenosChecks.Common" -Force
        Import-Module "TemenosChecks.NewDomain" -Force
    }
    
    Context "Test-NewDomainConnectivity" {
        It "Should return success for valid connection" {
            Mock Test-NetworkConnectivity { return $true }
            
            $result = Test-NewDomainConnectivity -Target "localhost" -Port 80
            
            $result.Status | Should -Be ([CheckStatus]::Success)
            $result.Domain | Should -Be ([MonitoringDomain]::Host)
            $result.Target | Should -Be "localhost"
            $result.Metric | Should -Be "Connectivity"
        }
        
        It "Should return critical for failed connection" {
            Mock Test-NetworkConnectivity { return $false }
            
            $result = Test-NewDomainConnectivity -Target "invalid-host" -Port 80
            
            $result.Status | Should -Be ([CheckStatus]::Critical)
            $result.Value | Should -Be "Not Connected"
        }
        
        It "Should handle exceptions gracefully" {
            Mock Test-NetworkConnectivity { throw "Network error" }
            
            $result = Test-NewDomainConnectivity -Target "localhost" -Port 80
            
            $result.Status | Should -Be ([CheckStatus]::Error)
            $result.ErrorMessage | Should -Be "Network error"
        }
    }
}
```

### Integration Testing
```powershell
# Integration test script
$testResults = @()

# Test SQL Module
$sqlResult = Test-SqlServerAvailability -InstanceName "localhost"
$testResults += $sqlResult

# Test Common Module
$networkResult = Test-NetworkConnectivity -TargetHost "localhost" -Port 443
$testResults += $networkResult

# Report results
$testResults | ForEach-Object {
    Write-Host "$($_.Domain) - $($_.Metric): $($_.Status)" -ForegroundColor $(
        switch ($_.Status) {
            "Success" { "Green" }
            "Warning" { "Yellow" }
            "Critical" { "Red" }
            "Error" { "Red" }
        }
    )
}
```

## Best Practices

### Security
1. **Credential Handling**: Always use `PSCredential` objects, never plain text passwords
2. **Input Validation**: Validate all parameters to prevent injection attacks
3. **Error Information**: Don't expose sensitive information in error messages
4. **Logging**: Log security events appropriately

### Performance
1. **Timeouts**: Always implement timeouts for external calls
2. **Resource Cleanup**: Properly dispose of connections and resources
3. **Parallel Execution**: Use parallel processing for multiple checks when appropriate
4. **Caching**: Cache expensive operations when appropriate

### Reliability
1. **Error Handling**: Comprehensive try-catch blocks with meaningful error messages
2. **Graceful Degradation**: Continue with other checks if one fails
3. **Retry Logic**: Implement retry logic for transient failures
4. **Validation**: Validate all inputs and outputs

### Documentation
1. **Function Help**: Comprehensive comment-based help for all functions
2. **Examples**: Include usage examples in documentation
3. **Dependencies**: Clearly document all dependencies and prerequisites
4. **Change Log**: Maintain version history and change logs

## Module Registration

### Automatic Discovery
Modules are automatically discovered by the monitoring service based on:
1. Module location in the configured `ModuleBasePath`
2. Proper manifest file with required metadata
3. Exported functions following naming conventions

### Manual Registration
For custom modules or specific configurations, modules can be registered via the API configuration system.

## Troubleshooting

### Common Issues
1. **Module Not Found**: Check module path configuration and file permissions
2. **Function Not Exported**: Ensure functions are listed in `FunctionsToExport`
3. **Dependency Issues**: Verify all required modules are available
4. **Permission Errors**: Check service account permissions for module execution

### Debug Mode
Enable debug logging for module development:
```powershell
$DebugPreference = "Continue"
Import-Module TemenosChecks.YourModule -Force -Verbose
```

This guide provides the foundation for developing robust, secure, and maintainable PowerShell monitoring modules for the Temenos Alert Manager.