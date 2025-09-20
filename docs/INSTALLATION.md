# Installation and Operations Guide

## Temenos Alert Manager - Production Deployment

### System Requirements

#### Minimum Requirements
- **Operating System**: Windows Server 2019 or later
- **Memory**: 8 GB RAM
- **Storage**: 50 GB available disk space
- **CPU**: 4 cores (2.0 GHz or higher)
- **Network**: Gigabit Ethernet

#### Recommended Requirements
- **Operating System**: Windows Server 2022
- **Memory**: 16 GB RAM
- **Storage**: 100 GB SSD storage
- **CPU**: 8 cores (2.5 GHz or higher)
- **Network**: Redundant Gigabit Ethernet

#### Software Prerequisites
- **.NET 8 Runtime**: ASP.NET Core Hosting Bundle
- **SQL Server**: 2019 or later (Express minimum, Standard/Enterprise recommended)
- **PowerShell**: 7.0 or later
- **IIS**: Internet Information Services with Windows Authentication
- **SQL Server PowerShell Module**: For database monitoring

### Pre-Installation Setup

#### 1. Service Accounts
Create dedicated service accounts for security isolation:

```powershell
# Create service account for application pool
$password = ConvertTo-SecureString "ComplexPassword123!" -AsPlainText -Force
New-LocalUser -Name "svc-temenosalert" -Password $password -Description "Temenos Alert Manager Service Account" -PasswordNeverExpires

# Grant Log on as a service right
$username = "svc-temenosalert"
$tempPath = [System.IO.Path]::GetTempPath()
$import = Join-Path -Path $tempPath -ChildPath "import.inf"
$export = Join-Path -Path $tempPath -ChildPath "export.inf"
$secedt = Join-Path -Path $tempPath -ChildPath "secedt.sdb"

# Export current policy
secedit /export /cfg $export

# Modify policy
$sid = (New-Object System.Security.Principal.NTAccount $username).Translate([System.Security.Principal.SecurityIdentifier])
$content = Get-Content $export
$content = $content -replace "SeServiceLogonRight = ", "SeServiceLogonRight = $sid,"
$content | Set-Content $import

# Import modified policy
secedit /import /cfg $import /db $secedt
secedit /configure /db $secedt
gpupdate /force
```

#### 2. Database Setup
```sql
-- Create database
CREATE DATABASE TemenosAlertManager;

-- Create application user (if using SQL Authentication)
USE [master]
CREATE LOGIN [TemenosAlertApp] WITH PASSWORD = 'SecurePassword123!'

USE [TemenosAlertManager]
CREATE USER [TemenosAlertApp] FOR LOGIN [TemenosAlertApp]
ALTER ROLE [db_datareader] ADD MEMBER [TemenosAlertApp]
ALTER ROLE [db_datawriter] ADD MEMBER [TemenosAlertApp]
ALTER ROLE [db_ddladmin] ADD MEMBER [TemenosAlertApp]
```

#### 3. Active Directory Groups
Create AD groups for role-based access:

```powershell
# Create AD groups (run on domain controller)
Import-Module ActiveDirectory

New-ADGroup -Name "TEMENOS_ALERT_ADMINS" -GroupScope Global -GroupCategory Security -Description "Temenos Alert Manager Administrators"
New-ADGroup -Name "TEMENOS_ALERT_OPERATORS" -GroupScope Global -GroupCategory Security -Description "Temenos Alert Manager Operators"
New-ADGroup -Name "TEMENOS_ALERT_VIEWERS" -GroupScope Global -GroupCategory Security -Description "Temenos Alert Manager Viewers"

# Add users to appropriate groups
Add-ADGroupMember -Identity "TEMENOS_ALERT_ADMINS" -Members "admin1", "admin2"
Add-ADGroupMember -Identity "TEMENOS_ALERT_OPERATORS" -Members "operator1", "operator2"
Add-ADGroupMember -Identity "TEMENOS_ALERT_VIEWERS" -Members "viewer1", "viewer2"
```

### Installation Steps

#### 1. Build and Publish Application
```bash
# Clone repository
git clone https://github.com/asembereng/Temenos-Scripts.git
cd Temenos-Scripts

# Build in Release mode
dotnet build -c Release

# Publish application
dotnet publish src/TemenosAlertManager.Api -c Release -o C:\Apps\TemenosAlertManager
```

#### 2. Configure IIS

