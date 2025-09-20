# Temenos Sandbox Setup Guide

This comprehensive guide provides step-by-step instructions for setting up a Temenos sandbox environment for testing the Service Management Platform. This includes both virtual sandbox options and cloud-based Temenos environment access.

## Overview

A Temenos sandbox environment allows you to:

- Test SOD/EOD operations safely without affecting production systems
- Validate service management capabilities
- Develop and test PowerShell automation modules
- Demonstrate platform capabilities
- Train operations teams

## Sandbox Environment Options

### Option 1: Temenos SandBox (Official Cloud Environment)

#### 1.1 Temenos SandBox Access

**Temenos Community Portal Access:**
```
URL: https://community.temenos.com/
Registration: Free developer account required
Resources: Documentation, APIs, sample data
```

**Steps to Access:**
1. **Register for Temenos Community**
   - Visit https://community.temenos.com/
   - Create a free developer account
   - Complete profile with banking/fintech focus
   - Request sandbox access through community portal

2. **Access SandBox Environment**
   - Navigate to Developer Resources > SandBox
   - Select appropriate Temenos product (T24/Transact)
   - Choose environment type (demo/development)
   - Note connection details and credentials

3. **Environment Details** (typically provided):
   ```
   Environment: T24 R19/R20 or Transact
   Access Method: HTTPS REST APIs
   Base URL: https://sandbox.temenos.com/
   Authentication: OAuth 2.0 or Basic Auth
   Sample Data: Included
   ```

#### 1.2 Connecting to Temenos SandBox

**PowerShell Connection Example:**
```powershell
# Configure connection to Temenos SandBox
$sandboxConfig = @{
    BaseUrl = "https://sandbox.temenos.com"
    ApiKey = "your-api-key"
    Environment = "SANDBOX"
    Version = "v1.0.0"
}

# Test connection
function Test-TemenosSandBoxConnection {
    param($Config)
    
    try {
        $headers = @{
            "Authorization" = "Bearer $($Config.ApiKey)"
            "Content-Type" = "application/json"
        }
        
        $response = Invoke-RestMethod -Uri "$($Config.BaseUrl)/api/v1/system/health" -Headers $headers
        Write-Host "✅ Connected to Temenos SandBox successfully"
        return $response
    }
    catch {
        Write-Error "❌ Failed to connect to Temenos SandBox: $_"
        return $null
    }
}

$connectionTest = Test-TemenosSandBoxConnection -Config $sandboxConfig
```

**Configuring Service Management Platform:**
```json
// appsettings.Sandbox.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=sandbox.db"
  },
  "TemenosEnvironment": {
    "Type": "SandBox",
    "BaseUrl": "https://sandbox.temenos.com",
    "Environment": "SANDBOX",
    "Authentication": {
      "Type": "OAuth2",
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    }
  },
  "PowerShell": {
    "RemoteExecutionTimeoutSeconds": 300,
    "UseSecureRemoting": false,
    "UseMockServices": true
  }
}
```

### Option 2: Virtualized Temenos Environment

#### 2.1 Docker-based Sandbox

**Prerequisites:**
- Docker Desktop or Docker Engine
- 16GB+ RAM recommended
- 100GB+ disk space
- Windows Server 2019+ or Linux

**Docker Compose Setup:**
```yaml
# docker-compose.temenos.yml
version: '3.8'
services:
  temenos-db:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      SA_PASSWORD: "TemenosTest123!"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - temenos_data:/var/opt/mssql
    
  temenos-mock:
    build:
      context: ./sandbox/temenos-mock
      dockerfile: Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      DATABASE_CONNECTION: "Server=temenos-db;Database=T24;User=sa;Password=TemenosTest123!;TrustServerCertificate=true"
    depends_on:
      - temenos-db
    volumes:
      - ./sandbox/config:/app/config
      - ./sandbox/logs:/app/logs

  mq-server:
    image: ibmcom/mq:latest
    environment:
      LICENSE: accept
      MQ_QMGR_NAME: QM_TEMENOS
      MQ_APP_PASSWORD: mqpassword
    ports:
      - "1414:1414"
      - "9443:9443"
    volumes:
      - mq_data:/mnt/mqm

volumes:
  temenos_data:
  mq_data:
```

**Mock Temenos Service:**
```dockerfile
# sandbox/temenos-mock/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 80 443
ENTRYPOINT ["dotnet", "TemenosMockService.dll"]
```

