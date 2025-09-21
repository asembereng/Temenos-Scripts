using System.DirectoryServices;
using System.Security.Principal;
using TemenosAlertManager.Core.Enums;
using TemenosAlertManager.Core.Interfaces;

namespace TemenosAlertManager.Api.Security;

public interface IActiveDirectoryService
{
    Task<UserRole[]> GetUserRolesAsync(string userPrincipalName, CancellationToken cancellationToken = default);
    Task<bool> IsUserInGroupAsync(string userPrincipalName, string groupName, CancellationToken cancellationToken = default);
    Task<string> GetUserDisplayNameAsync(string userPrincipalName, CancellationToken cancellationToken = default);
    Task<bool> ValidateGroupExistsAsync(string groupName, CancellationToken cancellationToken = default);
    Task<IEnumerable<ADGroupInfo>> SearchGroupsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActiveDirectoryService> _logger;
    private readonly IConfiguration _configuration;

    public ActiveDirectoryService(IUnitOfWork unitOfWork, ILogger<ActiveDirectoryService> logger, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<UserRole[]> GetUserRolesAsync(string userPrincipalName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting roles for user: {UserPrincipalName}", userPrincipalName);

            // Get AD group mappings from configuration
            var authConfigs = await _unitOfWork.Configuration.GetAuthConfigsAsync(enabledOnly: true, cancellationToken);
            var userRoles = new List<UserRole>();

            foreach (var authConfig in authConfigs)
            {
                if (await IsUserInGroupAsync(userPrincipalName, authConfig.AdGroupName, cancellationToken))
                {
                    userRoles.Add(authConfig.Role);
                    _logger.LogDebug("User {UserPrincipalName} is member of {GroupName}, assigned role {Role}", 
                        userPrincipalName, authConfig.AdGroupName, authConfig.Role);
                }
            }

            // Default to Viewer role if no specific roles found
            if (userRoles.Count == 0)
            {
                _logger.LogInformation("No specific roles found for user {UserPrincipalName}, assigning Viewer role", userPrincipalName);
                userRoles.Add(UserRole.Viewer);
            }

            return userRoles.Distinct().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get roles for user {UserPrincipalName}", userPrincipalName);
            return new[] { UserRole.Viewer }; // Fallback to least privileged role
        }
    }

    public async Task<bool> IsUserInGroupAsync(string userPrincipalName, string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            var authType = await GetAuthenticationTypeAsync();
            
            if (authType == "AzureAD")
            {
                return await IsUserInGroupAzureADAsync(userPrincipalName, groupName, cancellationToken);
            }
            else
            {
                return await IsUserInGroupWindowsADAsync(userPrincipalName, groupName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check group membership for user {UserPrincipalName} in group {GroupName}", 
                userPrincipalName, groupName);
            return false;
        }
    }

