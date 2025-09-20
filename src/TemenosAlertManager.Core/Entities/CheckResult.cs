using System.ComponentModel.DataAnnotations;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Core.Entities;

public class CheckResult : BaseEntity
{
    [Required]
    public MonitoringDomain Domain { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Target { get; set; } = string.Empty; // server, service, queue name, etc.
    
    [Required]
    [StringLength(200)]
    public string Metric { get; set; } = string.Empty; // what was checked
    
    public string? Value { get; set; } // actual value found
    
    [Required]
    public CheckStatus Status { get; set; }
    
    public string? Details { get; set; } // JSON data with additional details
    
    public double? ExecutionTimeMs { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    [Required]
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    
    public string? RunId { get; set; } // for grouping related checks
}