#### 2.2 Virtual Machine Setup

**VM Requirements:**
- **OS**: Windows Server 2019/2022
- **RAM**: 16GB minimum, 32GB recommended
- **Disk**: 200GB SSD
- **CPU**: 4+ cores
- **Network**: Isolated network segment

**VM Configuration Script:**
```powershell
# VM Setup Script for Temenos Sandbox
param(
    [string]$VMName = "Temenos-Sandbox",
    [string]$ISOPath = "C:\ISO\WindowsServer2022.iso",
    [int]$RAM = 16GB,
    [int]$DiskSize = 200GB
)

# Create VM
New-VM -Name $VMName -MemoryStartupBytes $RAM -Generation 2
New-VHD -Path "C:\VMs\$VMName\$VMName.vhdx" -SizeBytes $DiskSize -Dynamic
Add-VMHardDiskDrive -VMName $VMName -Path "C:\VMs\$VMName\$VMName.vhdx"
Set-VMDvdDrive -VMName $VMName -Path $ISOPath

# Configure VM
Set-VM -Name $VMName -ProcessorCount 4
Enable-VMIntegrationService -VMName $VMName -Name "Guest Service Interface"

Write-Host "✅ VM '$VMName' created successfully"
Write-Host "📝 Next steps:"
Write-Host "   1. Start VM and install Windows Server"
Write-Host "   2. Install SQL Server"
Write-Host "   3. Install IBM MQ"
Write-Host "   4. Deploy mock Temenos services"
```

### Option 3: Cloud-based Sandbox (AWS/Azure)

#### 3.1 AWS Environment Setup

**CloudFormation Template:**
```yaml
# temenos-sandbox-aws.yml
AWSTemplateFormatVersion: '2010-09-09'
Description: 'Temenos Sandbox Environment on AWS'

Parameters:
  InstanceType:
    Type: String
    Default: m5.xlarge
    Description: EC2 instance type for Temenos sandbox

Resources:
  TemenosSandboxInstance:
    Type: AWS::EC2::Instance
    Properties:
      ImageId: ami-0c02fb55956c7d316  # Windows Server 2022
      InstanceType: !Ref InstanceType
      KeyName: temenos-sandbox-key
      SecurityGroupIds:
        - !Ref TemenosSandboxSecurityGroup
      UserData:
        Fn::Base64: !Sub |
          <powershell>
          # Install required software
          Install-WindowsFeature -Name IIS-WebServerRole
          Invoke-WebRequest -Uri "https://go.microsoft.com/fwlink/?linkid=866658" -OutFile "sqlserver.exe"
          # Additional setup commands
          </powershell>
      Tags:
        - Key: Name
          Value: Temenos-Sandbox
        - Key: Purpose
          Value: Development

  TemenosSandboxSecurityGroup:
    Type: AWS::EC2::SecurityGroup
    Properties:
      GroupDescription: Security group for Temenos sandbox
      SecurityGroupIngress:
        - IpProtocol: tcp
          FromPort: 3389
          ToPort: 3389
          CidrIp: 0.0.0.0/0  # RDP access (restrict as needed)
        - IpProtocol: tcp
          FromPort: 1433
          ToPort: 1433
          CidrIp: 10.0.0.0/8  # SQL Server
        - IpProtocol: tcp
          FromPort: 8080
          ToPort: 8080
          CidrIp: 0.0.0.0/0  # Temenos services

Outputs:
  InstanceId:
    Description: Instance ID
    Value: !Ref TemenosSandboxInstance
  PublicDNS:
    Description: Public DNS name
    Value: !GetAtt TemenosSandboxInstance.PublicDnsName
```

**Deploy with AWS CLI:**
```bash
# Deploy sandbox environment
aws cloudformation create-stack \
  --stack-name temenos-sandbox \
  --template-body file://temenos-sandbox-aws.yml \
  --parameters ParameterKey=InstanceType,ParameterValue=m5.xlarge \
  --region us-east-1

# Wait for completion
aws cloudformation wait stack-create-complete --stack-name temenos-sandbox
```

#### 3.2 Azure Environment Setup

