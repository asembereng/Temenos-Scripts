# Temenos Service Management PowerShell Module
# This module provides functions for managing individual Temenos services

# Import required modules
Import-Module TemenosChecks.Common -Force -ErrorAction SilentlyContinue

<#
.SYNOPSIS
Start a Temenos service

.DESCRIPTION
Starts a specific Temenos service on the target host with proper error handling and logging.

.PARAMETER ServiceName
Name of the service to start

.PARAMETER ComputerName
Target computer where the service is located (default: localhost)

.PARAMETER Credential
Credentials for remote access (optional)

.PARAMETER TimeoutSeconds
Timeout for the operation in seconds (default: 300)

.EXAMPLE
Start-TemenosService -ServiceName "T24Server" -ComputerName "T24-APP01"

.EXAMPLE
Start-TemenosService -ServiceName "TPHPaymentService" -ComputerName "TPH-SRV01" -TimeoutSeconds 600
#>
function Start-TemenosService {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ServiceName,
        
        [Parameter(Mandatory = $false)]
        [string]$ComputerName = 'localhost',
        
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential,
        
        [Parameter(Mandatory = $false)]
        [int]$TimeoutSeconds = 300
    )
    
    $startTime = Get-Date
    Write-Host "Starting service '$ServiceName' on '$ComputerName'..."
    
    try {
        # Check if service exists
        $service = Get-ServiceSafely -ServiceName $ServiceName -ComputerName $ComputerName -Credential $Credential
        
        if (-not $service) {
            throw "Service '$ServiceName' not found on '$ComputerName'"
        }
        
        # Check current state
        if ($service.Status -eq 'Running') {
            Write-Host "Service '$ServiceName' is already running"
            return @{
                Success = $true
                ServiceName = $ServiceName
                ComputerName = $ComputerName
                Action = "Start"
                Status = "Already Running"
                Message = "Service was already running"
                StartTime = $startTime
                EndTime = Get-Date
                Duration = 0
            }
        }
        
        # Start the service
        Write-Host "Starting service '$ServiceName'..."
        
        if ($ComputerName -eq 'localhost' -or $ComputerName -eq $env:COMPUTERNAME) {
            Start-Service -Name $ServiceName -ErrorAction Stop
        } else {
            $scriptBlock = {
                param($ServiceName)
                Start-Service -Name $ServiceName -ErrorAction Stop
            }
            
            if ($Credential) {
                Invoke-Command -ComputerName $ComputerName -Credential $Credential -ScriptBlock $scriptBlock -ArgumentList $ServiceName
            } else {
                Invoke-Command -ComputerName $ComputerName -ScriptBlock $scriptBlock -ArgumentList $ServiceName
            }
        }
        
        # Wait for service to start with timeout
        $timeout = [DateTime]::Now.AddSeconds($TimeoutSeconds)
        do {
            Start-Sleep -Seconds 2
            $service = Get-ServiceSafely -ServiceName $ServiceName -ComputerName $ComputerName -Credential $Credential
        } while ($service.Status -ne 'Running' -and [DateTime]::Now -lt $timeout)
        
        if ($service.Status -ne 'Running') {
            throw "Service '$ServiceName' failed to start within $TimeoutSeconds seconds"
        }
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Host "Service '$ServiceName' started successfully in $([math]::Round($duration, 2)) seconds"
        
        return @{
            Success = $true
            ServiceName = $ServiceName
            ComputerName = $ComputerName
            Action = "Start"
            Status = "Completed"
            Message = "Service started successfully"
            StartTime = $startTime
            EndTime = $endTime
            Duration = $duration
        }
    }
    catch {
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Error "Failed to start service '$ServiceName': $($_.Exception.Message)"
        
        return @{
            Success = $false
            ServiceName = $ServiceName
            ComputerName = $ComputerName
            Action = "Start"
            Status = "Failed"
            Message = "Service start failed: $($_.Exception.Message)"
            ErrorDetails = $_.Exception.ToString()
            StartTime = $startTime
            EndTime = $endTime
            Duration = $duration
        }
    }
}

<#
.SYNOPSIS
Stop a Temenos service

.DESCRIPTION
Stops a specific Temenos service on the target host with proper error handling and logging.

.PARAMETER ServiceName
Name of the service to stop

.PARAMETER ComputerName
Target computer where the service is located (default: localhost)

.PARAMETER Credential
Credentials for remote access (optional)

.PARAMETER TimeoutSeconds
Timeout for the operation in seconds (default: 300)

.PARAMETER Force
Force stop the service if it doesn't stop gracefully

