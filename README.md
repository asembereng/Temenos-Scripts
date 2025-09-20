# Temenos Alert Manager

A production-grade monitoring and operations suite for Temenos environments (TPH, T24, IBM MQ, MSSQL Server) designed specifically for Central Bank environments with enterprise security requirements.

## Overview

The Temenos Alert Manager provides comprehensive monitoring capabilities with:

- **24/7 Monitoring**: Automated checks for TPH, T24, IBM MQ, and SQL Server
- **Web-Based Interface**: Modern React frontend with role-based access control
- **Windows AD Integration**: Seamless authentication using corporate Active Directory
- **Mission-Critical Alerting**: Reliable email delivery with outbox pattern and retries
- **PowerShell-Based Checks**: Extensible monitoring modules for all Temenos components
- **Audit Trail**: Comprehensive logging for regulatory compliance

## Architecture

### Technology Stack
- **Backend**: ASP.NET Core 8 with Windows Authentication
- **Database**: SQL Server with Entity Framework Core
- **Background Jobs**: Hangfire for scheduled monitoring
- **Frontend**: React + TypeScript (planned)
- **Monitoring**: PowerShell 7 modules with remote execution support
- **Logging**: Serilog with structured logging

### Deployment Architecture
The Temenos Alert Manager supports both co-located and distributed deployment patterns:

#### Co-located Deployment
- Alert Manager and Temenos systems on same infrastructure
- Suitable for development and small-scale environments
- Direct local monitoring with minimal network dependencies

#### Distributed Deployment (Recommended for Production)
- **Alert Manager**: Dedicated monitoring server or cloud hosting
- **T24 Core Banking**: Clustered application servers
- **TPH Payment Hub**: Load-balanced payment processing servers  
- **IBM MQ**: Queue manager clusters
- **SQL Server**: AlwaysOn availability groups

**Remote Monitoring Capabilities:**
- PowerShell remoting (WinRM) for secure remote execution
- Cross-network monitoring with proper authentication
- Support for multi-site and hybrid cloud deployments
- Centralized alerting from distributed infrastructure

For detailed remote deployment guidance, see [Remote Deployment Guide](docs/RemoteDeploymentGuide.md).

## Temenos Service Management Research

📋 **Comprehensive research and implementation planning for Temenos Start of Day (SOD) and End of Day (EOD) service management:**

- **[Executive Summary](docs/EXECUTIVE_SUMMARY.md)** - High-level overview and business case for implementation
- **[Detailed Research](docs/TEMENOS_SOD_EOD_RESEARCH.md)** - In-depth analysis of Temenos SOD/EOD procedures, best practices, and challenges
- **[Implementation Specification](docs/IMPLEMENTATION_SPECIFICATION.md)** - Complete technical specifications for development
- **[Operations Quick Reference](docs/TEMENOS_OPERATIONS_QUICK_REFERENCE.md)** - Practical guide for common Temenos operations

These documents provide comprehensive guidance for implementing automated service management capabilities for Temenos banking systems, including critical Start of Day and End of Day operations with proper risk management and regulatory compliance considerations.

### Security Model
- **Authentication**: Windows Authentication (Kerberos/NTLM)
- **Authorization**: Three-tier RBAC (Viewer, Operator, Admin)
- **AD Groups**: 
  - `TEMENOS_ALERT_ADMINS` - Full administrative access
  - `TEMENOS_ALERT_OPERATORS` - Operational access
  - `TEMENOS_ALERT_VIEWERS` - Read-only access
- **Audit Trail**: Tamper-evident logging with payload hashing

## Quick Start

