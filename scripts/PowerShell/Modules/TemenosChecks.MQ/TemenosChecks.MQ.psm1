# TemenosChecks.MQ Module
# IBM MQ monitoring checks for Temenos environments

using module TemenosChecks.Common

# Test MQ Connectivity
function Test-MqConnectivity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$QueueManager,
        
        [Parameter(Mandatory)]
        [string]$ServerName,
        
        [int]$Port = 1414,
        
        [string]$Channel = 'SYSTEM.DEF.SVRCONN',
        
        [PSCredential]$Credential,
        
        [int]$TimeoutSeconds = 30
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Testing MQ connectivity to $ServerName`:$Port (QM: $QueueManager)" -Source 'MQCheck'
        
        # Test network connectivity first
        $networkTest = Test-NetworkConnectivity -TargetHost $ServerName -Port $Port -TimeoutMs ($TimeoutSeconds * 1000)
        
        if (-not $networkTest) {
            throw "Network connectivity test failed to $ServerName`:$Port"
        }
        
        # Test MQ-specific connectivity using runmqsc or MQ .NET classes
        # Note: In a real implementation, you would use IBM MQ .NET classes or PowerShell cmdlets
        # For demonstration, we'll simulate MQ connectivity test
        
        $mqConnected = $true  # Simulated - in real implementation, use actual MQ connection test
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $status = if ($networkTest -and $mqConnected) {
            [CheckStatus]::Success
        } elseif ($networkTest) {
            [CheckStatus]::Warning
        } else {
            [CheckStatus]::Critical
        }
        
        $details = @{
            QueueManager = $QueueManager
            ServerName = $ServerName
            Port = $Port
            Channel = $Channel
            NetworkConnectivity = $networkTest
            MQConnectivity = $mqConnected
            ResponseTime = $executionTime
        } | ConvertTo-Json -Depth 3
        
        $value = if ($networkTest -and $mqConnected) { "Connected" } else { "Disconnected" }
        
        Write-CheckLog -Message "MQ connectivity test completed: $value" -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$ServerName`:$QueueManager" -Metric 'Connectivity' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "MQ connectivity test failed: $errorMessage" -Level 'Error' -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$ServerName`:$QueueManager" -Metric 'Connectivity' -Status ([CheckStatus]::Critical) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Get MQ Queue Depth
