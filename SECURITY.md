# Security Guidelines for AI Personal Assistant

## Overview
This document outlines security best practices for handling sensitive configuration in the AI Personal Assistant application.

## API Key Management

### ❌ Never Do This:
- Hardcode API keys in source code
- Commit API keys to Git repository
- Share API keys in plain text (emails, chat, etc.)
- Store API keys in client-side JavaScript
- Use the same API keys across environments

### ✅ Always Do This:
- Use User Secrets for local development
- Use Environment Variables in Azure
- Use Azure Key Vault for production secrets
- Rotate API keys regularly (every 90 days recommended)
- Use different API keys for development/staging/production

## Local Development Security

### Authentication Bypass for Local Testing

The application supports a `BYPASS_AUTH` environment variable for local development and Playwright testing:

- **Only active when** `ASPNETCORE_ENVIRONMENT=Development` **AND** `BYPASS_AUTH=true`
- **Never active in production** — the bypass is ignored when the environment is not Development
- **Purpose**: Enables running Playwright tests and local development without configuring Azure AD

```bash
# PowerShell
$env:BYPASS_AUTH="true"

# Or set in launchSettings.json
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development",
  "BYPASS_AUTH": "true"
}
```

⚠️ **Do NOT set `BYPASS_AUTH` in any production or staging environment.** The code explicitly checks for the Development environment before honoring this variable.

### User Secrets Setup

User Secrets are stored **outside** your project directory and **never** committed to Git.

**Location:**
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- macOS/Linux: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

**Setup Commands:**
```bash
cd AIPersonalAssistant.Web

# Azure AD
dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"

# Exchange Rate APIs
dotnet user-secrets set "ExchangeRateAPIs:ExchangeRateApi:ApiKey" "your-key"
dotnet user-secrets set "ExchangeRateAPIs:OpenExchangeRates:ApiKey" "your-key"
dotnet user-secrets set "ExchangeRateAPIs:CurrencyApi:ApiKey" "your-key"

# Stock Tools API
dotnet user-secrets set "StockAPI:AlphaVantage:ApiKey" "your-key"

# Taxes Manager (Azure Document Intelligence)
dotnet user-secrets set "AzureDocumentIntelligence:Endpoint" "your-endpoint"
dotnet user-secrets set "AzureDocumentIntelligence:ApiKey" "your-key"
```

**List Secrets:**
```bash
dotnet user-secrets list
```

**Remove a Secret:**
```bash
dotnet user-secrets remove "ExchangeRateAPIs:ExchangeRateApi:ApiKey"
```

**Clear All Secrets:**
```bash
dotnet user-secrets clear
```

### .gitignore Protection

The following files are **excluded** from Git:
- `appsettings.*.json` (except base templates)
- `*.secrets.json`
- `*.azurePubxml`
- User secrets directory (already outside project)

## Production Security (Azure)

### Azure App Service Configuration

**Option 1: App Service Settings (Quick Setup)**

Via Azure Portal:
1. Go to Azure Portal → Your App Service
2. Settings → Configuration → Application settings
3. Click "New application setting"
4. Add each key-value pair:
   - Name: `ExchangeRateAPIs__ExchangeRateApi__ApiKey`
   - Value: `your-actual-key`
   - ⚠️ Note: Use double underscores `__` in Azure (not colons)

Via Azure CLI:
```bash
az webapp config appsettings set \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant \
  --settings \
    "ExchangeRateAPIs__ExchangeRateApi__ApiKey=your-key" \
    "ExchangeRateAPIs__OpenExchangeRates__ApiKey=your-key" \
    "ExchangeRateAPIs__CurrencyApi__ApiKey=your-key"
```

**Option 2: Azure Key Vault (Recommended for Production)**

1. **Create Key Vault:**
```bash
az keyvault create \
  --name camilo-assistant-kv \
  --resource-group camilo-personal-assistant-rg \
  --location eastus
```

2. **Store Secrets:**
```bash
az keyvault secret set \
  --vault-name camilo-assistant-kv \
  --name "ExchangeRateApi-Key" \
  --value "your-actual-key"

az keyvault secret set \
  --vault-name camilo-assistant-kv \
  --name "OpenExchangeRates-Key" \
  --value "your-actual-key"

az keyvault secret set \
  --vault-name camilo-assistant-kv \
  --name "CurrencyApi-Key" \
  --value "your-actual-key"
```

