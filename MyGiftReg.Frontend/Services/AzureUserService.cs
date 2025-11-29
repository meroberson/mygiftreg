using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Client;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace MyGiftReg.Frontend.Services
{
    public interface IAzureUserService
    {
        AzureUser? GetCurrentUser();
        string GetCurrentUserId();
        string GetCurrentUserDisplayName();
        bool IsAuthenticated();
        bool HasRequiredRole();
    }

    public class AzureUser
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public AzureUser(string id, string displayName, string email, bool isActive = true)
        {
            Id = id;
            DisplayName = displayName;
            Email = email;
            IsActive = isActive;
        }
    }

    public class AzureUserService : IAzureUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AzureUserService> _logger;
        private readonly IConfiguration _configuration;

        public AzureUserService(IHttpContextAccessor httpContextAccessor, ILogger<AzureUserService> logger, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _configuration = configuration;
        }

        public AzureUser? GetCurrentUser()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("User is not authenticated");
                return null;
            }

            try
            {
                var user = httpContext.User;
                
                // Extract user information from Azure AD claims
                var userId = GetClaimValue(user, "oid") ?? GetClaimValue(user, ClaimTypes.NameIdentifier);
                var displayName = GetClaimValue(user, "name") ?? GetClaimValue(user, ClaimTypes.Name) ?? "Unknown User";
                var email = GetClaimValue(user, "preferred_username") ?? 
                           GetClaimValue(user, ClaimTypes.Email) ?? 
                           GetClaimValue(user, "email") ?? 
                           $"{displayName}@unknown.com";

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Could not extract user ID from claims");
                    return null;
                }

                return new AzureUser(userId, displayName, email, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user information from claims");
                return null;
            }
        }

        public string GetCurrentUserId()
        {
            var currentUser = GetCurrentUser();
            return currentUser?.Id ?? string.Empty;
        }

        public string GetCurrentUserDisplayName()
        {
            var currentUser = GetCurrentUser();
            return currentUser?.DisplayName ?? string.Empty;
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        }

        public bool HasRequiredRole()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            var requiredRole = _configuration["AzureAd:RequiredRole"] ?? "MyGiftReg.Access";
            var userRoles = httpContext.User.Claims
                .Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var hasRequiredRole = userRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase);
            
            _logger.LogInformation("User roles: {UserRoles}, Required role: {RequiredRole}, Has access: {HasAccess}", 
                string.Join(", ", userRoles), requiredRole, hasRequiredRole);

            return hasRequiredRole;
        }

        private string? GetClaimValue(ClaimsPrincipal user, string claimType)
        {
            return user.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }
    }
}
