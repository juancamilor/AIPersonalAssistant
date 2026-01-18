# AI Personal Assistant

A modern web application for personal productivity with Microsoft Account authentication and various productivity tools.

## 🌟 Features

### Authentication
- **Microsoft Account Integration**: Secure OAuth 2.0 authentication using Azure AD
- Supports personal Microsoft accounts (Outlook.com, Hotmail, Live)
- Protected API endpoints with role-based authorization

### Tools
- **Rate Exchange**: Currency converter with real-time exchange rates
  - Support for USD, CAD, MXN, and COP
  - Historical date selection
  - Multi-currency conversion

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
   dotnet user-secrets set "AzureAd:ClientId" "YOUR_CLIENT_ID"
   dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_CLIENT_SECRET"
   ```

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
│   │   └── RateExchangeController.cs # Currency conversion API
│   ├── wwwroot/                      # Static files
│   │   ├── css/
│   │   │   └── style.css             # Global styles + Microsoft auth button
│   │   ├── js/
│   │   │   ├── app.js                # Authentication checking & tools loading
│   │   │   ├── login.js              # OAuth redirect handler
│   │   │   └── rate-exchange.js      # Currency converter logic
│   │   ├── login.html                # Login page with Microsoft sign-in
│   │   ├── tools.html                # Protected tools dashboard
│   │   └── rate-exchange.html        # Protected currency converter tool
│   ├── Program.cs                    # ASP.NET Core startup & auth configuration
│   ├── appsettings.json              # App configuration (no secrets)
│   └── appsettings.Development.json  # Development-specific settings
├── AIPersonalAssistant.Tests/        # xUnit test project
├── infrastructure/                    # Azure infrastructure as code
│   ├── main.bicep                    # App Service & Plan definitions
│   └── parameters.json               # Bicep deployment parameters
├── .github/workflows/
│   └── deploy.yml                    # CI/CD pipeline (build, test, deploy)
├── setup-azure-ad.ps1                # Automated Azure AD app registration
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
   - `GET /api/rate-exchange` - Returns supported currencies
   - `POST /api/rate-exchange/convert` - Converts amounts between currencies
   - Uses mock exchange rate data (ready for API integration)

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
- Fetches supported currencies from API
- Handles form submission for currency conversion
- Displays converted amounts with proper formatting
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
- Client secrets stored in Azure Key Vault (production)
- User secrets for local development (not committed)

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
  }
}
```

**Production** (Azure App Service Configuration):
- Store `ClientSecret` in Azure Key Vault
- Reference via App Settings in Azure Portal

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

