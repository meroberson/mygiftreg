using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Client;
using System.Security.Claims;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MyGiftReg.Frontend.Services;

namespace MyGiftReg.Tests.Frontend.Services
{
    public class AzureUserServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ILogger<AzureUserService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AzureUserService _service;

        public AzureUserServiceTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockLogger = new Mock<ILogger<AzureUserService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup default configuration for tests using indexer syntax
            _mockConfiguration.Setup(x => x["AzureAd:RequiredRole"]).Returns("MyGiftReg.Access");
            
            _service = new AzureUserService(_mockHttpContextAccessor.Object, _mockLogger.Object, _mockConfiguration.Object);
        }

        [Fact]
        public void IsAuthenticated_WithAuthenticatedUser_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.IsAuthenticated();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAuthenticated_WithAnonymousUser_ReturnsFalse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(); // Empty principal (not authenticated)
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.IsAuthenticated();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAuthenticated_WithNullHttpContext_ReturnsFalse()
        {
            // Arrange
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

            // Act
            var result = _service.IsAuthenticated();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetCurrentUserId_WithAuthenticatedUser_ReturnsUserId()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUserId();

            // Assert
            Assert.Equal("test-user-id", result);
        }

        [Fact]
        public void GetCurrentUserId_WithNameIdentifierClaim_ReturnsUserId()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUserId();

            // Assert
            Assert.Equal("test-user-id", result);
        }

        [Fact]
        public void GetCurrentUserId_WithAnonymousUser_ReturnsEmptyString()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal();
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUserId();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetCurrentUserId_WithNullUserIdClaim_ReturnsEmptyString()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUserId();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetCurrentUserId_WithNullHttpContext_ReturnsEmptyString()
        {
            // Arrange
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

            // Act
            var result = _service.GetCurrentUserId();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetCurrentUser_WithAuthenticatedUser_ReturnsUser()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-user-id", result.Id);
            Assert.Equal("Test User", result.DisplayName);
            Assert.Equal("test@example.com", result.Email);
            Assert.True(result.IsActive);
        }

        [Fact]
        public void GetCurrentUser_WithNameIdentifierClaim_ReturnsUser()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-user-id", result.Id);
            Assert.Equal("Test User", result.DisplayName);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public void GetCurrentUser_WithEmailClaims_ReturnsUser()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("email", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-user-id", result.Id);
            Assert.Equal("Test User", result.DisplayName);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public void GetCurrentUser_WithMissingName_ReturnsClaimTypesName()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim(ClaimTypes.Name, "Test User Name"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-user-id", result.Id);
            Assert.Equal("Test User Name", result.DisplayName);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public void GetCurrentUser_WithMissingNameAndClaimTypesName_ReturnsDefaultUser()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-user-id", result.Id);
            Assert.Equal("Unknown User", result.DisplayName);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public void GetCurrentUser_WithMissingEmail_ReturnsConstructedEmail()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-user-id", result.Id);
            Assert.Equal("Test User", result.DisplayName);
            Assert.Equal("Test User@unknown.com", result.Email);
        }

        [Fact]
        public void GetCurrentUser_WithAnonymousUser_ReturnsNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal();
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_WithMissingUserIdClaim_ReturnsNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_WithNullHttpContext_ReturnsNull()
        {
            // Arrange
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_WithException_ReturnsNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            // Make the Claims.FirstOrDefault throw an exception
            httpContext.User = null!;
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void HasRequiredRole_WithRequiredRole_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim("roles", "MyGiftReg.Access")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.HasRequiredRole();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasRequiredRole_WithClaimTypesRole_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim(ClaimTypes.Role, "MyGiftReg.Access")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.HasRequiredRole();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasRequiredRole_WithMultipleRoles_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim("roles", "OtherRole"),
                new Claim("roles", "MyGiftReg.Access")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.HasRequiredRole();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasRequiredRole_WithCaseInsensitiveRole_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim("roles", "mygiftreg.access")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.HasRequiredRole();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasRequiredRole_WithMissingRole_ReturnsFalse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim("roles", "OtherRole")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.HasRequiredRole();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasRequiredRole_WithAnonymousUser_ReturnsFalse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal();
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.HasRequiredRole();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasRequiredRole_WithNullHttpContext_ReturnsFalse()
        {
            // Arrange
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

            // Act
            var result = _service.HasRequiredRole();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetCurrentUser_WithEmptyUserIdClaim_ReturnsNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", ""),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.GetCurrentUser();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void HasRequiredRole_WithEmptyRoleClaim_ReturnsFalse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("oid", "test-user-id"),
                new Claim("name", "Test User"),
                new Claim("preferred_username", "test@example.com"),
                new Claim("roles", "")
            }, "TestAuth"));
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _service.HasRequiredRole();

            // Assert
            Assert.False(result);
        }
    }
}
