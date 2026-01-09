using MudBlazor.Services;
using QuickApiMapper.Designer.Web.Components;
using QuickApiMapper.Designer.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Enable detailed errors in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddServerSideBlazor(options =>
    {
        options.DetailedErrors = true;
    });
}

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add MudBlazor services
builder.Services.AddMudServices();

// Configure HttpClient for Management API
// Try to get URL from Aspire service discovery first, then fall back to config
var managementApiUrl = builder.Configuration["services:management-api:https:0"]
    ?? builder.Configuration["services:management-api:http:0"]
    ?? builder.Configuration["ManagementApi:BaseUrl"]
    ?? "https://localhost:7001";

Console.WriteLine($"[Designer Web] Management API URL: {managementApiUrl}");

builder.Services.AddHttpClient<IntegrationApiClient>(client =>
{
    client.BaseAddress = new Uri(managementApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Health check endpoint for Aspire
app.MapGet("/health", () => new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    managementApiUrl = managementApiUrl
});

app.Run();
