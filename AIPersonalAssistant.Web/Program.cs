using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authorization;
using AIPersonalAssistant.Web.Authorization;

var builder = WebApplication.CreateBuilder(args);

var bypassAuth = builder.Environment.IsDevelopment() && 
    builder.Configuration.GetValue<bool>("BYPASS_AUTH", false);

if (!bypassAuth)
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddAuthentication()
        .AddGoogle("Google", options =>
        {
            options.ClientId = builder.Configuration["Google:ClientId"] ?? "";
            options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
            options.CallbackPath = "/signin-google";
            options.SignInScheme = "Cookies";
        });

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
}
else
{
    builder.Services.AddAuthentication("BypassScheme")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, AIPersonalAssistant.Web.Authorization.BypassAuthHandler>("BypassScheme", null);
    builder.Services.AddAuthorization();
}

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IExchangeRateService, AIPersonalAssistant.Web.Services.ExchangeRateService>();
builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IStockService, AIPersonalAssistant.Web.Services.StockService>();
builder.Services.AddScoped<AIPersonalAssistant.Web.Services.ITaxesService, AIPersonalAssistant.Web.Services.TaxesService>();

// Use Blob storage in production, JSON file in development
if (builder.Environment.IsProduction() && !string.IsNullOrEmpty(builder.Configuration["AzureStorage:ConnectionString"]))
{
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.ITravelService, AIPersonalAssistant.Web.Services.BlobTravelService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IExchangeRateHistoryService, AIPersonalAssistant.Web.Services.BlobExchangeRateHistoryService>();
    builder.Services.AddSingleton<AIPersonalAssistant.Web.Services.IUserManagementService, AIPersonalAssistant.Web.Services.BlobUserManagementService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.ITravelImageService, AIPersonalAssistant.Web.Services.BlobTravelImageService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IWishesService, AIPersonalAssistant.Web.Services.BlobWishesService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IRecipeService, AIPersonalAssistant.Web.Services.BlobRecipeService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IRecipeImageService, AIPersonalAssistant.Web.Services.BlobRecipeImageService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IMenopauseService, AIPersonalAssistant.Web.Services.BlobMenopauseService>();
}
else
{
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.ITravelService, AIPersonalAssistant.Web.Services.TravelService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IExchangeRateHistoryService, AIPersonalAssistant.Web.Services.LocalExchangeRateHistoryService>();
    builder.Services.AddSingleton<AIPersonalAssistant.Web.Services.IUserManagementService, AIPersonalAssistant.Web.Services.LocalUserManagementService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.ITravelImageService, AIPersonalAssistant.Web.Services.LocalTravelImageService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IWishesService, AIPersonalAssistant.Web.Services.LocalWishesService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IRecipeService, AIPersonalAssistant.Web.Services.LocalRecipeService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IRecipeImageService, AIPersonalAssistant.Web.Services.LocalRecipeImageService>();
    builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IMenopauseService, AIPersonalAssistant.Web.Services.LocalMenopauseService>();
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

app.UseMiddleware<AIPersonalAssistant.Web.Authorization.ToolPermissionMiddleware>();

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

public partial class Program { }
