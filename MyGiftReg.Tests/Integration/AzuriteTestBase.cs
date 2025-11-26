using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyGiftReg.Backend.Storage;
using System.Diagnostics;
using System.Net;

namespace MyGiftReg.Tests.Integration
{
    public abstract class AzuriteTestBase : IAsyncLifetime
    {
        protected IServiceProvider ServiceProvider { get; private set; } = null!;
        protected AzureTableConfig TableConfig { get; private set; } = null!;
        protected TableServiceClient TableServiceClient { get; private set; } = null!;
        protected ILogger Logger { get; private set; } = null!;
        protected string _testPrefix = "";

        private IHost? _host;
        private readonly List<TableClient> _tableClients = new();

        public virtual async Task InitializeAsync()
        {
            _testPrefix = Guid.NewGuid().ToString("N")[..8];

            // Ensure Azurite is running - simple approach
            await EnsureAzuriteRunningAsync();
            
            // Set up services
            _host = CreateHostBuilder().Build();
            ServiceProvider = _host.Services;
            
            Logger = ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(GetType().Name);

            // Get table configuration from options
            var tableConfigOptions = ServiceProvider.GetRequiredService<IOptions<AzureTableConfig>>().Value;
            TableConfig = tableConfigOptions;
            
            // Verify connection string is set correctly
            if (string.IsNullOrEmpty(TableConfig.ConnectionString))
            {
                throw new InvalidOperationException("TableConfig.ConnectionString is not properly configured");
            }
            
            // Create table service client directly
            TableServiceClient = new TableServiceClient(TableConfig.ConnectionString);
            
            // Create tables
            await CreateTablesAsync();
            
            Logger.LogInformation("Azurite test environment initialized successfully");
        }

        public virtual async Task DisposeAsync()
        {
            //Logger?.LogInformation("Cleaning up test tables for {TestClass}", GetType().Name);
            
            //await CleanupTablesAsync();

            // Can't clean up the tables because they could be used by other tests still
            await Task.CompletedTask;
            
            // Dispose host
            _host?.Dispose();

            Logger?.LogInformation("Test cleanup completed");
        }

        protected TableClient GetTableClient(string tableName)
        {
            var client = TableServiceClient.GetTableClient(tableName);
            _tableClients.Add(client);
            return client;
        }

        private IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Configure Azure Table Storage with local emulator connection
                    services.Configure<AzureTableConfig>(options =>
                    {
                        options.ConnectionString = "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1";
                        options.EventTableName = "Events";
                        options.GiftListTableName = "GiftLists";
                        options.GiftItemTableName = "GiftItems";
                    });
                    
                    // Register AzureTableConfig as a singleton for tests that need it directly
                    services.AddSingleton(serviceProvider =>
                    {
                        var config = serviceProvider.GetRequiredService<IOptions<AzureTableConfig>>().Value;
                        return config;
                    });
                    
                    // Add logging for tests
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Debug);
                    });
                });
        }

        private async Task EnsureAzuriteRunningAsync()
        {
            // Check if Azurite is already running
            if (await IsAzuriteRunningAsync())
            {
                Logger?.LogInformation("Azurite is already running");
                await Task.CompletedTask;
                return;
            }

            Logger?.LogInformation("Azurite is not running, attempting to start it");

            // Try to start Azurite, but ignore failures
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "azurite",
                        Arguments = "--location .\\AzuriteData --blobPort 10000 --queuePort 10001 --tablePort 10002 --skipApiVersionCheck",
                        UseShellExecute = true
                    }
                };

                process.Start();
                Logger?.LogInformation("Started Azurite process");
            }
            catch (Exception ex)
            {
                // Ignore startup failures
                Logger?.LogWarning(ex, "Failed to start Azurite process, ignoring failure");
            }

            // Wait a bit for Azurite to potentially start
            await Task.Delay(2000);

            // Check again if it's running now
            if (await IsAzuriteRunningAsync())
            {
                Logger?.LogInformation("Azurite is now running");
                await Task.CompletedTask;
                return;
            }

            // If still not running, fail
            throw new InvalidOperationException("Azurite failed to start and is not running");
        }

        private async Task<bool> IsAzuriteRunningAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("http://127.0.0.1:10002", cts.Token);
                
                // Any response (even 400) indicates Azurite is running and responding
                return response.StatusCode != HttpStatusCode.RequestTimeout;
            }
            catch
            {
                // Any exception means Azurite is not running
                return false;
            }
        }

        private async Task CreateTablesAsync()
        {
            var tables = new[] { 
                TableConfig.EventTableName,
                TableConfig.GiftListTableName, 
                TableConfig.GiftItemTableName 
            };

            foreach (var tableName in tables)
            {
                try
                {
                    var response = await TableServiceClient.CreateTableIfNotExistsAsync(tableName);
                    Logger.LogDebug("Created table: {TableName}", tableName);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to create table: {TableName}", tableName);
                    throw;
                }
            }
        }

        private async Task CleanupTablesAsync()
        {
            var tables = new[] { 
                TableConfig.EventTableName,
                TableConfig.GiftListTableName, 
                TableConfig.GiftItemTableName 
            };

            foreach (var tableName in tables)
            {
                try
                {
                    var tableClient = TableServiceClient.GetTableClient(tableName);
                    await foreach (var entity in tableClient.QueryAsync<TableEntity>())
                    {
                        await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                    }
                    Logger.LogDebug("Cleaned up table: {TableName}", tableName);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to cleanup table: {TableName}", tableName);
                }
            }
        }

        protected TEntity CreateTestEntity<TEntity>(string partitionKey, string rowKey) where TEntity : class, ITableEntity, new()
        {
            return new TEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
