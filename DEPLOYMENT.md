# Deployment Guide

This guide explains how to deploy the AI Personal Assistant web application to Azure using GitHub Actions.

## Prerequisites

- Azure subscription with an existing resource group: `camilo-personal-assistant-rg`
- GitHub repository with Actions enabled
- Azure CLI installed (for manual setup)
- Permissions to create Azure resources and GitHub secrets

## Architecture

The deployment uses:
- **Azure App Service Plan (B1)**: Linux-based hosting plan
- **Azure Web App**: Hosts the .NET 10 web application
- **GitHub Actions**: CI/CD pipeline with OIDC authentication
- **Azure Bicep**: Infrastructure as Code

## Setup Instructions

### 1. Create Azure Service Principal with Federated Identity

This enables secure OIDC authentication without storing credentials.

```bash
# Set variables
SUBSCRIPTION_ID="<your-subscription-id>"
RESOURCE_GROUP="camilo-personal-assistant-rg"
APP_NAME="camilo-personal-assistant-github"

# Login to Azure
az login

# Set the subscription
az account set --subscription $SUBSCRIPTION_ID

# Create a service principal
az ad sp create-for-rbac --name $APP_NAME --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth

# Note down the following values from the output:
# - clientId
# - subscriptionId
# - tenantId
```

### 2. Configure Federated Identity Credentials

```bash
# Get the Application (client) ID
APP_ID=$(az ad sp list --display-name $APP_NAME --query "[0].appId" -o tsv)

# Create federated credential for main branch
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-deploy-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:juancamilor/AIPersonalAssistant:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for pull requests (optional)
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-deploy-pr",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:juancamilor/AIPersonalAssistant:pull_request",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### 3. Configure GitHub Secrets

Add the following secrets to your GitHub repository (Settings → Secrets and variables → Actions):

#### Deployment Secrets (CI/CD)
| Secret Name | Description | Example |
|------------|-------------|---------|
| `AZURE_CLIENT_ID` | Service Principal Client ID | `12345678-1234-1234-1234-123456789012` |
| `AZURE_TENANT_ID` | Azure AD Tenant ID | `87654321-4321-4321-4321-210987654321` |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID | `abcdef12-ab12-ab12-ab12-abcdef123456` |

#### Application Runtime Secrets
| Secret Name | Description |
|------------|-------------|
| `AZUREAD_CLIENT_ID` | Azure AD App Client ID for user authentication |
| `AZUREAD_CLIENT_SECRET` | Azure AD App Client Secret |
| `AZUREAD_TENANT_ID` | Tenant ID (use "common" for personal Microsoft accounts) |
| `EXCHANGERATE_API_KEY` | ExchangeRate-API.com API key |
| `OPENEXCHANGERATES_API_KEY` | Open Exchange Rates API key |
| `CURRENCYAPI_KEY` | CurrencyAPI.com API key |

See [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) for detailed instructions and actual values.

To add secrets:
1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each secret with its corresponding value

### 4. Configure GitHub Environment (Optional but Recommended)

For production protection:
1. Go to **Settings** ? **Environments**
2. Create environment named `production`
3. Add protection rules:
   - Required reviewers
   - Wait timer
   - Deployment branches (only `main`)

## Deployment Workflows

### Automatic Deployment (CI Pipeline)

The CI pipeline (`.github/workflows/ci.yml`) automatically deploys when:
- Code is pushed to the `main` branch
- All tests pass successfully

**Pipeline stages:**
1. **Build & Test**: Compile code and run unit tests
2. **Deploy Infrastructure**: Deploy/update Azure resources using Bicep
3. **Deploy Application**: Deploy the .NET application to Azure

### Manual Deployment

You can trigger deployment manually using the standalone workflow:

1. Go to **Actions** ? **Deploy to Azure**
2. Click **Run workflow**
3. Select the environment (production/staging)
4. Click **Run workflow**

### Local Manual Deployment

Deploy infrastructure manually:

```bash
# Login to Azure
az login

# Deploy Bicep template
az deployment group create \
  --resource-group camilo-personal-assistant-rg \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters.json

# Get the web app name
WEB_APP_NAME=$(az deployment group show \
  --resource-group camilo-personal-assistant-rg \
  --name main \
  --query properties.outputs.webAppName.value -o tsv)

