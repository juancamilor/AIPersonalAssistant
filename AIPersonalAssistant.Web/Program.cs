using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authorization;
using AIPersonalAssistant.Web.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

var allowedEmails = builder.Configuration.GetSection("Authorization:AllowedEmails").Get<List<string>>() ?? new List<string>();
var adminEmails = builder.Configuration.GetSection("Authorization:AdminEmails").Get<List<string>>() ?? new List<string>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EmailAllowList", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new EmailAllowListRequirement(allowedEmails));
    });
    
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new EmailAllowListRequirement(allowedEmails))
        .Build();

    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new AdminRequirement(adminEmails));
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, EmailAllowListHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, AdminHandler>();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IExchangeRateService, AIPersonalAssistant.Web.Services.ExchangeRateService>();
builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IStockService, AIPersonalAssistant.Web.Services.StockService>();

// Use Blob storage in production, JSON file in development
if (builder.Environment.IsProduction() && !string.IsNullOrEmpty(builder.Configuration["AzureStorage:ConnectionString"]))
{
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.ITravelService, AIPersonalAssistant.Web.Services.BlobTravelService>();
}
else
{
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.ITravelService, AIPersonalAssistant.Web.Services.TravelService>();
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePages(async context =>
{
    var statusCode = context.HttpContext.Response.StatusCode;
    if (statusCode == 403)
    {
        context.HttpContext.Response.Redirect("/access-denied.html");
    }
    else if (statusCode == 401)
    {
        context.HttpContext.Response.Redirect("/login.html?error=unauthorized&message=Please%20sign%20in%20to%20continue");
    }
});

app.MapControllers();

app.Run();

