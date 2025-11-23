namespace MyGiftReg.Frontend.Models
{
    public class DevelopmentUser
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public DevelopmentUser(string id, string displayName, string email, bool isActive = false)
        {
            Id = id;
            DisplayName = displayName;
            Email = email;
            IsActive = isActive;
        }
    }
}