function Get-MqQueueDepth {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$QueueManager,
        
        [Parameter(Mandatory)]
        [string]$QueueName,
        
        [string]$ServerName = 'localhost',
        
        [hashtable]$Thresholds = @{ Warning = 1000; Critical = 5000 },
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking MQ queue depth for $QueueManager`:$QueueName" -Source 'MQCheck'
        
        # In a real implementation, this would use runmqsc commands or MQ .NET API
        # Example runmqsc command: echo "DISPLAY QUEUE($QueueName) CURDEPTH MAXDEPTH" | runmqsc $QueueManager
        
        # Simulated queue depth for demonstration
        $currentDepth = Get-Random -Minimum 0 -Maximum 10000
        $maxDepth = Get-Random -Minimum 10000 -Maximum 50000
        
        # Simulate getting additional queue attributes
        $inputProcesses = Get-Random -Minimum 0 -Maximum 10
        $outputProcesses = Get-Random -Minimum 0 -Maximum 10
        $queueType = "LOCAL"
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $status = if ($currentDepth -gt $Thresholds.Critical) {
            [CheckStatus]::Critical
        } elseif ($currentDepth -gt $Thresholds.Warning) {
            [CheckStatus]::Warning
        } else {
            [CheckStatus]::Success
        }
        
        $usagePercentage = [Math]::Round(($currentDepth / $maxDepth) * 100, 2)
        
        $details = @{
            QueueManager = $QueueManager
            QueueName = $QueueName
            CurrentDepth = $currentDepth
            MaxDepth = $maxDepth
            UsagePercentage = $usagePercentage
            InputProcesses = $inputProcesses
            OutputProcesses = $outputProcesses
            QueueType = $queueType
            Thresholds = $Thresholds
            CheckTime = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        } | ConvertTo-Json -Depth 3
        
        $value = "$currentDepth messages ($usagePercentage% of max)"
        
        Write-CheckLog -Message "MQ queue depth check completed: $QueueManager`:$QueueName = $currentDepth" -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$QueueManager`:$QueueName" -Metric 'QueueDepth' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "MQ queue depth check failed: $errorMessage" -Level 'Error' -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$QueueManager`:$QueueName" -Metric 'QueueDepth' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Get MQ Channel Status
function Get-MqChannelStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$QueueManager,
        
        [string[]]$ChannelNames = @(),
        
        [string]$ServerName = 'localhost',
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking MQ channel status for $QueueManager" -Source 'MQCheck'
        
        # If no specific channels provided, get all channels
        if ($ChannelNames.Count -eq 0) {
            # In real implementation: echo "DISPLAY CHANNEL(*) STATUS" | runmqsc $QueueManager
            $ChannelNames = @('SYSTEM.DEF.SVRCONN', 'SYSTEM.AUTO.SVRCONN', 'TO.REMOTE.QM')
        }
        
        $channelResults = @()
        $overallStatus = [CheckStatus]::Success
        
        foreach ($channelName in $ChannelNames) {
            # Simulate channel status check
            $channelStatus = @('RUNNING', 'STOPPED', 'RETRYING', 'STARTING')[(Get-Random -Minimum 0 -Maximum 4)]
            $messagesTransferred = Get-Random -Minimum 0 -Maximum 10000
            $lastMsgTime = (Get-Date).AddMinutes(-([System.Random]::new().Next(1, 120)))
            
            $channelResult = @{
                ChannelName = $channelName
                Status = $channelStatus
                MessagesTransferred = $messagesTransferred
                LastMessageTime = $lastMsgTime.ToString('yyyy-MM-dd HH:mm:ss')
            }
            
            if ($channelStatus -eq 'STOPPED' -or $channelStatus -eq 'RETRYING') {
                $overallStatus = [CheckStatus]::Warning
            }
            
            $channelResults += $channelResult
        }
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $details = @{
            QueueManager = $QueueManager
            Channels = $channelResults
            CheckTime = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        } | ConvertTo-Json -Depth 4
        
        $runningChannels = ($channelResults | Where-Object { $_.Status -eq 'RUNNING' }).Count
        $value = "$runningChannels/$($channelResults.Count) channels running"
        
        Write-CheckLog -Message "MQ channel status check completed: $value" -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target $QueueManager -Metric 'ChannelStatus' -Status $overallStatus -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "MQ channel status check failed: $errorMessage" -Level 'Error' -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target $QueueManager -Metric 'ChannelStatus' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Test MQ Round Trip
function Test-MqRoundTrip {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$QueueManager,
        
        [Parameter(Mandatory)]
        [string]$TestQueue,
        
        [string]$ServerName = 'localhost',
        
        [hashtable]$Thresholds = @{ WarningMs = 1000; CriticalMs = 5000 },
        
        [PSCredential]$Credential,
        
        [int]$TimeoutSeconds = 30
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Testing MQ round trip for $QueueManager`:$TestQueue" -Source 'MQCheck'
        
        # In a real implementation, this would:
        # 1. Put a test message to the queue
        # 2. Get the message back from the queue
        # 3. Measure the round trip time
        
        # Simulated round trip test
        $testMessage = "TEST_MESSAGE_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        
        # Simulate putting message
        Start-Sleep -Milliseconds 100
        
        # Simulate getting message
        Start-Sleep -Milliseconds 100
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $roundTripSuccess = $true  # Simulated success
        
        $status = if (-not $roundTripSuccess) {
            [CheckStatus]::Critical
        } elseif ($executionTime -gt $Thresholds.CriticalMs) {
            [CheckStatus]::Critical
        } elseif ($executionTime -gt $Thresholds.WarningMs) {
            [CheckStatus]::Warning
        } else {
            [CheckStatus]::Success
        }
        
        $details = @{
            QueueManager = $QueueManager
            TestQueue = $TestQueue
            TestMessage = $testMessage
            RoundTripTime = $executionTime
            Success = $roundTripSuccess
            Thresholds = $Thresholds
            CheckTime = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        } | ConvertTo-Json -Depth 3
        
        $value = if ($roundTripSuccess) { "$([Math]::Round($executionTime, 0))ms" } else { "Failed" }
        
        Write-CheckLog -Message "MQ round trip test completed: $value" -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$QueueManager`:$TestQueue" -Metric 'RoundTripTime' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "MQ round trip test failed: $errorMessage" -Level 'Error' -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$QueueManager`:$TestQueue" -Metric 'RoundTripTime' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Get MQ Queue Manager Status
function Get-MqQueueManagerStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$QueueManager,
        
        [string]$ServerName = 'localhost',
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking MQ Queue Manager status: $QueueManager" -Source 'MQCheck'
        
        # In real implementation: dspmq -m $QueueManager or DISPLAY QMGR
        
        # Simulated queue manager status
        $qmStatus = @('RUNNING', 'ENDED', 'STARTING')[(Get-Random -Minimum 0 -Maximum 3)]
        $connections = Get-Random -Minimum 0 -Maximum 100
        $maxConnections = 1000
        $startedTime = (Get-Date).AddDays(-([System.Random]::new().Next(1, 30)))
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $status = switch ($qmStatus) {
            'RUNNING' { [CheckStatus]::Success }
            'STARTING' { [CheckStatus]::Warning }
            'ENDED' { [CheckStatus]::Critical }
            default { [CheckStatus]::Warning }
        }
        
        $connectionUsage = [Math]::Round(($connections / $maxConnections) * 100, 2)
        
        $details = @{
            QueueManager = $QueueManager
            Status = $qmStatus
            Connections = $connections
            MaxConnections = $maxConnections
            ConnectionUsage = $connectionUsage
            StartedTime = $startedTime.ToString('yyyy-MM-dd HH:mm:ss')
            Uptime = (Get-Date) - $startedTime
            CheckTime = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        } | ConvertTo-Json -Depth 3
        
        $value = "$qmStatus ($connections/$maxConnections connections)"
        
        Write-CheckLog -Message "MQ Queue Manager status check completed: $value" -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target $QueueManager -Metric 'QueueManagerStatus' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "MQ Queue Manager status check failed: $errorMessage" -Level 'Error' -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target $QueueManager -Metric 'QueueManagerStatus' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Get MQ Dead Letter Queue
function Get-MqDeadLetterQueue {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$QueueManager,
        
        [string]$DeadLetterQueue = 'SYSTEM.DEAD.LETTER.QUEUE',
        
        [hashtable]$Thresholds = @{ Warning = 10; Critical = 50 },
        
        [PSCredential]$Credential
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking MQ Dead Letter Queue: $QueueManager`:$DeadLetterQueue" -Source 'MQCheck'
        
        # Get DLQ depth using the same method as regular queues
        $dlqDepth = Get-Random -Minimum 0 -Maximum 100  # Simulated
        
        # Analyze recent DLQ messages (in real implementation, browse DLQ)
        $recentMessages = @()
        for ($i = 0; $i -lt [Math]::Min($dlqDepth, 5); $i++) {
            $recentMessages += @{
                MessageId = "MSG_$([System.Guid]::NewGuid().ToString().Substring(0,8))"
                SourceQueue = "APP.QUEUE.$i"
                Reason = @('MQRC_NO_MSG_AVAILABLE', 'MQRC_QUEUE_FULL', 'MQRC_GET_INHIBITED')[(Get-Random -Minimum 0 -Maximum 3)]
                Timestamp = (Get-Date).AddMinutes(-([System.Random]::new().Next(1, 1440))).ToString('yyyy-MM-dd HH:mm:ss')
            }
        }
        
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $status = if ($dlqDepth -gt $Thresholds.Critical) {
            [CheckStatus]::Critical
        } elseif ($dlqDepth -gt $Thresholds.Warning) {
            [CheckStatus]::Warning
        } else {
            [CheckStatus]::Success
        }
        
        $details = @{
            QueueManager = $QueueManager
            DeadLetterQueue = $DeadLetterQueue
            MessageCount = $dlqDepth
            RecentMessages = $recentMessages
            Thresholds = $Thresholds
            CheckTime = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        } | ConvertTo-Json -Depth 4
        
        $value = "$dlqDepth messages in DLQ"
        
        Write-CheckLog -Message "MQ Dead Letter Queue check completed: $value" -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$QueueManager`:$DeadLetterQueue" -Metric 'DeadLetterQueue' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "MQ Dead Letter Queue check failed: $errorMessage" -Level 'Error' -Source 'MQCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MQ) -Target "$QueueManager`:$DeadLetterQueue" -Metric 'DeadLetterQueue' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Export functions
Export-ModuleMember -Function @(
    'Test-MqConnectivity',
    'Get-MqQueueDepth',
    'Get-MqChannelStatus',
    'Test-MqRoundTrip',
    'Get-MqQueueManagerStatus',
    'Get-MqDeadLetterQueue'
)