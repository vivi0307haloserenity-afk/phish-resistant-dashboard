# Entra Verified ID Admin Dashboard

A comprehensive web application for managing Microsoft Entra Verified ID through an intuitive dashboard interface. Built with ASP.NET Core 8.0 and integrated with Microsoft Identity for secure authentication.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![Azure](https://img.shields.io/badge/Azure-Entra%20ID-0078D4?style=flat&logo=microsoft-azure)
![License](https://img.shields.io/badge/License-MIT-green.svg)

## � Deploy to Azure

Complete the [setup](#-getting-started) before deploying to Azure so that you have all the required parameters.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Frogulati%2Fentra-verifiedid-admin-dashboard%2Fmain%2FVIDAdminDashboard%2FARMTemplate%2Ftemplate.json)

You will be asked to enter the following parameters during deployment:

| Parameter | Description |
|-----------|-------------|
| **Web App Name** | Unique name for your Azure App Service (will be part of URL) |
| **Tenant Id** | Your Microsoft Entra ID tenant ID (GUID) |
| **Domain** | Your tenant domain (e.g., `yourtenant.onmicrosoft.com`) |
| **Client Id** | Application (client) ID from your app registration |
| **Client Secret** | Client secret from your app registration |
| **Verified Id Scope** | API scope (default: `6a8b4b39-c021-437c-b060-5a14a3fd65f3/full_access`) |

![Deployment Parameters](ReadmeFiles/DeployToAzure.png)

After deployment, update your app registration with the redirect URI:
- `https://<your-app-name>.azurewebsites.net/signin-oidc`

## �📋 Table of Contents

- [Features](#-features)
- [Architecture](#-architecture)
- [Prerequisites](#-prerequisites)
- [Getting Started](#-getting-started)
  - [Azure AD App Registration](#azure-ad-app-registration)
  - [Configuration](#configuration)
  - [Running Locally](#running-locally)
- [Deployment](#-deployment)
- [API Reference](#-api-reference)
- [Project Structure](#-project-structure)
- [Troubleshooting](#-troubleshooting)
- [Contributing](#-contributing)

## ✨ Features

### Authority Management
- **List Authorities** - View all Verified ID authorities in your tenant
- **Create Authority** - Set up new authorities with custom DID methods
- **Update Authority** - Modify authority display names and settings
- **Delete Authority** - Remove authorities (beta API)
- **Generate DID Document** - Create DID documents for authorities
- **Rotate Signing Keys** - Rotate cryptographic keys for security
- **Validate Domain Configuration** - Verify well-known DID configuration
- **Sync DID Document** - Synchronize authorities with their DID documents

### Contract Management
- **List Contracts** - View all credential contracts per authority
- **View Contract Details** - Inspect contract rules, attestations, and display settings
- **Edit Contracts** - Modify contract configurations via modal editor
- **Create Contracts** - Define new verifiable credential types with full wizard
- **Linked Domain Verification** - Configure and validate linked domains

### Credential Operations
- **Get Credential** - Retrieve specific credentials by ID
- **Search Credentials** - Find credentials using indexed claim values
- **Revoke Credentials** - Revoke issued credentials

### Activity Dashboard
- **Authorities Overview** - Summary table of all authorities with status
- **Transaction Reports** - View issuance and presentation transactions from VID Admin API
- **Audit Logs** - Detailed audit log entries from Microsoft Graph API
- **Summary Statistics** - Total transactions, success/failure rates
- **Visual Charts** - Transactions by credential type and over time
- **Date Range Filtering** - Filter activity by custom date ranges

### Admin Audit Actions
- **View Admin Activities** - Track administrative actions on authorities and contracts
- **Export Actions** - Export audit trails to CSV for compliance
- **Filter by Date Range** - Query actions within specific time periods

### FaceCheck Management
- **View FaceCheck Status** - Check if FaceCheck is enabled for the tenant
- **Toggle FaceCheck** - Enable or disable biometric verification
- **Real-time Status Updates** - See current configuration state

### Issue Notification Settings
- **Configure Notifications** - Set up email notifications for credential issuance
- **Per-Contract Settings** - Configure notification preferences per contract
- **Template Customization** - Customize notification content

## 🏗 Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Entra Verified ID Admin Dashboard            │
├─────────────────────────────────────────────────────────────────┤
│  ASP.NET Core 8.0 MVC                                           │
│  ├── Controllers (Home, Authority, Contract, Credential,        │
│  │               Activity, AuditActions, FaceCheck)             │
│  ├── Services (VerifiedIdService)                               │
│  └── Views (Razor Views with Bootstrap 5)                       │
├─────────────────────────────────────────────────────────────────┤
│  Microsoft.Identity.Web (MSAL)                                  │
│  └── OpenID Connect Authentication                              │
├─────────────────────────────────────────────────────────────────┤
│                         APIs                                    │
│  ├── Verified ID Admin API (verifiedid.did.msidentity.com)     │
│  │   ├── Authorities, Contracts, Credentials                   │
│  │   └── Transactions (/beta/.../{authorityId}/transactions)   │
│  ├── Microsoft Graph API (graph.microsoft.com)                  │
│  │   └── Audit Logs (/v1.0/auditLogs/directoryAudits)          │
│  └── Azure Resource Manager (management.azure.com)              │
│      └── FaceCheck Configuration                                │
└─────────────────────────────────────────────────────────────────┘
```

## 📋 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Subscription](https://azure.microsoft.com/free/)
- [Microsoft Entra ID Tenant](https://learn.microsoft.com/en-us/entra/fundamentals/create-new-tenant)
- [Verified ID Service](https://learn.microsoft.com/en-us/entra/verified-id/verifiable-credentials-configure-tenant) configured in your tenant
- User with **Authentication Policy Administrator** role

## 🚀 Getting Started

### Azure AD App Registration

1. **Create App Registration**
   - Go to [Azure Portal](https://portal.azure.com) → Microsoft Entra ID → App registrations
   - Click **New registration**
   - Name: `Verified ID Admin Dashboard`
   - Supported account types: **Accounts in this organizational directory only**
   - Redirect URI: **Web** → `https://localhost:5001/signin-oidc` (or your deployment URL)
   - Click **Register**

2. **Configure Authentication**
   - Go to **Authentication**
   - Add redirect URI: `http://localhost:5000/signin-oidc` (for local development)
   - Add Front-channel logout URL: `https://localhost:5001/signout-callback-oidc`
   - Enable **ID tokens** under Implicit grant and hybrid flows
   - Click **Save**

3. **Create Client Secret**
   - Go to **Certificates & secrets**
   - Click **New client secret**
   - Add a description and expiration
   - **Copy the secret value immediately** (it won't be shown again)

4. **Add API Permissions**
   - Go to **API permissions**
   - Click **Add a permission**
   
   **For Verified ID Admin API:**
   - Select **APIs my organization uses**
   - Search for `Verifiable Credentials Service Admin`
   - Select **Delegated permissions**
   - Check `full_access`
   - Click **Add permissions**
   
   **For Microsoft Graph (Audit Logs):**
   - Click **Add a permission** → **Microsoft Graph** → **Delegated permissions**
   - Add: `User.Read`
   - Add: `AuditLog.Read.All` (for audit logs)
   - (Optional) Add: `Reports.Read.All` for extended reporting
   
   **For Face Check Management (ARM API):**
   - Click **Add a permission** → **APIs my organization uses**
   - Search for `Azure Service Management` (or `Windows Azure Service Management API`)
   - Select **Delegated permissions**
   - Check `user_impersonation`
   - Click **Add permissions**
   
   - Click **Grant admin consent for [Your Tenant]**

5. **Assign Azure RBAC Role for Face Check**
   - The **user** (not the app) must have **Contributor** role on the subscription or resource group
   - Go to Azure Portal → Subscriptions → Your Subscription → Access control (IAM)
   - Click **Add** → **Add role assignment**
   - Select **Contributor** role
   - Assign access to: **User, group, or service principal**
   - Select the users who will manage Face Check
   - Click **Save**

6. **Note Your Values**
   - **Application (client) ID**: Found on Overview page
   - **Directory (tenant) ID**: Found on Overview page
   - **Client Secret**: Created in step 3

### Configuration

1. **Update appsettings.json**

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "your-tenant-id-guid",
    "ClientId": "your-client-id-guid",
    "ClientSecret": "your-client-secret",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "Graph": {
    "BaseUrl": "https://graph.microsoft.com/beta",
    "Scopes": [ "https://graph.microsoft.com/.default" ]
  },
  "VerifiedId": {
    "BaseUrl": "https://verifiedid.did.msidentity.com",
    "Scope": "6a8b4b39-c021-437c-b060-5a14a3fd65f3/full_access"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

2. **For Production**: Use Azure Key Vault or User Secrets for sensitive values:

```bash
# Using .NET User Secrets (development)
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret-here"
```

### Running Locally

```bash
# Clone the repository
git clone https://github.com/rogulati/entra-verifiedid-admin-dashboard.git
cd entra-verifiedid-admin-dashboard/VIDAdminDashboard

# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run

# Or run with hot reload
dotnet watch run
```

The application will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

## 📦 Deployment

### Option 1: Deploy using ARM Template (Recommended)

The easiest way to deploy is using the **Deploy to Azure** button at the top of this README. This creates an Azure App Service on the Free tier with all required configuration.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Frogulati%2Fentra-verifiedid-admin-dashboard%2Fmain%2FVIDAdminDashboard%2FARMTemplate%2Ftemplate.json)

After deployment:
1. Go to your App Service in Azure Portal
2. Copy the URL (e.g., `https://your-app-name.azurewebsites.net`)
3. Update your App Registration with the redirect URI: `https://your-app-name.azurewebsites.net/signin-oidc`
4. Grant admin consent for API permissions if not already done

### Option 2: Deploy using Azure CLI

1. **Create Azure App Service**

```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-vid-admin --location eastus

# Create App Service plan (Free tier)
az appservice plan create --name asp-vid-admin --resource-group rg-vid-admin --sku F1

# Create Web App
az webapp create --name vid-admin-dashboard --resource-group rg-vid-admin --plan asp-vid-admin --runtime "DOTNET|8.0"
```

2. **Configure App Settings**

```bash
az webapp config appsettings set --name vid-admin-dashboard --resource-group rg-vid-admin --settings \
  AzureAd__TenantId="your-tenant-id" \
  AzureAd__ClientId="your-client-id" \
  AzureAd__ClientSecret="your-client-secret" \
  AzureAd__Domain="yourtenant.onmicrosoft.com" \
  AzureAd__Instance="https://login.microsoftonline.com/" \
  AzureAd__CallbackPath="/signin-oidc" \
  AzureAd__SignedOutCallbackPath="/signout-callback-oidc" \
  Graph__BaseUrl="https://graph.microsoft.com/beta" \
  VerifiedId__BaseUrl="https://verifiedid.did.msidentity.com" \
  VerifiedId__Scope="6a8b4b39-c021-437c-b060-5a14a3fd65f3/full_access"
```

3. **Deploy the Application**

```bash
# Publish the application
dotnet publish -c Release -o ./publish

# Create ZIP file
cd publish && zip -r ../publish.zip . && cd ..

# Deploy using ZIP deploy
az webapp deployment source config-zip --name vid-admin-dashboard --resource-group rg-vid-admin --src ./publish.zip
```

4. **Update App Registration**
   - Add your App Service URL to redirect URIs: `https://vid-admin-dashboard.azurewebsites.net/signin-oidc`

### Option 3: Deploy using Visual Studio

1. Right-click the project → **Publish**
2. Select **Azure** → **Azure App Service (Windows/Linux)**
3. Sign in and select/create your App Service
4. Configure settings and click **Publish**

### Option 4: Deploy using GitHub Actions

Create `.github/workflows/azure-deploy.yml`:

```yaml
name: Deploy to Azure App Service

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build and publish
      run: |
        dotnet restore
        dotnet build --configuration Release
        dotnet publish -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'vid-admin-dashboard'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

## 📚 API Reference

### Verified ID Admin API

Base URL: `https://verifiedid.did.msidentity.com`

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/v1.0/verifiableCredentials/authorities` | GET | List all authorities |
| `/v1.0/verifiableCredentials/authorities` | POST | Create authority |
| `/v1.0/verifiableCredentials/authorities/{id}` | GET | Get authority |
| `/v1.0/verifiableCredentials/authorities/{id}` | PATCH | Update authority |
| `/beta/verifiableCredentials/authorities/{id}` | DELETE | Delete authority |
| `/v1.0/verifiableCredentials/authorities/{id}/contracts` | GET | List contracts |
| `/v1.0/verifiableCredentials/authorities/{id}/contracts/{contractId}` | GET | Get contract |
| `/v1.0/verifiableCredentials/authorities/{id}/contracts/{contractId}` | PATCH | Update contract |
| `/v1.0/verifiableCredentials/authorities/{id}/contracts` | POST | Create contract |
| `/v1.0/verifiableCredentials/authorities/{id}/contracts/{contractId}/credentials/{credId}` | GET | Get credential |
| `/v1.0/verifiableCredentials/authorities/{id}/contracts/{contractId}/credentials/{credId}/revoke` | POST | Revoke credential |
| `/beta/verifiableCredentials/authorities/{id}/transactions` | GET | List transactions (requires `from` and `to` query params) |
| `/v1.0/verifiableCredentials/authorities/{id}/didInfo/signingKeys/rotate` | POST | Rotate signing keys |
| `/v1.0/verifiableCredentials/authorities/{id}/didInfo/generateDidDocument` | POST | Generate DID document |
| `/v1.0/verifiableCredentials/authorities/{id}/validateWellKnownDidConfiguration` | POST | Validate domain configuration |

### Microsoft Graph API (Audit Logs)

Base URL: `https://graph.microsoft.com/v1.0`

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/auditLogs/directoryAudits` | GET | List audit log entries |

**Query Parameters for Audit Logs:**
- `$filter` - Filter by `loggedByService eq 'Verified ID'` and date range
- `$top` - Limit number of results
- `$orderby` - Sort by `activityDateTime desc`

**Example:**
```
GET /v1.0/auditLogs/directoryAudits?$filter=loggedByService eq 'Verified ID' and activityDateTime ge 2026-01-01T00:00:00Z&$top=100&$orderby=activityDateTime desc
```

### Azure Resource Manager API (FaceCheck)

Base URL: `https://management.azure.com`

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/subscriptions/{subId}/resourcegroups/{rg}/providers/Microsoft.VerifiedId/authorities/{authorityId}` | GET | Get FaceCheck status |
| `/subscriptions/{subId}/resourcegroups/{rg}/providers/Microsoft.VerifiedId/authorities/{authorityId}` | PUT | Update FaceCheck settings |

## 📁 Project Structure

```
VIDAdminDashboard/
├── Controllers/
│   ├── HomeController.cs          # Main dashboard
│   ├── AuthorityController.cs     # Authority CRUD operations
│   ├── ContractController.cs      # Contract operations
│   ├── CredentialController.cs    # Credential operations
│   ├── ActivityController.cs      # Activity dashboard & transactions
│   ├── AuditActionsController.cs  # Admin audit actions tracking
│   └── FaceCheckController.cs     # FaceCheck management
├── Models/
│   ├── Authority.cs               # Authority model & related types
│   ├── Contract.cs                # Contract model & attestations
│   ├── Credential.cs              # Credential model
│   ├── ViewModels.cs              # Dashboard view models
│   ├── ActivityModels.cs          # Activity/transaction models
│   └── FaceCheckModels.cs         # FaceCheck configuration models
├── Services/
│   ├── IVerifiedIdService.cs      # Service interface
│   └── VerifiedIdService.cs       # API client implementation
├── Views/
│   ├── Home/
│   │   └── Index.cshtml           # Main admin dashboard
│   ├── Activity/
│   │   └── Index.cshtml           # Activity dashboard
│   ├── AuditActions/
│   │   └── Index.cshtml           # Admin audit actions
│   ├── FaceCheck/
│   │   └── Index.cshtml           # FaceCheck management
│   └── Shared/
│       ├── _Layout.cshtml         # Main layout
│       └── Error.cshtml           # Error page
├── wwwroot/                        # Static files
├── Program.cs                      # Application entry point
├── appsettings.json               # Configuration
└── VIDAdminDashboard.csproj       # Project file
```

## 🔧 Troubleshooting

### Common Issues

#### 1. "AADSTS65001: The user or administrator has not consented to use the application"

**Solution**: Grant admin consent for API permissions in Azure Portal:
- App registrations → Your app → API permissions → Grant admin consent

#### 2. "MsalUiRequiredException: No account or login hint was passed"

**Solution**: The `[AuthorizeForScopes]` attribute handles this automatically. Ensure it's applied to controller actions:

```csharp
[AuthorizeForScopes(Scopes = new[] { "6a8b4b39-c021-437c-b060-5a14a3fd65f3/full_access" })]
public async Task<IActionResult> Index()
```

#### 3. "AADSTS28000: Provided value for the input parameter scope is not valid because it contains more than one resource"

**Solution**: OAuth2 doesn't allow requesting tokens for multiple resources in one request. Acquire tokens separately for each API.

#### 4. "The JSON value could not be converted to List&lt;SelfIssuedAttestation&gt;"

**Solution**: This is a deserialization issue. Ensure models match the API response structure. The `selfIssued` field should be a single object, not a list.

#### 5. Activity Dashboard shows no data

**Possible causes**:
- Missing Graph API permissions for audit logs
- Missing VID Admin API scope for transactions
- No transactions or activities in the selected date range

**Solution**: 
- Verify you have `AuditLog.Read.All` Graph permission for audit logs
- Verify you have `6a8b4b39-c021-437c-b060-5a14a3fd65f3/.default` scope for transactions
- Try expanding the date range

#### 6. FaceCheck toggle not working

**Possible causes**:
- User doesn't have Contributor role on the Azure subscription
- Azure Resource Manager API permission (`user_impersonation`) not granted

**Solution**:
- Assign Contributor role to the user on the subscription or resource group
- Add `Azure Service Management` → `user_impersonation` API permission

#### 7. "Application is not authorized to perform operation" for Graph API

**Possible causes**:
- Missing required Graph API permissions
- Admin consent not granted

**Solution**: 
- Add required permissions in Azure Portal
- Click "Grant admin consent" for the tenant

### Debugging Tips

1. **Enable detailed logging** in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.Identity": "Debug"
    }
  }
}
```

2. **Check token claims** using [jwt.ms](https://jwt.ms) to decode access tokens

3. **Test API calls directly** using tools like Postman or curl with a valid token

## 🔐 Security Considerations

1. **Never commit secrets** - Use User Secrets, Azure Key Vault, or environment variables
2. **Use HTTPS** in production
3. **Implement proper RBAC** - Only users with appropriate roles should access admin functions
4. **Audit logging** - Consider adding application-level logging for audit trails
5. **Token caching** - For production, use distributed token caching (Redis)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📞 Support

- [Microsoft Entra Verified ID Documentation](https://learn.microsoft.com/en-us/entra/verified-id/)
- [Verified ID Admin API Reference](https://learn.microsoft.com/en-us/entra/verified-id/admin-api)
- [Microsoft Identity Web Documentation](https://learn.microsoft.com/en-us/entra/msal/dotnet/microsoft-identity-web/)

## 📝 Changelog

### v1.1.0 (February 2026)
- **Activity Dashboard** - Now uses VID Admin API `/beta/verifiableCredentials/authorities/{id}/transactions` endpoint
- **Audit Logs** - Added Microsoft Graph audit logs integration
- **FaceCheck Management** - Toggle biometric verification via Azure Resource Manager API
- **Admin Audit Actions** - Track and export administrative actions
- **Create Contract Wizard** - Full contract creation with attestation configuration
- **Issue Notifications** - Configure email notifications per contract
- **Improved Error Handling** - Better error messages and troubleshooting info

### v1.0.0 (February 2026)
- Initial release
- Authority management (CRUD operations)
- Contract management with modal editor
- Credential operations (Get, Search, Revoke)
- Activity Dashboard with transaction reports
- Bootstrap 5 responsive UI
- Chart.js visualizations