### Prerequisites
- Windows Server 2019+ or Windows 10/11
- .NET 8 SDK
- SQL Server 2019+ (Express, Standard, or Enterprise)
- PowerShell 7.0+
- IIS (for production deployment)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/asembereng/Temenos-Scripts.git
   cd Temenos-Scripts
   ```

2. **Configure database connection**
   ```json
   // src/TemenosAlertManager.Api/appsettings.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.;Database=TemenosAlertManager;Trusted_Connection=true;"
     }
   }
   ```

3. **Configure remote monitoring (if applicable)**
   ```json
   // For distributed deployments, configure target hosts
   {
     "PowerShell": {
       "RemoteExecutionTimeoutSeconds": 300,
       "UseSecureRemoting": true
     }
   }
   ```

4. **Build and run**
   ```bash
   dotnet build
   cd src/TemenosAlertManager.Api
   dotnet run
   ```

5. **Access the application**
   - API: https://localhost:5001
   - Swagger UI: https://localhost:5001/swagger
   - Hangfire Dashboard: https://localhost:5001/hangfire

### Remote Deployment Setup

For production environments with distributed Temenos systems:

1. **Enable PowerShell Remoting on target hosts**
   ```powershell
   # Run on T24, TPH, MQ, SQL servers
   Enable-PSRemoting -Force
   Set-ExecutionPolicy RemoteSigned -Force
   ```

2. **Configure service accounts**
   - Create dedicated monitoring service accounts
   - Grant minimal required permissions on target systems
   - Configure authentication for cross-host access

3. **Set up network connectivity**
   - Ensure WinRM ports (5985/5986) are accessible
   - Configure firewall rules between Alert Manager and target hosts
   - Test connectivity with `Test-WSMan <target-host>`

4. **Configure monitoring targets**
   - Update database configuration with target host information
   - Configure credentials and connection parameters
   - Test monitoring functions from Alert Manager

For comprehensive deployment instructions, see [Remote Deployment Guide](docs/RemoteDeploymentGuide.md).

## PowerShell Modules

### Available Modules

#### TemenosChecks.Common
Base module with shared utilities for all monitoring checks. Includes remote execution capabilities via PowerShell remoting (WinRM).

#### TemenosChecks.TPH
Comprehensive TPH (Payment Hub) monitoring including service status, queue depths, transaction monitoring, and error log analysis. Supports monitoring TPH systems deployed on remote hosts.

#### TemenosChecks.Sql
Comprehensive SQL Server monitoring including availability, blocking sessions, long-running queries, and TempDB usage. Enables monitoring of distributed SQL Server deployments.

#### TemenosChecks.MQ
IBM MQ monitoring for queue managers, queue depths, channel status, and connectivity testing. Supports remote MQ environments.

### Remote Monitoring Features

All PowerShell modules support remote execution for distributed Temenos environments:

- **Cross-Host Monitoring**: Monitor services on separate T24, TPH, MQ, and SQL hosts
- **Secure Authentication**: Support for Windows Authentication and service accounts
- **Network Resilience**: Timeout handling and connection management for reliable remote operations
- **Error Handling**: Comprehensive error reporting with troubleshooting guidance

### Usage Example
```powershell
# Local monitoring
Import-Module TemenosChecks.Sql
$result = Test-SqlServerAvailability -InstanceName "SQLSERVER01"

# Remote monitoring  
$result = Test-TphServices -ServerName "TPH-PROD01.bank.local" -Credential $cred
```

## API Endpoints

- `GET /api/health/dashboard` - System overview
- `GET /api/health/summary/{domain}` - Domain health
- `POST /api/health/checks/run` - Manual check execution

## Deployment

### Local Deployment
Supports IIS deployment with Windows Authentication for co-located environments where the Alert Manager runs on the same infrastructure as Temenos systems.

### Remote/Distributed Deployment (Recommended)
Designed for production banking environments with:
- **Centralized Monitoring**: Single Alert Manager monitoring distributed Temenos infrastructure
- **Network Segmentation**: Support for DMZ/internal network architectures
- **Multi-Site Support**: Monitor Temenos systems across multiple data centers
- **Cloud/Hybrid**: Alert Manager in cloud monitoring on-premises banking systems

**Key Features:**
- PowerShell remoting (WinRM) for secure cross-host communication
- Service account management for authentication across domains
- Network timeout and retry logic for reliable remote operations
- Comprehensive logging and error handling for distributed monitoring

**Prerequisites:**
- PowerShell remoting enabled on target Temenos hosts
- Network connectivity on WinRM ports (5985/5986)
- Appropriate service accounts with monitoring permissions
- DNS resolution or IP connectivity between Alert Manager and targets

For complete deployment instructions, see [Remote Deployment Guide](docs/RemoteDeploymentGuide.md).

## Security Features

- Windows AD integration with role-based access control
- Comprehensive audit logging for compliance
- Encrypted configuration for sensitive data
- Tamper-evident audit trails

## License

AGPL-3.0 License - See [LICENSE](LICENSE) file for details.