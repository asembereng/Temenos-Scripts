# TemenosChecks.TPH Module
# Payment Hub monitoring checks for Temenos environments

using module TemenosChecks.Common

# Test TPH Services
function Test-TphServices {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServerName,
        
        [string[]]$ServiceNames = @('TPHService', 'TPHPaymentService', 'TPHProcessingService'),
        
        [PSCredential]$Credential,
        
        [hashtable]$Thresholds = @{}
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Testing TPH services on server: $ServerName" -Source 'TPHCheck'
        
        $results = @()
        $overallStatus = [CheckStatus]::Success
        $statusDetails = @()
        
        foreach ($serviceName in $ServiceNames) {
            try {
                if (Test-ServiceExists -ServiceName $serviceName -ComputerName $ServerName) {
                    $serviceStatus = Get-ServiceStatus -ServiceName $serviceName -ComputerName $ServerName
                    
                    if ($serviceStatus.Status -eq 'Running') {
                        $status = [CheckStatus]::Success
                        $statusDetails += "$serviceName`: Running"
                    } else {
                        $status = [CheckStatus]::Critical
                        $overallStatus = [CheckStatus]::Critical
                        $statusDetails += "$serviceName`: $($serviceStatus.Status)"
                    }
                } else {
                    $status = [CheckStatus]::Critical
                    $overallStatus = [CheckStatus]::Critical
                    $statusDetails += "$serviceName`: Not Found"
                }
            }
            catch {
                $status = [CheckStatus]::Error
                $overallStatus = [CheckStatus]::Error
                $statusDetails += "$serviceName`: Error - $($_.Exception.Message)"
            }
        }
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $details = @{
            Services = $statusDetails
            ServerName = $ServerName
            CheckTime = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        } | ConvertTo-Json -Depth 3
        
        $value = "$($ServiceNames.Count) services checked - $($statusDetails.Where({$_ -like '*Running*'}).Count) running"
        
        Write-CheckLog -Message "TPH services check completed: $value" -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target $ServerName -Metric 'Services' -Status $overallStatus -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "TPH services check failed: $errorMessage" -Level 'Error' -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target $ServerName -Metric 'Services' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Get TPH Queue Depth
function Get-TphQueueDepth {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$QueueManager,
        
        [Parameter(Mandatory)]
        [string]$QueueName,
        
        [hashtable]$Thresholds = @{ Warning = 1000; Critical = 5000 },
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking TPH queue depth for $QueueManager`:$QueueName" -Source 'TPHCheck'
        
        # In a real implementation, this would connect to the TPH API or database
        # For demonstration, we'll simulate queue depth checking
        
        # Example implementation - would need to be adapted based on actual TPH architecture
        # This might involve REST API calls, database queries, or MQ commands
        
        $queueDepth = Get-Random -Minimum 0 -Maximum 10000  # Simulated for demonstration
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $status = if ($queueDepth -gt $Thresholds.Critical) {
            [CheckStatus]::Critical
        } elseif ($queueDepth -gt $Thresholds.Warning) {
            [CheckStatus]::Warning
        } else {
            [CheckStatus]::Success
        }
        
        $details = @{
            QueueManager = $QueueManager
            QueueName = $QueueName
            CurrentDepth = $queueDepth
            Thresholds = $Thresholds
            CheckTime = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        } | ConvertTo-Json -Depth 3
        
        $value = "$queueDepth messages"
        
        Write-CheckLog -Message "TPH queue depth check completed: $QueueManager`:$QueueName = $queueDepth" -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target "$QueueManager`:$QueueName" -Metric 'QueueDepth' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "TPH queue depth check failed: $errorMessage" -Level 'Error' -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target "$QueueManager`:$QueueName" -Metric 'QueueDepth' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Test TPH Connectivity
function Test-TphConnectivity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServerName,
        
        [int]$Port = 8080,
        
        [string]$ApiEndpoint = '/tph/health',
        
        [PSCredential]$Credential,
        
        [int]$TimeoutSeconds = 30
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Testing TPH connectivity to $ServerName`:$Port" -Source 'TPHCheck'
        
        # Test network connectivity first
        $networkTest = Test-NetworkConnectivity -TargetHost $ServerName -Port $Port -TimeoutMs ($TimeoutSeconds * 1000)
        
        if (-not $networkTest) {
            throw "Network connectivity test failed to $ServerName`:$Port"
        }
        
        # Test HTTP/HTTPS endpoint if available
        $uri = "http://$ServerName`:$Port$ApiEndpoint"
        
        try {
            $response = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
            $apiStatus = "Healthy"
        }
        catch {
            $apiStatus = "API endpoint not responding: $($_.Exception.Message)"
        }
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $status = if ($networkTest -and $apiStatus -eq "Healthy") {
            [CheckStatus]::Success
        } elseif ($networkTest) {
            [CheckStatus]::Warning
        } else {
            [CheckStatus]::Critical
        }
        
        $details = @{
            ServerName = $ServerName
            Port = $Port
            NetworkConnectivity = $networkTest
            ApiStatus = $apiStatus
            ResponseTime = $executionTime
        } | ConvertTo-Json -Depth 3
        
        $value = if ($networkTest) { "Connected" } else { "Disconnected" }
        
        Write-CheckLog -Message "TPH connectivity test completed: $value" -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target "$ServerName`:$Port" -Metric 'Connectivity' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "TPH connectivity test failed: $errorMessage" -Level 'Error' -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target "$ServerName`:$Port" -Metric 'Connectivity' -Status ([CheckStatus]::Critical) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Get TPH Transaction Status
function Get-TphTransactionStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServerName,
        
        [hashtable]$Thresholds = @{ 
            PendingWarning = 100
            PendingCritical = 500
            FailedWarning = 10
            FailedCritical = 50
        },
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking TPH transaction status on $ServerName" -Source 'TPHCheck'
        
        # In a real implementation, this would query the TPH database or API
        # for transaction counts by status
        
        # Simulated transaction counts for demonstration
        $pendingTransactions = Get-Random -Minimum 0 -Maximum 1000
        $failedTransactions = Get-Random -Minimum 0 -Maximum 100
        $processingTransactions = Get-Random -Minimum 0 -Maximum 50
        $completedToday = Get-Random -Minimum 1000 -Maximum 10000
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        # Determine overall status based on thresholds
        $status = [CheckStatus]::Success
        
        if ($pendingTransactions -gt $Thresholds.PendingCritical -or $failedTransactions -gt $Thresholds.FailedCritical) {
            $status = [CheckStatus]::Critical
        } elseif ($pendingTransactions -gt $Thresholds.PendingWarning -or $failedTransactions -gt $Thresholds.FailedWarning) {
            $status = [CheckStatus]::Warning
        }
        
        $details = @{
            PendingTransactions = $pendingTransactions
            FailedTransactions = $failedTransactions
            ProcessingTransactions = $processingTransactions
            CompletedToday = $completedToday
            Thresholds = $Thresholds
            CheckTime = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        } | ConvertTo-Json -Depth 3
        
        $value = "Pending: $pendingTransactions, Failed: $failedTransactions, Processing: $processingTransactions"
        
        Write-CheckLog -Message "TPH transaction status check completed: $value" -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target $ServerName -Metric 'TransactionStatus' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "TPH transaction status check failed: $errorMessage" -Level 'Error' -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target $ServerName -Metric 'TransactionStatus' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Test TPH Listener Status
function Test-TphListenerStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServerName,
        
        [string[]]$ListenerNames = @('TPH_SWIFT_Listener', 'TPH_ISO20022_Listener', 'TPH_Internal_Listener'),
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking TPH listeners on $ServerName" -Source 'TPHCheck'
        
        $results = @()
        $overallStatus = [CheckStatus]::Success
        
        foreach ($listenerName in $ListenerNames) {
            # In a real implementation, this would check listener status via TPH API or database
            # For demonstration, we'll simulate listener status
            
            $isActive = (Get-Random -Minimum 0 -Maximum 10) -gt 1  # 90% chance of being active
            $lastActivity = (Get-Date).AddMinutes(-([System.Random]::new().Next(1, 60)))
            
            if ($isActive) {
                $results += "$listenerName`: Active (Last activity: $($lastActivity.ToString('HH:mm:ss')))"
            } else {
                $results += "$listenerName`: Inactive"
                $overallStatus = [CheckStatus]::Critical
            }
        }
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $details = @{
            ServerName = $ServerName
            Listeners = $results
            CheckTime = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        } | ConvertTo-Json -Depth 3
        
        $activeCount = $results.Where({$_ -like '*Active*'}).Count
        $value = "$activeCount/$($ListenerNames.Count) listeners active"
        
        Write-CheckLog -Message "TPH listener status check completed: $value" -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target $ServerName -Metric 'ListenerStatus' -Status $overallStatus -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "TPH listener status check failed: $errorMessage" -Level 'Error' -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target $ServerName -Metric 'ListenerStatus' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Get TPH Error Logs
function Get-TphErrorLogs {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServerName,
        
        [DateTime]$StartTime = (Get-Date).AddHours(-1),
        
        [DateTime]$EndTime = (Get-Date),
        
        [hashtable]$Thresholds = @{ 
            WarningErrorCount = 5
            CriticalErrorCount = 20
        },
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking TPH error logs on $ServerName" -Source 'TPHCheck'
        
        # Get Windows Event Logs for TPH application
        $errorEvents = Get-WindowsEventLogs -LogName 'Application' -Level @('Error', 'Critical') -StartTime $StartTime -EndTime $EndTime -ComputerName $ServerName -MaxEvents 100
        
        # Filter for TPH-related events (adjust Source names based on actual TPH configuration)
        $tphErrors = $errorEvents | Where-Object { $_.Source -like '*TPH*' -or $_.Source -like '*PaymentHub*' }
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $errorCount = ($tphErrors | Measure-Object).Count
        
        $status = if ($errorCount -gt $Thresholds.CriticalErrorCount) {
            [CheckStatus]::Critical
        } elseif ($errorCount -gt $Thresholds.WarningErrorCount) {
            [CheckStatus]::Warning
        } else {
            [CheckStatus]::Success
        }
        
        $recentErrors = $tphErrors | Select-Object -First 5 | ForEach-Object {
            @{
                Time = $_.TimeCreated.ToString('yyyy-MM-dd HH:mm:ss')
                Source = $_.Source
                Message = $_.Message.Substring(0, [Math]::Min(200, $_.Message.Length))
            }
        }
        
        $details = @{
            ServerName = $ServerName
            ErrorCount = $errorCount
            TimeRange = "$($StartTime.ToString('yyyy-MM-dd HH:mm:ss')) to $($EndTime.ToString('yyyy-MM-dd HH:mm:ss'))"
            RecentErrors = $recentErrors
            Thresholds = $Thresholds
        } | ConvertTo-Json -Depth 4
        
        $value = "$errorCount errors in last $([Math]::Round(($EndTime - $StartTime).TotalHours, 1)) hours"
        
        Write-CheckLog -Message "TPH error logs check completed: $value" -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target $ServerName -Metric 'ErrorLogs' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "TPH error logs check failed: $errorMessage" -Level 'Error' -Source 'TPHCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::TPH) -Target $ServerName -Metric 'ErrorLogs' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Export functions
Export-ModuleMember -Function @(
    'Test-TphServices',
    'Get-TphQueueDepth',
    'Test-TphConnectivity',
    'Get-TphTransactionStatus',
    'Test-TphListenerStatus',
    'Get-TphErrorLogs'
)