    public async Task<string> GetUserDisplayNameAsync(string userPrincipalName, CancellationToken cancellationToken = default)
    {
        try
        {
            var authType = await GetAuthenticationTypeAsync();
            
            if (authType == "AzureAD")
            {
                return await GetUserDisplayNameAzureADAsync(userPrincipalName, cancellationToken);
            }
            else
            {
                return await GetUserDisplayNameWindowsADAsync(userPrincipalName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get display name for user {UserPrincipalName}", userPrincipalName);
            return userPrincipalName.Split('@')[0]; // Fallback
        }
    }

    public async Task<bool> ValidateGroupExistsAsync(string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            var authType = await GetAuthenticationTypeAsync();
            
            if (authType == "AzureAD")
            {
                return await ValidateGroupExistsAzureADAsync(groupName, cancellationToken);
            }
            else
            {
                return await ValidateGroupExistsWindowsADAsync(groupName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate group existence: {GroupName}", groupName);
            return false;
        }
    }

    public async Task<IEnumerable<ADGroupInfo>> SearchGroupsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            var authType = await GetAuthenticationTypeAsync();
            
            if (authType == "AzureAD")
            {
                return await SearchGroupsAzureADAsync(searchTerm, cancellationToken);
            }
            else
            {
                return await SearchGroupsWindowsADAsync(searchTerm, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search groups with term: {SearchTerm}", searchTerm);
            return Enumerable.Empty<ADGroupInfo>();
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var authType = await GetAuthenticationTypeAsync();
            
            if (authType == "AzureAD")
            {
                return await TestConnectionAzureADAsync(cancellationToken);
            }
            else
            {
                return await TestConnectionWindowsADAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test AD connection");
            return false;
        }
    }

    // Windows AD implementations
    private async Task<bool> IsUserInGroupWindowsADAsync(string userPrincipalName, string groupName, CancellationToken cancellationToken)
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            
            // For development/testing - if running as local system, simulate group membership
            if (Environment.UserDomainName.Equals("NT AUTHORITY", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Running as system account, simulating group membership for testing");
                return groupName.Contains("ADMIN", StringComparison.OrdinalIgnoreCase);
            }

            // Use DirectorySearcher to check group membership
            using var searcher = new DirectorySearcher();
            searcher.Filter = $"(&(objectClass=user)(userPrincipalName={userPrincipalName}))";
            searcher.PropertiesToLoad.Add("memberOf");
            
            var result = searcher.FindOne();
            if (result?.Properties["memberOf"] != null)
            {
                foreach (string memberOf in result.Properties["memberOf"])
                {
                    if (memberOf.Contains($"CN={groupName},", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windows AD group membership check failed");
            return false;
        }
    }

    private async Task<string> GetUserDisplayNameWindowsADAsync(string userPrincipalName, CancellationToken cancellationToken)
    {
        try
        {
            using var searcher = new DirectorySearcher();
            searcher.Filter = $"(&(objectClass=user)(userPrincipalName={userPrincipalName}))";
            searcher.PropertiesToLoad.Add("displayName");
            searcher.PropertiesToLoad.Add("cn");
            
            var result = searcher.FindOne();
            if (result != null)
            {
                var displayName = result.Properties["displayName"]?.Count > 0 
                    ? result.Properties["displayName"][0]?.ToString()
                    : null;
                
                if (!string.IsNullOrEmpty(displayName))
                {
                    return displayName;
                }
                
                var commonName = result.Properties["cn"]?.Count > 0 
                    ? result.Properties["cn"][0]?.ToString()
                    : null;
                
                if (!string.IsNullOrEmpty(commonName))
                {
                    return commonName;
                }
            }

            // Fallback to username part of UPN
            return userPrincipalName.Split('@')[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windows AD user display name lookup failed");
            return userPrincipalName.Split('@')[0];
        }
    }

    private async Task<bool> ValidateGroupExistsWindowsADAsync(string groupName, CancellationToken cancellationToken)
    {
        try
        {
            using var searcher = new DirectorySearcher();
            searcher.Filter = $"(&(objectClass=group)(CN={groupName}))";
            var result = searcher.FindOne();
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windows AD group validation failed");
            return false;
        }
    }

    private async Task<IEnumerable<ADGroupInfo>> SearchGroupsWindowsADAsync(string searchTerm, CancellationToken cancellationToken)
    {
        try
        {
            var groups = new List<ADGroupInfo>();
            using var searcher = new DirectorySearcher();
            searcher.Filter = $"(&(objectClass=group)(CN=*{searchTerm}*))";
            searcher.PropertiesToLoad.Add("CN");
            searcher.PropertiesToLoad.Add("description");
            searcher.SizeLimit = 50; // Limit results
            
            var results = searcher.FindAll();
            foreach (SearchResult result in results)
            {
                var groupName = result.Properties["CN"]?.Count > 0 
                    ? result.Properties["CN"][0]?.ToString() 
                    : "";
                var description = result.Properties["description"]?.Count > 0 
                    ? result.Properties["description"][0]?.ToString() 
                    : "";

                if (!string.IsNullOrEmpty(groupName))
                {
                    groups.Add(new ADGroupInfo
                    {
                        Name = groupName,
                        Description = description,
                        DistinguishedName = result.Path
                    });
                }
            }

            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windows AD group search failed");
            return Enumerable.Empty<ADGroupInfo>();
        }
    }

    private async Task<bool> TestConnectionWindowsADAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var searcher = new DirectorySearcher();
            searcher.Filter = "(objectClass=domain)";
            var result = searcher.FindOne();
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windows AD connection test failed");
            return false;
        }
    }

    // Azure AD implementations (placeholder - requires Microsoft Graph SDK)
    private async Task<bool> IsUserInGroupAzureADAsync(string userPrincipalName, string groupName, CancellationToken cancellationToken)
    {
        // Implementation for Azure AD using Microsoft Graph API
        // This would require proper Azure AD app registration and credentials
        _logger.LogWarning("Azure AD group membership check not yet implemented");
        return false;
    }

    private async Task<string> GetUserDisplayNameAzureADAsync(string userPrincipalName, CancellationToken cancellationToken)
    {
        // Implementation for Azure AD using Microsoft Graph API
        _logger.LogWarning("Azure AD user display name lookup not yet implemented");
        return userPrincipalName.Split('@')[0];
    }

    private async Task<bool> ValidateGroupExistsAzureADAsync(string groupName, CancellationToken cancellationToken)
    {
        // Implementation for Azure AD using Microsoft Graph API
        _logger.LogWarning("Azure AD group validation not yet implemented");
        return false;
    }

    private async Task<IEnumerable<ADGroupInfo>> SearchGroupsAzureADAsync(string searchTerm, CancellationToken cancellationToken)
    {
        // Implementation for Azure AD using Microsoft Graph API
        _logger.LogWarning("Azure AD group search not yet implemented");
        return Enumerable.Empty<ADGroupInfo>();
    }

    private async Task<bool> TestConnectionAzureADAsync(CancellationToken cancellationToken)
    {
        // Implementation for Azure AD using Microsoft Graph API
        _logger.LogWarning("Azure AD connection test not yet implemented");
        return false;
    }

    private async Task<string> GetAuthenticationTypeAsync()
    {
        try
        {
            var configs = await _unitOfWork.Configuration.GetSystemConfigsAsync();
            var authType = configs.FirstOrDefault(c => c.Key == "AD_AUTH_TYPE")?.Value ?? "WindowsAuthentication";
            return authType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get authentication type from configuration");
            return "WindowsAuthentication"; // Default fallback
        }
    }
}

public class ADGroupInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
}