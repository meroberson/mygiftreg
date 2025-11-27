using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MyGiftReg.Frontend.Authorization
{
    public class RoleAuthorizationRequirement : IAuthorizationRequirement
    {
        public RoleAuthorizationRequirement(string requiredRole)
        {
            RequiredRole = requiredRole ?? throw new ArgumentNullException(nameof(requiredRole));
        }

        public string RequiredRole { get; }
    }

    public class RoleAuthorizationHandler : AuthorizationHandler<RoleAuthorizationRequirement>
    {
        private readonly ILogger<RoleAuthorizationHandler> _logger;

        public RoleAuthorizationHandler(ILogger<RoleAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            RoleAuthorizationRequirement requirement)
        {
            try
            {
                if (context.User?.Identity?.IsAuthenticated != true)
                {
                    _logger.LogWarning("User is not authenticated for role requirement");
                    return Task.CompletedTask;
                }

                // Directly extract roles from user claims instead of using the service
                var userRoles = context.User.Claims
                    .Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                var hasRequiredRole = userRoles.Contains(requirement.RequiredRole, StringComparer.OrdinalIgnoreCase);

                _logger.LogInformation("User roles: {UserRoles}, Required role: {RequiredRole}, Has access: {HasAccess}", 
                    string.Join(", ", userRoles), requirement.RequiredRole, hasRequiredRole);

                if (hasRequiredRole)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning("User does not have required role: {RequiredRole}", requirement.RequiredRole);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role authorization");
            }

            return Task.CompletedTask;
        }
    }
}