.EXAMPLE
Stop-TemenosService -ServiceName "T24Server" -ComputerName "T24-APP01"

.EXAMPLE
Stop-TemenosService -ServiceName "TPHPaymentService" -ComputerName "TPH-SRV01" -Force
#>
function Stop-TemenosService {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ServiceName,
        
        [Parameter(Mandatory = $false)]
        [string]$ComputerName = 'localhost',
        
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential,
        
        [Parameter(Mandatory = $false)]
        [int]$TimeoutSeconds = 300,
        
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    
    $startTime = Get-Date
    Write-Host "Stopping service '$ServiceName' on '$ComputerName'..."
    
    try {
        # Check if service exists
        $service = Get-ServiceSafely -ServiceName $ServiceName -ComputerName $ComputerName -Credential $Credential
        
        if (-not $service) {
            throw "Service '$ServiceName' not found on '$ComputerName'"
        }
        
        # Check current state
        if ($service.Status -eq 'Stopped') {
            Write-Host "Service '$ServiceName' is already stopped"
            return @{
                Success = $true
                ServiceName = $ServiceName
                ComputerName = $ComputerName
                Action = "Stop"
                Status = "Already Stopped"
                Message = "Service was already stopped"
                StartTime = $startTime
                EndTime = Get-Date
                Duration = 0
            }
        }
        
        # Stop the service
        Write-Host "Stopping service '$ServiceName'..."
        
        if ($ComputerName -eq 'localhost' -or $ComputerName -eq $env:COMPUTERNAME) {
            if ($Force) {
                Stop-Service -Name $ServiceName -Force -ErrorAction Stop
            } else {
                Stop-Service -Name $ServiceName -ErrorAction Stop
            }
        } else {
            $scriptBlock = {
                param($ServiceName, $Force)
                if ($Force) {
                    Stop-Service -Name $ServiceName -Force -ErrorAction Stop
                } else {
                    Stop-Service -Name $ServiceName -ErrorAction Stop
                }
            }
            
            if ($Credential) {
                Invoke-Command -ComputerName $ComputerName -Credential $Credential -ScriptBlock $scriptBlock -ArgumentList $ServiceName, $Force
            } else {
                Invoke-Command -ComputerName $ComputerName -ScriptBlock $scriptBlock -ArgumentList $ServiceName, $Force
            }
        }
        
        # Wait for service to stop with timeout
        $timeout = [DateTime]::Now.AddSeconds($TimeoutSeconds)
        do {
            Start-Sleep -Seconds 2
            $service = Get-ServiceSafely -ServiceName $ServiceName -ComputerName $ComputerName -Credential $Credential
        } while ($service.Status -ne 'Stopped' -and [DateTime]::Now -lt $timeout)
        
        if ($service.Status -ne 'Stopped') {
            throw "Service '$ServiceName' failed to stop within $TimeoutSeconds seconds"
        }
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Host "Service '$ServiceName' stopped successfully in $([math]::Round($duration, 2)) seconds"
        
        return @{
            Success = $true
            ServiceName = $ServiceName
            ComputerName = $ComputerName
            Action = "Stop"
            Status = "Completed"
            Message = "Service stopped successfully"
            StartTime = $startTime
            EndTime = $endTime
            Duration = $duration
        }
    }
    catch {
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Error "Failed to stop service '$ServiceName': $($_.Exception.Message)"
        
        return @{
            Success = $false
            ServiceName = $ServiceName
            ComputerName = $ComputerName
            Action = "Stop"
            Status = "Failed"
            Message = "Service stop failed: $($_.Exception.Message)"
            ErrorDetails = $_.Exception.ToString()
            StartTime = $startTime
            EndTime = $endTime
            Duration = $duration
        }
    }
}

<#
.SYNOPSIS
Restart a Temenos service

.DESCRIPTION
Restarts a specific Temenos service on the target host with proper error handling and logging.

.PARAMETER ServiceName
Name of the service to restart

.PARAMETER ComputerName
Target computer where the service is located (default: localhost)

.PARAMETER Credential
Credentials for remote access (optional)

.PARAMETER TimeoutSeconds
Timeout for the operation in seconds (default: 600)

.EXAMPLE
Restart-TemenosService -ServiceName "T24Server" -ComputerName "T24-APP01"

