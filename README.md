# AI Personal Assistant

A modern web application for personal productivity with Microsoft Account authentication and various productivity tools.

## 🌟 Features

### Authentication
- **Microsoft Account Integration**: Secure OAuth 2.0 authentication using Azure AD
- Supports personal Microsoft accounts (Outlook.com, Hotmail, Live)
- Protected API endpoints with role-based authorization

### Tools
- **Rate Exchange**: Currency converter with **real-time exchange rates from multiple sources**
  - **Multi-source data**: Fetches rates from 3 APIs (ExchangeRate-API, OpenExchangeRates, CurrencyAPI)
  - **Smart averaging**: Calculates average rate from all successful API sources
  - **Transparent pricing**: Expandable details showing individual rates from each source
  - **Color-coded status**: Green checkmarks for successful fetches, red X for failures
  - Support for USD, CAD, MXN, and COP
  - Historical date selection
  - Multi-currency conversion
  - 10-minute caching to optimize API usage
  - See [EXCHANGE_RATE_SETUP.md](EXCHANGE_RATE_SETUP.md) for API key setup instructions

### Coming Soon
- Code Generator
- Task Manager
- Note Keeper
- Calculator
- Timer/Stopwatch

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 10 (.NET 10)
- **Frontend**: Vanilla JavaScript (ES6+), HTML5, CSS3
- **Authentication**: Microsoft Identity Web (Azure AD OAuth 2.0)
- **External APIs**: ExchangeRate-API, Open Exchange Rates, CurrencyAPI (for real-time rates)
- **Caching**: In-memory caching (IMemoryCache) for API rate optimization
- **Hosting**: Azure App Service
- **Infrastructure**: Azure Bicep templates
- **CI/CD**: GitHub Actions

## 🚀 Getting Started

### Prerequisites

- .NET 10 SDK
- Azure subscription (for authentication)
- Visual Studio 2022 or Visual Studio Code

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/AIPersonalAssistant.git
   cd AIPersonalAssistant
   ```

2. **Set up Azure AD App Registration**
   
   Run the automated setup script:
   ```powershell
   .\setup-azure-ad.ps1
   ```
   
   This will:
   - Create an Azure AD app registration
   - Configure redirect URIs for localhost
   - Generate client credentials
   - Save credentials to user secrets automatically

   Or manually register at [Azure Portal](https://portal.azure.com):
   - Go to Azure AD → App registrations → New registration
   - Name: `camilo-personal-assistant`
   - Supported accounts: Personal Microsoft accounts
   - Redirect URI: `https://localhost:7028/signin-microsoft`
   - Create client secret
   - Note Client ID and Client Secret

3. **Configure User Secrets** (if not using script)
   ```bash
   cd AIPersonalAssistant.Web
   
   # Azure AD credentials
   dotnet user-secrets set "AzureAd:ClientId" "YOUR_CLIENT_ID"
   dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_CLIENT_SECRET"
   
   # Exchange Rate API keys (get from EXCHANGE_RATE_SETUP.md)
   dotnet user-secrets set "ExchangeRateAPIs:ExchangeRateApi:ApiKey" "YOUR_EXCHANGERATE_API_KEY"
   dotnet user-secrets set "ExchangeRateAPIs:OpenExchangeRates:ApiKey" "YOUR_OPENEXCHANGERATES_API_KEY"
   dotnet user-secrets set "ExchangeRateAPIs:CurrencyApi:ApiKey" "YOUR_CURRENCYAPI_KEY"
   ```
   
   **Security Note:** User secrets are stored outside the project directory in:
   - Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
   - macOS/Linux: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

4. **Run the application**
   
   **Option A: Visual Studio**
   - Open `AIPersonalAssistant.sln`
   - Press F5 to run
   
   **Option B: Command Line**
   ```bash
   cd AIPersonalAssistant.Web
   dotnet run --urls "https://localhost:7028;http://localhost:5133"
   ```
   
   **Note**: If running from PowerShell fails due to firewall, run PowerShell as Administrator or use Visual Studio.

