# Remote Deployment Guide for Temenos Alert Manager

## Overview

The Temenos Alert Manager is designed to support distributed deployments where the monitoring application runs on a separate host from the monitored Temenos systems (T24 Core Banking, TPH Payment Hub, IBM MQ, SQL Server). This guide provides comprehensive instructions for configuring and deploying the Alert Manager in distributed environments.

## Architecture Patterns

### 1. Centralized Monitoring (Recommended)

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Alert Manager  │    │  T24 Servers    │    │  TPH Servers    │
│   MON-SRV01     │───▶│   T24-APP01     │    │   TPH-SRV01     │
│                 │    │   T24-APP02     │    │   TPH-SRV02     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │              
         ├──────────────────────────────────────────┐
         ▼                                          ▼
┌─────────────────┐                        ┌─────────────────┐
│   MQ Servers    │                        │  SQL Servers    │
│   MQ-PROD01     │                        │   SQL-PROD01    │
│   MQ-PROD02     │                        │   SQL-PROD02    │
└─────────────────┘                        └─────────────────┘
```

**Benefits:**
- Single point of monitoring administration
- Centralized alerting and reporting
- Reduced resource overhead on production systems
- Simplified security and access control

### 2. Network-Segmented Deployment

```
DMZ Network                    Internal Banking Network
┌─────────────────┐           ┌─────────────────────────────────┐
│  Alert Manager  │           │  T24, TPH, MQ, SQL Servers     │
│   (DMZ-MON01)   │──Firewall─▶│                                │
│                 │           │  Requires WinRM port access    │
└─────────────────┘           └─────────────────────────────────┘
```

**Security Considerations:**
- Firewall rules for WinRM ports (5985/5986)
- Dedicated monitoring service accounts
- Network segregation between monitoring and production

### 3. Multi-Site Monitoring

```
Data Center 1               Data Center 2               Data Center 3
┌──────────────┐           ┌──────────────┐           ┌──────────────┐
│ Temenos Prod │           │ Temenos DR   │           │ Temenos Test │
│ Environment  │           │ Environment  │           │ Environment  │
└──────────────┘           └──────────────┘           └──────────────┘
       │                           │                           │
       └───────────────────────────┼───────────────────────────┘
                                   │
                            ┌──────────────┐
                            │ Centralized  │
                            │Alert Manager │
                            │   (Cloud)    │
                            └──────────────┘
```

## Prerequisites

### Network Requirements

1. **PowerShell Remoting (WinRM)**
   - Enabled on all target Temenos hosts
   - Ports 5985 (HTTP) or 5986 (HTTPS) accessible from Alert Manager
   - Network connectivity between Alert Manager and all target hosts

2. **Firewall Configuration**
   ```powershell
   # On target Temenos hosts
   Enable-PSRemoting -Force
   Set-NetFirewallRule -Name "WINRM-HTTP-In-TCP" -Enabled True
   Set-NetFirewallRule -Name "WINRM-HTTPS-In-TCP" -Enabled True
   ```

3. **DNS Resolution**
   - All target hostnames resolvable from Alert Manager
   - Consider using FQDNs for cross-domain scenarios

### Security Requirements

1. **Service Accounts**
   - Dedicated monitoring service accounts per domain/environment
   - Minimal required permissions on target systems
   - Regular credential rotation procedures

2. **Authentication**
   ```powershell
   # Test authentication to target hosts
   Test-WSMan -ComputerName "TPH-SRV01.bank.local"
   Test-WSMan -ComputerName "T24-APP01.bank.local" -Credential $cred
   ```

3. **Permissions Required**
   - Read access to Windows Services
   - Remote PowerShell execution rights
   - Local logon rights (for NTLM authentication)
   - Event log read access (for error monitoring)

## Configuration Guide

### 1. Alert Manager Host Configuration

#### appsettings.json Configuration
```json
{
  "PowerShell": {
    "ModuleBasePath": "scripts/PowerShell/Modules",
    "RemoteExecutionTimeoutSeconds": 300,
    "MaxConcurrentRemoteSessions": 10,
    "UseSecureRemoting": true,
    "RemotingPort": 5986
  },
  "Application": {
    "DashboardUrl": "https://monitor.bank.local:5001",
    "Environment": "Production",
    "DataCenter": "Primary"
  }
}
```

#### Database Configuration Setup
```sql
-- Service Configuration Examples
INSERT INTO ServiceConfigs (Name, Host, Type, IsEnabled, Description) VALUES
('TPHPaymentService', 'tph-prod01.bank.local', 0, 1, 'Primary TPH Payment Processing'),
('TPHProcessingService', 'tph-prod02.bank.local', 0, 1, 'Secondary TPH Processing'),
('T24Server', 't24-app01.bank.local', 1, 1, 'T24 Core Banking Application'),
('IBM MQ', 'mq-prod01.bank.local', 2, 1, 'Production MQ Queue Manager');

