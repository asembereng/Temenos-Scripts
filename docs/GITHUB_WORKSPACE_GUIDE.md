# GitHub Workspace Development Guide

This guide provides comprehensive instructions for setting up, running, and testing the Temenos Service Management Platform using GitHub Codespaces and GitHub Workspace environments.

## Overview

GitHub Codespaces provides a cloud-based development environment that allows you to develop, build, test, and debug the Temenos Service Management Platform without requiring local setup. This is particularly useful for:

- Quick development environment setup
- Consistent development environments across team members
- Testing in isolated environments
- Demonstration and evaluation purposes

## Prerequisites

- GitHub account with Codespaces access
- Basic familiarity with Git and GitHub
- Understanding of .NET Core development
- Knowledge of SQL Server basics

## Setting Up GitHub Codespace

### 1. Creating a Codespace

1. **Navigate to the Repository**
   ```
   https://github.com/asembereng/Temenos-Scripts
   ```

2. **Create Codespace**
   - Click the green "Code" button
   - Select "Codespaces" tab
   - Click "Create codespace on main"
   - Wait for the environment to initialize (typically 2-3 minutes)

3. **Verify Setup**
   ```bash
   # Check .NET version
   dotnet --version
   
   # Check PowerShell version
   pwsh --version
   
   # Verify repository structure
   ls -la
   ```

### 2. Development Container Configuration

The repository includes a `.devcontainer` configuration that automatically sets up:

- .NET 8 SDK
- PowerShell 7
- SQL Server tools
- Required extensions for VS Code

**`.devcontainer/devcontainer.json`** (create if needed):
```json
{
  "name": "Temenos Service Management Platform",
  "image": "mcr.microsoft.com/devcontainers/dotnet:8.0",
  "features": {
    "ghcr.io/devcontainers/features/powershell:1": {},
    "ghcr.io/devcontainers/features/azure-cli:1": {},
    "ghcr.io/devcontainers/features/github-cli:1": {}
  },
  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "ms-dotnettools.vscode-dotnet-runtime",
        "ms-vscode.powershell",
        "ms-mssql.mssql",
        "humao.rest-client"
      ]
    }
  },
  "postCreateCommand": "dotnet restore TemenosAlertManager.sln",
  "forwardPorts": [5000, 5001, 1433],
  "portsAttributes": {
    "5001": {
      "label": "HTTPS API",
      "onAutoForward": "notify"
    },
    "5000": {
      "label": "HTTP API",
      "onAutoForward": "ignore"
    }
  }
}
```

## Database Setup in Codespace

### 1. SQL Server LocalDB Setup

Since full SQL Server isn't available in Codespaces, we'll use SQLite for development:

1. **Install SQLite Provider**
   ```bash
   cd src/TemenosAlertManager.Api
   dotnet add package Microsoft.EntityFrameworkCore.Sqlite
   ```

2. **Update Connection String for Development**
   ```json
   // src/TemenosAlertManager.Api/appsettings.Development.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=temenos_dev.db"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```

3. **Configure DbContext for SQLite**
   ```csharp
   // Program.cs addition for development
   if (builder.Environment.IsDevelopment())
   {
       builder.Services.AddDbContext<TemenosAlertContext>(options =>
           options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
   }
   else
   {
       builder.Services.AddDbContext<TemenosAlertContext>(options =>
           options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
   }
   ```

### 2. Database Migration and Seeding

1. **Install EF Tools**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Create Initial Migration**
   ```bash
   cd src/TemenosAlertManager.Api
   dotnet ef migrations add InitialCreate
   ```

3. **Update Database**
   ```bash
   dotnet ef database update
   ```

4. **Seed Test Data** (create a seeding script):
   ```csharp
   // Create docs/scripts/seed-data.sql or C# seeding method
   // Insert sample service configurations, users, etc.
   ```

## Building and Running the Application

### 1. Build the Solution

```bash
# From repository root
dotnet build TemenosAlertManager.sln --configuration Debug
```

### 2. Run the API

```bash
cd src/TemenosAlertManager.Api
dotnet run --environment Development
```

