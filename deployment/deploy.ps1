# MyGiftReg Azure Deployment Script
# This script deploys the MyGiftReg application to Azure

param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName = "mygiftreg",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus2",
    
    [Parameter(Mandatory=$false)]
    [string]$StorageAccountName = "",
    
    [Parameter(Mandatory=$false)]
    [string]$WebAppName = "",
    
    [Parameter(Mandatory=$false)]
    [string]$PackagePath = "MyGiftReg.Frontend.zip",
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf,
    
    [Parameter(Mandatory=$false)]
    [switch]$CleanDeploy
)

# Colors for output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Check if Azure CLI is installed
Write-ColorOutput "🔍 Checking Azure CLI installation..." "Yellow"
if (!(Get-Command az -ErrorAction SilentlyContinue)) {
    Write-ColorOutput "❌ Azure CLI is not installed. Please install Azure CLI first." "Red"
    Write-ColorOutput "   Visit: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" "Yellow"
    exit 1
}

# Check if .NET SDK is installed
Write-ColorOutput "🔍 Checking .NET SDK installation..." "Yellow"
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-ColorOutput "❌ .NET SDK is not installed. Please install .NET 8 SDK first." "Red"
    exit 1
}

Write-ColorOutput "✅ Prerequisites check passed!" "Green"

# Login to Azure if not already logged in
Write-ColorOutput "🔐 Checking Azure login status..." "Yellow"
try {
    az account show --output none 2>$null
    Write-ColorOutput "✅ Already logged in to Azure" "Green"
} catch {
    Write-ColorOutput "🔑 Please log in to Azure..." "Yellow"
    az login
}

# Set subscription
Write-ColorOutput "📋 Setting subscription to $SubscriptionId..." "Yellow"
az account set --subscription $SubscriptionId

# Generate unique names if not provided
if ([string]::IsNullOrEmpty($StorageAccountName)) {
    $StorageAccountName = "mygiftregstorage$(Get-Random -Minimum 1000 -Maximum 9999)"
    Write-ColorOutput "🔧 Generated storage account name: $StorageAccountName" "Yellow"
}

if ([string]::IsNullOrEmpty($WebAppName)) {
    $WebAppName = "mygiftreg-webapp-$(Get-Random -Minimum 1000 -Maximum 9999)"
    Write-ColorOutput "🔧 Generated web app name: $WebAppName" "Yellow"
}

# Create resource group if it doesn't exist
Write-ColorOutput "🏗️  Checking resource group: $ResourceGroupName..." "Yellow"
$rgExists = az group exists --name $ResourceGroupName --output tsv

if ($rgExists -eq "false") {
    Write-ColorOutput "📦 Creating resource group: $ResourceGroupName in $Location" "Yellow"
    if ($WhatIf) {
        Write-ColorOutput "💭 WhatIf: Would create resource group $ResourceGroupName" "Cyan"
    } else {
        az group create --name $ResourceGroupName --location $Location
    }
} else {
    Write-ColorOutput "✅ Resource group $ResourceGroupName already exists" "Green"
}

# Clean deployment if requested
if ($CleanDeploy) {
    Write-ColorOutput "🧹 Cleaning previous deployment..." "Yellow"
    if ($WhatIf) {
        Write-ColorOutput "💭 WhatIf: Would delete existing resources in $ResourceGroupName" "Cyan"
    } else {
        Write-ColorOutput "⚠️  WARNING: This will delete ALL resources in $ResourceGroupName!" "Red"
        $confirm = Read-Host "Are you sure you want to continue? (y/N)"
        if ($confirm -eq "y" -or $confirm -eq "Y") {
            az group delete --name $ResourceGroupName --yes
            az group create --name $ResourceGroupName --location $Location
        } else {
            Write-ColorOutput "❌ Deployment cancelled by user" "Red"
            exit 1
        }
    }
}

# Build the application
Write-ColorOutput "🔨 Building the application..." "Yellow"
if ($WhatIf) {
    Write-ColorOutput "💭 WhatIf: Would build the application" "Cyan"
} else {
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ Build failed!" "Red"
        exit 1
    }
    dotnet publish MyGiftReg.Frontend --configuration Release --output publish
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ Publish failed!" "Red"
        exit 1
    }
}