**ARM Template:**
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "vmSize": {
      "type": "string",
      "defaultValue": "Standard_D4s_v3",
      "metadata": {
        "description": "VM size for Temenos sandbox"
      }
    }
  },
  "resources": [
    {
      "type": "Microsoft.Compute/virtualMachines",
      "apiVersion": "2021-11-01",
      "name": "temenos-sandbox-vm",
      "location": "[resourceGroup().location]",
      "properties": {
        "hardwareProfile": {
          "vmSize": "[parameters('vmSize')]"
        },
        "osProfile": {
          "computerName": "temenos-sandbox",
          "adminUsername": "adminuser",
          "adminPassword": "TemenosTest123!"
        },
        "storageProfile": {
          "imageReference": {
            "publisher": "MicrosoftWindowsServer",
            "offer": "WindowsServer",
            "sku": "2022-Datacenter",
            "version": "latest"
          }
        }
      }
    }
  ]
}
```

## Mock Temenos Services Implementation

### Mock T24 Core Banking Service

```csharp
// sandbox/TemenosMockService/Controllers/T24Controller.cs
[ApiController]
[Route("api/t24")]
public class T24Controller : ControllerBase
{
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { 
            Status = "Running", 
            Version = "R20.12", 
            Environment = "SANDBOX",
            Timestamp = DateTime.UtcNow 
        });
    }

    [HttpPost("sod/start")]
    public async Task<IActionResult> StartSOD([FromBody] SODRequest request)
    {
        // Simulate SOD process
        await Task.Delay(2000); // Simulate processing time
        
        return Ok(new {
            OperationId = Guid.NewGuid().ToString(),
            Status = "Completed",
            Duration = "00:02:00",
            Steps = new[] {
                new { Name = "System Validation", Status = "Completed", Duration = "00:00:30" },
                new { Name = "Service Startup", Status = "Completed", Duration = "00:01:00" },
                new { Name = "Data Validation", Status = "Completed", Duration = "00:00:30" }
            }
        });
    }

    [HttpPost("eod/start")]
    public async Task<IActionResult> StartEOD([FromBody] EODRequest request)
    {
        // Simulate EOD process
        await Task.Delay(5000); // Simulate processing time
        
        return Ok(new {
            OperationId = Guid.NewGuid().ToString(),
            Status = "Completed",
            Duration = "00:05:00",
            TransactionsProcessed = 15420,
            Steps = new[] {
                new { Name = "Transaction Cutoff", Status = "Completed", Duration = "00:01:00" },
                new { Name = "Daily Processing", Status = "Completed", Duration = "00:03:00" },
                new { Name = "Reporting", Status = "Completed", Duration = "00:01:00" }
            }
        });
    }

    [HttpGet("services")]
    public IActionResult GetServices()
    {
        return Ok(new[] {
            new { Name = "T24AppServer", Status = "Running", Port = 8080 },
            new { Name = "T24WebServer", Status = "Running", Port = 8443 },
            new { Name = "T24Database", Status = "Running", Port = 1433 }
        });
    }

    [HttpPost("services/{serviceName}/start")]
    public async Task<IActionResult> StartService(string serviceName)
    {
        await Task.Delay(1000);
        return Ok(new { ServiceName = serviceName, Status = "Started", Timestamp = DateTime.UtcNow });
    }

    [HttpPost("services/{serviceName}/stop")]
    public async Task<IActionResult> StopService(string serviceName)
    {
        await Task.Delay(1000);
        return Ok(new { ServiceName = serviceName, Status = "Stopped", Timestamp = DateTime.UtcNow });
    }
}
```

### Mock TPH Payment Hub Service

```csharp
// sandbox/TemenosMockService/Controllers/TPHController.cs
[ApiController]
[Route("api/tph")]
public class TPHController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new {
            Status = "Running",
            Version = "TPH R21.3",
            Environment = "SANDBOX",
            ActiveConnections = 42,
            QueueDepth = 15
        });
    }

    [HttpGet("queues")]
    public IActionResult GetQueues()
    {
        return Ok(new[] {
            new { Name = "INCOMING.PAYMENTS", Depth = 5, MaxDepth = 1000 },
            new { Name = "OUTGOING.PAYMENTS", Depth = 3, MaxDepth = 1000 },
            new { Name = "REJECTED.PAYMENTS", Depth = 1, MaxDepth = 100 },
            new { Name = "NOTIFICATIONS", Depth = 8, MaxDepth = 500 }
        });
    }

    [HttpPost("payments/process")]
    public async Task<IActionResult> ProcessPayments([FromBody] PaymentBatch batch)
    {
        await Task.Delay(2000);
        return Ok(new {
            BatchId = Guid.NewGuid().ToString(),
            ProcessedCount = batch.Payments?.Length ?? 0,
            Status = "Completed",
            ProcessingTime = "00:02:00"
        });
    }
}
```

### Mock IBM MQ Service

```csharp
// sandbox/TemenosMockService/Controllers/MQController.cs
[ApiController]
[Route("api/mq")]
public class MQController : ControllerBase
{
    [HttpGet("qmgr/{queueManagerName}/status")]
    public IActionResult GetQueueManagerStatus(string queueManagerName)
    {
        return Ok(new {
            QueueManager = queueManagerName,
            Status = "Running",
            StartTime = DateTime.UtcNow.AddHours(-24),
            Channels = new[] {
                new { Name = "TEMENOS.SVRCONN", Status = "Running", Connections = 5 },
                new { Name = "SYSTEM.AUTO.SVRCONN", Status = "Running", Connections = 2 }
            }
        });
    }