The application will start and be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: https://localhost:5001/swagger
- Hangfire Dashboard: https://localhost:5001/hangfire

### 3. Testing API Endpoints

Create test files in the workspace:

**`tests/api-tests.http`** (VS Code REST Client format):
```http
### Health Check
GET https://localhost:5001/health

### Get System Health
GET https://localhost:5001/api/monitoring/system-health

### Start SOD Operation (requires auth in production)
POST https://localhost:5001/api/temenos/operations/sod
Content-Type: application/json

{
  "environment": "DEV",
  "servicesFilter": [],
  "dryRun": true,
  "forceExecution": false,
  "comments": "Test SOD operation from Codespace"
}

### Get Services Status
GET https://localhost:5001/api/services/status

### Get Monitoring Dashboard
GET https://localhost:5001/api/monitoring/dashboard
```

## PowerShell Module Testing

### 1. Import Modules in Codespace

```bash
# Navigate to PowerShell modules
cd scripts/PowerShell/Modules

# Start PowerShell
pwsh

# Import modules
Import-Module ./TemenosChecks.SOD/TemenosChecks.SOD.psd1 -Force
Import-Module ./TemenosChecks.ServiceManagement/TemenosChecks.ServiceManagement.psd1 -Force

# Test module functions
Get-Command -Module TemenosChecks.SOD
Get-Help Start-TemenosSOD -Full
```

### 2. Mock Temenos Environment for Testing

Create mock PowerShell functions for testing without actual Temenos infrastructure:

**`scripts/test-environment/mock-temenos.ps1`**:
```powershell
# Mock Temenos services for testing
function Start-MockTemenosService {
    param([string]$ServiceName)
    Write-Host "Mock: Starting service $ServiceName"
    Start-Sleep -Seconds 2
    return @{ Status = "Running"; Message = "Service started successfully" }
}

function Stop-MockTemenosService {
    param([string]$ServiceName)
    Write-Host "Mock: Stopping service $ServiceName"
    Start-Sleep -Seconds 1
    return @{ Status = "Stopped"; Message = "Service stopped successfully" }
}

function Get-MockTemenosServiceStatus {
    param([string]$ServiceName)
    return @{
        Name = $ServiceName
        Status = "Running"
        StartTime = (Get-Date).AddHours(-2)
        ProcessId = Get-Random -Minimum 1000 -Maximum 9999
    }
}

# Export functions for testing
Export-ModuleMember -Function Start-MockTemenosService, Stop-MockTemenosService, Get-MockTemenosServiceStatus
```

### 3. Test PowerShell Integration

```powershell
# Test SOD operation with mocks
$testResult = Start-TemenosSOD -Environment "DEV" -DryRun -Verbose

# Test service management
$serviceResult = Restart-TemenosService -ServiceName "MockT24Service" -DryRun -Verbose

# Test performance monitoring
$perfResult = Get-TemenosPerformanceMetrics -Environment "DEV"
```

## Development Workflow

### 1. Making Changes

1. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Code Changes**
   - Edit source files using VS Code
   - Follow existing code patterns
   - Add appropriate logging and error handling

3. **Test Changes**
   ```bash
   # Build and test
   dotnet build
   dotnet test
   
   # Run application
   dotnet run --project src/TemenosAlertManager.Api
   ```

4. **Commit Changes**
   ```bash
   git add .
   git commit -m "Add feature: description of changes"
   git push origin feature/your-feature-name
   ```

### 2. Debugging in Codespace

1. **Set Breakpoints** in VS Code
2. **Start Debugging** with F5 or Debug menu
3. **Use Debug Console** for PowerShell and C# debugging
4. **Monitor Logs** in the integrated terminal

### 3. Testing API with Swagger

1. Navigate to https://localhost:5001/swagger
2. Explore available endpoints
3. Test operations using the Swagger UI
4. Review request/response schemas

## Simulating Production Scenarios

### 1. Multi-Environment Testing

Create configuration for different environments:

