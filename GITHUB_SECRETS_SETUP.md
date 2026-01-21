# GitHub Secrets Setup for CI/CD

## Overview
The CI/CD pipeline automatically configures API keys in Azure App Service during deployment. You need to add these secrets to GitHub **one time only**.

## Required GitHub Secrets

Add these 3 secrets to your GitHub repository:

| Secret Name | Value | Purpose |
|------------|-------|---------|
| `EXCHANGERATE_API_KEY` | `acd35787f895deee3418db8c` | ExchangeRate-API.com |
| `OPENEXCHANGERATES_API_KEY` | `bd67a76a53d342c1bfa9a7b1312687b9` | Open Exchange Rates |
| `CURRENCYAPI_KEY` | `cur_live_LGNfQAz7gukb6lwIDlvxqm5HQtu8hZdAMAYTTuJB` | CurrencyAPI.com |

## How to Add Secrets to GitHub

### Step 1: Go to Repository Settings
1. Open your browser and go to: https://github.com/juancamilor/AIPersonalAssistant
2. Click the **Settings** tab (at the top of the page)

### Step 2: Navigate to Secrets
1. In the left sidebar, click **Secrets and variables**
2. Click **Actions**

### Step 3: Add Each Secret
For each secret in the table above:

1. Click the **"New repository secret"** button (green button, top right)
2. Fill in the form:
   - **Name:** Copy the exact name from the "Secret Name" column (e.g., `EXCHANGERATE_API_KEY`)
   - **Secret:** Copy the value from the "Value" column
3. Click **"Add secret"**
4. Repeat for all 3 secrets

### Visual Guide

```
Settings → Secrets and variables → Actions → New repository secret
```

**Example:**
```
Name: EXCHANGERATE_API_KEY
Secret: acd35787f895deee3418db8c
[Add secret]
```

## What Happens After Adding Secrets?

Once you add the secrets:
1. ✅ Every deployment will automatically configure API keys in Azure
2. ✅ No manual steps needed after deployment
3. ✅ Keys are encrypted and secure in GitHub
4. ✅ Only GitHub Actions can access them

## Testing

After adding the secrets:
1. Push any change to `main` branch (or manually trigger workflow)
2. GitHub Actions will:
   - ✅ Build and test the app
   - ✅ Deploy infrastructure
   - ✅ Deploy application
   - ✅ **Automatically configure API keys in Azure**
3. Visit your Azure app and test the Exchange Rate tool
4. Click "View Details" - you should see all 3 sources with green checkmarks ✓

## Updating API Keys

If you need to rotate/update API keys:
1. Go to Settings → Secrets and variables → Actions
2. Click on the secret name
3. Click "Update secret"
4. Enter new value
5. Next deployment will use the new key automatically

## Security Notes

- ✅ Secrets are **encrypted** in GitHub
- ✅ Secrets **never appear** in logs
- ✅ Only repository admins can add/view secrets
- ✅ GitHub Actions can only **use** them (not view)
- ✅ Secrets are **not** committed to Git

## Already Configured Secrets

Your repository should already have these Azure secrets configured:
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

You're just adding the 3 new API key secrets.

## Need Help?

If you encounter issues:
1. Verify secret names are **exactly** as shown (case-sensitive)
2. Check there are no extra spaces in the values
3. Ensure you're in the **Actions** secrets section (not Dependabot or Codespaces)

## Next Steps

After adding secrets:
1. ✅ Commit and push the updated workflow
2. ✅ GitHub Actions will deploy automatically
3. ✅ API keys will be configured in Azure
4. ✅ Test the Exchange Rate tool
