using Azure.Data.Tables;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add session support for development user switching
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

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

// Add development user service
builder.Services.AddScoped<MyGiftReg.Frontend.Services.IDevelopmentUserService, MyGiftReg.Frontend.Services.DevelopmentUserService>();

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

app.UseSession();
app.UseAuthorization();

// Use attribute routing (configured in controllers) - no additional routes needed
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
