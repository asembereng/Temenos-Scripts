using System.ComponentModel.DataAnnotations;

namespace TemenosAlertManager.Core.Entities;

public class AuditEvent : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string UserName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    public string Resource { get; set; } = string.Empty;
    
    public string? PayloadHash { get; set; } // SHA256 hash for tamper detection
    
    public string? Details { get; set; } // JSON data
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    public bool IsSuccess { get; set; } = true;
    
    public string? ErrorMessage { get; set; }
    
    [Required]
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
}