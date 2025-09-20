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
}

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActiveDirectoryService> _logger;

    public ActiveDirectoryService(IUnitOfWork unitOfWork, ILogger<ActiveDirectoryService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogError(ex, "Failed to check group membership for user {UserPrincipalName} in group {GroupName}", 
                userPrincipalName, groupName);
            return false;
        }
    }

    public async Task<string> GetUserDisplayNameAsync(string userPrincipalName, CancellationToken cancellationToken = default)
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
            _logger.LogError(ex, "Failed to get display name for user {UserPrincipalName}", userPrincipalName);
            return userPrincipalName.Split('@')[0]; // Fallback
        }
    }
}