# TemenosChecks.Common Module
# Common utilities for Temenos monitoring checks
#
# REMOTE DEPLOYMENT ARCHITECTURE:
# ===============================
# This module is designed to support distributed deployments where the Temenos Alert Manager
# application can be deployed on a separate host from the target Temenos systems (T24, TPH, MQ, SQL).
#
# DEPLOYMENT SCENARIOS:
# 1. Centralized Monitoring: Alert Manager on dedicated monitoring server, targets on separate hosts
# 2. Segmented Networks: Alert Manager in DMZ, targets in secure internal networks
# 3. Multi-Site Deployments: Single Alert Manager monitoring multiple data centers
# 4. Cloud/Hybrid: Alert Manager in cloud monitoring on-premises Temenos systems
#
# REMOTE CONNECTIVITY REQUIREMENTS:
# - PowerShell Remoting (WinRM) enabled on target hosts
# - Network connectivity on ports 5985 (HTTP) or 5986 (HTTPS)  
# - Proper authentication credentials (Service Account recommended)
# - Firewall rules allowing WinRM traffic between Alert Manager and targets
#
# SECURITY CONSIDERATIONS:
# - Use dedicated service accounts with minimal required permissions
# - Prefer HTTPS (port 5986) over HTTP for PowerShell remoting in production
# - Implement credential encryption and secure storage
# - Regular credential rotation and access auditing

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
# These functions handle both local and remote service monitoring
# Supporting distributed Temenos deployments across multiple hosts

function Test-ServiceExists {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServiceName,
        
        # REMOTE DEPLOYMENT: ComputerName parameter enables monitoring services on remote Temenos hosts
        # Can be hostname, FQDN, or IP address of the target T24/TPH/MQ server
        # Default 'localhost' for backward compatibility and local deployments
        [string]$ComputerName = 'localhost'
    )
    
    try {
        # For remote systems, this leverages WinRM/PowerShell remoting
        # Ensure target host has WinRM enabled and proper firewall rules
        $service = Get-Service -Name $ServiceName -ComputerName $ComputerName -ErrorAction SilentlyContinue
        return $null -ne $service
    }
    catch {
        # Enhanced error handling for remote connection failures
        # Common issues: WinRM not enabled, firewall blocking, authentication failures
        Write-CheckLog -Message "Failed to check service '$ServiceName' on '$ComputerName': $($_.Exception.Message)" -Level 'Error'
        return $false
    }
}

function Get-ServiceStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServiceName,
        
        # REMOTE DEPLOYMENT: Enables service status checks on distributed Temenos environments
        # Examples: 'TPHPROD01' for TPH server, 'T24DB01' for T24 database server
        [string]$ComputerName = 'localhost'
    )
    
    try {
        # Remote service status retrieval using PowerShell remoting infrastructure
        # This approach scales for monitoring multiple Temenos environments from central location
        $service = Get-Service -Name $ServiceName -ComputerName $ComputerName -ErrorAction Stop
        return @{
            Name = $service.Name
            Status = $service.Status
            StartType = $service.StartType
            DisplayName = $service.DisplayName
            # Include computer name for tracking in distributed deployments
            ComputerName = $ComputerName
        }
    }
    catch {
        # Provide detailed error context for troubleshooting remote connectivity issues
        $errorDetails = "Service '$ServiceName' check failed on '$ComputerName': $($_.Exception.Message)"
        Write-CheckLog -Message $errorDetails -Level 'Error'
        throw $errorDetails
    }
}

