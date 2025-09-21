namespace TemenosAlertManager.Core.Models;

/// <summary>
/// Result DTO for service management actions
/// </summary>
public class ServiceActionResultDto
{
    /// <summary>
    /// ID of the service that was acted upon
    /// </summary>
    public int ServiceId { get; set; }
    
    /// <summary>
    /// Name of the service
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Action that was performed
    /// </summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Status of the action
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Message describing the action result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// When the action started
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Duration of the action in milliseconds
    /// </summary>
    public int? DurationMs { get; set; }
}

/// <summary>
/// Summary DTO for all service statuses
/// </summary>
public class ServiceStatusSummaryDto
{
    /// <summary>
    /// When the status was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }
    
    /// <summary>
    /// Status of individual services
    /// </summary>
    public ServiceStatusDto[] Services { get; set; } = Array.Empty<ServiceStatusDto>();
    
    /// <summary>
    /// Status grouped by domain
    /// </summary>
    public DomainStatusDto[] DomainStatus { get; set; } = Array.Empty<DomainStatusDto>();
    
    /// <summary>
    /// Currently active SOD operation (if any)
    /// </summary>
    public OperationSummaryDto? ActiveSODOperation { get; set; }
    
    /// <summary>
    /// Currently active EOD operation (if any)
    /// </summary>
    public OperationSummaryDto? ActiveEODOperation { get; set; }
}

/// <summary>
/// DTO for individual service status
/// </summary>
public class ServiceStatusDto
{
    /// <summary>
    /// Service configuration ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Host where service is located
    /// </summary>
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of service (monitoring domain)
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Current health status
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// When the status was last checked
    /// </summary>
    public DateTime LastChecked { get; set; }
    
    /// <summary>
    /// Whether user can start this service
    /// </summary>
    public bool CanStart { get; set; }
    
    /// <summary>
    /// Whether user can stop this service
    /// </summary>
    public bool CanStop { get; set; }
    
    /// <summary>
    /// Whether user can restart this service
    /// </summary>
    public bool CanRestart { get; set; }
    
    /// <summary>
    /// Whether service is critical for SOD
    /// </summary>
    public bool IsCriticalForSOD { get; set; }
    
    /// <summary>
    /// Whether service is critical for EOD
    /// </summary>
    public bool IsCriticalForEOD { get; set; }
}

/// <summary>
/// DTO for domain-level status summary
/// </summary>
public class DomainStatusDto
{
    /// <summary>
    /// Domain name
    /// </summary>
    public string Domain { get; set; } = string.Empty;
    
    /// <summary>
    /// Total number of services in domain
    /// </summary>
    public int TotalServices { get; set; }
    
    /// <summary>
    /// Number of healthy services
    /// </summary>
    public int HealthyServices { get; set; }
    
    /// <summary>
    /// Number of unhealthy services
    /// </summary>
    public int UnhealthyServices { get; set; }
    
    /// <summary>
    /// Overall health percentage
    /// </summary>
    public double HealthPercentage { get; set; }
}

/// <summary>
/// Summary DTO for operations
/// </summary>
public class OperationSummaryDto
{
    /// <summary>
    /// Operation identifier
    /// </summary>
    public string OperationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of operation
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Progress percentage
    /// </summary>
    public int ProgressPercentage { get; set; }
    
    /// <summary>
    /// When operation started
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Estimated completion time
    /// </summary>
    public DateTime? EstimatedEndTime { get; set; }
    
    /// <summary>
    /// Current step being executed
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;
}

/// <summary>
/// DTO for service action history
/// </summary>
public class ServiceActionDto
{
    /// <summary>
    /// Action ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Action performed
    /// </summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// User who initiated the action
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When action started
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// When action completed
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Action status
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Result details
    /// </summary>
    public string? Result { get; set; }
    
    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Generic paging DTO
/// </summary>
public class PagingDto
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Generic paged result DTO
/// </summary>
public class PagedResult<T>
{
    /// <summary>
    /// Items in current page
    /// </summary>
    public T[] Items { get; set; } = Array.Empty<T>();
    
    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}