3. **Enable Managed Identity:**
```bash
az webapp identity assign \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant
```

4. **Grant Access:**
```bash
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant \
  --query principalId -o tsv)

az keyvault set-policy \
  --name camilo-assistant-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

5. **Reference in App Settings:**
```bash
az webapp config appsettings set \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant \
  --settings \
    "ExchangeRateAPIs__ExchangeRateApi__ApiKey=@Microsoft.KeyVault(SecretUri=https://camilo-assistant-kv.vault.azure.net/secrets/ExchangeRateApi-Key/)" \
    "ExchangeRateAPIs__OpenExchangeRates__ApiKey=@Microsoft.KeyVault(SecretUri=https://camilo-assistant-kv.vault.azure.net/secrets/OpenExchangeRates-Key/)" \
    "ExchangeRateAPIs__CurrencyApi__ApiKey=@Microsoft.KeyVault(SecretUri=https://camilo-assistant-kv.vault.azure.net/secrets/CurrencyApi-Key/)"
```

## Key Propagation Verification

After deploying, verify all keys reached production:
1. Visit `https://camilo-personal-assistant.azurewebsites.net/api/health`
2. Check that all API keys show as "configured" 
3. If any key shows "missing", check the KEY REGISTRY in [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md)

**Common failure:** Key added to `ci.yml` but NOT to `deploy.yml` (or vice versa). Both files must be updated.

## API Key Rotation

### When to Rotate:
- Every 90 days (recommended)
- When a team member with access leaves
- If a key is accidentally exposed
- After a security incident

### How to Rotate:

**Step 1: Get New Keys**
- ExchangeRate-API: Dashboard → Regenerate API Key
- Open Exchange Rates: Dashboard → Generate New App ID
- CurrencyAPI: Dashboard → Regenerate Key

**Step 2: Update Configuration**

Local Development:
```bash
dotnet user-secrets set "ExchangeRateAPIs:ExchangeRateApi:ApiKey" "new-key"
```

Production:
```bash
az webapp config appsettings set \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant \
  --settings "ExchangeRateAPIs__ExchangeRateApi__ApiKey=new-key"
```

**Step 3: Restart App (if needed)**
```bash
az webapp restart \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant
```

## Monitoring & Auditing

### Enable Key Vault Audit Logging:
```bash
az monitor diagnostic-settings create \
  --resource $(az keyvault show --name camilo-assistant-kv --query id -o tsv) \
  --name "KeyVault-Audit" \
  --logs '[{"category":"AuditEvent","enabled":true}]' \
  --workspace <log-analytics-workspace-id>
```

### Check Who Accessed Secrets:
```bash
az monitor activity-log list \
  --resource-group camilo-personal-assistant-rg \
  --start-time 2026-01-01T00:00:00Z
```

## Incident Response

### If API Key is Compromised:

1. **Immediately Revoke the Key:**
   - Log in to the API provider
   - Disable/delete the compromised key

2. **Generate New Key:**
   - Create a new API key
   - Update configuration (User Secrets + Azure)

3. **Review Access Logs:**
   - Check API provider usage logs
   - Look for unauthorized usage
   - Monitor for unusual API call patterns

4. **Notify Stakeholders:**
   - Inform team members
   - Document the incident
   - Review security procedures

## Compliance Checklist

- [ ] All API keys stored in User Secrets (local) or Key Vault (production)
- [ ] No secrets committed to Git repository
- [ ] `.gitignore` properly configured
- [ ] API keys rotated within last 90 days
- [ ] Different keys for dev/staging/production
- [ ] Managed Identity enabled for Azure resources
- [ ] Key Vault audit logging enabled
- [ ] Team members have minimum required permissions
- [ ] Backup of Key Vault configuration exists
- [ ] Incident response plan documented

## Additional Resources

- [ASP.NET Core User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault Best Practices](https://docs.microsoft.com/en-us/azure/key-vault/general/best-practices)
- [Managed Identities for Azure](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)

## Contact

For security concerns or questions, contact: juan_camilo_r@hotmail.com
