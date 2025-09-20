# TemenosChecks.Sql Module
# SQL Server monitoring checks for Temenos environments

using module TemenosChecks.Common

# Test SQL Server availability
function Test-SqlServerAvailability {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$InstanceName,
        
        [string]$DatabaseName,
        
        [PSCredential]$Credential,
        
        [hashtable]$Thresholds = @{}
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Testing SQL Server availability for instance: $InstanceName" -Source 'SqlCheck'
        
        $connectionString = if ($Credential) {
            "Server=$InstanceName;Database=$($DatabaseName ?? 'master');User Id=$($Credential.UserName);Password=$($Credential.GetNetworkCredential().Password);TrustServerCertificate=true;Connection Timeout=30;"
        } else {
            "Server=$InstanceName;Database=$($DatabaseName ?? 'master');Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30;"
        }
        
        $query = @"
SELECT 
    @@SERVERNAME as ServerName,
    @@VERSION as Version,
    GETDATE() as CurrentTime,
    (SELECT COUNT(*) FROM sys.databases WHERE state = 0) as OnlineDatabases
"@
        
        $result = Invoke-Sqlcmd -ConnectionString $connectionString -Query $query -ErrorAction Stop
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $details = @{
            ServerName = $result.ServerName
            Version = $result.Version
            CurrentTime = $result.CurrentTime
            OnlineDatabases = $result.OnlineDatabases
            ExecutionTimeMs = $executionTime
        } | ConvertTo-Json -Depth 3
        
        Write-CheckLog -Message "SQL Server availability check completed in $($executionTime)ms" -Source 'SqlCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MSSQL) -Target $InstanceName -Metric 'Availability' -Status ([CheckStatus]::Success) -Value 'Online' -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "SQL Server availability check failed: $errorMessage" -Level 'Error' -Source 'SqlCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MSSQL) -Target $InstanceName -Metric 'Availability' -Status ([CheckStatus]::Critical) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Get blocking sessions
function Get-SqlBlockingSessions {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$InstanceName,
        
        [PSCredential]$Credential,
        
        [hashtable]$Thresholds = @{ MaxBlockingDurationMinutes = 5; CriticalBlockingDurationMinutes = 15 }
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking for blocking sessions on: $InstanceName" -Source 'SqlCheck'
        
        $connectionString = if ($Credential) {
            "Server=$InstanceName;Database=master;User Id=$($Credential.UserName);Password=$($Credential.GetNetworkCredential().Password);TrustServerCertificate=true;Connection Timeout=30;"
        } else {
            "Server=$InstanceName;Database=master;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30;"
        }
        
        $query = @"
WITH BlockingChain AS (
    SELECT 
        s1.session_id AS blocker_session_id,
        s1.login_name AS blocker_login,
        s1.host_name AS blocker_host,
        s2.session_id AS blocked_session_id,
        s2.login_name AS blocked_login,
        s2.host_name AS blocked_host,
        wt.wait_type,
        wt.wait_duration_ms,
        wt.wait_duration_ms / 1000.0 / 60.0 AS wait_duration_minutes,
        DB_NAME(s2.database_id) AS database_name,
        (SELECT TEXT FROM sys.dm_exec_sql_text(s2.sql_handle)) AS blocked_sql
    FROM sys.dm_tran_locks l1
    JOIN sys.dm_os_waiting_tasks wt ON l1.lock_owner_address = wt.resource_address
    JOIN sys.dm_exec_sessions s1 ON wt.blocking_session_id = s1.session_id
    JOIN sys.dm_exec_sessions s2 ON wt.session_id = s2.session_id
    WHERE s1.session_id != s2.session_id
)
SELECT * FROM BlockingChain
ORDER BY wait_duration_minutes DESC
"@
        
        $blockingData = Invoke-Sqlcmd -ConnectionString $connectionString -Query $query -ErrorAction Stop
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $maxBlockingMinutes = if ($blockingData) { ($blockingData | Measure-Object wait_duration_minutes -Maximum).Maximum } else { 0 }
        $blockingCount = ($blockingData | Measure-Object).Count
        
        $status = [CheckStatus]::Success
        if ($maxBlockingMinutes -gt $Thresholds.CriticalBlockingDurationMinutes) {
            $status = [CheckStatus]::Critical
        }
        elseif ($maxBlockingMinutes -gt $Thresholds.MaxBlockingDurationMinutes -or $blockingCount -gt 0) {
            $status = [CheckStatus]::Warning
        }
        
        $details = @{
            BlockingCount = $blockingCount
            MaxBlockingDurationMinutes = $maxBlockingMinutes
            BlockingSessions = $blockingData | Select-Object -First 10
            Thresholds = $Thresholds
        } | ConvertTo-Json -Depth 3
        
        $value = if ($blockingCount -gt 0) { "$blockingCount blocking sessions (max: $([math]::Round($maxBlockingMinutes, 2)) min)" } else { "No blocking" }
        
        Write-CheckLog -Message "Blocking sessions check completed: $value" -Source 'SqlCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MSSQL) -Target $InstanceName -Metric 'BlockingSessions' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "Blocking sessions check failed: $errorMessage" -Level 'Error' -Source 'SqlCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MSSQL) -Target $InstanceName -Metric 'BlockingSessions' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Get long-running queries
