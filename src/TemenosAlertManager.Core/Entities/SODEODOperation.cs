using System.ComponentModel.DataAnnotations;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Core.Entities;

/// <summary>
/// Tracks SOD/EOD operations for audit and monitoring purposes
/// </summary>
public class SODEODOperation : BaseEntity
{
    /// <summary>
    /// Type of operation - SOD or EOD
    /// </summary>
    [Required]
    [StringLength(10)]
    public string OperationType { get; set; } = string.Empty; // 'SOD' or 'EOD'
    
    /// <summary>
    /// Unique operation identifier for tracking
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OperationCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Business date for the operation
    /// </summary>
    [Required]
    public DateTime BusinessDate { get; set; }
    
    /// <summary>
    /// Environment where operation is running (PROD, UAT, DEV, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Environment { get; set; } = string.Empty;
    
    /// <summary>
    /// When the operation started
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// When the operation completed (null if still running)
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Current status of the operation
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty; // 'Initiated', 'Running', 'Completed', 'Failed', 'Cancelled'
    
    /// <summary>
    /// User who initiated the operation
    /// </summary>
    [Required]
    [StringLength(100)]
    public string InitiatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// How the operation was initiated
    /// </summary>
    [StringLength(50)]
    public string InitiationMethod { get; set; } = "Manual"; // Manual, Scheduled, API
    
    /// <summary>
    /// JSON array of step statuses for detailed tracking
    /// </summary>
    public string? Steps { get; set; }
    
    /// <summary>
    /// Error details if operation failed
    /// </summary>
    public string? ErrorDetails { get; set; }
    
    /// <summary>
    /// JSON array of service IDs involved in this operation
    /// </summary>
    public string? ServicesInvolved { get; set; }
    
    /// <summary>
    /// Navigation property for related operation steps
    /// </summary>
    public virtual ICollection<OperationStep> OperationSteps { get; set; } = new List<OperationStep>();
    
    /// <summary>
    /// Navigation property for related service actions
    /// </summary>
    public virtual ICollection<ServiceAction> ServiceActions { get; set; } = new List<ServiceAction>();
}