# Temenos Operations Quick Reference Guide

## Overview

This guide provides quick reference information for common Temenos Start of Day (SOD) and End of Day (EOD) operations, based on industry best practices and banking operations standards.

## Start of Day (SOD) Operations

### Pre-SOD Health Checks
```powershell
# Check database connectivity
Test-NetConnection -ComputerName "T24-DB01" -Port 1433

# Verify file system availability
Test-Path "\\T24-SRV01\T24Data" -PathType Container

# Check disk space
Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.Size -ne $null} | 
    Select-Object DeviceID, @{n="Size(GB)";e={[math]::Round($_.Size/1GB,2)}}, 
    @{n="FreeSpace(GB)";e={[math]::Round($_.FreeSpace/1GB,2)}}

# Verify service dependencies
Get-Service -Name "MSSQLSERVER", "MSMQ", "IIS Admin Service"
```

### SOD Service Startup Sequence

#### T24 Core Banking
```powershell
# 1. Start T24 Application Server
Start-Service -Name "T24AppServer" -PassThru

# 2. Initialize T24 Database Connections
Invoke-Command -ComputerName "T24-APP01" -ScriptBlock {
    & "C:\T24\bin\StartT24.cmd" -environment PROD -validate
}

# 3. Enable T24 Web Services
Start-Service -Name "T24WebServices" -PassThru
```

#### TPH Payment Hub
```powershell
# 1. Start TPH Core Services
Start-Service -Name "TPHCoreService" -PassThru

# 2. Initialize Payment Channels
Start-Service -Name "TPHChannelManager" -PassThru

# 3. Start SWIFT Connectivity
Start-Service -Name "TPHSWIFTConnector" -PassThru
```

#### IBM MQ Services
```powershell
# 1. Start Queue Manager
Start-Service -Name "IBM MQ" -PassThru

# 2. Verify Queue Manager Status
runmqsc PROD.QM < "DIS QMGR"

# 3. Start Channel Initiator
runmqsc PROD.QM < "START CHINIT"
```

### SOD Validation Steps
```powershell
# Verify T24 is accepting transactions
Invoke-RestMethod -Uri "http://T24-APP01:8080/BrowserWeb/servlet/BrowserServlet" -Method GET

# Check TPH payment processing
Test-NetConnection -ComputerName "TPH-SRV01" -Port 8080

# Validate MQ connectivity
echo "DIS CHSTATUS(*)" | runmqsc PROD.QM

# Verify database transaction log space
SELECT name, log_reuse_wait_desc FROM sys.databases WHERE name = 'T24'
```

## End of Day (EOD) Operations

### Pre-EOD Checks
```powershell
# Check pending transactions
SELECT COUNT(*) as PendingCount FROM T24.FBNK_STMT_ENTRY WHERE RECORD_STATUS = 'IHLD'

# Verify batch job completion
SELECT job_name, last_run_date, last_run_time, last_run_outcome 
FROM msdb.dbo.sysjobs_view WHERE last_run_outcome != 1

# Check system performance
Get-Counter "\Processor(_Total)\% Processor Time", "\Memory\Available MBytes"
```

### EOD Processing Sequence

#### Transaction Cut-off
```powershell
# Stop new transaction acceptance
Invoke-Command -ComputerName "T24-APP01" -ScriptBlock {
    & "C:\T24\bin\StopNewTransactions.cmd" -environment PROD
}

# Wait for in-flight transactions to complete
do {
    $pending = Invoke-Sqlcmd -Query "SELECT COUNT(*) as Count FROM T24.FBNK_STMT_ENTRY WHERE RECORD_STATUS = 'IHLD'"
    Start-Sleep -Seconds 30
} while ($pending.Count -gt 0)
```

#### Daily Processing
```powershell
# Run close of business batch
Invoke-Command -ComputerName "T24-APP01" -ScriptBlock {
    & "C:\T24\bin\RunCOB.cmd" -environment PROD -date (Get-Date -Format "yyyyMMdd")
}

# Execute interest calculations
Start-Job -Name "InterestCalc" -ScriptBlock {
    & "C:\T24\bin\RunInterest.cmd" -environment PROD
}

# Process standing instructions
Start-Job -Name "StandingInstructions" -ScriptBlock {
    & "C:\T24\bin\RunSI.cmd" -environment PROD
}
```