```powershell
# Import IIS module
Import-Module WebAdministration

# Create application pool
New-WebAppPool -Name "TemenosAlertManager" -Force
Set-ItemProperty -Path "IIS:\AppPools\TemenosAlertManager" -Name "processModel.identityType" -Value "SpecificUser"
Set-ItemProperty -Path "IIS:\AppPools\TemenosAlertManager" -Name "processModel.userName" -Value ".\svc-temenosalert"
Set-ItemProperty -Path "IIS:\AppPools\TemenosAlertManager" -Name "processModel.password" -Value "ComplexPassword123!"
Set-ItemProperty -Path "IIS:\AppPools\TemenosAlertManager" -Name "managedRuntimeVersion" -Value ""

# Create website
New-Website -Name "TemenosAlertManager" -Port 443 -SSL -PhysicalPath "C:\Apps\TemenosAlertManager" -ApplicationPool "TemenosAlertManager"

# Configure authentication
Set-WebConfiguration -Filter "/system.webServer/security/authentication/windowsAuthentication" -Value @{enabled="true"} -PSPath "IIS:\Sites\TemenosAlertManager"
Set-WebConfiguration -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Value @{enabled="false"} -PSPath "IIS:\Sites\TemenosAlertManager"

# Configure authorization
Add-WebConfiguration -Filter "/system.webServer/security/authorization" -Value @{accessType="Allow"; users=""; roles="TEMENOS_ALERT_ADMINS,TEMENOS_ALERT_OPERATORS,TEMENOS_ALERT_VIEWERS"} -PSPath "IIS:\Sites\TemenosAlertManager"
```

#### 3. SSL Certificate Configuration
```powershell
# Bind SSL certificate (replace thumbprint with your certificate)
$certThumbprint = "YOUR_CERTIFICATE_THUMBPRINT"
New-WebBinding -Name "TemenosAlertManager" -Protocol "https" -Port 443
Get-ChildItem Cert:\LocalMachine\My\$certThumbprint | New-Item IIS:\SslBindings\0.0.0.0!443
```

#### 4. Configure Application Settings
Update `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQLSERVER01;Database=TemenosAlertManager;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "TemenosAlertManager": "Information"
    }
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "C:\\Logs\\TemenosAlertManager\\temenos-alert-manager-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 90
        }
      },
      {
        "Name": "EventLog",
        "Args": {
          "source": "TemenosAlertManager",
          "logName": "Application"
        }
      }
    ]
  },
  "PowerShell": {
    "ModuleBasePath": "C:\\Apps\\TemenosAlertManager\\PowerShell\\Modules"
  },
  "Email": {
    "Smtp": {
      "Host": "smtp.company.local",
      "Port": "25",
      "EnableSsl": "false"
    },
    "FromAddress": "temenosalerts@company.local",
    "FromName": "Temenos Alert Manager"
  }
}
```

### Post-Installation Configuration

#### 1. Database Migration
```bash
# Run database migrations
cd C:\Apps\TemenosAlertManager
dotnet TemenosAlertManager.Api.dll --migrate
```

#### 2. PowerShell Module Setup
```powershell
# Copy PowerShell modules
Copy-Item -Path "scripts\PowerShell\Modules\*" -Destination "C:\Apps\TemenosAlertManager\PowerShell\Modules" -Recurse

# Import required modules globally
Import-Module C:\Apps\TemenosAlertManager\PowerShell\Modules\TemenosChecks.Common
Import-Module SqlServer  # Required for SQL monitoring
```

#### 3. Windows Firewall Configuration
```powershell
# Allow HTTPS traffic
New-NetFirewallRule -DisplayName "Temenos Alert Manager HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow

# Allow SQL Server connectivity (if on separate server)
New-NetFirewallRule -DisplayName "SQL Server" -Direction Outbound -Protocol TCP -RemotePort 1433 -Action Allow
```

### Operational Procedures

#### Daily Tasks
1. **Check Application Status**
   ```powershell
   # Verify IIS application pool is running
   Get-IISAppPool -Name "TemenosAlertManager"
   
   # Check website status
   Get-IISSite -Name "TemenosAlertManager"
   
   # Verify database connectivity
   Test-NetConnection -ComputerName "SQLSERVER01" -Port 1433
   ```

2. **Review Logs**
   ```powershell
   # Check application logs
   Get-Content "C:\Logs\TemenosAlertManager\temenos-alert-manager-$(Get-Date -Format 'yyyyMMdd').log" -Tail 20
   
   # Check Windows Event Log
   Get-WinEvent -LogName Application -FilterXPath "*[System[Provider[@Name='TemenosAlertManager']]]" -MaxEvents 10
   ```

