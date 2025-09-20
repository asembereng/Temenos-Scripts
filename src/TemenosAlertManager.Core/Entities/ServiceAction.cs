using System.ComponentModel.DataAnnotations;
using TemenosAlertManager.Core.Entities.Configuration;

namespace TemenosAlertManager.Core.Entities;

/// <summary>
/// Tracks service management actions for audit purposes
/// </summary>
public class ServiceAction : BaseEntity
{
    /// <summary>
    /// Reference to the service configuration
    /// </summary>
    [Required]
    public int ServiceConfigId { get; set; }
    
    /// <summary>
    /// Type of action performed
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Action { get; set; } = string.Empty; // 'Start', 'Stop', 'Restart', 'HealthCheck'
    
    /// <summary>
    /// User who initiated the action
    /// </summary>
    [Required]
    [StringLength(100)]
    public string InitiatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When the action started
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// When the action completed
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Status of the action
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Running"; // 'Running', 'Completed', 'Failed'
    
    /// <summary>
    /// Result details from the action
    /// </summary>
    public string? Result { get; set; }
    
    /// <summary>
    /// Error message if action failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Reference to SOD/EOD operation if this action was part of one
    /// </summary>
    public int? OperationId { get; set; }
    
    /// <summary>
    /// Navigation property to service configuration
    /// </summary>
    public virtual ServiceConfig ServiceConfig { get; set; } = null!;
    
    /// <summary>
    /// Navigation property to operation (if applicable)
    /// </summary>
    public virtual SODEODOperation? Operation { get; set; }
}