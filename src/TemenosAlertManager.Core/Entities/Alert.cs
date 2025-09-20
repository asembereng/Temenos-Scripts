using System.ComponentModel.DataAnnotations;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Core.Entities;

public class Alert : BaseEntity
{
    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public AlertSeverity Severity { get; set; }
    
    [Required]
    public AlertState State { get; set; }
    
    [Required]
    public MonitoringDomain Domain { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Source { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Fingerprint { get; set; } = string.Empty;
    
    public string? MetricValue { get; set; }
    
    public string? Threshold { get; set; }
    
    public DateTime? AcknowledgedAt { get; set; }
    
    public string? AcknowledgedBy { get; set; }
    
    public DateTime? ResolvedAt { get; set; }
    
    public string? ResolvedBy { get; set; }
    
    public string? Notes { get; set; }
    
    public string? AdditionalData { get; set; } // JSON data
    
    public int SuppressionWindowMinutes { get; set; } = 15;
    
    // Navigation properties
    public virtual ICollection<AlertOutbox> AlertOutboxes { get; set; } = new List<AlertOutbox>();
}