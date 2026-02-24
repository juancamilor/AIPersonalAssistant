# Azure AD App Registration Script for Personal Assistant
# This script creates an Azure AD app registration for Microsoft Account authentication

Write-Host "=== Azure AD App Registration Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if logged in to Azure
Write-Host "Checking Azure login status..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in to Azure. Please login..." -ForegroundColor Red
    az login
    $account = az account show | ConvertFrom-Json
}

Write-Host "Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "Subscription: $($account.name)" -ForegroundColor Green
Write-Host ""

# App registration details
$appName = "my-personal-assistant-hub"
$redirectUris = @(
    "https://localhost:5001/signin-microsoft",
    "http://localhost:5000/signin-microsoft"
)

Write-Host "Creating app registration: $appName" -ForegroundColor Yellow

# Create the app registration (supports personal Microsoft accounts)
$app = az ad app create `
    --display-name $appName `
    --sign-in-audience "AzureADandPersonalMicrosoftAccount" `
    --web-redirect-uris $redirectUris[0] $redirectUris[1] `
    --enable-id-token-issuance true `
    | ConvertFrom-Json

if (-not $app) {
    Write-Host "Failed to create app registration" -ForegroundColor Red
    exit 1
}

$appId = $app.appId
Write-Host "✓ App registration created successfully" -ForegroundColor Green
Write-Host "  Application (client) ID: $appId" -ForegroundColor Cyan

# Create a service principal
Write-Host ""
Write-Host "Creating service principal..." -ForegroundColor Yellow
az ad sp create --id $appId | Out-Null
Write-Host "✓ Service principal created" -ForegroundColor Green

# Add required API permissions
Write-Host ""
Write-Host "Adding API permissions..." -ForegroundColor Yellow

# Microsoft Graph User.Read permission (delegated)
$userReadPermission = "e1fe6dd8-ba31-4d61-89e7-88639da4683d"
$microsoftGraphId = "00000003-0000-0000-c000-000000000000"

az ad app permission add `
    --id $appId `
    --api $microsoftGraphId `
    --api-permissions "$userReadPermission=Scope" | Out-Null

Write-Host "✓ Added Microsoft Graph User.Read permission" -ForegroundColor Green

# Create a client secret
Write-Host ""
Write-Host "Creating client secret..." -ForegroundColor Yellow
$secretResult = az ad app credential reset `
    --id $appId `
    --append `
    --display-name "AIPersonalAssistant-Secret" `
    | ConvertFrom-Json

$clientSecret = $secretResult.password
Write-Host "✓ Client secret created" -ForegroundColor Green

# Save credentials to user secrets
Write-Host ""
Write-Host "Saving credentials to user secrets..." -ForegroundColor Yellow
Push-Location "AIPersonalAssistant.Web"
dotnet user-secrets set "AzureAd:ClientId" $appId
dotnet user-secrets set "AzureAd:ClientSecret" $clientSecret
Pop-Location
Write-Host "✓ Credentials saved to user secrets" -ForegroundColor Green

# Display summary
Write-Host ""
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Application Details:" -ForegroundColor Cyan
Write-Host "  Application (client) ID: $appId" -ForegroundColor White
Write-Host "  Tenant ID: common" -ForegroundColor White
Write-Host "  Client Secret: $clientSecret" -ForegroundColor White
Write-Host ""
Write-Host "Redirect URIs configured:" -ForegroundColor Cyan
foreach ($uri in $redirectUris) {
    Write-Host "  $uri" -ForegroundColor White
}
Write-Host ""
Write-Host "User secrets have been configured automatically." -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Run 'cd AIPersonalAssistant.Web'" -ForegroundColor White
Write-Host "  2. Run 'dotnet run'" -ForegroundColor White
Write-Host "  3. Navigate to https://localhost:5001" -ForegroundColor White
Write-Host "  4. Click 'Sign in with Microsoft'" -ForegroundColor White
Write-Host "  5. Login with your Outlook.com account" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  IMPORTANT: Save these credentials securely!" -ForegroundColor Red
Write-Host "    Client Secret: $clientSecret" -ForegroundColor Red
Write-Host ""