function Get-SqlLongRunningQueries {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$InstanceName,
        
        [PSCredential]$Credential,
        
        [hashtable]$Thresholds = @{ WarningMinutes = 10; CriticalMinutes = 30 }
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking for long-running queries on: $InstanceName" -Source 'SqlCheck'
        
        $connectionString = if ($Credential) {
            "Server=$InstanceName;Database=master;User Id=$($Credential.UserName);Password=$($Credential.GetNetworkCredential().Password);TrustServerCertificate=true;Connection Timeout=30;"
        } else {
            "Server=$InstanceName;Database=master;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30;"
        }
        
        $query = @"
SELECT TOP 20 
    r.session_id,
    r.status,
    r.cpu_time,
    r.total_elapsed_time,
    r.total_elapsed_time / 1000.0 / 60.0 AS elapsed_minutes,
    r.reads,
    r.writes,
    r.logical_reads,
    DB_NAME(r.database_id) AS database_name,
    s.login_name,
    s.host_name,
    s.program_name,
    SUBSTRING(t.text, (r.statement_start_offset/2)+1,
        ((CASE r.statement_end_offset
            WHEN -1 THEN DATALENGTH(t.text)
            ELSE r.statement_end_offset
        END - r.statement_start_offset)/2) + 1) AS sql_text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
JOIN sys.dm_exec_sessions s ON r.session_id = s.session_id
WHERE r.total_elapsed_time > $($Thresholds.WarningMinutes * 60 * 1000) -- Convert minutes to milliseconds
ORDER BY r.total_elapsed_time DESC
"@
        
        $longQueries = Invoke-Sqlcmd -ConnectionString $connectionString -Query $query -ErrorAction Stop
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $maxElapsedMinutes = if ($longQueries) { ($longQueries | Measure-Object elapsed_minutes -Maximum).Maximum } else { 0 }
        $longQueryCount = ($longQueries | Measure-Object).Count
        
        $status = [CheckStatus]::Success
        if ($maxElapsedMinutes -gt $Thresholds.CriticalMinutes) {
            $status = [CheckStatus]::Critical
        }
        elseif ($longQueryCount -gt 0) {
            $status = [CheckStatus]::Warning
        }
        
        $details = @{
            LongQueryCount = $longQueryCount
            MaxElapsedMinutes = $maxElapsedMinutes
            LongQueries = $longQueries
            Thresholds = $Thresholds
        } | ConvertTo-Json -Depth 3
        
        $value = if ($longQueryCount -gt 0) { "$longQueryCount long queries (max: $([math]::Round($maxElapsedMinutes, 2)) min)" } else { "No long queries" }
        
        Write-CheckLog -Message "Long-running queries check completed: $value" -Source 'SqlCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MSSQL) -Target $InstanceName -Metric 'LongRunningQueries' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "Long-running queries check failed: $errorMessage" -Level 'Error' -Source 'SqlCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MSSQL) -Target $InstanceName -Metric 'LongRunningQueries' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Get TempDB usage
function Get-SqlTempDbUsage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$InstanceName,
        
        [PSCredential]$Credential,
        
        [hashtable]$Thresholds = @{ WarningPercentage = 70; CriticalPercentage = 90 }
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Checking TempDB usage on: $InstanceName" -Source 'SqlCheck'
        
        $connectionString = if ($Credential) {
            "Server=$InstanceName;Database=tempdb;User Id=$($Credential.UserName);Password=$($Credential.GetNetworkCredential().Password);TrustServerCertificate=true;Connection Timeout=30;"
        } else {
            "Server=$InstanceName;Database=tempdb;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30;"
        }
        
        $query = @"
SELECT 
    SUM(user_object_reserved_page_count) * 8 AS user_object_kb,
    SUM(internal_object_reserved_page_count) * 8 AS internal_object_kb,
    SUM(version_store_reserved_page_count) * 8 AS version_store_kb,
    SUM(mixed_extent_page_count) * 8 AS mixed_extent_kb,
    (SUM(user_object_reserved_page_count) + 
     SUM(internal_object_reserved_page_count) + 
     SUM(version_store_reserved_page_count) + 
     SUM(mixed_extent_page_count)) * 8 AS total_used_kb
FROM sys.dm_db_file_space_usage;