-- SQL Target Configuration
INSERT INTO SqlTargetConfigs (InstanceName, DatabaseName, Role, IsEnabled, Description) VALUES
('sql-prod01.bank.local', 'T24_PROD', 'Primary', 1, 'T24 Production Database'),
('sql-dr01.bank.local', 'T24_PROD', 'Secondary', 1, 'T24 DR Database');
```

### 2. Target Host Configuration

#### Enable PowerShell Remoting
```powershell
# Run on each target Temenos host
Enable-PSRemoting -Force
Set-ExecutionPolicy RemoteSigned -Force

# Configure HTTPS (recommended for production)
New-SelfSignedCertificate -DnsName $env:COMPUTERNAME -CertStoreLocation Cert:\LocalMachine\My
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object {$_.Subject -like "*$env:COMPUTERNAME*"}
winrm create winrm/config/Listener?Address=*+Transport=HTTPS @{Hostname="$env:COMPUTERNAME";CertificateThumbprint="$($cert.Thumbprint)"}
```

#### Service Account Setup
```powershell
# Create dedicated monitoring service account
$username = "BANK\svc-temenos-monitor"
$password = ConvertTo-SecureString "SecurePassword123!" -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential($username, $password)

# Grant required permissions
Add-LocalGroupMember -Group "Remote Management Users" -Member $username
```

### 3. Network Configuration

#### Firewall Rules
```powershell
# On Alert Manager host (outbound)
New-NetFirewallRule -DisplayName "WinRM-Out" -Direction Outbound -Protocol TCP -LocalPort 5985,5986 -Action Allow

# On target hosts (inbound)
New-NetFirewallRule -DisplayName "WinRM-In" -Direction Inbound -Protocol TCP -LocalPort 5985,5986 -Action Allow
```

#### Test Connectivity
```powershell
# Test from Alert Manager to target hosts
Test-NetConnection -ComputerName "tph-prod01.bank.local" -Port 5985
Test-NetConnection -ComputerName "t24-app01.bank.local" -Port 5986
Test-WSMan -ComputerName "mq-prod01.bank.local"
```

## Deployment Scenarios

### Scenario 1: Single Data Center Deployment

**Architecture:**
- Alert Manager: `monitor.bank.local`
- T24 Cluster: `t24-app01.bank.local`, `t24-app02.bank.local`
- TPH Cluster: `tph-srv01.bank.local`, `tph-srv02.bank.local`
- MQ Cluster: `mq-prod01.bank.local`, `mq-prod02.bank.local`
- SQL AlwaysOn: `sql-prod01.bank.local` (Primary), `sql-prod02.bank.local` (Secondary)

**Configuration Steps:**
1. Install Alert Manager on dedicated monitoring server
2. Configure service accounts with appropriate permissions
3. Set up database with target host configurations
4. Enable PowerShell remoting on all target hosts
5. Test connectivity and authentication

### Scenario 2: Multi-Data Center Deployment

**Architecture:**
- Primary DC: Production Temenos environment
- DR DC: Disaster recovery environment  
- Cloud: Centralized Alert Manager

**Additional Considerations:**
- WAN latency and timeout adjustments
- Site-specific service accounts
- Network security between sites
- DNS resolution across domains

### Scenario 3: Hybrid Cloud Deployment

**Architecture:**
- On-premises: Temenos production systems
- Cloud: Alert Manager and dashboard
- Connectivity: VPN or ExpressRoute

**Security Requirements:**
- Encrypted PowerShell remoting (HTTPS)
- Certificate-based authentication
- Network ACLs and security groups
- Regular credential rotation

## Troubleshooting

### Common Issues

#### PowerShell Remoting Failures
```powershell
# Enable detailed logging
$PSSessionConfigurationName = "Microsoft.PowerShell"
Set-PSSessionConfiguration -Name $PSSessionConfigurationName -ShowSecurityDescriptorUI