#### Weekly Tasks
1. **Database Maintenance**
   ```sql
   -- Check database size and growth
   SELECT 
       name,
       size/128.0 AS SizeMB,
       max_size/128.0 AS MaxSizeMB
   FROM sys.database_files;
   
   -- Cleanup old audit logs (retain 90 days)
   DELETE FROM AuditEvents 
   WHERE EventTime < DATEADD(day, -90, GETDATE());
   
   -- Update statistics
   EXEC sp_updatestats;
   ```

2. **Performance Review**
   - Review Hangfire dashboard for job performance
   - Check PowerShell execution times
   - Analyze alert response times

#### Monthly Tasks
1. **Security Review**
   - Review AD group memberships
   - Audit user access logs
   - Update service account passwords

2. **Capacity Planning**
   - Monitor disk space usage
   - Review database growth trends
   - Analyze system performance metrics

### Troubleshooting Guide

#### Common Issues

1. **Windows Authentication Failures**
   ```
   Error: 401 Unauthorized
   ```
   **Solution:**
   - Verify SPN registration: `setspn -L svc-temenosalert`
   - Check application pool identity
   - Ensure domain connectivity

2. **Database Connection Issues**
   ```
   Error: Cannot open database "TemenosAlertManager"
   ```
   **Solution:**
   - Test SQL connectivity: `Test-NetConnection SQLSERVER01 -Port 1433`
   - Verify service account permissions
   - Check SQL Server authentication mode

3. **PowerShell Module Not Found**
   ```
   Error: Module 'TemenosChecks.Sql' not found
   ```
   **Solution:**
   - Verify module path in configuration
   - Check PowerShell execution policy: `Get-ExecutionPolicy`
   - Ensure modules are accessible to service account

#### Performance Issues

1. **Slow Dashboard Loading**
   - Check database query performance
   - Review Hangfire job queue
   - Analyze PowerShell execution times

2. **High Memory Usage**
   - Monitor application pool recycling
   - Check for memory leaks in PowerShell scripts
   - Review large query results

### Backup and Recovery

#### Database Backup
```sql
-- Full backup
BACKUP DATABASE TemenosAlertManager 
TO DISK = 'C:\Backup\TemenosAlertManager_Full.bak'
WITH FORMAT, INIT, NAME = 'Full Backup of TemenosAlertManager';

-- Differential backup (daily)
BACKUP DATABASE TemenosAlertManager 
TO DISK = 'C:\Backup\TemenosAlertManager_Diff.bak'
WITH DIFFERENTIAL, FORMAT, INIT, NAME = 'Differential Backup of TemenosAlertManager';
```

#### Application Backup
```powershell
# Backup application files and configuration
$backupPath = "C:\Backup\TemenosAlertManager_$(Get-Date -Format 'yyyyMMdd')"
Copy-Item "C:\Apps\TemenosAlertManager" $backupPath -Recurse
```

### Monitoring the Monitor

#### Health Checks
1. **Application Health Endpoint**
   ```
   GET https://temenosalerts.company.local/api/health/dashboard
   ```

2. **Database Health**
   ```sql
   -- Check active connections
   SELECT 
       DB_NAME(database_id) as DatabaseName,
       COUNT(*) as Connections
   FROM sys.dm_exec_sessions
   WHERE database_id > 0
   GROUP BY database_id;
   ```

3. **PowerShell Module Health**
   ```powershell
   # Test module functionality
   Import-Module TemenosChecks.Common
   Test-NetworkConnectivity -TargetHost "localhost" -Port 443
   ```

#### Performance Metrics
- Response times for API endpoints
- PowerShell script execution times
- Database query performance
- Alert delivery success rates

### Security Hardening

#### Application Security
1. **Disable unnecessary HTTP methods**
2. **Configure security headers**
3. **Implement rate limiting**
4. **Enable detailed error logging**

#### Server Security
1. **Apply Windows updates regularly**
2. **Configure Windows Firewall**
3. **Enable Windows Defender**
4. **Regular security scanning**

#### Database Security
1. **Enable TDE (Transparent Data Encryption)**
2. **Configure SQL Server audit**
3. **Regular security updates**
4. **Monitor failed login attempts**

This comprehensive guide provides the foundation for a secure, reliable deployment of the Temenos Alert Manager in a Central Bank environment.