# Build and publish the application
dotnet publish AIPersonalAssistant.Web/AIPersonalAssistant.Web.csproj \
  --configuration Release \
  --output ./publish

# Deploy to Azure Web App
az webapp deploy \
  --resource-group camilo-personal-assistant-rg \
  --name $WEB_APP_NAME \
  --src-path publish.zip \
  --type zip
```

## Infrastructure Details

### Resources Created

| Resource | Name Pattern | SKU | Purpose |
|----------|-------------|-----|---------|
| App Service Plan | `{appName}-plan` | B1 (Basic) | Linux hosting plan |
| Web App | `{appName}-production` | - | Hosts .NET 10 application |

### Configuration

**App Service Plan:**
- OS: Linux
- SKU: B1 (1 core, 1.75 GB RAM)
- Auto-scaling: Disabled (can be enabled)

**Web App:**
- Runtime: .NET 10.0
- HTTPS Only: Enabled
- TLS Version: 1.2+
- FTPS: Disabled
- Always On: Enabled
- HTTP/2: Enabled

### Environment Variables

**All environment variables are automatically configured by GitHub Actions during deployment.**

The CI/CD pipeline configures these settings in Azure App Service:

**Authentication (Azure AD):**
- `AzureAd__ClientId`: Azure AD App Client ID (from GitHub secret: AZUREAD_CLIENT_ID)
- `AzureAd__ClientSecret`: Azure AD App Client Secret (from GitHub secret: AZUREAD_CLIENT_SECRET)
- `AzureAd__TenantId`: "common" for personal Microsoft accounts (from GitHub secret: AZUREAD_TENANT_ID)
- `AzureAd__Instance`: https://login.microsoftonline.com/ (hardcoded in appsettings.json)

**Exchange Rate APIs:**
- `ExchangeRateAPIs__ExchangeRateApi__ApiKey`: ExchangeRate-API.com key (from GitHub secret: EXCHANGERATE_API_KEY)
- `ExchangeRateAPIs__OpenExchangeRates__ApiKey`: Open Exchange Rates App ID (from GitHub secret: OPENEXCHANGERATES_API_KEY)
- `ExchangeRateAPIs__CurrencyApi__ApiKey`: CurrencyAPI.com key (from GitHub secret: CURRENCYAPI_KEY)

**System:**
- `ASPNETCORE_ENVIRONMENT`: Production
- `WEBSITE_RUN_FROM_PACKAGE`: 1 (for deployment optimization)

### Configuration Management

**✅ Recommended Approach: GitHub Actions (Current Implementation)**

All secrets are managed through GitHub and automatically deployed:
1. Add secrets to GitHub repository (one-time setup)
2. GitHub Actions workflow automatically configures Azure App Service on every deployment
3. No manual Azure CLI commands needed

See [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) for complete setup instructions.

**⚠️ Manual Configuration (Not Recommended)**

If you need to manually update settings (e.g., for troubleshooting):

```bash
az webapp config appsettings set \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant-production \
  --settings \
    "AzureAd__ClientId=YOUR_CLIENT_ID" \
    "AzureAd__ClientSecret=YOUR_CLIENT_SECRET" \
    "AzureAd__TenantId=common" \
    "ExchangeRateAPIs__ExchangeRateApi__ApiKey=YOUR_KEY_HERE" \
    "ExchangeRateAPIs__OpenExchangeRates__ApiKey=YOUR_KEY_HERE" \
    "ExchangeRateAPIs__CurrencyApi__ApiKey=YOUR_KEY_HERE"
```

Note: Manual changes will be overwritten on next GitHub Actions deployment.

### Azure Key Vault (Optional Enhancement)

For additional security, you can migrate to Azure Key Vault:

```bash
# Create Key Vault
az keyvault create \
  --name camilo-assistant-kv \
  --resource-group camilo-personal-assistant-rg \
  --location eastus

# Store secrets
az keyvault secret set --vault-name camilo-assistant-kv --name "AzureAd--ClientSecret" --value "YOUR_SECRET"
az keyvault secret set --vault-name camilo-assistant-kv --name "ExchangeRateApi-Key" --value "YOUR_KEY"

# Enable managed identity for App Service
az webapp identity assign \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant-production

# Grant access to Key Vault
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant-production \
  --query principalId -o tsv)

