# Azure Blob Storage Setup for Travel Map

This guide explains how to configure Azure Blob Storage for the Travel Map feature to persist travel pins in production.

## Overview

The Travel Map feature uses different storage backends depending on the environment:
- **Development**: JSON files stored locally in `App_Data/travel-pins-{userId}.json`
- **Production**: Azure Blob Storage with per-user JSON blobs

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Travel Map Service                    │
├─────────────────────────────────────────────────────────┤
│  Development (local)     │  Production (Azure)          │
│  ───────────────────     │  ────────────────────        │
│  TravelService.cs        │  BlobTravelService.cs        │
│  └─ App_Data/*.json      │  └─ Azure Blob Storage       │
│                          │     └─ travel-pins container │
│                          │        └─ user-{id}.json     │
└─────────────────────────────────────────────────────────┘
```

## Automatic Setup (Recommended)

The Azure Storage Account is automatically created by the CI/CD pipeline using Bicep templates.

### What Gets Created

When you deploy via GitHub Actions, the `infrastructure/main.bicep` template creates:

| Resource | Name Pattern | Purpose |
|----------|-------------|---------|
| Storage Account | `{appName}storage` | Stores travel pins |
| Blob Container | `travel-pins` | Container for user pin files |

### Configuration

The Bicep template automatically configures these app settings in Azure App Service:
- `AzureStorage__ConnectionString`: Connection string to the storage account
- `AzureStorage__ContainerName`: `travel-pins`

## Manual Setup (Optional)

If you need to set up storage manually:

### 1. Create Storage Account

```bash
# Set variables
RESOURCE_GROUP="camilo-personal-assistant-rg"
STORAGE_NAME="camilopersonalassistantstorage"
LOCATION="eastus"

# Create storage account
az storage account create \
  --name $STORAGE_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false

# Create container
az storage container create \
  --name travel-pins \
  --account-name $STORAGE_NAME \
  --auth-mode login
```

### 2. Get Connection String

```bash
# Get connection string
CONNECTION_STRING=$(az storage account show-connection-string \
  --name $STORAGE_NAME \
  --resource-group $RESOURCE_GROUP \
  --query connectionString -o tsv)

echo $CONNECTION_STRING
```

### 3. Configure App Service

```bash
# Set app settings
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name my-personal-assistant-hub \
  --settings \
    "AzureStorage__ConnectionString=$CONNECTION_STRING" \
    "AzureStorage__ContainerName=travel-pins"
```

## Local Development

For local development, no Azure Storage configuration is needed. The application automatically uses JSON file storage when:
- Running in Development environment, OR
- `AzureStorage:ConnectionString` is not configured

### Testing with Azure Storage Locally

If you want to test Azure Blob Storage locally:

1. Add the connection string to user secrets:
```bash
cd AIPersonalAssistant.Web
dotnet user-secrets set "AzureStorage:ConnectionString" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "AzureStorage:ContainerName" "travel-pins"
```

2. Set environment to Production in `launchSettings.json`:
```json
"ASPNETCORE_ENVIRONMENT": "Production"
```

## How It Works

### Service Selection (Program.cs)

```csharp
// Use Blob storage in production, JSON file in development
if (builder.Environment.IsProduction() && 
    !string.IsNullOrEmpty(builder.Configuration["AzureStorage:ConnectionString"]))
{
    builder.Services.AddScoped<ITravelService, BlobTravelService>();
}
else
{
    builder.Services.AddScoped<ITravelService, TravelService>();
}
```

### Blob Naming Convention

Each user's pins are stored in a separate blob:
- Pattern: `user-{base64-encoded-userId}.json`
- Example: `user-anVhbl9jYW1pbG9faG90bWFpbC5jb20=.json`

The user ID is Base64-encoded to ensure safe blob names.

## Security

### Storage Account Security
- ✅ HTTPS traffic only
- ✅ TLS 1.2 minimum
- ✅ No public blob access
- ✅ Access via connection string (managed by Azure)

### Data Isolation
- Each user's data is stored in a separate blob
- User ID derived from authenticated Microsoft account
- No cross-user data access possible

## Troubleshooting

### Pins Not Persisting in Production

1. **Check app settings:**
```bash
az webapp config appsettings list \
  --resource-group camilo-personal-assistant-rg \
  --name my-personal-assistant-hub \
  --query "[?name=='AzureStorage__ConnectionString']"
```

2. **Check container exists:**
```bash
az storage container list \
  --account-name camilopersonalassistantstorage \
  --auth-mode login
```

3. **Check application logs:**
```bash
az webapp log tail \
  --resource-group camilo-personal-assistant-rg \
  --name my-personal-assistant-hub
```

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Pins not saving | Missing connection string | Verify `AzureStorage__ConnectionString` in app settings |
| 403 Forbidden | Storage account firewall | Add App Service outbound IPs to storage firewall |
| Container not found | Container not created | Run `az storage container create` or redeploy infrastructure |

## GitHub Secrets

No additional GitHub secrets are needed for Azure Storage. The connection string is automatically generated by the Bicep template and configured in Azure App Service during deployment.

## Cost

Azure Blob Storage costs for this use case are minimal:
- **Storage**: ~$0.02/GB/month (Standard LRS)
- **Operations**: ~$0.004 per 10,000 read/write operations
- **Estimated monthly cost**: < $1 for typical personal use

## Next Steps

- [ ] Enable soft delete for blob recovery
- [ ] Configure lifecycle management for old data
- [ ] Add Azure Monitor alerts for storage issues
- [ ] Consider geo-redundant storage (GRS) for backup
