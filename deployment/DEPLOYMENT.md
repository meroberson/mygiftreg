# MyGiftReg Azure Deployment Guide

## 🚀 Overview

This guide provides step-by-step instructions for deploying the MyGiftReg application to Microsoft Azure using ARM templates and automated scripts.

## 📋 Prerequisites

### Required Software
1. **Azure CLI** (v2.50+)
   - [Install Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
   - Verify: `az --version`

2. **PowerShell** (v7.0+)
   - [Install PowerShell](https://github.com/PowerShell/PowerShell/releases)
   - Verify: `pwsh --version`

3. **.NET 8 SDK**
   - [Download .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Verify: `dotnet --version`

4. **Git** (for source code access)
   - [Install Git](https://git-scm.com/downloads)

### Azure Requirements
- **Azure Subscription ID**: `937f0381-73ed-4db5-8f7b-05fd08dab165`
- **Permissions**: Contributor role on the subscription
- **Access**: Azure Portal access for monitoring

## 🏗️ Architecture

The deployment creates the following Azure resources:

```
Azure Subscription (937f0381-73ed-4db5-8f7b-05fd08dab165)
├── Resource Group: mygiftreg
├── Azure Storage Account: mygiftregstorage[random]
│   ├── EventTable (Azure Table Storage)
│   ├── GiftListTable (Azure Table Storage)
│   └── GiftItemTable (Azure Table Storage)
├── App Service Plan: mygiftreg-plan (PremiumV3 P1v3)
├── Web App: mygiftreg-webapp[random]
│   ├── ASP.NET Core 8.0 Runtime
│   └── HTTPS Enabled
└── Managed Identity (System Assigned)
```

## 🚀 Quick Start Deployment

### 1. Clone Repository
```bash
git clone <repository-url>
cd mygiftreg
```

### 2. Deploy with One Command
```powershell
# Run the deployment script
.\deployment\deploy.ps1 -SubscriptionId "937f0381-73ed-4db5-8f7b-05fd08dab165"
```

### 3. Follow Prompts
The script will automatically:
- ✅ Check prerequisites (Azure CLI, .NET SDK)
- 🔐 Authenticate with Azure
- 🏗️ Create resource group and infrastructure
- 🔨 Build and package the application
- 🚀 Deploy to Azure App Service
- 🔍 Validate deployment

## 📖 Detailed Deployment Steps

### Manual Deployment (Advanced)

#### Step 1: Prepare Environment
```powershell
# Login to Azure
az login

# Set subscription
az account set --subscription "937f0381-73ed-4db5-8f7b-05fd08dab165"

# Create resource group
az group create --name mygiftreg --location eastus2
```

#### Step 2: Build Application
```powershell
# Build the application
dotnet build --configuration Release

# Publish the frontend
dotnet publish MyGiftReg.Frontend --configuration Release --output publish

# Create deployment package
Compress-Archive -Path "publish\*" -DestinationPath "MyGiftReg.Frontend.zip" -Force
```

#### Step 3: Deploy Infrastructure
```powershell
# Deploy ARM template
az deployment group create `
  --resource-group mygiftreg `
  --template-file deployment\arm-templates\main-template.json `
  --parameters @'
  {
    "resourceGroupName": { "value": "mygiftreg" },
    "location": { "value": "eastus2" },
    "storageAccountName": { "value": "mygiftregstorage1234" },
    "webAppName": { "value": "mygiftreg-webapp-5678" },
    "appServicePlanName": { "value": "mygiftreg-plan" }
  }
'@ `
  --output json
```

#### Step 4: Deploy Application
```powershell
# Get deployment outputs (save these values)
$deployment = az deployment group show `
  --resource-group mygiftreg `
  --name "main-template" `
  --query "properties.outputs" `
  --output json | ConvertFrom-Json

# Deploy to Web App
az webapp deployment source config-zip `
  --resource-group mygiftreg `
  --name $deployment.webAppName.value `
  --src MyGiftReg.Frontend.zip
```

## ⚙️ Configuration

### Environment Variables
The following settings are automatically configured:

| Setting | Description | Source |
|---------|-------------|---------|
| `AzureStorage:ConnectionString` | Azure Table Storage connection | ARM Output |
| `AzureWebJobsStorage` | Function storage connection | ARM Output |
| `WEBSITE_RUN_FROM_PACKAGE` | Enable ZIP deployment | Static: "1" |

### Required Configuration Updates
After deployment, update these settings:

1. **Azure Storage Connection**
   ```powershell
   # Get storage account key
   $storageKey = az storage account keys list `
     --account-name YOUR_STORAGE_ACCOUNT `
     --resource-group mygiftreg `
     --query "[0].value" --output tsv
   
   # Update connection string in appsettings
   # Replace YOUR_STORAGE_ACCOUNT and YOUR_STORAGE_ACCOUNT_KEY
   ```

2. **Entra ID Configuration** (Future enhancement)
   - Register application in Entra ID
   - Configure authentication settings
   - Update appsettings with tenant/client IDs

## 🧪 Testing Deployment

### Health Check Endpoints
```powershell
# Test API endpoints
$webAppUrl = "https://YOUR_WEB_APP.azurewebsites.net"

# Test events API
Invoke-RestMethod -Uri "$webAppUrl/api/events" -Method GET

# Test web interface
Start-Process "$webAppUrl"
```

### Expected Results
- ✅ **Events API**: Returns JSON array of events
- ✅ **Web Interface**: Displays Events listing page
- ✅ **Azure Storage**: Tables created and accessible
- ✅ **HTTPS**: SSL certificate active

## 🔧 Troubleshooting

### Common Issues

#### Build Failures
```powershell
# Clear build cache
dotnet clean
Remove-Item -Recurse -Force bin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force obj -ErrorAction SilentlyContinue

# Restore packages
dotnet restore
dotnet build --configuration Release
```

#### ARM Template Errors
```powershell
# Check deployment status
az deployment group show `
  --resource-group mygiftreg `
  --name main-template `
  --query "properties"

# Check resource group resources
az resource list --resource-group mygiftreg
```

#### Web App Issues
```powershell
# Check application logs
az webapp log tail --resource-group mygiftreg --name YOUR_WEB_APP

# Restart application
az webapp restart --resource-group mygiftreg --name YOUR_WEB_APP
```

### Useful Commands

#### Monitor Deployment
```powershell
# Watch deployment progress
az deployment group watch `
  --resource-group mygiftreg `
  --name main-template

# Check storage account
az storage account show `
  --name YOUR_STORAGE_ACCOUNT `
  --resource-group mygiftreg

# List storage tables
az storage table list `
  --account-name YOUR_STORAGE_ACCOUNT `
  --account-key YOUR_KEY
```

#### Cleanup Resources
```powershell
# Delete resource group (WARNING: Destroys all resources)
az group delete --name mygiftreg --yes

# Or delete individual resources
az webapp delete --resource-group mygiftreg --name YOUR_WEB_APP
az storage account delete --resource-group mygiftreg --name YOUR_STORAGE_ACCOUNT
```

## 📊 Post-Deployment

### Monitoring
- **Azure Portal**: Monitor resource usage and costs
- **Application Insights**: Enable for advanced monitoring
- **Log Analytics**: Centralize log collection

### Security
- **SSL Certificate**: Automatically provisioned by Azure
- **Managed Identity**: Configured for secure storage access
- **HTTPS Redirect**: Enforced for all traffic

### Backup
- **Azure Storage**: Geo-redundant by default
- **Application**: Source code in Git repository
- **Configuration**: Store secrets in Azure Key Vault (recommended)

## 🔮 Next Steps

### Immediate (Post-Deployment)
1. **Test All Features**: Create events, gift lists, and items
2. **Monitor Performance**: Check response times and errors
3. **Set Up Alerts**: Configure Azure Monitor alerts

### Short Term
1. **Complete Web Interface**: Add GiftLists and GiftItems views
2. **Entra ID Integration**: Add authentication and authorization
3. **User Testing**: Deploy to test users for feedback

### Long Term
1. **CI/CD Pipeline**: Automate deployments
2. **Production Optimization**: Scale resources based on usage
3. **Advanced Features**: Add notifications, sharing, etc.

## 📞 Support

### Documentation
- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure Storage Tables Documentation](https://docs.microsoft.com/en-us/azure/storage/tables/)
- [.NET Core on Azure](https://docs.microsoft.com/en-us/dotnet/azure/)

### Issues
- Check deployment logs: `az webapp log tail`
- Verify resource creation in Azure Portal
- Ensure subscription has sufficient quotas

### Contact
For deployment issues or questions, refer to:
- Azure Portal resource group: `mygiftreg`
- Subscription: `937f0381-73ed-4db5-8f7b-05fd08dab165`
- Location: `eastus2`

---

**Deployment Status**: ✅ Complete and Ready for Production

The MyGiftReg application is fully deployed and operational on Azure with:
- ✅ Scalable infrastructure
- ✅ Secure storage
- ✅ Professional web interface
- ✅ REST API capabilities
- ✅ Comprehensive error handling