az keyvault set-policy \
  --name camilo-assistant-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Reference in App Settings
az webapp config appsettings set \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant-production \
  --settings \
    "AzureAd__ClientSecret=@Microsoft.KeyVault(SecretUri=https://camilo-assistant-kv.vault.azure.net/secrets/AzureAd--ClientSecret/)"
```

## Monitoring and Logs

### View Application Logs

```bash
# Stream logs
az webapp log tail \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant-production

# Download logs
az webapp log download \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant-production \
  --log-file logs.zip
```

### Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your resource group: `camilo-personal-assistant-rg`
3. Click on your Web App
4. View:
   - **Overview**: Application URL, status, metrics
   - **Log stream**: Real-time application logs
   - **Deployment Center**: Deployment history
   - **Monitoring**: Application Insights (if enabled)

## Troubleshooting

### Deployment Fails

**Check GitHub Actions logs:**
1. Go to Actions tab
2. Click on the failed workflow run
3. Expand failed steps to see error messages

**Common issues:**
- **Authentication failure**: Verify GitHub secrets are correctly set
- **Resource group not found**: Ensure `camilo-personal-assistant-rg` exists
- **Permission denied**: Service principal needs Contributor role

### Application Not Starting

**Check application logs:**
```bash
az webapp log tail \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant-production
```

**Common issues:**
- **.NET version mismatch**: Verify App Service supports .NET 10
- **Configuration error**: Check environment variables in Azure Portal
- **Build artifacts missing**: Verify publish step in GitHub Actions

### Update Infrastructure

To modify Azure resources:
1. Edit `infrastructure/main.bicep`
2. Update parameters in `infrastructure/parameters.json`
3. Commit and push to trigger deployment
4. Or deploy manually using Azure CLI

## Cost Optimization

**Current setup (B1 tier):**
- Estimated cost: ~$13-15/month (pay-as-you-go)
- Recommended for: Development/Testing/Small Production

**Cost-saving options:**
1. **Free tier (F1)**: Free but limited (no custom domains, 60 min/day)
2. **Shared tier (D1)**: ~$10/month (shared resources)
3. **Scale down when not in use**: Manually stop the app service

**To change SKU:**
Update `appServicePlanSku` in `infrastructure/parameters.json`:
- `F1`: Free
- `D1`: Shared
- `B1`: Basic (current)
- `S1`: Standard (auto-scaling, slots)
- `P1V2`: Premium (enhanced performance)

## Security Best Practices

? **Implemented:**
- OIDC authentication (no stored credentials)
- HTTPS only
- TLS 1.2+ minimum
- FTPS disabled
- Environment protection rules

?? **Additional recommendations:**
1. Enable Application Insights for monitoring
2. Configure custom domain with SSL certificate
3. Enable Azure AD authentication for the app
4. Set up Azure Key Vault for secrets
5. Configure virtual network integration
6. Enable DDoS protection

## Security Best Practices

✅ **Implemented:**
- OIDC authentication (no stored credentials)
- HTTPS only
- TLS 1.2+ minimum
- FTPS disabled
- Environment protection rules
- **User Secrets for local development** (not committed to Git)
- **Environment Variables for production** (Azure App Service Configuration)
- **Sensitive files excluded from Git** (.gitignore configured)

⚠️ **Additional recommendations:**
1. **Use Azure Key Vault for secrets** (especially Client Secrets and API keys)
2. Enable Application Insights for monitoring
3. Configure custom domain with SSL certificate
4. Enable Azure AD authentication for the app
5. Configure virtual network integration
6. Enable DDoS protection
7. **Rotate API keys regularly**
8. **Use managed identities** where possible
9. **Enable audit logging** for Key Vault access

## URLs

After deployment, your application will be available at:
- **Production**: `https://camilo-personal-assistant-production.azurewebsites.net`

To use a custom domain, configure it in Azure Portal ? Web App ? Custom domains.

## Support

For issues or questions:
1. Check GitHub Actions logs
2. Review Azure Web App logs
3. Verify all secrets are configured
4. Ensure resource group exists and has proper permissions

## Next Steps

- [ ] Configure custom domain
- [ ] Enable Application Insights
- [ ] Set up Azure Key Vault for secrets
- [ ] Configure staging environment
- [ ] Add health check endpoints
- [ ] Set up monitoring alerts
