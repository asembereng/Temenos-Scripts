using System.ComponentModel.DataAnnotations;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Core.Entities;

public class AlertOutbox : BaseEntity
{
    [Required]
    public int AlertId { get; set; }
    
    [Required]
    public AlertChannel Channel { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Recipient { get; set; } = string.Empty;
    
    [Required]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    public string Payload { get; set; } = string.Empty;
    
    [Required]
    public AlertDeliveryStatus Status { get; set; }
    
    public int Attempts { get; set; } = 0;
    
    public int MaxAttempts { get; set; } = 5;
    
    public DateTime? NextRetryAt { get; set; }
    
    public DateTime? DeliveredAt { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public string? DeliveryId { get; set; } // External delivery tracking
    
    // Navigation properties
    public virtual Alert Alert { get; set; } = null!;
}