using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace MyGiftReg.Backend.Storage
{
    public class AzureTableConfig
    {
        private readonly IConfiguration? _configuration;
        private readonly bool _useManagedIdentity;
        private readonly string? _managedIdentityClientId;
        private TableServiceClient? _cachedTableServiceClient;

        public string ConnectionString { get; set; } = string.Empty;
        public string EventTableName { get; set; } = "EventTable";
        public string GiftListTableName { get; set; } = "GiftListTable";
        public string GiftItemTableName { get; set; } = "GiftItemTable";

        // Parameterless constructor for testing
        public AzureTableConfig()
        {
            // Default values for testing - use connection string authentication
            _useManagedIdentity = false;
            _managedIdentityClientId = null;
            ConnectionString = "UseDevelopmentStorage=true";
        }

        // Constructor with IConfiguration for production
        public AzureTableConfig(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Check if managed identity is enabled
            var managedIdentityConfig = _configuration.GetSection("ManagedIdentity");
            _useManagedIdentity = bool.Parse(managedIdentityConfig["Enabled"] ?? "false");
            _managedIdentityClientId = managedIdentityConfig["ClientId"];

            // Get connection string (may be empty for managed identity)
            ConnectionString = _configuration.GetConnectionString("AzureTableStorage") ?? string.Empty;
        }

        public TableClient CreateTableClient(string tableName)
        {
            var serviceClient = CreateTableServiceClient();
            return serviceClient.GetTableClient(tableName);
        }

        public TableServiceClient CreateTableServiceClient()
        {
            if (_cachedTableServiceClient != null)
            {
                return _cachedTableServiceClient;
            }

            if (_useManagedIdentity && _configuration != null)
            {
                _cachedTableServiceClient = CreateTableServiceClientWithManagedIdentity();
            }
            else
            {
                _cachedTableServiceClient = CreateTableServiceClientWithConnectionString();
            }

            return _cachedTableServiceClient;
        }

        private TableServiceClient CreateTableServiceClientWithConnectionString()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new InvalidOperationException("Azure Table Storage connection string is required when managed identity is not enabled.");
            }

            return new TableServiceClient(ConnectionString);
        }

        private TableServiceClient CreateTableServiceClientWithManagedIdentity()
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("IConfiguration is required when managed identity is enabled.");
            }

            var storageAccountEndpoint = _configuration["StorageAccountEndpoint"];
            if (string.IsNullOrEmpty(storageAccountEndpoint))
            {
                throw new InvalidOperationException("StorageAccountEndpoint configuration is required when managed identity is enabled.");
            }

            // Create the table service URL
            var tableServiceUri = new Uri($"https://{storageAccountEndpoint}/");
            
            // Create DefaultAzureCredential which will use managed identity when running in Azure
            DefaultAzureCredential credential;
            
            if (!string.IsNullOrEmpty(_managedIdentityClientId))
            {
                // Use specific managed identity client ID
                credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = _managedIdentityClientId
                });
            }
            else
            {
                // Use default managed identity
                credential = new DefaultAzureCredential();
            }

            return new TableServiceClient(tableServiceUri, credential);
        }
    }
}
