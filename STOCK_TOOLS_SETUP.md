# Stocks Feature - API Setup Instructions

## Overview
The Stocks feature displays historical stock performance using data from **Alpha Vantage API** with interactive Chart.js charts.

## Features
- Select from 3 stocks: Microsoft (MSFT), Meta (META), Google (GOOGL)
- **Auto-add on selection**: Selecting a stock from the dropdown automatically adds it to the chart (no separate Add button)
- Choose custom date range
- Interactive line chart showing closing prices
- Summary statistics: Start/End price, High, Low, Change, Change %
- Info banner noting free-tier data limitations

## Required API Key

### Alpha Vantage
- **Sign up:** https://www.alphavantage.co/support/#api-key
- **Free tier:** 25 requests/day, 5 calls/minute
- **Free tier data:** Returns last 100 trading days (~5 months) using `outputsize=compact`
- **Premium tier:** Full historical data (20+ years) with `outputsize=full`
- **Steps:**
  1. Visit the link above and request a free API key
  2. Copy your API key
  3. Store using user secrets (see below)

## Configuration

### Local Development (User Secrets - Recommended)

```bash
cd AIPersonalAssistant.Web
dotnet user-secrets set "StockAPI:AlphaVantage:ApiKey" "your_api_key_here"
```

User secrets are stored securely outside your project:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- macOS/Linux: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

### Production (Azure App Service)

Set as application setting:
- Name: `StockAPI__AlphaVantage__ApiKey`
- Value: `your_api_key_here`

Note: Use double underscores `__` instead of colons in Azure.
```

### Production Deployment

The Stock API key is automatically deployed via GitHub Actions. See [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) for the KEY REGISTRY.

**GitHub Secret:** `ALPHAVANTAGE_API_KEY`  
**Azure Setting:** `StockAPI__AlphaVantage__ApiKey`

## Testing Without API Key

The application will show an error message if the API key is not configured. Unlike the exchange rate tool, stock data requires a valid API key to function.

## Architecture

```
Frontend (stock-tools.html + Chart.js)
           ↓
    StockController
           ↓
      StockService
           ↓
    Alpha Vantage API
           ↓
    [Cache for 1 hour]
           ↓
    Return filtered data
```

## Caching Strategy

- Stock data is cached per symbol for **1 hour**
- Date filtering happens on cached data (reduces API calls)
- Free tier returns ~5 months of data (`outputsize=compact`); premium returns full history (`outputsize=full`)

## Supported Stocks

| Company   | Symbol | Full Name              |
|-----------|--------|------------------------|
| Microsoft | MSFT   | Microsoft Corporation  |
| Meta      | META   | Meta Platforms, Inc.   |
| Google    | GOOGL  | Alphabet Inc.          |

## API Response Format

The endpoint `POST /api/stock/data` returns:

```json
{
  "symbol": "MSFT",
  "companyName": "Microsoft Corporation",
  "dataPoints": [
    { "date": "2024-01-15", "close": 390.25 },
    { "date": "2024-01-16", "close": 392.50 }
  ],
  "success": true,
  "errorMessage": null
}
```

## Troubleshooting

### "Failed to fetch stock data from Alpha Vantage"
- Check that your API key is correctly configured
- Verify you haven't exceeded the rate limit (5 calls/minute)

### "No data available for the selected date range"
- Try expanding the date range
- Note: Free tier only returns ~5 months of data; older dates require a premium API key
- Alpha Vantage may not have data for very recent dates (today/yesterday)

### Chart not displaying
- Ensure you have an internet connection (Chart.js loads from CDN)
- Check browser console for JavaScript errors
