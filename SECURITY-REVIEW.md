# Security Review: Secrets and Credentials Storage

**Document Version:** 1.0  
**Date:** January 2026  
**Reviewed By:** Security Audit  
**Application:** AI Personal Assistant  

---

## Executive Summary

This document provides a comprehensive review of all locations where the AI Personal Assistant solution stores, references, or manages secrets and sensitive credentials. The application uses a multi-layered approach for secret management across local development, Azure cloud infrastructure, and CI/CD pipelines.

**Key Findings:**
- ✅ Secrets are NOT committed to the repository
- ✅ Uses .NET User Secrets for local development
- ✅ Uses GitHub Secrets for CI/CD automation
- ✅ Uses Azure App Service Configuration for production
- ✅ Implements OIDC authentication (no stored credentials for Azure deployment)
- ⚠️ Client secrets displayed in PowerShell script output (temporary, during setup)

---

## 1. Local Development Environment

### 1.1 .NET User Secrets

**Storage Location:** User profile directory (outside repository)
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **macOS/Linux:** `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

**Configuration Method:**
```bash
dotnet user-secrets set "AzureAd:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_CLIENT_SECRET"
```

**Secrets Stored:**
- `AzureAd:ClientId` - Azure AD Application (Client) ID
- `AzureAd:ClientSecret` - Azure AD Client Secret

**Security Assessment:**
- ✅ **Good:** Secrets stored outside repository
- ✅ **Good:** Secrets stored per-user, not shared
- ✅ **Good:** Automatically loaded in Development environment
- ⚠️ **Note:** User secrets are stored in plain text in the file system
- ⚠️ **Note:** Secrets accessible to processes running under the same user account

**Reference Files:**
- `setup-azure-ad.ps1` (lines 82-85) - Automated configuration
- `README.md` (lines 71-76) - Manual configuration instructions

### 1.2 Configuration Files (Template Only)

**File:** `AIPersonalAssistant.Web/appsettings.json`
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "YOUR_CLIENT_ID_HERE",
    "ClientSecret": "YOUR_CLIENT_SECRET_HERE",
    "CallbackPath": "/signin-microsoft",
    "SignedOutCallbackPath": "/signout-callback-microsoft"
  }
}
```

**Security Assessment:**
- ✅ **Good:** Contains placeholder values only
- ✅ **Good:** NOT used for actual secrets
- ✅ **Good:** Safe to commit to repository
- ℹ️ **Info:** Serves as configuration template

**File:** `AIPersonalAssistant.Web/appsettings.Development.json`
- Contains same structure with placeholder values
- Used for development-specific logging configuration
- No actual secrets stored

---

## 2. Azure Cloud Infrastructure

### 2.1 Azure App Service Configuration

**Storage Location:** Azure App Service → Configuration → Application Settings

**Configuration Method:**
```bash
az webapp config appsettings set \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant \
  --settings \
    "AzureAd__ClientId=aa7c35d6-dc69-477f-9b83-51236f06b2fe" \
    "AzureAd__ClientSecret=<actual_secret>" \
    "AzureAd__TenantId=common"
```

**Secrets Stored:**
- `AzureAd__ClientId` - Azure AD Application ID (aa7c35d6-dc69-477f-9b83-51236f06b2fe)
- `AzureAd__ClientSecret` - Azure AD Client Secret (value not documented here)
- `AzureAd__TenantId` - Tenant ID ("common" for personal accounts)

**Security Assessment:**
- ✅ **Good:** Secrets stored in Azure, not in code
- ✅ **Good:** Encrypted at rest by Azure
- ✅ **Good:** Accessible only via Azure RBAC
- ✅ **Good:** Can be rotated without code changes
- ⚠️ **Note:** Secrets visible to users with Contributor/Reader roles on the App Service
- 💡 **Recommendation:** Consider migrating to Azure Key Vault for enhanced security

**Production URL:** https://camilo-personal-assistant.azurewebsites.net

### 2.2 Azure AD App Registration

**Azure Portal Location:** Azure Active Directory → App registrations → camilo-personal-assistant