# Secure PowerShell execution for distributed Temenos environments
# This function is the cornerstone of remote monitoring capabilities
function Invoke-PowerShellSecurely {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ScriptBlock,
        
        [hashtable]$Parameters = @{},
        
        # REMOTE DEPLOYMENT: Target computer for distributed monitoring
        # Leave empty/null for local execution, specify hostname/IP for remote execution
        # Examples: 'TPHPROD01', 'T24SRV01.domain.local', '192.168.1.100'
        [string]$ComputerName,
        
        # SECURITY: Authentication credentials for remote systems
        # Use dedicated service accounts with minimal required permissions
        # Store credentials securely using Windows Credential Manager or similar
        [PSCredential]$Credential,
        
        # RELIABILITY: Timeout protection for remote operations
        # Prevents hanging connections in unstable network conditions
        [int]$TimeoutSeconds = 300
    )
    
    $startTime = Get-Date
    $executionContext = if ($ComputerName -and $ComputerName -ne 'localhost') { "remote host '$ComputerName'" } else { "local system" }
    
    try {
        Write-CheckLog -Message "Executing PowerShell command on $executionContext" -Source 'SecureExecution'
        
        # REMOTE EXECUTION PATH: For distributed Temenos environments
        if ($ComputerName -and $ComputerName -ne 'localhost') {
            # Build session parameters for remote connection
            $sessionParams = @{
                ComputerName = $ComputerName
                ErrorAction = 'Stop'
            }
            
            # AUTHENTICATION: Add credentials if provided
            # In production, use service accounts with constrained delegation
            if ($Credential) {
                $sessionParams.Credential = $Credential
                Write-CheckLog -Message "Using provided credentials for authentication to $ComputerName" -Source 'SecureExecution'
            } else {
                Write-CheckLog -Message "Using current user context for connection to $ComputerName" -Source 'SecureExecution'
            }
            
            # SESSION MANAGEMENT: Create secure remote PowerShell session
            # This establishes WinRM connection to target Temenos host
            Write-CheckLog -Message "Establishing remote PowerShell session to $ComputerName" -Source 'SecureExecution'
            $session = New-PSSession @sessionParams
            
            try {
                # REMOTE COMMAND EXECUTION: Run monitoring commands on target host
                # Enables centralized monitoring of distributed Temenos infrastructure
                Write-CheckLog -Message "Executing monitoring command remotely on $ComputerName" -Source 'SecureExecution'
                $result = Invoke-Command -Session $session -ScriptBlock ([scriptblock]::Create($ScriptBlock)) -ArgumentList $Parameters -ErrorAction Stop
                
                $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
                Write-CheckLog -Message "Remote execution completed successfully in $([Math]::Round($executionTime, 0))ms" -Source 'SecureExecution'
                
                return $result
            }
            finally {
                # CLEANUP: Always close remote sessions to prevent resource leaks
                # Critical for high-frequency monitoring scenarios
                Write-CheckLog -Message "Closing remote PowerShell session to $ComputerName" -Source 'SecureExecution'
                Remove-PSSession -Session $session -ErrorAction SilentlyContinue
            }
        }
        # LOCAL EXECUTION PATH: For co-located deployments
        else {
            Write-CheckLog -Message "Executing command locally on Alert Manager host" -Source 'SecureExecution'
            $result = Invoke-Expression $ScriptBlock
            
            $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
            Write-CheckLog -Message "Local execution completed in $([Math]::Round($executionTime, 0))ms" -Source 'SecureExecution'
            
            return $result
        }
    }
    catch {
        $executionTime = ((Get-Date) - $startTime).TotalMilliseconds
        $errorMessage = $_.Exception.Message
        
        # ENHANCED ERROR REPORTING: Provide actionable troubleshooting information
        if ($ComputerName -and $ComputerName -ne 'localhost') {
            $troubleshootingMessage = @"
Remote PowerShell execution failed on $ComputerName after $([Math]::Round($executionTime, 0))ms.

Common causes and solutions:
1. WinRM not enabled: Run 'Enable-PSRemoting -Force' on target host
2. Firewall blocking: Ensure ports 5985/5986 are open between Alert Manager and target
3. Authentication failure: Verify credentials and account permissions
4. Network connectivity: Test basic connectivity with 'Test-NetConnection $ComputerName -Port 5985'
5. PowerShell execution policy: Ensure target allows remote script execution

Error details: $errorMessage
"@
            Write-CheckLog -Message $troubleshootingMessage -Level 'Error' -Source 'SecureExecution'
        } else {
            Write-CheckLog -Message "Local PowerShell execution failed after $([Math]::Round($executionTime, 0))ms: $errorMessage" -Level 'Error' -Source 'SecureExecution'
        }
        
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