# Check WinRM configuration
winrm enumerate winrm/config/listener
winrm get winrm/config/service
```

#### Authentication Issues
```powershell
# Test different authentication methods
$cred = Get-Credential
New-PSSession -ComputerName "target-host" -Credential $cred -Authentication Negotiate
New-PSSession -ComputerName "target-host" -Credential $cred -Authentication Kerberos
```

#### Network Connectivity
```powershell
# Comprehensive connectivity test
function Test-RemoteAccess {
    param($ComputerName)
    
    Write-Host "Testing connectivity to $ComputerName"
    Test-NetConnection -ComputerName $ComputerName -Port 5985
    Test-NetConnection -ComputerName $ComputerName -Port 5986
    Test-WSMan -ComputerName $ComputerName
    
    try {
        Invoke-Command -ComputerName $ComputerName -ScriptBlock { $env:COMPUTERNAME }
        Write-Host "PowerShell remoting successful"
    } catch {
        Write-Error "PowerShell remoting failed: $($_.Exception.Message)"
    }
}
```

### Monitoring and Logging

#### Alert Manager Logs
```bash
# View Alert Manager logs
tail -f logs/temenos-alert-manager-*.log

# Filter for remote execution issues
grep -i "remote\|powershell\|session" logs/temenos-alert-manager-*.log
```

#### Windows Event Logs
```powershell
# Check WinRM logs on target hosts
Get-WinEvent -LogName "Microsoft-Windows-WinRM/Operational" | Select-Object -First 10

# Check PowerShell execution logs
Get-WinEvent -LogName "Microsoft-Windows-PowerShell/Operational" | Where-Object {$_.Id -eq 4104}
```

## Security Best Practices

### 1. Credential Management
- Use dedicated service accounts per environment
- Implement regular password rotation
- Store credentials securely using Windows Credential Manager
- Avoid hardcoded credentials in configuration files

### 2. Network Security
- Use HTTPS for PowerShell remoting in production
- Implement network segmentation and firewall rules
- Monitor and log all remote access attempts
- Regular security assessments of remote connections

### 3. Access Control
- Principle of least privilege for monitoring accounts
- Regular review of account permissions
- Audit trail for all monitoring activities
- Separation of duties for production access

### 4. Monitoring Security
- Alert on authentication failures
- Monitor for unusual remote access patterns
- Log all configuration changes
- Regular verification of security configurations

## Performance Considerations

### 1. Connection Pooling
- Limit concurrent remote sessions
- Implement connection timeouts
- Reuse PowerShell sessions where possible
- Monitor resource usage on target hosts

### 2. Scheduling
- Distribute monitoring checks across time windows
- Avoid peak business hours for intensive checks
- Implement jitter in check intervals
- Consider target host maintenance windows

### 3. Network Optimization
- Use compression for PowerShell remoting
- Optimize check frequency based on criticality
- Implement check result caching where appropriate
- Monitor network bandwidth usage

## Maintenance Procedures

### 1. Regular Tasks
- Credential rotation (monthly/quarterly)
- Certificate renewal for HTTPS remoting
- Security configuration reviews
- Performance monitoring and tuning

### 2. Disaster Recovery
- Document all remote host configurations
- Backup Alert Manager configuration and database
- Test failover procedures for multi-site deployments
- Maintain updated network diagrams and documentation

### 3. Upgrades and Changes
- Test PowerShell module updates in non-production
- Coordinate with target host maintenance windows
- Version control for all configuration changes
- Rollback procedures for failed deployments

## Support and Troubleshooting Contacts

- **Network Issues**: Contact network team for firewall and connectivity
- **Authentication Issues**: Contact security team for service account problems  
- **Target Host Issues**: Contact system administrators for specific Temenos hosts
- **Alert Manager Issues**: Contact monitoring team for application problems

For additional support, refer to the PowerShell module documentation and Windows PowerShell remoting guides.