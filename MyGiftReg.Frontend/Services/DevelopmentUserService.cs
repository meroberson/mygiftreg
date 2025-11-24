using Microsoft.AspNetCore.Http;
using MyGiftReg.Frontend.Models;

namespace MyGiftReg.Frontend.Services
{
    public interface IDevelopmentUserService
    {
        IEnumerable<DevelopmentUser> GetAllUsers();
        DevelopmentUser? GetCurrentUser();
        void SetCurrentUser(string userId);
        string GetCurrentUserId();
    }

    public class DevelopmentUserService : IDevelopmentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CurrentUserSessionKey = "DevelopmentCurrentUserId";
        
        // Predefined development users
        private readonly List<DevelopmentUser> _users;

        public DevelopmentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _users = new List<DevelopmentUser>
            {
                new DevelopmentUser("development-user-1", "Alice Johnson", "alice.johnson@example.com", true),
                new DevelopmentUser("development-user-2", "Bob Smith", "bob.smith@example.com", false)
            };
        }

        public IEnumerable<DevelopmentUser> GetAllUsers()
        {
            return _users;
        }

        public DevelopmentUser? GetCurrentUser()
        {
            var currentUserId = _httpContextAccessor.HttpContext?.Session.GetString(CurrentUserSessionKey);
            
            if (string.IsNullOrEmpty(currentUserId))
            {
                // Set default user (first active user or first user)
                var defaultUser = _users.FirstOrDefault(u => u.IsActive) ?? _users.First();
                SetCurrentUser(defaultUser.Id);
                return defaultUser;
            }

            return _users.FirstOrDefault(u => u.Id == currentUserId);
        }

        public void SetCurrentUser(string userId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                _httpContextAccessor.HttpContext?.Session.SetString(CurrentUserSessionKey, userId);
            }
        }

        public string GetCurrentUserId()
        {
            var currentUser = GetCurrentUser();
            return currentUser?.Id ?? _users.First().Id;
        }
    }
}