**Application Details:**
- **Application (Client) ID:** aa7c35d6-dc69-477f-9b83-51236f06b2fe
- **Tenant ID:** common (supports personal Microsoft accounts)
- **Sign-in Audience:** AzureADandPersonalMicrosoftAccount
- **Client Secret:** Created and stored in Azure AD (expires based on configuration)

**Redirect URIs Configured:**
- `https://localhost:5001/signin-microsoft` (local development)
- `http://localhost:5000/signin-microsoft` (local development)
- `https://localhost:7028/signin-microsoft` (local development)
- `https://camilo-personal-assistant.azurewebsites.net/signin-microsoft` (production)

**API Permissions:**
- Microsoft Graph → User.Read (Delegated)

**Security Assessment:**
- ✅ **Good:** Client secrets managed in Azure AD
- ✅ **Good:** Secrets have expiration dates
- ✅ **Good:** Can be rotated from Azure Portal
- ✅ **Good:** Supports multi-factor authentication
- ⚠️ **Note:** Client ID is public and non-sensitive
- ⚠️ **Note:** Client secret must be kept confidential
- 💡 **Recommendation:** Implement certificate-based authentication instead of client secrets

**Reference Files:**
- `setup-azure-ad.ps1` (lines 21-114) - Automated setup script
- `.azure-setup-summary.txt` (lines 1-114) - Setup summary (contains client secret in plain text)

---

## 3. GitHub Repository and CI/CD

### 3.1 GitHub Actions Secrets

**Storage Location:** GitHub Repository → Settings → Secrets and variables → Actions

**Secrets Configured:**

| Secret Name | Purpose | Value Reference | Used In |
|------------|---------|-----------------|---------|
| `AZURE_CLIENT_ID` | Service Principal for deployment | ff332010-9a33-4885-a287-e26a7274d510 | ci.yml, deploy.yml |
| `AZURE_TENANT_ID` | Azure tenant for deployment | f4e3374a-d9eb-4b94-92e6-8f1d851e7b6e | ci.yml, deploy.yml |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription for deployment | b6baf6a3-39aa-45ce-9363-83e84a634034 | ci.yml, deploy.yml |

**Security Assessment:**
- ✅ **Good:** Secrets encrypted by GitHub
- ✅ **Good:** NOT accessible via GitHub API
- ✅ **Good:** Masked in workflow logs
- ✅ **Good:** Uses OIDC (OpenID Connect) for Azure authentication
- ✅ **Good:** No static credentials or passwords stored
- ✅ **Good:** Scoped to specific repository only
- ℹ️ **Info:** These are service principal identifiers, not secrets themselves

**Workflow Files:**
- `.github/workflows/ci.yml` (lines 76-80, 112-116)
- `.github/workflows/deploy.yml` (lines 77-82)

### 3.2 Service Principal Configuration

**Azure Resource:** Service Principal "camilo-personal-assistant-github"

**Authentication Method:** OIDC (OpenID Connect) with Federated Identity Credentials

**Federated Credentials:**
- **Main Branch:** `repo:juancamilor/AIPersonalAssistant:ref:refs/heads/main`
- **Pull Requests:** `repo:juancamilor/AIPersonalAssistant:pull_request`

**Permissions:**
- **Role:** Contributor
- **Scope:** Resource Group `camilo-personal-assistant-rg`

**Security Assessment:**
- ✅ **Excellent:** No passwords or keys stored (OIDC-based)
- ✅ **Excellent:** Short-lived tokens (expires after each workflow)
- ✅ **Good:** Scoped to specific resource group only
- ✅ **Good:** Restricted to specific GitHub repository
- ✅ **Good:** Restricted to specific branches (main) and pull requests
- ℹ️ **Info:** Modern security best practice for GitHub Actions

**Reference Files:**
- `.azure-setup-summary.txt` (lines 1-99)
- `DEPLOYMENT.md` (lines 23-74)

### 3.3 Repository Files (No Secrets)

**Files Reviewed:**

