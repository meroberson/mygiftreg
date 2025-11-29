using Azure.Data.Tables;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MyGiftReg.Frontend.Authorization;
using MyGiftReg.Frontend.Models;

var builder = WebApplication.CreateBuilder(args);

// Add authentication services
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDistributedTokenCaches();

// Add authorization services
builder.Services.AddAuthorization(options =>
{
    // Add policy for role-based access
    options.AddPolicy("RequireMyGiftRegRole", policy =>
        policy.Requirements.Add(new RoleAuthorizationRequirement("MyGiftReg.Access")));
    
    // Add policy for admin role - required for event deletion
    options.AddPolicy("RequireAdminRole", policy =>
        policy.Requirements.Add(new RoleAuthorizationRequirement("MyGiftReg.Admin")));
});

// Add the authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, RoleAuthorizationHandler>();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Add session support for development user switching (keeping for fallback)
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
    var config = sp.GetRequiredService<MyGiftReg.Backend.Storage.AzureTableConfig>();
    // Use the config to create a TableServiceClient that supports managed identity
    return config.CreateTableServiceClient();
});

// Add backend services and dependency injection
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IEventRepository, MyGiftReg.Backend.Storage.EventRepository>();
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IGiftListRepository, MyGiftReg.Backend.Storage.GiftListRepository>();
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IGiftItemRepository, MyGiftReg.Backend.Storage.GiftItemRepository>();
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IEventService, MyGiftReg.Backend.Services.EventService>();
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IGiftListService, MyGiftReg.Backend.Services.GiftListService>();
builder.Services.AddScoped<MyGiftReg.Backend.Interfaces.IGiftItemService, MyGiftReg.Backend.Services.GiftItemService>();

// Add Azure user service (replacing development user service)
builder.Services.AddScoped<MyGiftReg.Frontend.Services.IAzureUserService, MyGiftReg.Frontend.Services.AzureUserService>();

var app = builder.Build();

// Initialize Azure Table Storage tables
await InitializeAzureTables(app.Services);

static async Task InitializeAzureTables(IServiceProvider serviceProvider)
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    var config = serviceProvider.GetRequiredService<MyGiftReg.Backend.Storage.AzureTableConfig>();
    
    var requiredTables = new[] { "Events", "GiftLists", "GiftItems" };
    
    foreach (var tableName in requiredTables)
    {
        try
        {
            var tableClient = config.CreateTableClient(tableName);
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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Default route first
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Handle Microsoft Identity authentication routes specifically
app.MapControllerRoute(
    name: "MicrosoftIdentity_SignIn",
    pattern: "MicrosoftIdentity/Account/SignIn",
    defaults: new { area = "MicrosoftIdentity", controller = "Account", action = "SignIn" });

app.MapControllerRoute(
    name: "MicrosoftIdentity_SignOut",
    pattern: "MicrosoftIdentity/Account/SignOut",
    defaults: new { area = "MicrosoftIdentity", controller = "Account", action = "SignOut" });

app.MapControllerRoute(
    name: "MicrosoftIdentity_SignedOut",
    pattern: "MicrosoftIdentity/Account/SignedOut",
    defaults: new { area = "MicrosoftIdentity", controller = "Account", action = "SignedOut" });

// Access denied route
app.MapControllerRoute(
    name: "access_denied",
    pattern: "MicrosoftIdentity/Account/AccessDenied",
    defaults: new { controller = "Home", action = "AccessDenied" });

app.Run();
