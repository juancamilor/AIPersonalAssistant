# Stock Tools Feature - API Setup Instructions

## Overview
The Stock Tools feature displays historical stock performance using data from **Alpha Vantage API** with interactive Chart.js charts.

## Features
- Select from 3 stocks: Microsoft (MSFT), Meta (META), Google (GOOGL)
- Choose custom date range
- Interactive line chart showing closing prices
- Summary statistics: Start/End price, High, Low, Change, Change %

## Required API Key

### Alpha Vantage
- **Sign up:** https://www.alphavantage.co/support/#api-key
- **Free tier:** 500 requests/day, 5 calls/minute
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

- Full daily data is cached per stock symbol for **1 hour**
- Date filtering happens on cached data (reduces API calls)
- This keeps you well within the 500 requests/day limit

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
- Note: Alpha Vantage may not have data for very recent dates (today/yesterday)

### Chart not displaying
- Ensure you have an internet connection (Chart.js loads from CDN)
- Check browser console for JavaScript errors