-- Get total TempDB size
SELECT 
    name,
    size * 8 AS size_kb,
    max_size * 8 AS max_size_kb,
    (size * 8.0) / 1024 / 1024 AS size_gb
FROM sys.database_files
WHERE type_desc = 'ROWS';
"@
        
        $tempDbData = Invoke-Sqlcmd -ConnectionString $connectionString -Query $query -ErrorAction Stop
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        # Parse results (Invoke-Sqlcmd returns multiple result sets)
        $usage = $tempDbData[0]
        $files = $tempDbData[1..($tempDbData.Count-1)]
        
        $totalSizeKB = ($files | Measure-Object size_kb -Sum).Sum
        $usedPercentage = if ($totalSizeKB -gt 0) { ($usage.total_used_kb / $totalSizeKB) * 100 } else { 0 }
        
        $status = [CheckStatus]::Success
        if ($usedPercentage -gt $Thresholds.CriticalPercentage) {
            $status = [CheckStatus]::Critical
        }
        elseif ($usedPercentage -gt $Thresholds.WarningPercentage) {
            $status = [CheckStatus]::Warning
        }
        
        $details = @{
            UsageKB = $usage
            Files = $files
            TotalSizeKB = $totalSizeKB
            UsedPercentage = $usedPercentage
            Thresholds = $Thresholds
        } | ConvertTo-Json -Depth 3
        
        $value = "$([math]::Round($usedPercentage, 1))% used ($([math]::Round($usage.total_used_kb/1024/1024, 2)) GB / $([math]::Round($totalSizeKB/1024/1024, 2)) GB)"
        
        Write-CheckLog -Message "TempDB usage check completed: $value" -Source 'SqlCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MSSQL) -Target $InstanceName -Metric 'TempDbUsage' -Status $status -Value $value -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "TempDB usage check failed: $errorMessage" -Level 'Error' -Source 'SqlCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MSSQL) -Target $InstanceName -Metric 'TempDbUsage' -Status ([CheckStatus]::Error) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Test SQL connectivity
function Test-SqlConnectivity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$InstanceName,
        
        [PSCredential]$Credential,
        
        [int]$TimeoutSeconds = 30
    )
    
    $startTime = Get-Date
    
    try {
        Write-CheckLog -Message "Testing SQL connectivity to: $InstanceName" -Source 'SqlCheck'
        
        # Test network connectivity first
        $serverParts = $InstanceName -split ','
        $serverName = $serverParts[0]
        $port = if ($serverParts.Count -gt 1) { [int]$serverParts[1] } else { 1433 }
        
        $networkTest = Test-NetworkConnectivity -TargetHost $serverName -Port $port -TimeoutMs ($TimeoutSeconds * 1000)
        
        if (-not $networkTest) {
            throw "Network connectivity test failed to $serverName`:$port"
        }
        
        # Test SQL authentication
        $connectionString = if ($Credential) {
            "Server=$InstanceName;Database=master;User Id=$($Credential.UserName);Password=$($Credential.GetNetworkCredential().Password);TrustServerCertificate=true;Connection Timeout=$TimeoutSeconds;"
        } else {
            "Server=$InstanceName;Database=master;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=$TimeoutSeconds;"
        }
        
        $result = Invoke-Sqlcmd -ConnectionString $connectionString -Query "SELECT @@SERVERNAME as ServerName, GETDATE() as ConnectedAt" -ErrorAction Stop
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        
        $details = @{
            ServerName = $result.ServerName
            ConnectedAt = $result.ConnectedAt
            NetworkConnectivity = $true
            AuthenticationType = if ($Credential) { 'SQL Authentication' } else { 'Windows Authentication' }
        } | ConvertTo-Json -Depth 3
        
        Write-CheckLog -Message "SQL connectivity test successful in $($executionTime)ms" -Source 'SqlCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MSSQL) -Target $InstanceName -Metric 'Connectivity' -Status ([CheckStatus]::Success) -Value 'Connected' -Details $details -ExecutionTimeMs $executionTime
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        Write-CheckLog -Message "SQL connectivity test failed: $errorMessage" -Level 'Error' -Source 'SqlCheck'
        
        return New-CheckResult -Domain ([MonitoringDomain]::MSSQL) -Target $InstanceName -Metric 'Connectivity' -Status ([CheckStatus]::Critical) -ErrorMessage $errorMessage -ExecutionTimeMs $executionTime
    }
}

# Additional functions would be implemented here (Get-SqlFailedJobs, Get-SqlAgReplicationLag, etc.)
# For brevity, I'm including the key monitoring functions as specified in the ToR

# Export functions
Export-ModuleMember -Function @(
    'Test-SqlServerAvailability',
    'Get-SqlBlockingSessions', 
    'Get-SqlLongRunningQueries',
    'Get-SqlTempDbUsage',
    'Test-SqlConnectivity'
)