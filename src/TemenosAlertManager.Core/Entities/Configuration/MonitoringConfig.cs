using System.ComponentModel.DataAnnotations;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Core.Entities.Configuration;

public class ServiceConfig : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Host { get; set; } = string.Empty;
    
    [Required]
    public MonitoringDomain Type { get; set; }
    
    public string? StartCommand { get; set; }
    
    public string? StopCommand { get; set; }
    
    public string? HealthCheckCommand { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public string? AdditionalConfig { get; set; } // JSON
    
    [StringLength(500)]
    public string? Description { get; set; }
}

public class QueueConfig : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string QueueManager { get; set; } = string.Empty;
    
    public int WarningDepth { get; set; } = 1000;
    
    public int CriticalDepth { get; set; } = 5000;
    
    public string? DeadLetterQueue { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public string? AdditionalConfig { get; set; } // JSON
    
    [StringLength(500)]
    public string? Description { get; set; }
}

public class SqlTargetConfig : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string InstanceName { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? DatabaseName { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Role { get; set; } = string.Empty; // Primary, Secondary, etc.
    
    public string? ConnectionString { get; set; } // Encrypted
    
    public string? Thresholds { get; set; } // JSON with threshold configurations
    
    public bool IsEnabled { get; set; } = true;
    
    [StringLength(500)]
    public string? Description { get; set; }
}