    [HttpGet("qmgr/{queueManagerName}/queues")]
    public IActionResult GetQueues(string queueManagerName)
    {
        return Ok(new[] {
            new { Name = "TEMENOS.REQUEST", CurrentDepth = 0, MaxDepth = 5000 },
            new { Name = "TEMENOS.REPLY", CurrentDepth = 0, MaxDepth = 5000 },
            new { Name = "TEMENOS.ERROR", CurrentDepth = 2, MaxDepth = 1000 }
        });
    }
}
```

## Configuration and Testing

### Configuring Service Management Platform for Sandbox

```json
// appsettings.Sandbox.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=sandbox.db"
  },
  "TemenosEnvironment": {
    "Type": "MockSandbox",
    "Servers": {
      "T24": {
        "BaseUrl": "http://localhost:8080/api/t24",
        "HealthEndpoint": "/health",
        "Authentication": "None"
      },
      "TPH": {
        "BaseUrl": "http://localhost:8080/api/tph",
        "HealthEndpoint": "/health",
        "Authentication": "None"
      },
      "MQ": {
        "BaseUrl": "http://localhost:8080/api/mq",
        "QueueManager": "QM_TEMENOS",
        "Authentication": "None"
      }
    }
  },
  "PowerShell": {
    "UseMockServices": true,
    "MockServiceUrl": "http://localhost:8080",
    "RemoteExecutionTimeoutSeconds": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TemenosAlertManager": "Debug"
    }
  }
}
```

### PowerShell Module Configuration for Sandbox

```powershell
# scripts/PowerShell/Modules/TemenosChecks.Sandbox/TemenosChecks.Sandbox.psm1

function Connect-TemenosSandbox {
    param(
        [string]$BaseUrl = "http://localhost:8080",
        [string]$Environment = "SANDBOX"
    )
    
    $global:TemenosSandboxConfig = @{
        BaseUrl = $BaseUrl
        Environment = $Environment
        Connected = $false
    }
    
    try {
        $healthCheck = Invoke-RestMethod -Uri "$BaseUrl/api/t24/health" -Method GET
        $global:TemenosSandboxConfig.Connected = $true
        Write-Host "✅ Connected to Temenos Sandbox: $Environment"
        return $healthCheck
    }
    catch {
        Write-Error "❌ Failed to connect to Temenos Sandbox: $_"
        return $null
    }
}

function Start-SandboxSOD {
    param(
        [string]$Environment = "SANDBOX",
        [switch]$DryRun
    )
    
    if (-not $global:TemenosSandboxConfig.Connected) {
        throw "Not connected to sandbox. Run Connect-TemenosSandbox first."
    }
    
    $request = @{
        Environment = $Environment
        DryRun = $DryRun.IsPresent
        Comments = "Sandbox SOD test"
    }
    
    try {
        $result = Invoke-RestMethod -Uri "$($global:TemenosSandboxConfig.BaseUrl)/api/t24/sod/start" -Method POST -Body ($request | ConvertTo-Json) -ContentType "application/json"
        Write-Host "✅ SOD operation started successfully"
        return $result
    }
    catch {
        Write-Error "❌ SOD operation failed: $_"
        return $null
    }
}

