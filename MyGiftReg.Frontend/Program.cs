using Azure.Data.Tables;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Azure Table Storage configuration and clients
builder.Services.AddSingleton<MyGiftReg.Backend.Storage.AzureTableConfig>();
builder.Services.AddSingleton(sp => 
{
    var connectionString = builder.Configuration.GetConnectionString("AzureTableStorage") 
                          ?? "UseDevelopmentStorage=true";
    
    return new TableServiceClient(connectionString);
});

// Add backend services and dependency injection
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IEventRepository, MyGiftReg.Backend.Storage.EventRepository>();
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IGiftListRepository, MyGiftReg.Backend.Storage.GiftListRepository>();
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IGiftItemRepository, MyGiftReg.Backend.Storage.GiftItemRepository>();
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IEventService, MyGiftReg.Backend.Services.EventService>();
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IGiftListService, MyGiftReg.Backend.Services.GiftListService>();
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IGiftItemService, MyGiftReg.Backend.Services.GiftItemService>();

var app = builder.Build();

// Initialize Azure Table Storage tables
await InitializeAzureTables(app.Services);

static async Task InitializeAzureTables(IServiceProvider serviceProvider)
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    var tableServiceClient = serviceProvider.GetRequiredService<TableServiceClient>();
    
    var requiredTables = new[] { "Events", "GiftLists", "GiftItems" };
    
    foreach (var tableName in requiredTables)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(tableName);
            await tableClient.CreateIfNotExistsAsync();
            logger.LogInformation("Table '{TableName}' is ready", tableName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize table '{TableName}'", tableName);
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Use attribute routing (configured in controllers) - no additional routes needed
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
