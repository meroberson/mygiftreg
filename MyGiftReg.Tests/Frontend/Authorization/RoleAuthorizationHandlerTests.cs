using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using MyGiftReg.Frontend.Authorization;

namespace MyGiftReg.Tests.Frontend.Authorization
{
    public class RoleAuthorizationHandlerTests
    {
        private readonly Mock<ILogger<RoleAuthorizationHandler>> _mockLogger;
        private readonly RoleAuthorizationHandler _handler;

        public RoleAuthorizationHandlerTests()
        {
            _mockLogger = new Mock<ILogger<RoleAuthorizationHandler>>();
            _handler = new RoleAuthorizationHandler(_mockLogger.Object);
        }

        [Fact]
        public async Task HandleRequirementAsync_WithAuthenticatedUserAndRequiredRole_Succeeds()
        {
            // Arrange
            var requirement = new RoleAuthorizationRequirement("MyGiftReg.Access");
            var context = CreateAuthorizationHandlerContext(requirement, new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim("roles", "MyGiftReg.Access")
            });

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_WithAuthenticatedUserAndMultipleRoles_Succeeds()
        {
            // Arrange
            var requirement = new RoleAuthorizationRequirement("MyGiftReg.Access");
            var context = CreateAuthorizationHandlerContext(requirement, new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim("roles", "OtherRole"),
                new Claim("roles", "MyGiftReg.Access")
            });

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_WithCaseInsensitiveRole_Succeeds()
        {
            // Arrange
            var requirement = new RoleAuthorizationRequirement("MyGiftReg.Access");
            var context = CreateAuthorizationHandlerContext(requirement, new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim("roles", "mygiftreg.access")
            });

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_WithClaimTypesRole_Succeeds()
        {
            // Arrange
            var requirement = new RoleAuthorizationRequirement("MyGiftReg.Access");
            var context = CreateAuthorizationHandlerContext(requirement, new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim(ClaimTypes.Role, "MyGiftReg.Access")
            });

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_WithAuthenticatedUserAndWrongRole_Fails()
        {
            // Arrange
            var requirement = new RoleAuthorizationRequirement("MyGiftReg.Access");
            var context = CreateAuthorizationHandlerContext(requirement, new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim("roles", "OtherRole")
            });

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_WithAnonymousUser_Fails()
        {
            // Arrange
            var requirement = new RoleAuthorizationRequirement("MyGiftReg.Access");
            var context = CreateAuthorizationHandlerContext(requirement, Array.Empty<Claim>());

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_WithEmptyRolesClaim_Fails()
        {
            // Arrange
            var requirement = new RoleAuthorizationRequirement("MyGiftReg.Access");
            var context = CreateAuthorizationHandlerContext(requirement, new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim("roles", "")
            });

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        private static AuthorizationHandlerContext CreateAuthorizationHandlerContext(
            IAuthorizationRequirement requirement, 
            Claim[] claims)
        {
            return new AuthorizationHandlerContext(
                new[] { requirement },
                CreateClaimsPrincipal(claims),
                "resource");
        }

        private static ClaimsPrincipal CreateClaimsPrincipal(Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            return principal;
        }
    }

    public class RoleAuthorizationRequirementTests
    {
        [Fact]
        public void Constructor_WithRequiredRole_SetsRequiredRole()
        {
            // Arrange
            var requiredRole = "MyGiftReg.Access";

            // Act
            var requirement = new RoleAuthorizationRequirement(requiredRole);

            // Assert
            Assert.Equal(requiredRole, requirement.RequiredRole);
        }

        [Fact]
        public void Constructor_WithNullRequiredRole_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RoleAuthorizationRequirement(null!));
        }

        [Fact]
        public void Constructor_WithEmptyRequiredRole_AcceptsEmptyString()
        {
            // Arrange
            var requiredRole = string.Empty;

            // Act
            var requirement = new RoleAuthorizationRequirement(requiredRole);

            // Assert
            Assert.Equal(requiredRole, requirement.RequiredRole);
        }
    }
}