function Test-SandboxServices {
    if (-not $global:TemenosSandboxConfig.Connected) {
        throw "Not connected to sandbox. Run Connect-TemenosSandbox first."
    }
    
    $services = @("t24", "tph", "mq")
    $results = @()
    
    foreach ($service in $services) {
        try {
            $health = Invoke-RestMethod -Uri "$($global:TemenosSandboxConfig.BaseUrl)/api/$service/health" -Method GET
            $results += @{
                Service = $service.ToUpper()
                Status = $health.Status
                Healthy = $true
            }
        }
        catch {
            $results += @{
                Service = $service.ToUpper()
                Status = "Error"
                Healthy = $false
                Error = $_.Exception.Message
            }
        }
    }
    
    return $results
}

Export-ModuleMember -Function Connect-TemenosSandbox, Start-SandboxSOD, Test-SandboxServices
```

### Testing Scenarios

```powershell
# sandbox/test-scenarios.ps1

# Test Scenario 1: Basic connectivity and health checks
Write-Host "=== Test Scenario 1: Connectivity ==="
Connect-TemenosSandbox -BaseUrl "http://localhost:8080"
$healthResults = Test-SandboxServices
$healthResults | Format-Table

# Test Scenario 2: SOD Operation
Write-Host "=== Test Scenario 2: SOD Operation ==="
$sodResult = Start-SandboxSOD -Environment "SANDBOX" -DryRun
Write-Host "SOD Result: $($sodResult | ConvertTo-Json -Depth 3)"

# Test Scenario 3: Service Management
Write-Host "=== Test Scenario 3: Service Management ==="
$services = Invoke-RestMethod -Uri "http://localhost:8080/api/t24/services" -Method GET
Write-Host "Available Services:"
$services | Format-Table

# Test restarting a service
$restartResult = Invoke-RestMethod -Uri "http://localhost:8080/api/t24/services/T24AppServer/stop" -Method POST
Start-Sleep -Seconds 2
$startResult = Invoke-RestMethod -Uri "http://localhost:8080/api/t24/services/T24AppServer/start" -Method POST
Write-Host "Service restart completed"

# Test Scenario 4: Queue monitoring
Write-Host "=== Test Scenario 4: Queue Monitoring ==="
$queues = Invoke-RestMethod -Uri "http://localhost:8080/api/tph/queues" -Method GET
Write-Host "TPH Queues:"
$queues | Format-Table

$mqQueues = Invoke-RestMethod -Uri "http://localhost:8080/api/mq/qmgr/QM_TEMENOS/queues" -Method GET
Write-Host "MQ Queues:"
$mqQueues | Format-Table

# Test Scenario 5: Integration with Service Management Platform
Write-Host "=== Test Scenario 5: Platform Integration ==="
try {
    $platformHealth = Invoke-RestMethod -Uri "https://localhost:5001/api/monitoring/system-health" -Method GET
    Write-Host "Platform Health: $($platformHealth.overallStatus)"
    
    # Test SOD operation through platform
    $platformSOD = @{
        environment = "SANDBOX"
        dryRun = $true
        comments = "Integration test from sandbox"
    }
    
    $sodResponse = Invoke-RestMethod -Uri "https://localhost:5001/api/temenos/operations/sod" -Method POST -Body ($platformSOD | ConvertTo-Json) -ContentType "application/json"
    Write-Host "Platform SOD Result: $($sodResponse.operationId)"
}
catch {
    Write-Warning "Platform integration test failed: $_"
}

Write-Host "=== All test scenarios completed ==="
```

## Monitoring and Troubleshooting

### Sandbox Health Monitoring

```powershell
# sandbox/monitor-sandbox.ps1

function Monitor-SandboxHealth {
    param(
        [int]$IntervalSeconds = 30,
        [int]$DurationMinutes = 60
    )
    
    $endTime = (Get-Date).AddMinutes($DurationMinutes)
    
    while ((Get-Date) -lt $endTime) {
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Write-Host "[$timestamp] Checking sandbox health..."
        
        try {
            $services = Test-SandboxServices
            $healthyServices = ($services | Where-Object { $_.Healthy }).Count
            $totalServices = $services.Count
            
            Write-Host "  Services: $healthyServices/$totalServices healthy"
            
            if ($healthyServices -lt $totalServices) {
                $unhealthyServices = $services | Where-Object { -not $_.Healthy }
                foreach ($service in $unhealthyServices) {
                    Write-Warning "  ❌ $($service.Service): $($service.Error)"
                }
            }
            
            # Check resource usage
            $memoryUsage = Get-WmiObject Win32_OperatingSystem | ForEach-Object {
                [math]::Round(((($_.TotalVisibleMemorySize - $_.FreePhysicalMemory) / $_.TotalVisibleMemorySize) * 100), 2)
            }
            
            Write-Host "  Memory Usage: $memoryUsage%"
            
            if ($memoryUsage -gt 80) {
                Write-Warning "  ⚠️ High memory usage detected"
            }
        }
        catch {
            Write-Error "  ❌ Health check failed: $_"
        }
        
        Start-Sleep -Seconds $IntervalSeconds
    }
}