5. **Access the application**
   - Navigate to `https://localhost:7028`
   - Click "Sign in with Microsoft"
   - Authenticate with your Outlook.com or Microsoft account
   - Access the tools dashboard

## 📁 Project Structure

```
AIPersonalAssistant/
├── AIPersonalAssistant.Web/          # Main web application
│   ├── Controllers/                  # API controllers
│   │   ├── AuthController.cs         # Authentication endpoints (login, logout, user info)
│   │   ├── ToolsController.cs        # Tools listing API
│   │   └── RateExchangeController.cs # Currency conversion API with multi-source integration
│   ├── Services/                     # Business logic services
│   │   ├── IExchangeRateService.cs   # Exchange rate service interface
│   │   └── ExchangeRateService.cs    # Multi-API exchange rate implementation
│   ├── Models/                       # Data models
│   │   └── ExchangeRateModels.cs     # Exchange rate DTOs and response models
│   ├── wwwroot/                      # Static files
│   │   ├── css/
│   │   │   ├── style.css             # Global styles + Microsoft auth button
│   │   │   └── rate-exchange.css     # Currency converter styles with expandable details
│   │   ├── js/
│   │   │   ├── app.js                # Authentication checking & tools loading
│   │   │   ├── login.js              # OAuth redirect handler
│   │   │   └── rate-exchange.js      # Currency converter with source breakdown UI
│   │   ├── login.html                # Login page with Microsoft sign-in
│   │   ├── tools.html                # Protected tools dashboard
│   │   └── rate-exchange.html        # Protected currency converter tool
│   ├── Program.cs                    # ASP.NET Core startup & auth configuration
│   ├── appsettings.json              # App configuration (includes API key placeholders)
│   └── appsettings.Development.json  # Development-specific settings
├── AIPersonalAssistant.Tests/        # xUnit test project
├── infrastructure/                    # Azure infrastructure as code
│   ├── main.bicep                    # App Service & Plan definitions
│   └── parameters.json               # Bicep deployment parameters
├── .github/workflows/
│   └── deploy.yml                    # CI/CD pipeline (build, test, deploy)
├── setup-azure-ad.ps1                # Automated Azure AD app registration
├── EXCHANGE_RATE_SETUP.md            # Exchange rate API setup instructions
└── README.md
```

## 🏗️ Architecture Overview

### Backend (ASP.NET Core)

**Program.cs**
- Configures Microsoft Identity authentication with OpenID Connect
- Registers authentication and authorization services
- Applies middleware for HTTPS redirection, static files, auth, and routing
- Uses cookie-based authentication for web apps

**Controllers**

1. **AuthController.cs**
   - `GET /api/auth/login` - Initiates OAuth challenge, redirects to Microsoft login
   - `POST /api/auth/logout` - Signs out user and redirects to login page
   - `GET /api/auth/user` - Returns authenticated user info (email, name)

2. **ToolsController.cs** (Protected with `[Authorize]`)
   - `GET /api/tools` - Returns list of available tools with metadata
   - Each tool has: id, name, description, icon, route, category, color

3. **RateExchangeController.cs** (Protected with `[Authorize]`)
   - `POST /api/rateexchange/convert` - Converts currencies using real-time rates
   - Integrates with ExchangeRateService for multi-source data
   - Returns aggregated rates with individual source breakdown
   - Includes error handling for API failures

### Frontend (Vanilla JavaScript)

**login.html + login.js**
- Displays Microsoft sign-in button with brand styling
- Redirects to `/api/auth/login` on button click
- OAuth flow handles the rest automatically

**tools.html + app.js**
- Checks authentication status via `/api/auth/user` on page load
- Redirects to login if not authenticated
- Fetches and displays available tools dynamically
- Shows user email in header
- Logout button calls `/api/auth/logout`