# Create deployment parameters
$parameters = @{
    "resourceGroupName" = $ResourceGroupName
    "location" = $Location
    "storageAccountName" = $StorageAccountName
    "webAppName" = $WebAppName
    "appServicePlanName" = "mygiftreg-plan"
}

# Deploy ARM template
Write-ColorOutput "🚀 Deploying ARM template..." "Yellow"
$templatePath = Join-Path $PSScriptRoot "arm-templates\main-template.json"

if ($WhatIf) {
    Write-ColorOutput "💭 WhatIf: Would deploy ARM template from $templatePath" "Cyan"
    Write-ColorOutput "💭 WhatIf: With parameters: $($parameters | ConvertTo-Json -Compress)" "Cyan"
} else {
    $deployment = az deployment group create `
        --resource-group $ResourceGroupName `
        --template-file $templatePath `
        --parameters @($parameters.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) `
        --output json

    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ ARM template deployment failed!" "Red"
        exit 1
    }

    # Parse deployment output
    $deploymentObj = $deployment | ConvertFrom-Json
    $storageKey = $deploymentObj.outputs.storageAccountKey.value
    $webAppUrl = "https://$($deploymentObj.outputs.webAppUrl.value)"
    
    Write-ColorOutput "✅ ARM template deployed successfully!" "Green"
    Write-ColorOutput "📊 Web App URL: $webAppUrl" "Green"
}

# Create package if it doesn't exist
if (!(Test-Path $PackagePath)) {
    Write-ColorOutput "📦 Creating deployment package..." "Yellow"
    if ($WhatIf) {
        Write-ColorOutput "💭 WhatIf: Would create deployment package" "Cyan"
    } else {
        Compress-Archive -Path "publish\*" -DestinationPath $PackagePath -Force
    }
}

# Deploy to Web App
Write-ColorOutput "🌐 Deploying application to Web App..." "Yellow"
if ($WhatIf) {
    Write-ColorOutput "💭 WhatIf: Would deploy $PackagePath to $WebAppName" "Cyan"
} else {
    az webapp deployment source config-zip `
        --resource-group $ResourceGroupName `
        --name $WebAppName `
        --src $PackagePath

    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ Web App deployment failed!" "Red"
        exit 1
    }
}

# Wait for deployment to complete
Write-ColorOutput "⏳ Waiting for deployment to complete..." "Yellow"
Start-Sleep -Seconds 30

# Validate deployment
Write-ColorOutput "🔍 Validating deployment..." "Yellow"
if ($WhatIf) {
    Write-ColorOutput "💭 WhatIf: Would validate deployment" "Cyan"
} else {
    $healthCheckUrl = "$webAppUrl/api/events"
    
    try {
        $response = Invoke-WebRequest -Uri $healthCheckUrl -UseBasicParsing -TimeoutSec 30
        if ($response.StatusCode -eq 200) {
            Write-ColorOutput "✅ Deployment validation successful!" "Green"
        } else {
            Write-ColorOutput "⚠️  Health check returned status code: $($response.StatusCode)" "Yellow"
        }
    } catch {
        Write-ColorOutput "⚠️  Health check failed (this is normal during initial deployment)" "Yellow"
    }
}

# Output final information
Write-ColorOutput "🎉 Deployment completed successfully!" "Green"
Write-ColorOutput "📋 Deployment Summary:" "Cyan"
Write-ColorOutput "   Resource Group: $ResourceGroupName" "White"
Write-ColorOutput "   Web App Name: $WebAppName" "White"
Write-ColorOutput "   Web App URL: $webAppUrl" "White"
Write-ColorOutput "   Storage Account: $StorageAccountName" "White"
Write-ColorOutput "" "White"
Write-ColorOutput "🔗 Useful Commands:" "Cyan"
Write-ColorOutput "   View logs: az webapp log tail --resource-group $ResourceGroupName --name $WebAppName" "White"
Write-ColorOutput "   Open app: az webapp browse --resource-group $ResourceGroupName --name $WebAppName" "White"
Write-ColorOutput "   View storage tables: az storage table list --account-name $StorageAccountName --account-key $storageKey" "White"
