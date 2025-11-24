namespace MyGiftReg.Frontend.Models
{
    public class AzureAdConfig
    {
        public string Instance { get; set; } = "https://login.microsoftonline.com/";
        public string Domain { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = "/signin-oidc";
        public string Scope { get; set; } = "openid profile email offline_access";
        public string RequiredRole { get; set; } = "MyGiftReg.Access";
    }
}
