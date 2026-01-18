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
│   │   ├── AuthController.cs         # Authentication endpoints
│   │   ├── ToolsController.cs        # Tools listing
│   │   └── RateExchangeController.cs # Currency conversion
│   ├── wwwroot/                      # Static files
│   │   ├── css/                      # Stylesheets
│   │   ├── js/                       # JavaScript files
│   │   ├── login.html                # Login page
│   │   ├── tools.html                # Tools dashboard
│   │   └── rate-exchange.html        # Currency converter
│   ├── Program.cs                    # App configuration
│   └── appsettings.json              # Configuration
├── AIPersonalAssistant.Tests/        # Unit tests
├── infrastructure/                    # Azure Bicep templates
├── .github/workflows/                # CI/CD pipelines
├── setup-azure-ad.ps1                # Azure AD setup script
└── README.md
```

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