**`appsettings.Staging.json`**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=staging.db"
  },
  "PowerShell": {
    "RemoteExecutionTimeoutSeconds": 600,
    "UseSecureRemoting": false
  }
}
```

### 2. Load Testing (Simplified)

Create simple load testing scripts:

**`tests/load-test.ps1`**:
```powershell
# Simple load test for API endpoints
$endpoint = "https://localhost:5001/api/monitoring/system-health"
$requests = 100
$concurrent = 10

$jobs = @()
for ($i = 0; $i -lt $concurrent; $i++) {
    $job = Start-Job -ScriptBlock {
        param($url, $count)
        for ($j = 0; $j -lt $count; $j++) {
            try {
                Invoke-RestMethod -Uri $url -Method GET
                Write-Host "Request $j completed"
            }
            catch {
                Write-Error "Request $j failed: $_"
            }
        }
    } -ArgumentList $endpoint, ($requests / $concurrent)
    $jobs += $job
}

# Wait for all jobs to complete
$jobs | Wait-Job | Receive-Job
$jobs | Remove-Job
```

## Troubleshooting Common Issues

### 1. Port Conflicts

```bash
# Check what's running on ports
netstat -tulpn | grep :5001

# Kill process if needed
sudo kill -9 $(sudo lsof -t -i:5001)
```

### 2. Database Connection Issues

```bash
# Check SQLite database
sqlite3 temenos_dev.db ".tables"

# Reset database
rm temenos_dev.db
dotnet ef database update
```

### 3. PowerShell Module Issues

```powershell
# Reload modules
Remove-Module TemenosChecks.SOD -Force
Import-Module ./TemenosChecks.SOD/TemenosChecks.SOD.psd1 -Force

# Check module path
$env:PSModulePath -split ':'
```

### 4. Certificate Issues (HTTPS)

```bash
# Trust development certificate
dotnet dev-certs https --trust
```

## Performance Considerations

### 1. Codespace Resource Limits

- Monitor CPU and memory usage in Codespace
- Use 4-core or higher Codespace for better performance
- Consider using GitHub Codespace prebuild for faster startup

### 2. Optimization Tips

- Use SQLite for development database (lighter than SQL Server)
- Implement proper logging levels for development
- Use mock services for external dependencies
- Cache frequently accessed data

## Security Considerations

### 1. Development Security

- Never commit secrets or credentials
- Use development certificates for HTTPS
- Implement proper authentication even in development
- Test authorization scenarios

### 2. Environment Variables

```bash
# Set development environment variables
export ASPNETCORE_ENVIRONMENT=Development
export TEMENOS_DEV_MODE=true
export LOG_LEVEL=Debug
```

## Advanced Scenarios

### 1. Integration Testing

Create integration tests that can run in Codespace:

```csharp
[Test]
public async Task TestSODOperation()
{
    // Arrange
    var client = new HttpClient();
    var request = new SODRequest 
    { 
        Environment = "DEV", 
        DryRun = true 
    };

    // Act
    var response = await client.PostAsJsonAsync(
        "https://localhost:5001/api/temenos/operations/sod", 
        request);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<OperationResult>();
    Assert.IsNotNull(result);
}
```

### 2. End-to-End Testing

Create comprehensive test scenarios:

```powershell
# E2E test script
$testScenarios = @(
    @{ Name = "SOD Operation"; Endpoint = "/api/temenos/operations/sod" },
    @{ Name = "Service Status"; Endpoint = "/api/services/status" },
    @{ Name = "Health Check"; Endpoint = "/api/monitoring/system-health" }
)

foreach ($scenario in $testScenarios) {
    Write-Host "Testing: $($scenario.Name)"
    # Implement test logic
}
```

## Conclusion

This guide provides a comprehensive approach to developing and testing the Temenos Service Management Platform using GitHub Codespaces. The cloud-based development environment offers:

- **Quick Setup**: Get started in minutes without local configuration
- **Consistent Environment**: Same environment across all developers
- **Isolation**: Test without affecting local systems
- **Collaboration**: Easy sharing and pair programming

For production deployment, refer to the [Production Deployment Guide](PRODUCTION_DEPLOYMENT_GUIDE.md) and [Temenos Sandbox Guide](TEMENOS_SANDBOX_GUIDE.md).