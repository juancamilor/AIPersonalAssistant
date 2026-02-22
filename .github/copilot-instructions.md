# GitHub Copilot Instructions

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

### For Services

* Follow the existing dual-storage pattern: local JSON storage (dev) + Azure Blob Storage (prod)
* Current service pairs: ExchangeRate, Travel/TravelImage, Taxes, UserManagement, Wishes, Recipe/RecipeImage, Menopause
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

## Documentation Updates

* When pushing changes to GitHub, always update the documentation (README.md and related docs) to reflect the changes made.
* Update the Features section in README.md when adding new tools or features.
* Update the Project Structure section when adding new files or folders.
* Ensure API documentation is updated when adding new endpoints.
* Update setup documentation (e.g., STOCK_TOOLS_SETUP.md, EXCHANGE_RATE_SETUP.md) when adding new integrations.
* Always update documentation before committing/checking in code to keep everything up to date.
* Update this file (.github/copilot-instructions.md) when adding new controllers, services, or tools to keep the reference lists current.
