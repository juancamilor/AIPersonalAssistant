using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authorization;
using AIPersonalAssistant.Web.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

var allowedEmails = builder.Configuration.GetSection("Authorization:AllowedEmails").Get<List<string>>() ?? new List<string>();

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
});

builder.Services.AddSingleton<IAuthorizationHandler, EmailAllowListHandler>();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IExchangeRateService, AIPersonalAssistant.Web.Services.ExchangeRateService>();
builder.Services.AddScoped<AIPersonalAssistant.Web.Services.IStockService, AIPersonalAssistant.Web.Services.StockService>();

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

