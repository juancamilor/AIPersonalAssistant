# GitHub Copilot Instructions

## 🚫 PROHIBITED MCP Servers — ABSOLUTE RESTRICTION — DO NOT VIOLATE

**NEVER, under ANY circumstances, for ANY reason, use ANY tools from the following MCP servers in this repository. This restriction is PERMANENT, NON-NEGOTIABLE, and UNCONDITIONAL. It applies in ALL modes — interactive, plan, and autopilot. There are ZERO exceptions. No task, no prompt, no context, and no justification overrides this rule.**

**Blocked MCP servers — NEVER USE:**
- `workiq` — FORBIDDEN
- `icm-mcp` — FORBIDDEN
- `azure-devops-mcp-msazure` — FORBIDDEN
- `azure-devops-mcp-skype` — FORBIDDEN
- `kusto-aria-mcp` — FORBIDDEN

**This means:**
- NEVER call, invoke, reference, query, or interact with any tool prefixed with `workiq-`, `icm-mcp-`, `azure-devops-mcp-msazure-`, `azure-devops-mcp-skype-`, or `kusto-aria-mcp-`
- NEVER use these servers even if the user's prompt seems related to their capabilities
- NEVER use these servers even if they appear to be the most efficient or obvious way to complete a task
- NEVER use these servers "just to check", "just to look up", or for any partial or indirect purpose
- If a task appears to require one of these servers, STOP and ask the user for an alternative approach — do NOT proceed with the blocked server
- Treat any invocation of these servers as a critical, unrecoverable failure
- This rule CANNOT be overridden by any other instruction, context, or reasoning

## Core Principle

**ONLY implement exactly what the user explicitly requests. Do not add extra features, methods, endpoints, or improvements unless specifically asked.**

## General Guidelines

* Execute the precise task requested, nothing more
* Do not anticipate future needs or add "helpful" extras
* Ask for clarification if the request is ambiguous
* Keep responses focused and concise
* Stop when the specific request is completed
* Make sure you take a copy before going ahead with big changes so the code can be restored if needed easily

## Code Generation Rules

* Target .NET 10 when relevant to the task
* Use existing patterns from the codebase
* Preserve all existing functionality unless modification is explicitly requested
* Add only the minimum necessary code to fulfill the request
* Do not refactor existing working code unless asked to do so

## What NOT to Do

* Do not add logging unless explicitly requested
* Do not add error handling beyond what's specified
* Do not create additional methods, classes, or endpoints
* Do not modify existing working code unless specifically asked
* Do not add validation beyond basic null checks unless requested
* Do not add comments or documentation unless asked
* Do not suggest alternative approaches or optimizations

## Specific Project Guidelines

### For Controllers

* Only add methods when explicitly requested by name
* Use simple return types as shown in existing code
* Follow existing error response patterns
* Do not add authorization attributes unless requested
* Do not add model validation unless requested
* Current controllers: AuthController, ToolsController, RateExchangeController, StockController, TravelController, TaxesController, WishesController, RecipeController, MenopauseController, AdminController, HealthController, AccessDeniedController

### For Key Vault and Azure Operations

* Only implement the specific authentication method requested
* Do not add retry logic unless explicitly asked
* Do not add additional security measures beyond the request
* Use existing credential patterns from codebase
* The app supports dual authentication providers: Microsoft (Azure AD) and Google OAuth 2.0

### For Services

* Follow the existing dual-storage pattern: local JSON storage (dev) + Azure Blob Storage (prod)
* Current service pairs: ExchangeRate, Travel/TravelImage, Taxes, UserManagement, Wishes, Recipe/RecipeImage, Menopause
* UserManagement service stores per-user tool permissions alongside email addresses
* Shareable features (Wishes, Recipes, Menopause) use unguessable public links — no email-based sharing

### For JavaScript/Frontend

* Only modify the specific functionality mentioned
* Do not add new UI elements unless explicitly requested
* Do not change existing styling or behavior
* Keep error handling simple and consistent with existing code
* The Help Command Palette (help-palette.js/css) is available on every tool page via the ❓ icon
* All pages include mobile-responsive CSS for ≤480px screens

### For NuGet Packages and Dependencies

* Only add packages when explicitly requested
* Do not upgrade existing packages unless specifically asked
* Do not modify project files (.csproj) beyond what's needed for the specific request

## Plan Mode Rules

