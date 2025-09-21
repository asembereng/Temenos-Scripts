using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemenosAlertManager.Core.Enums;
using TemenosAlertManager.Core.Entities.Configuration;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Api.Security;

namespace TemenosAlertManager.Api.Controllers;

/// <summary>
/// Controller for user management and Active Directory configuration
/// </summary>
[ApiController]
[Route("api/user-management")]
[Authorize(Policy = "AdminOnly")]
public class UserManagementController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActiveDirectoryService _activeDirectoryService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        IUnitOfWork unitOfWork,
        IActiveDirectoryService activeDirectoryService,
        ILogger<UserManagementController> logger)
    {
        _unitOfWork = unitOfWork;
        _activeDirectoryService = activeDirectoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all authentication configurations (AD groups to role mappings)
    /// </summary>
    [HttpGet("auth-configs")]
    public async Task<ActionResult<IEnumerable<AuthConfigDto>>> GetAuthConfigs()
    {
        try
        {
            var authConfigs = await _unitOfWork.Configuration.GetAuthConfigsAsync(enabledOnly: false);
            var result = authConfigs.Select(ac => new AuthConfigDto
            {
                Id = ac.Id,
                AdGroupName = ac.AdGroupName,
                Role = ac.Role,
                IsEnabled = ac.IsEnabled,
                Description = ac.Description,
                CreatedAt = ac.CreatedAt,
                UpdatedAt = ac.UpdatedAt ?? DateTime.UtcNow
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get authentication configurations");
            return StatusCode(500, new { message = "Failed to get authentication configurations", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new authentication configuration
    /// </summary>
    [HttpPost("auth-configs")]
    public async Task<ActionResult<AuthConfigDto>> CreateAuthConfig([FromBody] CreateAuthConfigRequest request)
    {
        try
        {
            var authConfig = new AuthConfig
            {
                AdGroupName = request.AdGroupName,
                Role = request.Role,
                IsEnabled = request.IsEnabled,
                Description = request.Description
            };

            var createdConfig = await _unitOfWork.Configuration.AddAsync(authConfig);
            await _unitOfWork.SaveChangesAsync();

            var result = new AuthConfigDto
            {
                Id = createdConfig.Id,
                AdGroupName = createdConfig.AdGroupName,
                Role = createdConfig.Role,
                IsEnabled = createdConfig.IsEnabled,
                Description = createdConfig.Description,
                CreatedAt = authConfig.CreatedAt,
                UpdatedAt = createdConfig.UpdatedAt ?? DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetAuthConfig), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create authentication configuration for group {GroupName}", request.AdGroupName);
            return StatusCode(500, new { message = "Failed to create authentication configuration", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific authentication configuration
    /// </summary>
    [HttpGet("auth-configs/{id}")]
    public async Task<ActionResult<AuthConfigDto>> GetAuthConfig(int id)
    {
        try
        {
            var authConfig = await _unitOfWork.Configuration.GetByIdAsync(id);
            if (authConfig == null)
            {
                return NotFound(new { message = $"Authentication configuration with ID {id} not found" });
            }

            var result = new AuthConfigDto
            {
                Id = authConfig.Id,
                AdGroupName = authConfig.AdGroupName,
                Role = authConfig.Role,
                IsEnabled = authConfig.IsEnabled,
                Description = authConfig.Description,
                CreatedAt = authConfig.CreatedAt,
                UpdatedAt = authConfig.UpdatedAt ?? DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get authentication configuration {Id}", id);
            return StatusCode(500, new { message = "Failed to get authentication configuration", error = ex.Message });
        }
    }

    /// <summary>
    /// Update an authentication configuration
    /// </summary>
    [HttpPut("auth-configs/{id}")]
    public async Task<ActionResult<AuthConfigDto>> UpdateAuthConfig(int id, [FromBody] UpdateAuthConfigRequest request)
    {
        try
        {
            var authConfig = await _unitOfWork.Configuration.GetByIdAsync(id);
            if (authConfig == null)
            {
                return NotFound(new { message = $"Authentication configuration with ID {id} not found" });
            }

            authConfig.AdGroupName = request.AdGroupName;
            authConfig.Role = request.Role;
            authConfig.IsEnabled = request.IsEnabled;
            authConfig.Description = request.Description;

            await _unitOfWork.Configuration.UpdateAsync(authConfig);
            await _unitOfWork.SaveChangesAsync();

            var result = new AuthConfigDto
            {
                Id = authConfig.Id,
                AdGroupName = authConfig.AdGroupName,
                Role = authConfig.Role,
                IsEnabled = authConfig.IsEnabled,
                Description = authConfig.Description,
                CreatedAt = authConfig.CreatedAt,
                UpdatedAt = authConfig.UpdatedAt ?? DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update authentication configuration {Id}", id);
            return StatusCode(500, new { message = "Failed to update authentication configuration", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete an authentication configuration
    /// </summary>
    [HttpDelete("auth-configs/{id}")]
    public async Task<ActionResult> DeleteAuthConfig(int id)
    {
        try
        {
            var authConfig = await _unitOfWork.Configuration.GetByIdAsync(id);
            if (authConfig == null)
            {
                return NotFound(new { message = $"Authentication configuration with ID {id} not found" });
            }

            await _unitOfWork.Configuration.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete authentication configuration {Id}", id);
            return StatusCode(500, new { message = "Failed to delete authentication configuration", error = ex.Message });
        }
    }

    /// <summary>
    /// Test Active Directory connection and group validation
    /// </summary>
    [HttpPost("test-ad-connection")]
    public async Task<ActionResult<ADConnectionTestResult>> TestADConnection([FromBody] TestADConnectionRequest request)
    {
        try
        {
            var result = new ADConnectionTestResult
            {
                IsConnectionSuccessful = false,
                GroupExists = false,
                ErrorMessage = null
            };

            // Test if we can query for the group
            try
            {
                var testUser = User.Identity?.Name ?? "test@domain.com";
                result.IsConnectionSuccessful = await _activeDirectoryService.IsUserInGroupAsync(testUser, request.GroupName);
                result.GroupExists = true; // If no exception, group exists
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "AD connection test failed for group {GroupName}", request.GroupName);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test AD connection for group {GroupName}", request.GroupName);
            return StatusCode(500, new { message = "Failed to test AD connection", error = ex.Message });
        }
    }

    /// <summary>
    /// Get user roles for a specific user
    /// </summary>
    [HttpGet("users/{userPrincipalName}/roles")]
    public async Task<ActionResult<UserRolesDto>> GetUserRoles(string userPrincipalName)
    {
        try
        {
            var roles = await _activeDirectoryService.GetUserRolesAsync(userPrincipalName);
            var displayName = await _activeDirectoryService.GetUserDisplayNameAsync(userPrincipalName);

            var result = new UserRolesDto
            {
                UserPrincipalName = userPrincipalName,
                DisplayName = displayName,
                Roles = roles,
                IsActive = roles.Any()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get roles for user {UserPrincipalName}", userPrincipalName);
            return StatusCode(500, new { message = "Failed to get user roles", error = ex.Message });
        }
    }

    /// <summary>
    /// Get Active Directory configuration settings
    /// </summary>
    [HttpGet("ad-config")]
    public async Task<ActionResult<ADConfigurationDto>> GetADConfiguration()
    {
        try
        {
            var systemConfigs = await _unitOfWork.Configuration.GetSystemConfigsAsync();
            var adConfigs = systemConfigs.Where(sc => sc.Category == "ActiveDirectory").ToList();

            var result = new ADConfigurationDto
            {
                AuthenticationType = GetConfigValue(adConfigs, "AD_AUTH_TYPE", "WindowsAuthentication"),
                Domain = GetConfigValue(adConfigs, "AD_DOMAIN", ""),
                ServerAddress = GetConfigValue(adConfigs, "AD_SERVER", ""),
                BaseDN = GetConfigValue(adConfigs, "AD_BASE_DN", ""),
                ServiceAccount = GetConfigValue(adConfigs, "AD_SERVICE_ACCOUNT", ""),
                IsEnabled = GetConfigValue(adConfigs, "AD_ENABLED", "true") == "true",
                UseSSL = GetConfigValue(adConfigs, "AD_USE_SSL", "false") == "true",
                Port = int.TryParse(GetConfigValue(adConfigs, "AD_PORT", "389"), out int port) ? port : 389
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AD configuration");
            return StatusCode(500, new { message = "Failed to get AD configuration", error = ex.Message });
        }
    }

    /// <summary>
    /// Update Active Directory configuration settings
    /// </summary>
    [HttpPut("ad-config")]
    public async Task<ActionResult<ADConfigurationDto>> UpdateADConfiguration([FromBody] UpdateADConfigurationRequest request)
    {
        try
        {
            var systemConfigs = await _unitOfWork.Configuration.GetSystemConfigsAsync();
            var adConfigs = systemConfigs.Where(sc => sc.Category == "ActiveDirectory").ToList();

            await SetConfigValue(adConfigs, "AD_AUTH_TYPE", request.AuthenticationType, "Authentication type (WindowsAuthentication or AzureAD)");
            await SetConfigValue(adConfigs, "AD_DOMAIN", request.Domain, "Active Directory domain");
            await SetConfigValue(adConfigs, "AD_SERVER", request.ServerAddress, "AD server address");
            await SetConfigValue(adConfigs, "AD_BASE_DN", request.BaseDN, "Base Distinguished Name");
            await SetConfigValue(adConfigs, "AD_SERVICE_ACCOUNT", request.ServiceAccount, "Service account for AD queries");
            await SetConfigValue(adConfigs, "AD_ENABLED", request.IsEnabled.ToString(), "Enable/disable AD authentication");
            await SetConfigValue(adConfigs, "AD_USE_SSL", request.UseSSL.ToString(), "Use SSL for AD connections");
            await SetConfigValue(adConfigs, "AD_PORT", request.Port.ToString(), "AD server port");

            await _unitOfWork.SaveChangesAsync();

            var result = new ADConfigurationDto
            {
                AuthenticationType = request.AuthenticationType,
                Domain = request.Domain,
                ServerAddress = request.ServerAddress,
                BaseDN = request.BaseDN,
                ServiceAccount = request.ServiceAccount,
                IsEnabled = request.IsEnabled,
                UseSSL = request.UseSSL,
                Port = request.Port
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update AD configuration");
            return StatusCode(500, new { message = "Failed to update AD configuration", error = ex.Message });
        }
    }

    private string GetConfigValue(List<SystemConfig> configs, string key, string defaultValue)
    {
        return configs.FirstOrDefault(c => c.Key == key)?.Value ?? defaultValue;
    }

    private async Task SetConfigValue(List<SystemConfig> configs, string key, string value, string description)
    {
        var config = configs.FirstOrDefault(c => c.Key == key);
        if (config == null)
        {
            config = new SystemConfig
            {
                Key = key,
                Value = value,
                Category = "ActiveDirectory",
                Description = description
            };
            await _unitOfWork.Configuration.AddAsync(config);
        }
        else
        {
            config.Value = value;
            await _unitOfWork.Configuration.UpdateAsync(config);
        }
    }
}

// DTOs for the API
public class AuthConfigDto
{
    public int Id { get; set; }
    public string AdGroupName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAuthConfigRequest
{
    public string AdGroupName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateAuthConfigRequest
{
    public string AdGroupName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
}

public class TestADConnectionRequest
{
    public string GroupName { get; set; } = string.Empty;
}

public class ADConnectionTestResult
{
    public bool IsConnectionSuccessful { get; set; }
    public bool GroupExists { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UserRolesDto
{
    public string UserPrincipalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole[] Roles { get; set; } = Array.Empty<UserRole>();
    public bool IsActive { get; set; }
}

public class ADConfigurationDto
{
    public string AuthenticationType { get; set; } = "WindowsAuthentication";
    public string Domain { get; set; } = string.Empty;
    public string ServerAddress { get; set; } = string.Empty;
    public string BaseDN { get; set; } = string.Empty;
    public string ServiceAccount { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool UseSSL { get; set; } = false;
    public int Port { get; set; } = 389;
}

public class UpdateADConfigurationRequest
{
    public string AuthenticationType { get; set; } = "WindowsAuthentication";
    public string Domain { get; set; } = string.Empty;
    public string ServerAddress { get; set; } = string.Empty;
    public string BaseDN { get; set; } = string.Empty;
    public string ServiceAccount { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool UseSSL { get; set; } = false;
    public int Port { get; set; } = 389;
}