**rate-exchange.html + rate-exchange.js**
- Protected tool requiring authentication
- Real-time currency conversion with multi-source data
- Expandable details showing individual API source rates
- Color-coded status indicators (✓ success / ✗ failed)
- Smart averaging from successful sources
- 10-minute caching for performance
- Handles form submission and API errors
- Includes 401 error handling (redirects to login)

### Authentication Flow

1. User visits `/login.html`
2. Clicks "Sign in with Microsoft"
3. Redirected to `/api/auth/login` → Microsoft login page
4. User authenticates with Microsoft account (Outlook.com, Hotmail, etc.)
5. Microsoft redirects to `/signin-microsoft` with auth code
6. ASP.NET Core exchanges code for tokens, creates auth cookie
7. User redirected to `/tools.html`
8. Subsequent API calls include auth cookie automatically
9. `[Authorize]` attribute validates cookie on protected endpoints

### Deployment Architecture

**Azure Resources:**
- App Service Plan (B1 Linux)
- App Service (camilo-personal-assistant)
- Configuration stored in App Service settings (not in code)

**CI/CD Pipeline:**
1. **Build Job**: Restore, build, test, publish, upload artifact
2. **Deploy Job**: Download artifact, authenticate to Azure, deploy via ZIP

**Infrastructure:** Managed via Bicep templates in `infrastructure/` folder

## 🔒 Security

- OAuth 2.0 with OpenID Connect for authentication
- HTTPS enforced in production
- Secure cookie-based session management
- API endpoints protected with `[Authorize]` attribute
- **API keys stored securely:**
  - **Local Development**: User Secrets (not committed to Git)
  - **Production**: Azure App Service Configuration (Environment Variables)
  - Never hardcoded in source code
- Client secrets stored in Azure Key Vault (production)
- User secrets for local development (not committed)
- Sensitive configuration files excluded via .gitignore

## 🚢 Deployment

### Deploy to Azure

The application uses GitHub Actions for CI/CD.

**Automatic Deployment:**
- Push to `main` branch triggers CI build and tests
- Manual deployment workflow available in GitHub Actions

**Manual Deployment:**
```bash
# From GitHub Actions tab
# Select "Deploy to Azure" workflow
# Click "Run workflow"
```

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed deployment instructions.

## 🧪 Testing

Run unit tests:
```bash
dotnet test
```

## 📝 Configuration

### Required App Settings

**Development** (`appsettings.Development.json` or User Secrets):
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "CallbackPath": "/signin-microsoft",
    "SignedOutCallbackPath": "/signout-callback-microsoft"
  },
  "ExchangeRateAPIs": {
    "ExchangeRateApi": {
      "ApiKey": "YOUR_EXCHANGERATE_API_KEY"
    },
    "OpenExchangeRates": {
      "ApiKey": "YOUR_OPENEXCHANGERATES_API_KEY"
    },
    "CurrencyApi": {
      "ApiKey": "YOUR_CURRENCYAPI_KEY"
    }
  }
}
```

**Production** (Azure App Service Configuration):
- Store `ClientSecret` in Azure Key Vault
- Store API keys in Azure App Service Configuration
- Reference via App Settings in Azure Portal

**Exchange Rate API Keys:**
See [EXCHANGE_RATE_SETUP.md](EXCHANGE_RATE_SETUP.md) for detailed instructions on obtaining free API keys.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is private and proprietary.

## 🐛 Troubleshooting

**Issue: App won't run from PowerShell**
- Solution: Run PowerShell as Administrator or use Visual Studio

**Issue: Certificate not trusted**
- Solution: `dotnet dev-certs https --trust`

**Issue: Authentication fails**
- Check Azure AD app redirect URIs match your local URLs
- Verify Client ID and Secret are correct
- Ensure user secrets are configured

**Issue: 401 Unauthorized on API calls**
- Verify you're signed in
- Check browser dev tools for cookie issues
- Clear browser cache and try again

## 📧 Contact

For questions or support, contact: juan_camilo_r@hotmail.com

