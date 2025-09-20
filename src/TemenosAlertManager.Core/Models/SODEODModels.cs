namespace TemenosAlertManager.Core.Models;

/// <summary>
/// Request DTO for Starting Start of Day operations
/// </summary>
public class SODRequest
{
    /// <summary>
    /// Environment to run SOD on (PROD, UAT, DEV, etc.)
    /// </summary>
    public string Environment { get; set; } = string.Empty;
    
    /// <summary>
    /// Filter to only run specific services (empty means all services)
    /// </summary>
    public string[] ServicesFilter { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Run in dry-run mode without making actual changes
    /// </summary>
    public bool DryRun { get; set; } = false;
    
    /// <summary>
    /// Force execution even if pre-conditions fail
    /// </summary>
    public bool ForceExecution { get; set; } = false;
    
    /// <summary>
    /// Comments about why this operation is being performed
    /// </summary>
    public string? Comments { get; set; }
}

/// <summary>
/// Request DTO for Starting End of Day operations
/// </summary>
public class EODRequest
{
    /// <summary>
    /// Environment to run EOD on (PROD, UAT, DEV, etc.)
    /// </summary>
    public string Environment { get; set; } = string.Empty;
    
    /// <summary>
    /// Filter to only run specific services (empty means all services)
    /// </summary>
    public string[] ServicesFilter { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Run in dry-run mode without making actual changes
    /// </summary>
    public bool DryRun { get; set; } = false;
    
    /// <summary>
    /// Force execution even if pre-conditions fail
    /// </summary>
    public bool ForceExecution { get; set; } = false;
    
    /// <summary>
    /// Transaction cutoff time for EOD processing
    /// </summary>
    public DateTime? CutoffTime { get; set; }
    
    /// <summary>
    /// Comments about why this operation is being performed
    /// </summary>
    public string? Comments { get; set; }
}

/// <summary>
/// Response DTO for operation initiation
/// </summary>
public class OperationResultDto
{
    /// <summary>
    /// Unique identifier for tracking the operation
    /// </summary>
    public string OperationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the operation
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Message describing the operation result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// When the operation started
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Estimated duration in minutes
    /// </summary>
    public int EstimatedDurationMinutes { get; set; }
}

/// <summary>
/// Detailed status DTO for operation tracking
/// </summary>
public class OperationStatusDto
{
    /// <summary>
    /// Unique identifier for the operation
    /// </summary>
    public string OperationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of operation (SOD or EOD)
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the operation
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// When the operation started
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// When the operation completed (null if still running)
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; }
    
    /// <summary>
    /// Name of the currently executing step
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed step information
    /// </summary>
    public OperationStepDto[] Steps { get; set; } = Array.Empty<OperationStepDto>();
    
    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for individual operation steps
/// </summary>
public class OperationStepDto
{
    /// <summary>
    /// Name of the step
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the step
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// When the step started
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// When the step completed
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Additional step details
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Error message if step failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}