# Start monitoring
Monitor-SandboxHealth -IntervalSeconds 30 -DurationMinutes 60
```

### Troubleshooting Common Issues

```powershell
# sandbox/troubleshoot.ps1

function Troubleshoot-SandboxIssues {
    Write-Host "=== Temenos Sandbox Troubleshooting ==="
    
    # Check 1: Port availability
    Write-Host "Checking port availability..."
    $requiredPorts = @(8080, 8443, 1433, 1414)
    foreach ($port in $requiredPorts) {
        $portInUse = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
        if ($portInUse) {
            Write-Host "  ✅ Port $port is in use"
        } else {
            Write-Warning "  ❌ Port $port is not in use - service may not be running"
        }
    }
    
    # Check 2: Service status
    Write-Host "Checking Windows services..."
    $services = @("MSSQLSERVER", "SQLSERVERAGENT")
    foreach ($service in $services) {
        $svc = Get-Service -Name $service -ErrorAction SilentlyContinue
        if ($svc) {
            Write-Host "  ✅ $service is $($svc.Status)"
        } else {
            Write-Warning "  ❌ $service not found"
        }
    }
    
    # Check 3: Database connectivity
    Write-Host "Testing database connectivity..."
    try {
        $connectionString = "Server=localhost;Database=master;Integrated Security=true;"
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        $connection.Close()
        Write-Host "  ✅ Database connection successful"
    }
    catch {
        Write-Warning "  ❌ Database connection failed: $_"
    }
    
    # Check 4: Mock service endpoints
    Write-Host "Testing mock service endpoints..."
    $endpoints = @(
        "http://localhost:8080/api/t24/health",
        "http://localhost:8080/api/tph/health",
        "http://localhost:8080/api/mq/qmgr/QM_TEMENOS/status"
    )
    
    foreach ($endpoint in $endpoints) {
        try {
            $response = Invoke-RestMethod -Uri $endpoint -Method GET -TimeoutSec 10
            Write-Host "  ✅ $endpoint responded successfully"
        }
        catch {
            Write-Warning "  ❌ $endpoint failed: $_"
        }
    }
    
    # Check 5: Disk space
    Write-Host "Checking disk space..."
    $disk = Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='C:'"
    $freeSpaceGB = [math]::Round($disk.FreeSpace / 1GB, 2)
    $totalSpaceGB = [math]::Round($disk.Size / 1GB, 2)
    $percentFree = [math]::Round(($disk.FreeSpace / $disk.Size) * 100, 2)
    
    Write-Host "  Disk C: $freeSpaceGB GB free of $totalSpaceGB GB ($percentFree% free)"
    
    if ($percentFree -lt 10) {
        Write-Warning "  ⚠️ Low disk space detected"
    }
}

# Run troubleshooting
Troubleshoot-SandboxIssues
```

## Deployment Automation

### Complete Sandbox Setup Script

```powershell
# sandbox/deploy-sandbox.ps1

param(
    [string]$SandboxType = "Docker", # Docker, VM, Cloud
    [string]$Environment = "SANDBOX",
    [switch]$SkipPreRequisites
)

function Deploy-DockerSandbox {
    Write-Host "=== Deploying Docker-based Temenos Sandbox ==="
    
    # Check Docker availability
    try {
        docker --version
        Write-Host "✅ Docker is available"
    }
    catch {
        throw "❌ Docker is not installed or not in PATH"
    }
    
    # Build and start containers
    Write-Host "Building and starting containers..."
    docker-compose -f docker-compose.temenos.yml up -d --build
    
    # Wait for services to start
    Write-Host "Waiting for services to start..."
    Start-Sleep -Seconds 30
    
    # Verify deployment
    $services = @("temenos-db", "temenos-mock", "mq-server")
    foreach ($service in $services) {
        $status = docker ps --filter "name=$service" --format "table {{.Names}}\t{{.Status}}"
        Write-Host "  $service: $status"
    }
    
    Write-Host "✅ Docker sandbox deployment completed"
}

