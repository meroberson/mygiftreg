# PowerShell Deployment Script for MyGiftReg
# Uses Az PowerShell modules (not az cli)

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location,
    
    [Parameter(Mandatory=$true)]
    [string]$AppName,
    
    [Parameter(Mandatory=$false)]
    [string]$BuildConfiguration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$ArmTemplatePath = "deployment\arm-templates\main-template.json",
    
    [Parameter(Mandatory=$false)]
    [string]$ArmParametersPath = "deployment\arm-templates\parameters.json",
    
    [Parameter(Mandatory=$false)]
    [string]$ProductionSettingsPath = "deployment\appsettings.Production.json"
)

# Enable strict mode
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Function to write colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Function to log step completion
function Write-Step {
    param(
        [string]$Step
    )
    Write-ColorOutput "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Step" "Green"
}

# Function to log errors
function Write-Error-Step {
    param(
        [string]$ErrorMessage
    )
    Write-ColorOutput "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] ERROR: $ErrorMessage" "Red"
    exit 1
}

Write-ColorOutput "=== MyGiftReg Deployment Script ===" "Cyan"
Write-ColorOutput "Starting deployment process..." "White"

# Step 1: Check if Az PowerShell modules are installed
Write-Step "Checking Az PowerShell modules..."
try {
    if (-not (Get-Module -ListAvailable -Name Az.Accounts)) {
        Write-Error-Step "Az.Accounts module not found. Please install Az PowerShell modules: Install-Module -Name Az -AllowClobber -Force"
    }
    if (-not (Get-Module -ListAvailable -Name Az.Resources)) {
        Write-Error-Step "Az.Resources module not found. Please install Az PowerShell modules: Install-Module -Name Az -AllowClobber -Force"
    }
    if (-not (Get-Module -ListAvailable -Name Az.Websites)) {
        Write-Error-Step "Az.Websites module not found. Please install Az PowerShell modules: Install-Module -Name Az -AllowClobber -Force"
    }
    Write-ColorOutput "Az PowerShell modules found." "Green"
}
catch {
    Write-Error-Step "Failed to check Az PowerShell modules: $_"
}

# Step 2: Import required modules
Write-Step "Importing Az PowerShell modules..."
try {
    Import-Module Az.Accounts -Force
    Import-Module Az.Resources -Force
    Import-Module Az.Websites -Force
    Write-ColorOutput "Az PowerShell modules imported successfully." "Green"
}
catch {
    Write-Error-Step "Failed to import Az PowerShell modules: $_"
}

# Step 3: Authenticate to Azure
Write-Step "Checking Azure authentication..."
try {
    $context = Get-AzContext
    if (-not $context) {
        Write-ColorOutput "Please authenticate to Azure..." "Yellow"
        Connect-AzAccount
        $context = Get-AzContext
    }
    Write-ColorOutput "Connected to Azure subscription: $($context.Subscription.Name)" "Green"
}
catch {
    Write-Error-Step "Failed to authenticate to Azure: $_"
}

# Step 4: Clean previous build artifacts
Write-Step "Cleaning previous build artifacts..."
try {
    $publishPath = "publish"
    if (Test-Path $publishPath) {
        Remove-Item -Path $publishPath -Recurse -Force
        Write-ColorOutput "Cleaned previous publish directory." "Green"
    }
}
catch {
    Write-Error-Step "Failed to clean previous build artifacts: $_"
}

# Step 5: Restore NuGet packages and build in Release configuration
Write-Step "Building solution in $BuildConfiguration configuration..."
try {
    & dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Step "dotnet restore failed with exit code $LASTEXITCODE"
    }
    
    & dotnet build --configuration $BuildConfiguration --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Step "dotnet build failed with exit code $LASTEXITCODE"
    }
    
    Write-ColorOutput "Solution built successfully in $BuildConfiguration configuration." "Green"
}
catch {
    Write-Error-Step "Failed to build solution: $_"
}

# Step 6: Publish the frontend application
Write-Step "Publishing frontend application..."
try {
    $frontendProjectPath = "MyGiftReg.Frontend"
    $publishOutputPath = "publish\frontend"
    
    & dotnet publish $frontendProjectPath --configuration $BuildConfiguration --output $publishOutputPath --no-build
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Step "dotnet publish failed with exit code $LASTEXITCODE"
    }
    
    Write-ColorOutput "Frontend published successfully to $publishOutputPath." "Green"
}
catch {
    Write-Error-Step "Failed to publish frontend application: $_"
}