#### Reporting and Reconciliation
```powershell
# Generate regulatory reports
& "C:\T24\Reports\GenerateDailyReports.cmd" -date (Get-Date -Format "yyyyMMdd")

# Run reconciliation processes
& "C:\TPH\bin\RunReconciliation.cmd" -date (Get-Date -Format "yyyyMMdd")

# Generate management reports
& "C:\Reports\GenerateManagementReports.cmd" -date (Get-Date -Format "yyyyMMdd")
```

### Post-EOD Cleanup
```powershell
# Archive transaction logs
Backup-SqlDatabase -ServerInstance "T24-DB01" -Database "T24" -BackupAction Log -BackupFile "C:\Backup\T24_Log_$(Get-Date -Format 'yyyyMMdd_HHmm').trn"

# Cleanup temporary files
Remove-Item "C:\T24\temp\*" -Recurse -Force -ErrorAction SilentlyContinue

# Update system date for next business day
Invoke-Command -ComputerName "T24-APP01" -ScriptBlock {
    & "C:\T24\bin\AdvanceDate.cmd" -environment PROD
}
```

## Common Service Management Commands

### Service Status Checks
```powershell
# Check all Temenos services
$services = @("T24AppServer", "TPHCoreService", "IBM MQ", "MSSQLSERVER")
foreach ($service in $services) {
    Get-Service -Name $service | Select-Object Name, Status, StartType
}

# Check service dependencies
Get-Service -Name "T24AppServer" -DependentServices
Get-Service -Name "T24AppServer" -RequiredServices
```

### Service Control Operations
```powershell
# Start service with timeout
function Start-ServiceWithTimeout {
    param([string]$ServiceName, [int]$TimeoutSeconds = 300)
    
    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq 'Running') {
        Write-Output "Service $ServiceName is already running"
        return $true
    }
    
    Start-Service -Name $ServiceName -PassThru
    $timeout = (Get-Date).AddSeconds($TimeoutSeconds)
    
    do {
        Start-Sleep -Seconds 5
        $service = Get-Service -Name $ServiceName
    } while ($service.Status -ne 'Running' -and (Get-Date) -lt $timeout)
    
    return ($service.Status -eq 'Running')
}

# Stop service gracefully
function Stop-ServiceGracefully {
    param([string]$ServiceName, [int]$TimeoutSeconds = 300)
    
    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq 'Stopped') {
        Write-Output "Service $ServiceName is already stopped"
        return $true
    }
    
    Stop-Service -Name $ServiceName -Force:$false -PassThru
    $timeout = (Get-Date).AddSeconds($TimeoutSeconds)
    
    do {
        Start-Sleep -Seconds 5
        $service = Get-Service -Name $ServiceName
    } while ($service.Status -ne 'Stopped' -and (Get-Date) -lt $timeout)
    
    if ($service.Status -ne 'Stopped') {
        Write-Warning "Forcing stop of service $ServiceName"
        Stop-Service -Name $ServiceName -Force -PassThru
    }
    
    return ($service.Status -eq 'Stopped')
}
```

## Monitoring and Health Checks

### Database Health
```sql
-- Check database status
SELECT name, state_desc, user_access_desc FROM sys.databases 
WHERE name IN ('T24', 'TPH', 'TemenosAlertManager')

-- Check database file sizes
SELECT 
    DB_NAME(database_id) AS DatabaseName,
    name AS LogicalName,
    type_desc AS FileType,
    size/128.0 AS SizeMB,
    max_size/128.0 AS MaxSizeMB,
    is_percent_growth,
    growth/128.0 AS GrowthMB
FROM sys.master_files
WHERE database_id > 4

-- Check active connections
SELECT 
    DB_NAME(database_id) as DatabaseName,
    COUNT(*) as Connections
FROM sys.dm_exec_sessions
WHERE database_id > 0
GROUP BY database_id
```

### Application Health
```powershell
# Test T24 web interface
try {
    $response = Invoke-WebRequest -Uri "http://T24-APP01:8080/BrowserWeb" -TimeoutSec 30
    Write-Output "T24 Web Interface: $($response.StatusCode)"
} catch {
    Write-Error "T24 Web Interface: Failed - $($_.Exception.Message)"
}

# Test TPH API
try {
    $response = Invoke-RestMethod -Uri "http://TPH-SRV01:8080/api/health" -TimeoutSec 30
    Write-Output "TPH API Health: $($response.status)"
} catch {
    Write-Error "TPH API: Failed - $($_.Exception.Message)"
}

# Check MQ queue depths
$queues = @("PAYMENT.IN", "PAYMENT.OUT", "SWIFT.IN", "SWIFT.OUT")
foreach ($queue in $queues) {
    $depth = echo "DIS Q($queue) CURDEPTH" | runmqsc PROD.QM | Select-String "CURDEPTH"
    Write-Output "Queue $queue depth: $depth"
}
```