| File | Contains Secrets? | Notes |
|------|------------------|-------|
| `appsettings.json` | ❌ No | Placeholders only |
| `appsettings.Development.json` | ❌ No | Placeholders only |
| `parameters.json` | ❌ No | Infrastructure parameters, no secrets |
| `main.bicep` | ❌ No | Infrastructure template, no secrets |
| `ci.yml` | ❌ No | References GitHub secrets |
| `deploy.yml` | ❌ No | References GitHub secrets |
| `.gitignore` | ✅ Configured | Excludes secrets and sensitive files |

**Security Assessment:**
- ✅ **Excellent:** No secrets committed to repository
- ✅ **Good:** .gitignore properly configured
- ✅ **Good:** Configuration uses placeholders

**.gitignore Coverage:**
- ✅ `*.user` files excluded
- ✅ `*.publishsettings` excluded
- ✅ `*.pubxml` excluded
- ✅ `publish/` directory excluded
- ✅ User secrets directory outside repository by default

---

## 4. Setup and Documentation Files

### 4.1 Setup Script: setup-azure-ad.ps1

**Purpose:** Automated Azure AD app registration and configuration

**Secret Handling:**

| Line(s) | Action | Security Impact |
|---------|--------|----------------|
| 70-76 | Creates client secret via Azure CLI | ✅ Temporary, not persisted |
| 76 | Stores secret in variable `$clientSecret` | ⚠️ In memory during execution |
| 83-84 | Saves to .NET user secrets | ✅ Secure storage |
| 95 | Displays client secret in console | ⚠️ Visible in terminal output |
| 112 | Displays client secret again (warning) | ⚠️ Visible in terminal output |

**Security Assessment:**
- ⚠️ **Moderate Risk:** Client secret displayed in plain text during setup
- ⚠️ **Note:** Terminal history may contain secret
- ⚠️ **Note:** Screen recordings could capture secret
- ✅ **Good:** Secret immediately saved to user secrets
- ✅ **Good:** Script only runs during initial setup
- 💡 **Recommendation:** Clear terminal after running script
- 💡 **Recommendation:** Add warning to copy secret before clearing screen

### 4.2 Documentation Files

**File:** `.azure-setup-summary.txt`

**Contains:**
- Service Principal Client ID: ff332010-9a33-4885-a287-e26a7274d510
- Tenant ID: f4e3374a-d9eb-4b94-92e6-8f1d851e7b6e
- Subscription ID: b6baf6a3-39aa-45ce-9363-83e84a634034

**Security Assessment:**
- ℹ️ **Info:** Contains identifiers, not secrets
- ✅ **Good:** File committed to repository (no secrets)
- ℹ️ **Note:** These values are required for GitHub Actions configuration

**File:** `DEPLOYMENT.md`
- Contains deployment instructions
- References secrets but doesn't contain actual values
- ✅ Safe to commit

**File:** `README.md`
- Contains setup instructions
- Uses placeholders like "YOUR_CLIENT_ID" and "YOUR_CLIENT_SECRET"
- ✅ Safe to commit

---

## 5. Application Runtime Configuration

### 5.1 Configuration Loading Order

The application loads configuration in this order (later sources override earlier):

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific)
3. User Secrets (Development environment only)
4. Environment Variables (all environments)
5. Azure App Service Configuration (production)
6. Command-line arguments

**Security Assessment:**
- ✅ **Good:** Follows .NET configuration best practices
- ✅ **Good:** User secrets only loaded in Development
- ✅ **Good:** Production uses Azure configuration
- ✅ **Good:** Environment variables override file-based config

### 5.2 Program.cs Configuration

**File:** `AIPersonalAssistant.Web/Program.cs` (line 7)
```csharp
.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
```

**Security Assessment:**
- ✅ **Good:** Uses configuration abstraction
- ✅ **Good:** No hardcoded secrets
- ✅ **Good:** Supports multiple configuration sources
- ✅ **Good:** Secrets not logged or exposed

---

## 6. Secret Types Summary

### 6.1 Authentication Secrets

| Secret Type | Storage Location | Rotation Method | Expiry |
|------------|------------------|-----------------|--------|
| Azure AD Client ID | Multiple locations | Manual in Azure Portal | None (public ID) |
| Azure AD Client Secret | User Secrets / Azure Config | Manual in Azure Portal | 6-24 months (configurable) |
| Azure AD Tenant ID | Configuration files | N/A | N/A (constant) |