# Step 7: Copy production appsettings to overwrite appsettings.json
Write-Step "Copying production appsettings..."
try {
    $sourceSettings = $ProductionSettingsPath
    $destinationSettings = "publish\frontend\appsettings.json"
    
    if (-not (Test-Path $sourceSettings)) {
        Write-Error-Step "Production settings file not found: $sourceSettings"
    }
    
    Copy-Item -Path $sourceSettings -Destination $destinationSettings -Force
    Write-ColorOutput "Production appsettings copied successfully." "Green"
}
catch {
    Write-Error-Step "Failed to copy production appsettings: $_"
}

# Step 8: Create zip file of published content
Write-Step "Creating deployment zip file..."
try {
    $zipFileName = "${AppName}-deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss').zip"
    $zipFilePath = ".\$zipFileName"
    
    Compress-Archive -Path "publish\frontend\*" -DestinationPath $zipFilePath -Force
    Write-ColorOutput "Deployment zip file created: $zipFilePath" "Green"
}
catch {
    Write-Error-Step "Failed to create deployment zip file: $_"
}

# Step 9: Deploy ARM template
Write-Step "Deploying ARM template..."
try {
    # Read and parse ARM parameters
    $armParams = @{}
    if (Test-Path $ArmParametersPath) {
        $paramContent = Get-Content $ArmParametersPath | ConvertFrom-Json
        foreach ($property in $paramContent.parameters.PSObject.Properties) {
            if ($property.Value.value) {
                $armParams[$property.Name] = $property.Value.value
            }
        }
    }
    
    # Add required parameters (override defaults from parameters.json)
    $armParams["webAppName"] = $AppName
    $armParams["location"] = $Location
    
    # Optional parameters - only add if not already set
    if (-not $armParams.ContainsKey("managedIdentityName")) {
        $armParams["managedIdentityName"] = "$AppName-managed-identity"
    }
    
    $deploymentName = "MyGiftReg-Deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    
    New-AzResourceGroupDeployment `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile $ArmTemplatePath `
        -TemplateParameterObject $armParams `
        -Name $deploymentName
    
    Write-ColorOutput "ARM template deployed successfully." "Green"
}
catch {
    Write-Error-Step "Failed to deploy ARM template: $_"
}

# Step 10: Deploy web app
Write-Step "Deploying web application..."
try {
    # Wait a moment for the web app to be fully provisioned
    Start-Sleep -Seconds 30
    
    # Get the web app
    $webApp = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppName
    
    if (-not $webApp) {
        Write-Error-Step "Web app '$AppName' not found in resource group '$ResourceGroupName'"
    }
    
    # Deploy using Az WebApp
    Publish-AzWebApp `
        -ResourceGroupName $ResourceGroupName `
        -Name $AppName `
        -ArchivePath $zipFilePath `
        -Force
    
    Write-ColorOutput "Web application deployed successfully." "Green"
}
catch {
    Write-Error-Step "Failed to deploy web application: $_"
}

# Step 11: Final cleanup
Write-Step "Cleaning up temporary files..."
try {
    if (Test-Path "publish") {
        Remove-Item -Path "publish" -Recurse -Force
        Write-ColorOutput "Cleaned up publish directory." "Green"
    }
}
catch {
    Write-Warning "Failed to clean up temporary files: $_"
}

Write-ColorOutput "=== Deployment Complete ===" "Cyan"
Write-ColorOutput "Deployment Summary:" "White"
Write-ColorOutput "- Resource Group: $ResourceGroupName" "White"
Write-ColorOutput "- App Name: $AppName" "White"
Write-ColorOutput "- Location: $Location" "White"
Write-ColorOutput "- Build Configuration: $BuildConfiguration" "White"
Write-ColorOutput "- Deployment Zip: $zipFileName" "White"
Write-ColorOutput "" "White"
Write-ColorOutput "Your application should now be available at:" "Yellow"
Write-ColorOutput "https://$AppName.azurewebsites.net" "Yellow"