.EXAMPLE
Restart-TemenosService -ServiceName "TPHPaymentService" -ComputerName "TPH-SRV01" -TimeoutSeconds 900
#>
function Restart-TemenosService {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ServiceName,
        
        [Parameter(Mandatory = $false)]
        [string]$ComputerName = 'localhost',
        
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential,
        
        [Parameter(Mandatory = $false)]
        [int]$TimeoutSeconds = 600
    )
    
    $startTime = Get-Date
    Write-Host "Restarting service '$ServiceName' on '$ComputerName'..."
    
    try {
        # Stop the service first
        $stopResult = Stop-TemenosService -ServiceName $ServiceName -ComputerName $ComputerName -Credential $Credential -TimeoutSeconds ($TimeoutSeconds / 2)
        
        if (-not $stopResult.Success) {
            throw "Failed to stop service during restart: $($stopResult.Message)"
        }
        
        # Wait a moment before starting
        Start-Sleep -Seconds 5
        
        # Start the service
        $startResult = Start-TemenosService -ServiceName $ServiceName -ComputerName $ComputerName -Credential $Credential -TimeoutSeconds ($TimeoutSeconds / 2)
        
        if (-not $startResult.Success) {
            throw "Failed to start service during restart: $($startResult.Message)"
        }
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Host "Service '$ServiceName' restarted successfully in $([math]::Round($duration, 2)) seconds"
        
        return @{
            Success = $true
            ServiceName = $ServiceName
            ComputerName = $ComputerName
            Action = "Restart"
            Status = "Completed"
            Message = "Service restarted successfully"
            StartTime = $startTime
            EndTime = $endTime
            Duration = $duration
            StopResult = $stopResult
            StartResult = $startResult
        }
    }
    catch {
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Error "Failed to restart service '$ServiceName': $($_.Exception.Message)"
        
        return @{
            Success = $false
            ServiceName = $ServiceName
            ComputerName = $ComputerName
            Action = "Restart"
            Status = "Failed"
            Message = "Service restart failed: $($_.Exception.Message)"
            ErrorDetails = $_.Exception.ToString()
            StartTime = $startTime
            EndTime = $endTime
            Duration = $duration
        }
    }
}

<#
.SYNOPSIS
Test health of a Temenos service

.DESCRIPTION
Performs comprehensive health check of a specific Temenos service including status, performance, and connectivity.

.PARAMETER ServiceName
Name of the service to check

.PARAMETER ComputerName
Target computer where the service is located (default: localhost)

.PARAMETER Credential
Credentials for remote access (optional)

.EXAMPLE
Test-TemenosServiceHealth -ServiceName "T24Server" -ComputerName "T24-APP01"

.EXAMPLE
Test-TemenosServiceHealth -ServiceName "TPHPaymentService" -ComputerName "TPH-SRV01"
#>
function Test-TemenosServiceHealth {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ServiceName,
        
        [Parameter(Mandatory = $false)]
        [string]$ComputerName = 'localhost',
        
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential
    )
    
    $startTime = Get-Date
    Write-Host "Checking health of service '$ServiceName' on '$ComputerName'..."
    
    try {
        $healthChecks = @()
        
        # Basic service status check
        $service = Get-ServiceSafely -ServiceName $ServiceName -ComputerName $ComputerName -Credential $Credential
        
        if (-not $service) {
            throw "Service '$ServiceName' not found on '$ComputerName'"
        }
        
        $healthChecks += @{
            Check = "Service Status"
            Status = if ($service.Status -eq 'Running') { "Healthy" } else { "Unhealthy" }
            Details = "Service status: $($service.Status)"
            Value = $service.Status
        }
        
        # Process information check (if service is running)
        if ($service.Status -eq 'Running') {
            $processInfo = Get-ServiceProcessInfo -ServiceName $ServiceName -ComputerName $ComputerName -Credential $Credential
            
            $healthChecks += @{
                Check = "Process Information"
                Status = if ($processInfo) { "Healthy" } else { "Warning" }
                Details = if ($processInfo) { "PID: $($processInfo.Id), CPU: $($processInfo.CPU)%" } else { "Process information unavailable" }
                Value = $processInfo
            }
        }
        
        # Memory usage check
        $memoryInfo = Get-ServiceMemoryInfo -ServiceName $ServiceName -ComputerName $ComputerName -Credential $Credential
        
        if ($memoryInfo) {
            $memoryMB = [math]::Round($memoryInfo.WorkingSet / 1MB, 2)
            $healthChecks += @{
                Check = "Memory Usage"
                Status = if ($memoryMB -lt 1000) { "Healthy" } elseif ($memoryMB -lt 2000) { "Warning" } else { "Critical" }
                Details = "Working Set: $memoryMB MB"
                Value = $memoryMB
            }
        }
        
        # Overall health assessment
        $healthyCount = ($healthChecks | Where-Object { $_.Status -eq "Healthy" }).Count
        $warningCount = ($healthChecks | Where-Object { $_.Status -eq "Warning" }).Count
        $criticalCount = ($healthChecks | Where-Object { $_.Status -eq "Critical" }).Count
        
        $overallStatus = "Healthy"
        if ($criticalCount -gt 0) {
            $overallStatus = "Critical"
        } elseif ($warningCount -gt 0) {
            $overallStatus = "Warning"
        }
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Host "Health check completed for service '$ServiceName' - Overall Status: $overallStatus"
        
        return @{
            Success = $true
            ServiceName = $ServiceName
            ComputerName = $ComputerName
            Action = "HealthCheck"
            OverallStatus = $overallStatus
            HealthChecks = $healthChecks
            Summary = @{
                Healthy = $healthyCount
                Warning = $warningCount
                Critical = $criticalCount
            }
            StartTime = $startTime
            EndTime = $endTime
            Duration = $duration
        }
    }
    catch {
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Error "Failed to check health of service '$ServiceName': $($_.Exception.Message)"
        
        return @{
            Success = $false
            ServiceName = $ServiceName
            ComputerName = $ComputerName
            Action = "HealthCheck"
            OverallStatus = "Error"
            Message = "Health check failed: $($_.Exception.Message)"
            ErrorDetails = $_.Exception.ToString()
            StartTime = $startTime
            EndTime = $endTime
            Duration = $duration
        }
    }
}

