# Temenos Service Management Implementation Specification

## Overview

This document provides detailed technical specifications for implementing Temenos service management capabilities in the Alert Manager application, including Start of Day (SOD), End of Day (EOD), and general service control functionality.

## 1. Architecture Overview

### 1.1 Current System Integration

The implementation will extend the existing Alert Manager architecture:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   React UI      │    │  .NET Core API  │    │  PowerShell     │
│                 │    │                 │    │  Modules        │
│ • Service Dash  │◄──►│ • Service Ctrl  │◄──►│ • SOD/EOD       │
│ • SOD/EOD Ctrl  │    │ • Health Mon    │    │ • Service Mgmt  │
│ • Status Mon    │    │ • Auth/Audit    │    │ • Health Checks │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌─────────────────┐
                       │  SQL Server DB  │
                       │                 │
                       │ • Service Config│
                       │ • SOD/EOD Logs  │
                       │ • Audit Trail   │
                       └─────────────────┘
```

### 1.2 New Components to be Added

1. **TemenosOperationsController**: New API controller for SOD/EOD operations
2. **ServiceManagementController**: Enhanced service control endpoints
3. **SODEODOrchestrator**: Service for managing complex operations
4. **TemenosOperationService**: Business logic for Temenos operations
5. **PowerShell Modules**: Specialized modules for SOD/EOD procedures

## 2. Database Schema Extensions

### 2.1 New Tables

```sql
-- Table for tracking SOD/EOD operations
CREATE TABLE SODEODOperations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OperationType VARCHAR(10) NOT NULL CHECK (OperationType IN ('SOD', 'EOD')),
    OperationCode VARCHAR(50) NOT NULL, -- Unique operation identifier
    BusinessDate DATE NOT NULL,
    Environment VARCHAR(50) NOT NULL, -- PROD, UAT, DEV, etc.
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NULL,
    Status VARCHAR(20) NOT NULL CHECK (Status IN ('Initiated', 'Running', 'Completed', 'Failed', 'Cancelled')),
    InitiatedBy VARCHAR(100) NOT NULL,
    InitiationMethod VARCHAR(50) DEFAULT 'Manual', -- Manual, Scheduled, API
    Steps NVARCHAR(MAX), -- JSON array of step statuses
    ErrorDetails NVARCHAR(MAX) NULL,
    ServicesInvolved NVARCHAR(MAX), -- JSON array of service IDs
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Table for individual operation steps
CREATE TABLE OperationSteps (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OperationId INT NOT NULL FOREIGN KEY REFERENCES SODEODOperations(Id),
    StepName VARCHAR(100) NOT NULL,
    StepOrder INT NOT NULL,
    StartTime DATETIME2 NULL,
    EndTime DATETIME2 NULL,
    Status VARCHAR(20) NOT NULL CHECK (Status IN ('Pending', 'Running', 'Completed', 'Failed', 'Skipped')),
    Details NVARCHAR(MAX),
    ErrorMessage NVARCHAR(MAX) NULL,
    RetryCount INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Table for service actions audit
CREATE TABLE ServiceActions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ServiceConfigId INT NOT NULL FOREIGN KEY REFERENCES ServiceConfig(Id),
    Action VARCHAR(20) NOT NULL CHECK (Action IN ('Start', 'Stop', 'Restart', 'HealthCheck')),
    InitiatedBy VARCHAR(100) NOT NULL,
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NULL,
    Status VARCHAR(20) NOT NULL CHECK (Status IN ('Running', 'Completed', 'Failed')),
    Result NVARCHAR(MAX),
    ErrorMessage NVARCHAR(MAX) NULL,
    OperationId INT NULL FOREIGN KEY REFERENCES SODEODOperations(Id), -- Link to SOD/EOD operation if applicable
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

### 2.2 ServiceConfig Table Extensions

```sql
-- Add SOD/EOD specific columns to existing ServiceConfig table
ALTER TABLE ServiceConfig ADD 
    SODCommand NVARCHAR(500) NULL,
    EODCommand NVARCHAR(500) NULL,
    SODOrder INT DEFAULT 0,
    EODOrder INT DEFAULT 0,
    SODTimeout INT DEFAULT 300, -- Timeout in seconds
    EODTimeout INT DEFAULT 600,
    IsCriticalForSOD BIT DEFAULT 0,
    IsCriticalForEOD BIT DEFAULT 0,
    SODDependencies NVARCHAR(MAX) NULL, -- JSON array of service IDs this depends on
    EODDependencies NVARCHAR(MAX) NULL,
    AllowParallelExecution BIT DEFAULT 1,
    RequiresManualConfirmation BIT DEFAULT 0;
```

## 3. API Specifications

### 3.1 New Controllers

#### TemenosOperationsController

```csharp
[Route("api/temenos/operations")]
[ApiController] 
[Authorize(Policy = "AdminOnly")]
public class TemenosOperationsController : ControllerBase
{
    /// <summary>
    /// Initiate Start of Day operation
    /// </summary>
    [HttpPost("sod")]
    public async Task<ActionResult<OperationResultDto>> StartSOD([FromBody] SODRequest request);

    /// <summary>
    /// Initiate End of Day operation
    /// </summary>
    [HttpPost("eod")]
    public async Task<ActionResult<OperationResultDto>> StartEOD([FromBody] EODRequest request);

    /// <summary>
    /// Get status of specific operation
    /// </summary>
    [HttpGet("{operationId}")]
    public async Task<ActionResult<OperationStatusDto>> GetOperationStatus(string operationId);

    /// <summary>
    /// Cancel running operation
    /// </summary>
    [HttpPost("{operationId}/cancel")]
    public async Task<ActionResult<OperationResultDto>> CancelOperation(string operationId);

    /// <summary>
    /// Get all recent operations
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<OperationSummaryDto>>> GetOperations([FromQuery] OperationQueryDto query);
}
```

#### ServiceManagementController (Enhancement)

```csharp
[Route("api/services")]
[ApiController]
[Authorize(Policy = "OperatorOrAdmin")]
public class ServiceManagementController : ControllerBase
{
    /// <summary>
    /// Start a specific service
    /// </summary>
    [HttpPost("{serviceId}/start")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ServiceActionResultDto>> StartService(int serviceId);

    /// <summary>
    /// Stop a specific service
    /// </summary>
    [HttpPost("{serviceId}/stop")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ServiceActionResultDto>> StopService(int serviceId);

    /// <summary>
    /// Restart a specific service
    /// </summary>
    [HttpPost("{serviceId}/restart")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ServiceActionResultDto>> RestartService(int serviceId);

    /// <summary>
    /// Get comprehensive status of all services
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ServiceStatusSummaryDto>> GetServicesStatus([FromQuery] string? domain = null);

    /// <summary>
    /// Get service action history
    /// </summary>
    [HttpGet("{serviceId}/actions")]
    public async Task<ActionResult<PagedResult<ServiceActionDto>>> GetServiceActions(int serviceId, [FromQuery] PagingDto paging);
}
```

### 3.2 DTOs and Models

```csharp
// Request DTOs
public class SODRequest
{
    public string Environment { get; set; } = string.Empty;
    public string[] ServicesFilter { get; set; } = Array.Empty<string>();
    public bool DryRun { get; set; } = false;
    public bool ForceExecution { get; set; } = false;
    public string? Comments { get; set; }
}

public class EODRequest
{
    public string Environment { get; set; } = string.Empty;
    public string[] ServicesFilter { get; set; } = Array.Empty<string>();
    public bool DryRun { get; set; } = false;
    public bool ForceExecution { get; set; } = false;
    public DateTime? CutoffTime { get; set; }
    public string? Comments { get; set; }
}

// Response DTOs
public class OperationResultDto
{
    public string OperationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int EstimatedDurationMinutes { get; set; }
}

public class OperationStatusDto
{
    public string OperationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int ProgressPercentage { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public OperationStepDto[] Steps { get; set; } = Array.Empty<OperationStepDto>();
    public string? ErrorMessage { get; set; }
}

public class ServiceActionResultDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int? DurationMs { get; set; }
}

public class ServiceStatusSummaryDto
{
    public DateTime LastUpdated { get; set; }
    public ServiceStatusDto[] Services { get; set; } = Array.Empty<ServiceStatusDto>();
    public DomainStatusDto[] DomainStatus { get; set; } = Array.Empty<DomainStatusDto>();
    public OperationSummaryDto? ActiveSODOperation { get; set; }
    public OperationSummaryDto? ActiveEODOperation { get; set; }
}
```

## 4. PowerShell Module Specifications

### 4.1 TemenosChecks.SOD Module

```powershell
function Start-TemenosSOD {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Environment,
        
        [Parameter(Mandatory = $false)]
        [string[]]$Services,
        
        [Parameter(Mandatory = $false)]
        [switch]$DryRun,
        
        [Parameter(Mandatory = $false)]
        [string]$OperationId,
        
        [Parameter(Mandatory = $false)]
        [hashtable]$Configuration
    )
    
    # Implementation for SOD orchestration
    # Returns: PSObject with operation status and results
}

function Start-TemenosEOD {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Environment,
        
        [Parameter(Mandatory = $false)]
        [string[]]$Services,
        
        [Parameter(Mandatory = $false)]
        [switch]$DryRun,
        
        [Parameter(Mandatory = $false)]
        [datetime]$CutoffTime,
        
        [Parameter(Mandatory = $false)]
        [string]$OperationId
    )
    
    # Implementation for EOD orchestration
    # Returns: PSObject with operation status and results
}

function Get-TemenosOperationStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$OperationId
    )
    
    # Implementation for status checking
    # Returns: PSObject with detailed operation status
}

function Stop-TemenosOperation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$OperationId,
        
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    
    # Implementation for operation cancellation
    # Returns: PSObject with cancellation result
}
```

### 4.2 TemenosChecks.ServiceManagement Module

```powershell
function Start-TemenosService {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ServiceName,
        
        [Parameter(Mandatory = $false)]
        [string]$ComputerName = 'localhost',
        
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential,
        
        [Parameter(Mandatory = $false)]
        [int]$TimeoutSeconds = 300
    )
    
    # Implementation for service startup
    # Returns: PSObject with service action result
}

function Stop-TemenosService {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ServiceName,
        
        [Parameter(Mandatory = $false)]
        [string]$ComputerName = 'localhost',
        
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential,
        
        [Parameter(Mandatory = $false)]
        [int]$TimeoutSeconds = 300,
        
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    
    # Implementation for service shutdown
    # Returns: PSObject with service action result
}

function Restart-TemenosService {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ServiceName,
        
        [Parameter(Mandatory = $false)]
        [string]$ComputerName = 'localhost',
        
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential,
        
        [Parameter(Mandatory = $false)]
        [int]$TimeoutSeconds = 600
    )
    
    # Implementation for service restart
    # Returns: PSObject with service action result
}

function Test-TemenosServiceHealth {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ServiceName,
        
        [Parameter(Mandatory = $false)]
        [string]$ComputerName = 'localhost',
        
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential
    )
    
    # Implementation for comprehensive health check
    # Returns: PSObject with health status details
}
```

## 5. Business Logic Services

### 5.1 ITemenosOperationService Interface

```csharp
public interface ITemenosOperationService
{
    Task<OperationResult> StartSODAsync(SODRequest request, string initiatedBy, CancellationToken cancellationToken = default);
    Task<OperationResult> StartEODAsync(EODRequest request, string initiatedBy, CancellationToken cancellationToken = default);
    Task<OperationStatus> GetOperationStatusAsync(string operationId, CancellationToken cancellationToken = default);
    Task<OperationResult> CancelOperationAsync(string operationId, string cancelledBy, CancellationToken cancellationToken = default);
    Task<PagedResult<OperationSummary>> GetOperationsAsync(OperationQuery query, CancellationToken cancellationToken = default);
}
```

### 5.2 IServiceManagementService Interface

```csharp
public interface IServiceManagementService
{
    Task<ServiceActionResult> StartServiceAsync(int serviceId, string initiatedBy, CancellationToken cancellationToken = default);
    Task<ServiceActionResult> StopServiceAsync(int serviceId, string initiatedBy, CancellationToken cancellationToken = default);
    Task<ServiceActionResult> RestartServiceAsync(int serviceId, string initiatedBy, CancellationToken cancellationToken = default);
    Task<ServiceStatusSummary> GetServicesStatusAsync(string? domain = null, CancellationToken cancellationToken = default);
    Task<PagedResult<ServiceAction>> GetServiceActionsAsync(int serviceId, PagingParameters paging, CancellationToken cancellationToken = default);
}
```

## 6. UI Components Specification

### 6.1 Service Management Dashboard

```typescript
interface ServiceManagementDashboardProps {
  services: ServiceStatus[];
  onServiceAction: (serviceId: number, action: ServiceAction) => Promise<void>;
  onRefresh: () => Promise<void>;
  currentUser: User;
}

interface ServiceStatus {
  id: number;
  name: string;
  host: string;
  type: MonitoringDomain;
  status: ServiceHealthStatus;
  lastChecked: Date;
  canStart: boolean;
  canStop: boolean;
  canRestart: boolean;
}

enum ServiceAction {
  Start = 'start',
  Stop = 'stop',
  Restart = 'restart',
  HealthCheck = 'healthcheck'
}
```

### 6.2 SOD/EOD Operations Interface

```typescript
interface SODEODControlPanelProps {
  environment: string;
  availableServices: ServiceConfig[];
  onStartSOD: (request: SODRequest) => Promise<void>;
  onStartEOD: (request: EODRequest) => Promise<void>;
  activeOperations: OperationStatus[];
  currentUser: User;
}

interface OperationStatus {
  operationId: string;
  operationType: 'SOD' | 'EOD';
  status: OperationStatusType;
  progressPercentage: number;
  currentStep: string;
  startTime: Date;
  estimatedEndTime?: Date;
  steps: OperationStep[];
}

interface OperationStep {
  name: string;
  status: StepStatus;
  startTime?: Date;
  endTime?: Date;
  details?: string;
  errorMessage?: string;
}
```

## 7. Security Considerations

### 7.1 Authorization Requirements

- **Service Control Actions**: Require Admin role
- **SOD/EOD Operations**: Require Admin role with additional confirmation
- **Status Viewing**: Allow Operator and Admin roles
- **Operation Cancellation**: Require Admin role or operation initiator

### 7.2 Audit Requirements

- Log all service management actions with user identity
- Track all SOD/EOD operations with detailed step logging
- Maintain audit trail for regulatory compliance
- Implement data retention policies for audit logs

### 7.3 Additional Security Measures

- Implement two-factor authentication for critical operations
- Use encrypted communication for all PowerShell remoting
- Validate all user inputs and sanitize command parameters
- Implement rate limiting for API endpoints

## 8. Performance Requirements

### 8.1 Response Time Targets

- **Service Status Display**: < 2 seconds
- **Individual Service Actions**: < 30 seconds
- **SOD Operation Completion**: < 2 hours
- **EOD Operation Completion**: < 4 hours
- **API Response Times**: < 5 seconds for status, < 10 seconds for actions

### 8.2 Scalability Considerations

- Support for up to 100 concurrent service operations
- Handle up to 50 Temenos environments
- Support for 500+ service configurations
- Maintain performance with 6 months of historical data

## 9. Implementation Timeline

### 9.1 Phase 1: Core Service Management (Week 1-2)
- Implement ServiceManagementController
- Create PowerShell service management modules
- Add service action audit logging
- Create basic UI for service status and control

### 9.2 Phase 2: SOD/EOD Foundation (Week 3-4)
- Implement database schema extensions
- Create TemenosOperationService
- Build SOD/EOD orchestration logic
- Add operation tracking and status endpoints

### 9.3 Phase 3: Advanced Orchestration (Week 5-6)
- Implement dependency management
- Add error handling and rollback procedures
- Create comprehensive PowerShell modules
- Build SOD/EOD UI components

### 9.4 Phase 4: Testing and Refinement (Week 7-8)
- Comprehensive testing in non-production environments
- Performance testing and optimization
- Security testing and penetration testing
- Documentation and user training materials

## 10. Testing Strategy

### 10.1 Unit Testing
- Test all business logic services
- Test PowerShell module functions
- Test API controllers and endpoints
- Achieve minimum 80% code coverage

### 10.2 Integration Testing
- Test complete SOD/EOD workflows
- Test service management operations
- Test cross-service dependencies
- Test error scenarios and rollback procedures

### 10.3 User Acceptance Testing
- Test with actual operations teams
- Validate UI usability and workflows
- Test with realistic service configurations
- Validate compliance with operational procedures

This implementation specification provides the foundation for building robust Temenos service management capabilities while leveraging the existing Alert Manager infrastructure.