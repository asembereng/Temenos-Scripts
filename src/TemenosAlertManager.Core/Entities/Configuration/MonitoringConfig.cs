using System.ComponentModel.DataAnnotations;
using TemenosAlertManager.Core.Enums;

namespace TemenosAlertManager.Core.Entities.Configuration;

/*
 * REMOTE DEPLOYMENT CONFIGURATION SUPPORT
 * =======================================
 * 
 * These configuration entities support distributed Temenos deployments where the Alert Manager
 * application runs on a separate host from the monitored Temenos systems (T24, TPH, MQ, SQL).
 * 
 * DEPLOYMENT ARCHITECTURE PATTERNS:
 * 
 * 1. CENTRALIZED MONITORING:
 *    - Alert Manager: Dedicated monitoring server (e.g., MON-SRV01)
 *    - T24 Core Banking: Clustered application servers (e.g., T24-APP01, T24-APP02)
 *    - TPH Payment Hub: Load-balanced payment servers (e.g., TPH-SRV01, TPH-SRV02)
 *    - MQ Messaging: Queue manager cluster (e.g., MQ-SRV01, MQ-SRV02)
 *    - SQL Database: AlwaysOn availability group (e.g., SQL-PRI01, SQL-SEC01)
 * 
 * 2. SEGMENTED NETWORK DEPLOYMENT:
 *    - Alert Manager: DMZ or management network
 *    - Temenos Systems: Secure internal banking network
 *    - Requires firewall rules for WinRM (5985/5986) and monitoring protocols
 * 
 * 3. MULTI-SITE MONITORING:
 *    - Single Alert Manager monitoring multiple data centers
 *    - Geographic distribution requires WAN connectivity considerations
 *    - Different credential sets per site/environment
 * 
 * CONFIGURATION BEST PRACTICES:
 * - Use FQDNs for cross-domain scenarios
 * - Implement least-privilege service accounts per system type
 * - Store encrypted connection strings and credentials
 * - Document network requirements and firewall rules
 * - Plan for credential rotation and management
 */

/// <summary>
/// Service Configuration for Remote Temenos System Monitoring
/// 
/// Defines configuration for monitoring services deployed on separate hosts from the Alert Manager.
/// Supports distributed architectures where Temenos components (T24, TPH, etc.) run on dedicated servers.
/// 
/// REMOTE DEPLOYMENT EXAMPLES:
/// - T24 Services: Host = "T24-APP01.bank.local", Name = "T24Server", Type = T24
/// - TPH Services: Host = "TPH-SRV01", Name = "TPHPaymentService", Type = TPH  
/// - MQ Services: Host = "MQ-PROD01", Name = "IBM MQ", Type = MQ
/// 
/// NETWORK REQUIREMENTS:
/// - PowerShell remoting (WinRM) enabled on target hosts
/// - Firewall rules allowing Alert Manager to target hosts on ports 5985/5986
/// - DNS resolution or IP connectivity to specified hosts
/// 
/// SECURITY CONSIDERATIONS:
/// - Use service accounts with minimal required permissions
/// - Consider using HTTPS (port 5986) for encrypted PowerShell remoting
/// - Implement credential rotation procedures
/// </summary>
public class ServiceConfig : BaseEntity
{
    /// <summary>
    /// Service name as it appears in Windows Services or process list
    /// Examples: "T24Server", "TPHPaymentService", "IBM MQ"
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Target host where the service is deployed - CRITICAL for remote monitoring
    /// 
    /// SUPPORTED FORMATS:
    /// - Hostname: "TPHSERVER01"
    /// - FQDN: "tph-srv01.bank.local" (recommended for cross-domain)
    /// - IP Address: "192.168.1.100" (use for DNS issues)
    /// 
    /// SPECIAL VALUES:
    /// - "localhost" or empty: Monitor services on same host as Alert Manager
    /// - Multiple hosts: Use separate ServiceConfig entries for each host
    /// 
    /// DEPLOYMENT EXAMPLES:
    /// - Production TPH: "tph-prod01.bank.local"
    /// - T24 Cluster Node: "t24-app02.internal.bank"
    /// - DR Site MQ: "mq-dr01.dr.bank.local"
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of Temenos system being monitored - determines which PowerShell module to use
    /// </summary>
    [Required]
    public MonitoringDomain Type { get; set; }
    
    /// <summary>
    /// Remote command to start the service (optional)
    /// Can be PowerShell command or batch file path on target host
    /// Example: "Start-Service -Name 'TPHPaymentService'"
    /// </summary>
    public string? StartCommand { get; set; }
    
    /// <summary>
    /// Remote command to stop the service (optional)
    /// Example: "Stop-Service -Name 'TPHPaymentService' -Force"
    /// </summary>
    public string? StopCommand { get; set; }
    
    /// <summary>
    /// Custom health check command for advanced monitoring (optional)
    /// Can be PowerShell script block or command to run on target host
    /// Example: "Test-NetConnection -ComputerName 'database-server' -Port 1433"
    /// </summary>
    public string? HealthCheckCommand { get; set; }
    
    /// <summary>
    /// Enable/disable monitoring for this service configuration
    /// Useful for maintenance windows or service decomissioning
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Additional configuration in JSON format for service-specific settings
    /// 
    /// EXAMPLES:
    /// TPH Service: {"Port": 8080, "HealthEndpoint": "/api/health", "Username": "monitor"}
    /// T24 Service: {"JVMHeapThreshold": "80%", "LogPath": "C:\\T24\\logs"}
    /// MQ Service: {"QueueManager": "PROD.QM", "Channels": ["SYSTEM.ADMIN.SVRCONN"]}
    /// 
    /// REMOTE DEPLOYMENT SPECIFIC:
    /// {"RemoteLogPath": "\\\\TPH-SRV01\\logs", "Credential": "TPH-Monitor", "WinRMPort": 5986}
    /// </summary>
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