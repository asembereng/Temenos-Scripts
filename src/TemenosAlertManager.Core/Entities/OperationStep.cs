using System.ComponentModel.DataAnnotations;

namespace TemenosAlertManager.Core.Entities;

/// <summary>
/// Tracks individual steps within SOD/EOD operations
/// </summary>
public class OperationStep : BaseEntity
{
    /// <summary>
    /// Reference to the parent operation
    /// </summary>
    [Required]
    public int OperationId { get; set; }
    
    /// <summary>
    /// Name of the step
    /// </summary>
    [Required]
    [StringLength(100)]
    public string StepName { get; set; } = string.Empty;
    
    /// <summary>
    /// Order of execution for this step
    /// </summary>
    [Required]
    public int StepOrder { get; set; }
    
    /// <summary>
    /// When the step started
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// When the step completed
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Current status of the step
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending"; // 'Pending', 'Running', 'Completed', 'Failed', 'Skipped'
    
    /// <summary>
    /// Additional details about the step execution
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Error message if step failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Number of retry attempts for this step
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Navigation property to parent operation
    /// </summary>
    public virtual SODEODOperation Operation { get; set; } = null!;
}