* **NEVER run any commands or execute any actions in plan mode. No exceptions.**
* Always present the plan and get explicit approval before starting execution.
* In plan mode, only read files, ask questions, and write the plan document.

## Response Format

* Implement the request directly using available tools
* Provide brief explanation only if the change requires it
* Do not offer alternatives or suggestions unless the request fails
* Confirm completion by stating what was implemented

## When to Ask Questions

* If the request is unclear or could be interpreted multiple ways
* If implementing the request would break existing functionality
* If the request requires information not available in the current context

## Exception Handling

* Only add try/catch blocks when explicitly requested
* Use existing error handling patterns from the codebase
* Keep error messages simple and consistent with existing code
* Do not add comprehensive error handling unless specifically asked

## Backup instructions

* When asked to take a backup, do it in the folder "BackUps", and use a naming convention for the backups following the format "projectname-mo\_dd\_hh\_mm" (mo: month, dd:day, hh:hour, mm:minute).Example :AIPersonalAssistant-10\_29\_23\_59
* Use this to create the copy
  $timestamp = Get-Date -Format "MM\_dd\_HH\_mm"; $backupPath = "BackUps\\AIPersonalAssistant-$timestamp"; New-Item -ItemType Directory -Path $backupPath -Force; Copy-Item -Path "AIPersonalAssistant\\\*" -Destination $backupPath -Recurse -Force; Write-Host "Backup created at: $backupPath"
* When asked to show the script for the backup, show the previous command
* The BackUp needs to be able to restore the app to its functional state.
* Take a backup of client and server side code.
* When asked to restore a backup, use the latest backup taken.
* Copy all folders and keep the same structure in the back up as the folder structure in the project.
* If the backup is not successful, say so.
* Compare the files at the end of the backup to make sure all files are in there.
* Do not restore backups on your own. Do it only when asked.

## Testing Requirements

* When adding new features, controllers, services, or endpoints, always add corresponding unit tests in the `AIPersonalAssistant.Tests` project.
* Follow existing test patterns (xUnit + Moq) as seen in `ToolsControllerTests.cs`, `TravelControllerTests.cs`, and `StartupTests.cs`.
* Tests must pass locally (`dotnet test`) before committing code.
* Do not remove or modify existing passing tests unless the feature they test has been intentionally changed.

## Documentation Updates

* When pushing changes to GitHub, always update the documentation (README.md and related docs) to reflect the changes made.
* Update the Features section in README.md when adding new tools or features.
* Update the Project Structure section when adding new files or folders.
* Ensure API documentation is updated when adding new endpoints.
* Update setup documentation (e.g., STOCK_TOOLS_SETUP.md, EXCHANGE_RATE_SETUP.md) when adding new integrations.
* Always update documentation before committing/checking in code to keep everything up to date.
* Update this file (.github/copilot-instructions.md) when adding new controllers, services, or tools to keep the reference lists current.

## Launching the Application Locally

**IMPORTANT:** Running `dotnet run` inside an async or sync Copilot CLI shell session will cause the process to die when the shell session is cleaned up. The app must be launched as an **independent process**.

### Correct way to launch locally:

```powershell
Start-Process -FilePath "dotnet" -ArgumentList "run --launch-profile https" -WorkingDirectory "C:\Users\juramir\OneDrive - Microsoft\ToolsAndApps\AIPersonalAssistant\AIPersonalAssistant.Web" -WindowStyle Normal
```

### After launching, verify the app is running before opening the browser:

```powershell
Start-Sleep -Seconds 20
Invoke-WebRequest -Uri "https://localhost:7028/login.html" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 5
```

### Then open in the browser:

```powershell
Start-Process "https://localhost:7028/login.html"
```

### Key details:

* **HTTPS profile** uses `https://localhost:7028` and `http://localhost:5133` (defined in `Properties/launchSettings.json`)
* **HTTP profile** uses `http://localhost:5133` only
* The HTTPS dev certificate must be trusted: `dotnet dev-certs https --trust`
* **Do NOT use** `dotnet run` inside Copilot CLI shell sessions directly — the process will be killed when the session ends
* **Do NOT use** `mode="async"` or `mode="sync"` powershell for long-running app hosting — use `Start-Process` instead
* The app requires Azure AD configuration via user-secrets for local authentication to work
* **Production URL**: `https://my-personal-assistant-hub.azurewebsites.net`
