# TemenosChecks.Common Module
# Common utilities for Temenos monitoring checks

# Enums to match .NET application
enum CheckStatus {
    Success = 0
    Warning = 1
    Critical = 2
    Error = 3
}

enum MonitoringDomain {
    TPH = 0
    T24 = 1
    MQ = 2
    MSSQL = 3
    Host = 4
    JVM = 5
}

# Common logging function
function Write-CheckLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Message,
        
        [ValidateSet('Debug', 'Info', 'Warning', 'Error')]
        [string]$Level = 'Info',
        
        [string]$Source = 'TemenosCheck'
    )
    
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [$Level] [$Source] $Message"
    
    switch ($Level) {
        'Debug' { Write-Debug $logMessage }
        'Info' { Write-Information $logMessage -InformationAction Continue }
        'Warning' { Write-Warning $logMessage }
        'Error' { Write-Error $logMessage }
    }
    
    # Also log to Windows Event Log for auditing
    try {
        if (-not [System.Diagnostics.EventLog]::SourceExists($Source)) {
            [System.Diagnostics.EventLog]::CreateEventSource($Source, 'Application')
        }
        
        $eventType = switch ($Level) {
            'Error' { 'Error' }
            'Warning' { 'Warning' }
            default { 'Information' }
        }
        
        Write-EventLog -LogName 'Application' -Source $Source -EventId 1000 -EntryType $eventType -Message $logMessage
    }
    catch {
        # Ignore event log errors to prevent check failures
    }
}

# Create standardized check result
function New-CheckResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [MonitoringDomain]$Domain,
        
        [Parameter(Mandatory)]
        [string]$Target,
        
        [Parameter(Mandatory)]
        [string]$Metric,
        
        [Parameter(Mandatory)]
        [CheckStatus]$Status,
        
        [string]$Value,
        
        [string]$Details,
        
        [string]$ErrorMessage,
        
        [double]$ExecutionTimeMs
    )
    
    return [PSCustomObject]@{
        Domain = $Domain
        Target = $Target
        Metric = $Metric
        Status = $Status
        Value = $Value
        Details = $Details
        ErrorMessage = $ErrorMessage
        ExecutionTimeMs = $ExecutionTimeMs
        CheckedAt = (Get-Date).ToUniversalTime()
    }
}

# Service validation functions
function Test-ServiceExists {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServiceName,
        
        [string]$ComputerName = 'localhost'
    )
    
    try {
        $service = Get-Service -Name $ServiceName -ComputerName $ComputerName -ErrorAction SilentlyContinue
        return $null -ne $service
    }
    catch {
        return $false
    }
}

function Get-ServiceStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServiceName,
        
        [string]$ComputerName = 'localhost'
    )
    
    try {
        $service = Get-Service -Name $ServiceName -ComputerName $ComputerName -ErrorAction Stop
        return @{
            Name = $service.Name
            Status = $service.Status
            StartType = $service.StartType
            DisplayName = $service.DisplayName
        }
    }
    catch {
        throw "Service '$ServiceName' not found on '$ComputerName': $($_.Exception.Message)"
    }
}

# Secure PowerShell execution
function Invoke-PowerShellSecurely {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ScriptBlock,
        
        [hashtable]$Parameters = @{},
        
        [string]$ComputerName,
        
        [PSCredential]$Credential,
        
        [int]$TimeoutSeconds = 300
    )
    
    $startTime = Get-Date
    
    try {
        if ($ComputerName -and $ComputerName -ne 'localhost') {
            $sessionParams = @{
                ComputerName = $ComputerName
                ErrorAction = 'Stop'
            }
            
            if ($Credential) {
                $sessionParams.Credential = $Credential
            }
            
            $session = New-PSSession @sessionParams
            
            try {
                $result = Invoke-Command -Session $session -ScriptBlock ([scriptblock]::Create($ScriptBlock)) -ArgumentList $Parameters -ErrorAction Stop
                return $result
            }
            finally {
                Remove-PSSession -Session $session -ErrorAction SilentlyContinue
            }
        }
        else {
            $result = Invoke-Expression $ScriptBlock
            return $result
        }
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        Write-CheckLog -Message "PowerShell execution failed after $($executionTime)ms: $($_.Exception.Message)" -Level 'Error'
        throw
    }
}

# Threshold parsing utilities
function ConvertTo-ThresholdObject {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ThresholdJson
    )
    
    try {
        return $ThresholdJson | ConvertFrom-Json
    }
    catch {
        Write-CheckLog -Message "Failed to parse threshold JSON: $ThresholdJson" -Level 'Warning'
        return @{}
    }
}

# Network connectivity testing
function Test-NetworkConnectivity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TargetHost,
        
        [int]$Port,
        
        [int]$TimeoutMs = 5000
    )
    
    try {
        if ($Port) {
            $tcpClient = New-Object System.Net.Sockets.TcpClient
            $connectTask = $tcpClient.ConnectAsync($TargetHost, $Port)
            
            if ($connectTask.Wait($TimeoutMs)) {
                $tcpClient.Close()
                return $true
            }
            else {
                $tcpClient.Close()
                return $false
            }
        }
        else {
            $ping = New-Object System.Net.NetworkInformation.Ping
            $result = $ping.Send($TargetHost, $TimeoutMs)
            return $result.Status -eq 'Success'
        }
    }
    catch {
        return $false
    }
}

# Windows Event Log analysis
function Get-WindowsEventLogs {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$LogName,
        
        [string[]]$EventId,
        
        [ValidateSet('Critical', 'Error', 'Warning', 'Information', 'Verbose')]
        [string[]]$Level = @('Critical', 'Error'),
        
        [DateTime]$StartTime = (Get-Date).AddHours(-1),
        
        [DateTime]$EndTime = (Get-Date),
        
        [string]$ComputerName = 'localhost',
        
        [int]$MaxEvents = 100
    )
    
    try {
        $filterHashtable = @{
            LogName = $LogName
            StartTime = $StartTime
            EndTime = $EndTime
        }
        
        if ($EventId) {
            $filterHashtable.Id = $EventId
        }
        
        if ($Level) {
            $filterHashtable.Level = $Level
        }
        
        $events = Get-WinEvent -FilterHashtable $filterHashtable -ComputerName $ComputerName -MaxEvents $MaxEvents -ErrorAction SilentlyContinue
        
        return $events | ForEach-Object {
            [PSCustomObject]@{
                TimeCreated = $_.TimeCreated
                Id = $_.Id
                LevelDisplayName = $_.LevelDisplayName
                Message = $_.Message
                Source = $_.ProviderName
                MachineName = $_.MachineName
            }
        }
    }
    catch {
        Write-CheckLog -Message "Failed to retrieve events from log '$LogName': $($_.Exception.Message)" -Level 'Warning'
        return @()
    }
}

# Export functions
Export-ModuleMember -Function @(
    'Write-CheckLog',
    'New-CheckResult',
    'Test-ServiceExists',
    'Get-ServiceStatus',
    'Invoke-PowerShellSecurely',
    'ConvertTo-ThresholdObject',
    'Test-NetworkConnectivity',
    'Get-WindowsEventLogs'
)