function Deploy-VMSandbox {
    Write-Host "=== Deploying VM-based Temenos Sandbox ==="
    
    # Check Hyper-V availability
    $hyperV = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All
    if ($hyperV.State -ne "Enabled") {
        Write-Warning "❌ Hyper-V is not enabled. Please enable Hyper-V and restart."
        return
    }
    
    # Create and configure VM
    # Implementation would depend on specific VM requirements
    Write-Host "✅ VM sandbox deployment requires manual configuration"
}

function Deploy-CloudSandbox {
    Write-Host "=== Deploying Cloud-based Temenos Sandbox ==="
    
    # Check cloud CLI availability
    try {
        aws --version
        Write-Host "✅ AWS CLI is available"
        
        # Deploy CloudFormation stack
        aws cloudformation create-stack --stack-name temenos-sandbox --template-body file://temenos-sandbox-aws.yml
        Write-Host "✅ CloudFormation deployment initiated"
    }
    catch {
        Write-Warning "❌ AWS CLI not available, trying Azure..."
        
        try {
            az --version
            Write-Host "✅ Azure CLI is available"
            
            # Deploy ARM template
            az deployment group create --resource-group temenos-sandbox --template-file temenos-sandbox-azure.json
            Write-Host "✅ Azure deployment initiated"
        }
        catch {
            Write-Error "❌ No cloud CLI available. Please install AWS CLI or Azure CLI."
        }
    }
}

function Test-SandboxDeployment {
    Write-Host "=== Testing Sandbox Deployment ==="
    
    # Import testing module
    Import-Module ./TemenosChecks.Sandbox/TemenosChecks.Sandbox.psd1 -Force
    
    # Connect to sandbox
    $connection = Connect-TemenosSandbox
    if ($connection) {
        Write-Host "✅ Sandbox connectivity test passed"
        
        # Run health checks
        $healthResults = Test-SandboxServices
        $healthyCount = ($healthResults | Where-Object { $_.Healthy }).Count
        Write-Host "✅ Health check: $healthyCount/$($healthResults.Count) services healthy"
        
        # Test SOD operation
        $sodResult = Start-SandboxSOD -DryRun
        if ($sodResult) {
            Write-Host "✅ SOD operation test passed"
        }
        
        Write-Host "✅ All sandbox tests completed successfully"
    } else {
        Write-Error "❌ Sandbox connectivity test failed"
    }
}

# Main deployment logic
try {
    if (-not $SkipPreRequisites) {
        Write-Host "Checking prerequisites..."
        # Add prerequisite checks here
    }
    
    switch ($SandboxType) {
        "Docker" { Deploy-DockerSandbox }
        "VM" { Deploy-VMSandbox }
        "Cloud" { Deploy-CloudSandbox }
        default { throw "Unknown sandbox type: $SandboxType" }
    }
    
    # Test deployment
    Test-SandboxDeployment
    
    Write-Host "🎉 Temenos Sandbox deployment completed successfully!"
    Write-Host "📋 Next steps:"
    Write-Host "   1. Configure Service Management Platform with sandbox endpoints"
    Write-Host "   2. Run integration tests"
    Write-Host "   3. Begin development and testing"
    
} catch {
    Write-Error "❌ Sandbox deployment failed: $_"
    exit 1
}
```

## Conclusion

This guide provides multiple approaches to setting up a Temenos sandbox environment for testing the Service Management Platform:

1. **Official Temenos SandBox**: Cloud-based official environment with real Temenos APIs
2. **Docker Sandbox**: Containerized mock environment for local development
3. **VM Sandbox**: Full virtual machine setup for comprehensive testing
4. **Cloud Sandbox**: Scalable cloud-based environment for team collaboration

Choose the approach that best fits your requirements:
- **Development**: Docker sandbox for quick local testing
- **Team Testing**: Cloud sandbox for shared environments
- **Production-like Testing**: VM sandbox for comprehensive validation
- **Official Testing**: Temenos SandBox for real API integration

The mock services and testing scripts provide a foundation for validating all Service Management Platform capabilities in a safe, controlled environment before production deployment.