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
- **Monitoring**: PowerShell 7 modules
- **Logging**: Serilog with structured logging

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

3. **Build and run**
   ```bash
   dotnet build
   cd src/TemenosAlertManager.Api
   dotnet run
   ```

4. **Access the application**
   - API: https://localhost:5001
   - Swagger UI: https://localhost:5001/swagger
   - Hangfire Dashboard: https://localhost:5001/hangfire

## PowerShell Modules

### Available Modules

#### TemenosChecks.Common
Base module with shared utilities for all monitoring checks.

#### TemenosChecks.Sql
Comprehensive SQL Server monitoring including availability, blocking sessions, long-running queries, and TempDB usage.

### Usage Example
```powershell
Import-Module TemenosChecks.Sql
$result = Test-SqlServerAvailability -InstanceName "SQLSERVER01"
```

## API Endpoints

- `GET /api/health/dashboard` - System overview
- `GET /api/health/summary/{domain}` - Domain health
- `POST /api/health/checks/run` - Manual check execution

## Deployment

Supports IIS deployment with Windows Authentication for enterprise environments.

## Security Features

- Windows AD integration with role-based access control
- Comprehensive audit logging for compliance
- Encrypted configuration for sensitive data
- Tamper-evident audit trails

## License

AGPL-3.0 License - See [LICENSE](LICENSE) file for details.