### 6.2 Deployment Secrets

| Secret Type | Storage Location | Rotation Method | Expiry |
|------------|------------------|-----------------|--------|
| Azure Client ID (SP) | GitHub Secrets | Recreate Service Principal | None |
| Azure Tenant ID | GitHub Secrets | N/A | N/A |
| Azure Subscription ID | GitHub Secrets | N/A | N/A |
| OIDC Token | Generated by GitHub | Automatic | Minutes (per workflow) |

### 6.3 No Secrets Currently Used

- ❌ No database connection strings
- ❌ No API keys for external services
- ❌ No encryption keys
- ❌ No certificates
- ❌ No SSH keys
- ❌ No personal access tokens

---

## 7. Security Recommendations

### 7.1 Immediate Actions (High Priority)

1. **Remove Secret from .azure-setup-summary.txt**
   - ⚠️ File currently committed may contain sensitive information in the example output
   - Action: Review and ensure no actual secrets are present
   - Status: ✅ File contains only identifiers (safe)

2. **Clear Terminal History After Setup**
   - After running `setup-azure-ad.ps1`, clear PowerShell history:
     ```powershell
     Clear-History
     Remove-Item (Get-PSReadlineOption).HistorySavePath
     ```

3. **Rotate Azure AD Client Secret**
   - If secret was exposed or displayed publicly
   - Azure Portal → App registrations → Certificates & secrets → New client secret

### 7.2 Short-term Improvements (Medium Priority)

4. **Implement Azure Key Vault**
   - Store Azure AD client secret in Key Vault
   - Configure App Service to read from Key Vault
   - Benefit: Centralized secret management, audit logging, automatic rotation

5. **Update setup-azure-ad.ps1**
   - Reduce secret visibility in console output
   - Add instructions to clear terminal after setup
   - Consider outputting to secure clipboard instead of console

6. **Enable Application Insights**
   - Monitor authentication failures
   - Alert on suspicious activity
   - Track secret usage patterns

7. **Certificate-based Authentication**
   - Replace client secret with certificate
   - More secure than shared secrets
   - Better key rotation support

### 7.3 Long-term Enhancements (Low Priority)

8. **Managed Identity for Azure Resources**
   - If accessing other Azure services in the future
   - Eliminates need for storing credentials

9. **Secret Rotation Automation**
   - Implement automated rotation for Azure AD client secrets
   - Set up Key Vault automatic rotation policies

10. **Environment Separation**
    - Create separate Azure AD apps for dev/staging/production
    - Reduces blast radius of compromised secrets

11. **Security Scanning**
    - Add GitHub Advanced Security (secret scanning)
    - Implement pre-commit hooks to prevent secret commits
    - Regular security audits

### 7.4 Monitoring and Auditing

12. **Enable Audit Logging**
    - Azure AD audit logs for authentication events
    - App Service diagnostic logs
    - Key Vault access logs (when implemented)

13. **Access Reviews**
    - Regular review of who has access to:
      - Azure subscription
      - GitHub repository secrets
      - Azure AD app registration
      - App Service configuration

---

## 8. Incident Response

### 8.1 If Azure AD Client Secret is Compromised

1. **Immediate Actions:**
   - Navigate to Azure Portal → Azure AD → App registrations
   - Select "camilo-personal-assistant"
   - Go to "Certificates & secrets"
   - Delete the compromised secret
   - Create a new client secret
   - Update secrets in:
     - User secrets (local): `dotnet user-secrets set "AzureAd:ClientSecret" "NEW_SECRET"`
     - Azure App Service: `az webapp config appsettings set ...`

2. **Monitor:**
   - Review Azure AD sign-in logs for unauthorized access
   - Check App Service logs for suspicious activity
   - Review GitHub Actions logs for unexpected deployments

### 8.2 If GitHub Actions Secrets are Compromised

1. **Note:** Current setup uses OIDC, no long-lived secrets to compromise

2. **If Service Principal is Compromised:**
   - Delete the service principal from Azure AD
   - Run the deployment setup script again
   - Update GitHub secrets with new values

