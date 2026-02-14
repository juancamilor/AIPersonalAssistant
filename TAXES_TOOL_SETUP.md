# Taxes Manager Tool - Setup Instructions

## Overview
The Taxes Manager tool provides a **federal tax estimate** by combining:
- **W2 OCR extraction** — Upload a W2 PDF/image and extract income data using Azure Document Intelligence
- **Stock sales import** — Upload an Excel file with capital gains/losses from stock transactions
- **Tax calculation** — Computes estimated federal taxes using 2025 MFJ brackets, standard deduction, and capital gains rates

## Azure Document Intelligence Setup

The W2 OCR feature requires an **Azure Document Intelligence** (formerly Form Recognizer) resource.

### Create the Resource
1. Go to the [Azure Portal](https://portal.azure.com/)
2. Search for **"Document Intelligence"** and click **Create**
3. Select your subscription and resource group
4. Choose a region and pricing tier (Free tier: 500 pages/month)
5. Click **Review + Create**, then **Create**
6. Once deployed, go to the resource and copy:
   - **Endpoint** (e.g., `https://your-resource.cognitiveservices.azure.com/`)
   - **API Key** (from the **Keys and Endpoint** section)

## Configuration

### Local Development (User Secrets - Recommended)

```bash
cd AIPersonalAssistant.Web
dotnet user-secrets set "AzureDocumentIntelligence:Endpoint" "https://your-resource.cognitiveservices.azure.com/"
dotnet user-secrets set "AzureDocumentIntelligence:ApiKey" "your-api-key"
```

User secrets are stored securely outside your project:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- macOS/Linux: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

### Production (Azure App Service)

Set as application settings:
- Name: `AzureDocumentIntelligence__Endpoint` — Value: `https://your-resource.cognitiveservices.azure.com/`
- Name: `AzureDocumentIntelligence__ApiKey` — Value: `your-api-key`

Note: Use double underscores `__` instead of colons in Azure.

### Production Deployment

The Document Intelligence keys are automatically deployed via GitHub Actions. See [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) for the KEY REGISTRY.

**GitHub Secrets:**
- `AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT` → `AzureDocumentIntelligence__Endpoint`
- `AZURE_DOCUMENT_INTELLIGENCE_API_KEY` → `AzureDocumentIntelligence__ApiKey`

## Stock Sales Excel Format

Upload an Excel file (`.xlsx`) with the following columns:

| Column    | Description                          | Example          |
|-----------|--------------------------------------|------------------|
| Symbol    | Stock ticker symbol                  | MSFT             |
| Buy Date  | Date the stock was purchased         | 01/15/2024       |
| Buy Price | Price per share at purchase          | 350.00           |
| Sell Date | Date the stock was sold              | 06/20/2025       |
| Sell Price| Price per share at sale              | 450.00           |
| Quantity  | Number of shares sold                | 100              |
| Type      | Holding period classification        | Short-Term or Long-Term |

- **Short-Term**: Held for 1 year or less (taxed as ordinary income)
- **Long-Term**: Held for more than 1 year (taxed at capital gains rates)

## Tax Calculation Details

### 2025 Federal Tax Brackets (Married Filing Jointly)

| Bracket         | Rate  |
|-----------------|-------|
| $0 – $23,850    | 10%   |
| $23,851 – $96,950 | 12% |
| $96,951 – $206,700 | 22% |
| $206,701 – $394,600 | 24% |
| $394,601 – $501,050 | 32% |
| $501,051 – $751,600 | 35% |
| Over $751,600   | 37%   |

### Standard Deduction
- **$30,000** for Married Filing Jointly (2025)

### Capital Gains Rates (Long-Term)

| Taxable Income (MFJ)   | Rate |
|-------------------------|------|
| $0 – $96,700            | 0%   |
| $96,701 – $600,050      | 15%  |
| Over $600,050            | 20%  |

- Short-term capital gains are taxed as ordinary income
- Long-term capital gains use the preferential rates above

## Architecture

```
Frontend (taxes.html)
        ↓
  TaxesController
        ↓
  TaxesService
    ├── W2 OCR → Azure Document Intelligence
    ├── Stock Sales → Excel parsing (EPPlus/ClosedXML)
    └── Tax Calculation → 2025 brackets + capital gains
        ↓
  Return tax estimate to Frontend
```

## Limitations & Disclaimer

⚠️ **This tool provides estimates only and is NOT tax advice.**

- **Federal taxes only** — State and local taxes are not calculated
- **Married Filing Jointly (MFJ) only** — Other filing statuses are not supported
- **Standard deduction only** — Itemized deductions are not supported
- **No AMT** — Alternative Minimum Tax is not calculated
- **No credits** — Tax credits (child tax credit, etc.) are not applied
- **2025 tax year only** — Brackets and rates are for the 2025 tax year
- **Estimates may differ** from actual tax liability

**Always consult a qualified tax professional for official tax preparation and advice.**
