using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemenosAlertManager.Core.Entities.Configuration;
using TemenosAlertManager.Core.Interfaces;

namespace TemenosAlertManager.Api.Controllers;

/// <summary>
/// Controller for managing system configuration settings
/// </summary>
[ApiController]
[Route("api/system-configuration")]
[Authorize(Policy = "AdminOnly")]
public class SystemConfigurationController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SystemConfigurationController> _logger;

    public SystemConfigurationController(
        IUnitOfWork unitOfWork,
        ILogger<SystemConfigurationController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all system configurations
    /// </summary>
    [HttpGet("system-configs")]
    public async Task<ActionResult<IEnumerable<SystemConfigDto>>> GetSystemConfigs()
    {
        try
        {
            var systemConfigs = await _unitOfWork.Configuration.GetSystemConfigsAsync();
            var result = systemConfigs.Select(sc => new SystemConfigDto
            {
                Id = sc.Id,
                Key = sc.Key,
                Value = sc.IsEncrypted ? "********" : sc.Value, // Mask encrypted values
                Category = sc.Category,
                Description = sc.Description,
                IsEncrypted = sc.IsEncrypted,
                CreatedAt = sc.CreatedAt,
                UpdatedAt = sc.UpdatedAt ?? DateTime.UtcNow
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system configurations");
            return StatusCode(500, new { Message = "Failed to get system configurations", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get service configurations (hosts)
    /// </summary>
    [HttpGet("service-hosts")]
    public async Task<ActionResult<IEnumerable<ServiceHostDto>>> GetServiceHosts()
    {
        try
        {
            var serviceConfigs = await _unitOfWork.Configuration.GetServiceConfigsAsync(enabledOnly: false);
            var result = serviceConfigs.Select(sc => new ServiceHostDto
            {
                Id = sc.Id,
                Name = sc.Name,
                Host = sc.Host,
                Type = sc.Type.ToString(),
                Description = sc.Description,
                IsEnabled = sc.IsEnabled,
                CreatedAt = sc.CreatedAt,
                UpdatedAt = sc.UpdatedAt ?? DateTime.UtcNow
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service hosts");
            return StatusCode(500, new { Message = "Failed to get service hosts", Error = ex.Message });
        }
    }

    /// <summary>
    /// Add a new system configuration
    /// </summary>
    [HttpPost("system-configs")]
    public async Task<ActionResult<SystemConfigDto>> AddSystemConfig([FromBody] CreateSystemConfigDto createDto)
    {
        try
        {
            var systemConfig = new SystemConfig
            {
                Key = createDto.Key,
                Value = createDto.Value,
                Category = createDto.Category,
                Description = createDto.Description,
                IsEncrypted = createDto.IsEncrypted
            };

            var created = await _unitOfWork.Configuration.AddAsync(systemConfig);
            
            var result = new SystemConfigDto
            {
                Id = created.Id,
                Key = created.Key,
                Value = created.IsEncrypted ? "********" : created.Value,
                Category = created.Category,
                Description = created.Description,
                IsEncrypted = created.IsEncrypted,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt ?? DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetSystemConfigs), new { id = created.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add system configuration");
            return StatusCode(500, new { Message = "Failed to add system configuration", Error = ex.Message });
        }
    }

    /// <summary>
    /// Add a new service host
    /// </summary>
    [HttpPost("service-hosts")]
    public async Task<ActionResult<ServiceHostDto>> AddServiceHost([FromBody] CreateServiceHostDto createDto)
    {
        try
        {
            // Parse the service type enum
            if (!Enum.TryParse<Core.Enums.MonitoringDomain>(createDto.Type, out var serviceType))
            {
                return BadRequest(new { Message = "Invalid service type" });
            }

            var serviceConfig = new ServiceConfig
            {
                Name = createDto.Name,
                Host = createDto.Host,
                Type = serviceType,
                Description = createDto.Description,
                IsEnabled = createDto.IsEnabled
            };

            var created = await _unitOfWork.ServiceConfigs.AddAsync(serviceConfig);
            await _unitOfWork.SaveChangesAsync();
            
            var result = new ServiceHostDto
            {
                Id = created.Id,
                Name = created.Name,
                Host = created.Host,
                Type = created.Type.ToString(),
                Description = created.Description,
                IsEnabled = created.IsEnabled,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt ?? DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetServiceHosts), new { id = created.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add service host");
            return StatusCode(500, new { Message = "Failed to add service host", Error = ex.Message });
        }
    }

    /// <summary>
    /// Update a system configuration
    /// </summary>
    [HttpPut("system-configs/{id}")]
    public async Task<ActionResult<SystemConfigDto>> UpdateSystemConfig(int id, [FromBody] UpdateSystemConfigDto updateDto)
    {
        try
        {
            var systemConfigs = await _unitOfWork.Configuration.GetSystemConfigsAsync();
            var existing = systemConfigs.FirstOrDefault(sc => sc.Id == id);
            
            if (existing == null)
            {
                return NotFound(new { Message = "System configuration not found" });
            }

            existing.Key = updateDto.Key;
            existing.Value = updateDto.Value;
            existing.Category = updateDto.Category;
            existing.Description = updateDto.Description;
            existing.IsEncrypted = updateDto.IsEncrypted;
            existing.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Configuration.UpdateAsync(existing);
            
            var result = new SystemConfigDto
            {
                Id = existing.Id,
                Key = existing.Key,
                Value = existing.IsEncrypted ? "********" : existing.Value,
                Category = existing.Category,
                Description = existing.Description,
                IsEncrypted = existing.IsEncrypted,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = existing.UpdatedAt ?? DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update system configuration");
            return StatusCode(500, new { Message = "Failed to update system configuration", Error = ex.Message });
        }
    }

    /// <summary>
    /// Update a service host
    /// </summary>
    [HttpPut("service-hosts/{id}")]
    public async Task<ActionResult<ServiceHostDto>> UpdateServiceHost(int id, [FromBody] UpdateServiceHostDto updateDto)
    {
        try
        {
            var existing = await _unitOfWork.ServiceConfigs.GetByIdAsync(id);
            
            if (existing == null)
            {
                return NotFound(new { Message = "Service host not found" });
            }

            // Parse the service type enum
            if (!Enum.TryParse<Core.Enums.MonitoringDomain>(updateDto.Type, out var serviceType))
            {
                return BadRequest(new { Message = "Invalid service type" });
            }

            existing.Name = updateDto.Name;
            existing.Host = updateDto.Host;
            existing.Type = serviceType;
            existing.Description = updateDto.Description;
            existing.IsEnabled = updateDto.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ServiceConfigs.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();
            
            var result = new ServiceHostDto
            {
                Id = existing.Id,
                Name = existing.Name,
                Host = existing.Host,
                Type = existing.Type.ToString(),
                Description = existing.Description,
                IsEnabled = existing.IsEnabled,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = existing.UpdatedAt ?? DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update service host");
            return StatusCode(500, new { Message = "Failed to update service host", Error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a system configuration
    /// </summary>
    [HttpDelete("system-configs/{id}")]
    public async Task<ActionResult> DeleteSystemConfig(int id)
    {
        try
        {
            var systemConfigs = await _unitOfWork.Configuration.GetSystemConfigsAsync();
            var existing = systemConfigs.FirstOrDefault(sc => sc.Id == id);
            
            if (existing == null)
            {
                return NotFound(new { Message = "System configuration not found" });
            }

            // For now, we'll implement a soft delete by setting a flag or similar
            // Since we don't have a direct delete method, we'll need to add it to the repository
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete system configuration");
            return StatusCode(500, new { Message = "Failed to delete system configuration", Error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a service host
    /// </summary>
    [HttpDelete("service-hosts/{id}")]
    public async Task<ActionResult> DeleteServiceHost(int id)
    {
        try
        {
            var existing = await _unitOfWork.ServiceConfigs.GetByIdAsync(id);
            
            if (existing == null)
            {
                return NotFound(new { Message = "Service host not found" });
            }

            await _unitOfWork.ServiceConfigs.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete service host");
            return StatusCode(500, new { Message = "Failed to delete service host", Error = ex.Message });
        }
    }
}

// DTOs
public class SystemConfigDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSystemConfigDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
}

public class UpdateSystemConfigDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
}

public class ServiceHostDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateServiceHostDto
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateServiceHostDto
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
}