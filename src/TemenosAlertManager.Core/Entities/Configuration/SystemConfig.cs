using System.ComponentModel.DataAnnotations;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Core.Entities.Configuration;

public class AuthConfig : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string AdGroupName { get; set; } = string.Empty;
    
    [Required]
    public UserRole Role { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    [StringLength(500)]
    public string? Description { get; set; }
}

public class AlertConfig : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public MonitoringDomain Domain { get; set; }
    
    [Required]
    [StringLength(200)]
    public string MetricName { get; set; } = string.Empty;
    
    public string? WarningThreshold { get; set; }
    
    public string? CriticalThreshold { get; set; }
    
    public int SuppressionWindowMinutes { get; set; } = 15;
    
    public bool IsEnabled { get; set; } = true;
    
    public string? EscalationChain { get; set; } // JSON array of recipients
    
    public string? RunbookUrl { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
}

public class SystemConfig : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? Category { get; set; }
    
    public bool IsEncrypted { get; set; } = false;
    
    [StringLength(500)]
    public string? Description { get; set; }
}