# Helper functions

function Get-ServiceSafely {
    param($ServiceName, $ComputerName, $Credential)
    
    try {
        if ($ComputerName -eq 'localhost' -or $ComputerName -eq $env:COMPUTERNAME) {
            return Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        } else {
            $scriptBlock = {
                param($ServiceName)
                Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
            }
            
            if ($Credential) {
                return Invoke-Command -ComputerName $ComputerName -Credential $Credential -ScriptBlock $scriptBlock -ArgumentList $ServiceName
            } else {
                return Invoke-Command -ComputerName $ComputerName -ScriptBlock $scriptBlock -ArgumentList $ServiceName
            }
        }
    }
    catch {
        return $null
    }
}

function Get-ServiceProcessInfo {
    param($ServiceName, $ComputerName, $Credential)
    
    try {
        $scriptBlock = {
            param($ServiceName)
            $service = Get-WmiObject -Class Win32_Service -Filter "Name='$ServiceName'"
            if ($service -and $service.ProcessId -gt 0) {
                $process = Get-Process -Id $service.ProcessId -ErrorAction SilentlyContinue
                if ($process) {
                    return @{
                        Id = $process.Id
                        CPU = $process.CPU
                        StartTime = $process.StartTime
                    }
                }
            }
            return $null
        }
        
        if ($ComputerName -eq 'localhost' -or $ComputerName -eq $env:COMPUTERNAME) {
            return & $scriptBlock $ServiceName
        } else {
            if ($Credential) {
                return Invoke-Command -ComputerName $ComputerName -Credential $Credential -ScriptBlock $scriptBlock -ArgumentList $ServiceName
            } else {
                return Invoke-Command -ComputerName $ComputerName -ScriptBlock $scriptBlock -ArgumentList $ServiceName
            }
        }
    }
    catch {
        return $null
    }
}

function Get-ServiceMemoryInfo {
    param($ServiceName, $ComputerName, $Credential)
    
    try {
        $scriptBlock = {
            param($ServiceName)
            $service = Get-WmiObject -Class Win32_Service -Filter "Name='$ServiceName'"
            if ($service -and $service.ProcessId -gt 0) {
                $process = Get-Process -Id $service.ProcessId -ErrorAction SilentlyContinue
                if ($process) {
                    return @{
                        WorkingSet = $process.WorkingSet64
                        VirtualMemory = $process.VirtualMemorySize64
                        PrivateMemory = $process.PrivateMemorySize64
                    }
                }
            }
            return $null
        }
        
        if ($ComputerName -eq 'localhost' -or $ComputerName -eq $env:COMPUTERNAME) {
            return & $scriptBlock $ServiceName
        } else {
            if ($Credential) {
                return Invoke-Command -ComputerName $ComputerName -Credential $Credential -ScriptBlock $scriptBlock -ArgumentList $ServiceName
            } else {
                return Invoke-Command -ComputerName $ComputerName -ScriptBlock $scriptBlock -ArgumentList $ServiceName
            }
        }
    }
    catch {
        return $null
    }
}

Export-ModuleMember -Function Start-TemenosService, Stop-TemenosService, Restart-TemenosService, Test-TemenosServiceHealth