### Performance Monitoring
```powershell
# Check system resources
Get-Counter -Counter @(
    "\Processor(_Total)\% Processor Time",
    "\Memory\Available MBytes", 
    "\PhysicalDisk(_Total)\% Disk Time",
    "\Network Interface(*)\Bytes Total/sec"
) -SampleInterval 5 -MaxSamples 12

# Check T24 JVM memory usage
$jvmStats = Get-WmiObject -ComputerName "T24-APP01" -Class Win32_Process | Where-Object {$_.Name -eq "java.exe"}
foreach ($process in $jvmStats) {
    $memoryMB = [math]::Round($process.WorkingSetSize / 1MB, 2)
    Write-Output "T24 JVM Process ID $($process.ProcessId): $memoryMB MB"
}
```

## Error Handling and Recovery

### Common Error Scenarios
```powershell
# Service fails to start
function Recover-FailedService {
    param([string]$ServiceName)
    
    Write-Output "Attempting to recover service: $ServiceName"
    
    # Check event logs for errors
    $events = Get-WinEvent -FilterHashtable @{LogName='System'; Level=2,3; StartTime=(Get-Date).AddHours(-1)} | 
              Where-Object {$_.Message -like "*$ServiceName*"}
    
    if ($events) {
        Write-Output "Recent errors found:"
        $events | Select-Object TimeCreated, LevelDisplayName, Message | Format-Table
    }
    
    # Attempt restart
    if (Stop-ServiceGracefully -ServiceName $ServiceName) {
        Start-Sleep -Seconds 10
        if (Start-ServiceWithTimeout -ServiceName $ServiceName) {
            Write-Output "Service $ServiceName recovered successfully"
            return $true
        }
    }
    
    Write-Error "Failed to recover service $ServiceName"
    return $false
}

# Database connection issues
function Test-DatabaseConnectivity {
    param([string]$ServerInstance, [string]$Database)
    
    try {
        $result = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query "SELECT 1 as Test" -QueryTimeout 30
        Write-Output "Database connectivity successful: $ServerInstance\$Database"
        return $true
    } catch {
        Write-Error "Database connectivity failed: $($_.Exception.Message)"
        return $false
    }
}
```

### Rollback Procedures
```powershell
# Rollback SOD if issues detected
function Rollback-SOD {
    param([string]$Environment)
    
    Write-Warning "Initiating SOD rollback for environment: $Environment"
    
    # Stop processing services
    $services = @("T24AppServer", "TPHCoreService")
    foreach ($service in $services) {
        Stop-ServiceGracefully -ServiceName $service
    }
    
    # Restore previous business date
    Invoke-Command -ComputerName "T24-APP01" -ScriptBlock {
        & "C:\T24\bin\RestoreDate.cmd" -environment $Environment
    }
    
    # Send notifications
    Send-MailMessage -To "operations@bank.com" -Subject "SOD Rollback Completed" -Body "SOD rollback completed for $Environment"
}
```

## Emergency Procedures

### Complete System Restart
```powershell
# Emergency restart sequence
function Emergency-SystemRestart {
    Write-Warning "Initiating emergency system restart"
    
    # 1. Stop all Temenos services
    $services = @("T24AppServer", "T24WebServices", "TPHCoreService", "TPHChannelManager")
    foreach ($service in $services) {
        Stop-Service -Name $service -Force -ErrorAction SilentlyContinue
    }
    
    # 2. Wait for graceful shutdown
    Start-Sleep -Seconds 60
    
    # 3. Restart in dependency order
    Start-Service -Name "MSSQLSERVER"
    Start-Sleep -Seconds 30
    
    Start-Service -Name "IBM MQ"
    Start-Sleep -Seconds 30
    
    Start-Service -Name "T24AppServer"
    Start-Sleep -Seconds 60
    
    Start-Service -Name "TPHCoreService"
    
    # 4. Verify system health
    Test-DatabaseConnectivity -ServerInstance "T24-DB01" -Database "T24"
}
```

This quick reference guide provides the essential commands and procedures for managing Temenos environments. All operations should be tested in non-production environments before use in production.