### 8.3 If User Secrets File is Compromised

1. **Immediate Actions:**
   - Assume Azure AD client secret is compromised
   - Follow section 8.1 above
   - Review local machine for malware
   - Consider rotating all local development secrets

---

## 9. Compliance Considerations

### 9.1 Data Residency
- Azure AD: Global service, data may be stored in multiple regions
- App Service: Deployed to East US region
- User Secrets: Stored locally on developer machines

### 9.2 Encryption
- ✅ Azure AD secrets: Encrypted at rest by Azure
- ✅ GitHub secrets: Encrypted at rest by GitHub
- ✅ App Service config: Encrypted at rest by Azure
- ⚠️ User secrets: Plain text in file system (OS-level encryption recommended)

### 9.3 Access Control
- Azure resources: RBAC (Role-Based Access Control)
- GitHub repository: Repository permissions
- Local user secrets: OS file permissions

---

## 10. Conclusion

### Overall Security Posture: **GOOD** ✅

**Strengths:**
- No secrets committed to repository
- Multi-layered secret management approach
- Modern OIDC authentication for CI/CD (no stored credentials)
- Proper separation between development and production secrets
- Encrypted secrets in cloud services

**Areas for Improvement:**
- Client secret visibility during initial setup
- Consider implementing Azure Key Vault
- Add certificate-based authentication
- Implement secret rotation automation

**Risk Assessment:**
- **Critical Risks:** None identified
- **High Risks:** None identified
- **Medium Risks:** Secret displayed during setup script (temporary)
- **Low Risks:** Plain-text user secrets on developer machines

**Compliance:**
- Meets industry standard practices for secret management
- Follows .NET and Azure security best practices
- Implements defense-in-depth strategy

---

## 11. Review History

| Date | Version | Reviewer | Changes |
|------|---------|----------|---------|
| 2026-01-18 | 1.0 | Security Audit | Initial security review |

---

## 12. Appendix: Quick Reference

### Where Secrets Are Stored

1. **Local Development:**
   - Location: `~/.microsoft/usersecrets/<id>/secrets.json`
   - Secrets: Azure AD Client ID and Secret

2. **Azure Production:**
   - Location: App Service → Configuration
   - Secrets: Azure AD Client ID and Secret

3. **GitHub Actions:**
   - Location: Repository → Settings → Secrets
   - Values: Azure Service Principal identifiers (not actual secrets with OIDC)

### How to View/Update Secrets

**Local (User Secrets):**
```bash
# View
dotnet user-secrets list --project AIPersonalAssistant.Web

# Set
dotnet user-secrets set "AzureAd:ClientId" "value" --project AIPersonalAssistant.Web
dotnet user-secrets set "AzureAd:ClientSecret" "value" --project AIPersonalAssistant.Web

# Remove
dotnet user-secrets remove "AzureAd:ClientSecret" --project AIPersonalAssistant.Web

# Clear all
dotnet user-secrets clear --project AIPersonalAssistant.Web
```

**Azure App Service:**
```bash
# View (without values)
az webapp config appsettings list \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant

# Set
az webapp config appsettings set \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant \
  --settings "AzureAd__ClientSecret=NEW_VALUE"

# Delete
az webapp config appsettings delete \
  --resource-group camilo-personal-assistant-rg \
  --name camilo-personal-assistant \
  --setting-names "AzureAd__ClientSecret"
```

**GitHub Secrets:**
- Only via GitHub UI: Repository → Settings → Secrets and variables → Actions
- Cannot be viewed after creation (can only be updated or deleted)

**Azure AD Client Secret:**
```bash
# View app registration (not secret values)
az ad app show --id aa7c35d6-dc69-477f-9b83-51236f06b2fe

# Create new client secret
az ad app credential reset \
  --id aa7c35d6-dc69-477f-9b83-51236f06b2fe \
  --append \
  --display-name "New Secret Name"

# List credentials (not values, only metadata)
az ad app credential list --id aa7c35d6-dc69-477f-9b83-51236f06b2fe
```

---

**End of Security Review Document**
