# Azure Entra ID Setup Guide for MyGiftReg

This guide provides step-by-step instructions to configure Azure Entra ID authentication for the MyGiftReg application.

## Prerequisites

- Azure subscription with Entra ID (formerly Azure Active Directory) access
- .NET 8.0 SDK installed
- Visual Studio or VS Code with C# extension

## Step 1: Create an App Registration

1. Sign in to the [Azure Portal](https://portal.azure.com)
2. Navigate to **Entra ID** > **App registrations** > **New registration**
3. Configure the application:
   - **Name**: MyGiftReg
   - **Supported account types**: Select appropriate option for your scenario
   - **Redirect URI**: 
     - Web: `https://your-domain.com/signin-oidc`
     - Development: `https://localhost:5001/signin-oidc`

4. Click **Register** and note the **Application (client) ID** and **Directory (tenant) ID**

## Step 2: Configure Authentication

1. In your app registration, go to **Authentication**
2. Under **Platform configurations**, click **Add a platform**
3. Select **Web** and configure:
   - **Redirect URIs**: 
     - `https://your-domain.com/signin-oidc`
     - `https://localhost:5001/signin-oidc` (for development)
   - **Front-channel logout URL**: `https://your-domain.com/signout-callback-oidc`

## Step 3: Create a Client Secret

1. Go to **Certificates & secrets** > **New client secret**
2. Add a description and select expiration period
3. Copy the **Secret Value** (you won't be able to see it again)

## Step 4: Create Application Roles

1. Go to **App registrations** > **Your App** > **App roles** > **Create app role**
2. Create the required role:
   - **Display name**: MyGiftReg.Access
   - **Value**: MyGiftReg.Access
   - **Description**: Access to MyGiftReg application
   - **Who can assign**: Users/Groups
   - **Users/Groups**: Select users who should have access

## Step 5: Assign Users to Roles

1. Go to **Enterprise applications** > **Your App** > **Users and groups**
2. Click **Add user/group**
3. Select users who should have access to the application
4. Assign the **MyGiftReg.Access** role

## Step 6: Configure Application Settings

Update your configuration files with the values from your app registration:

### appsettings.json (Production)
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-tenant-domain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-application-id",
    "ClientSecret": "your-client-secret",
    "CallbackPath": "/signin-oidc",
    "Scope": "openid profile email offline_access",
    "RequiredRole": "MyGiftReg.Access"
  }
}
```

### appsettings.Development.json (Development)
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-tenant-domain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-application-id",
    "ClientSecret": "your-client-secret",
    "CallbackPath": "/signin-oidc",
    "Scope": "openid profile email offline_access",
    "RequiredRole": "MyGiftReg.Access"
  }
}
```

## Step 7: Environment Variables (Alternative)

Instead of hardcoding secrets, you can use environment variables:

- `AzureAd__TenantId`: Your tenant ID
- `AzureAd__ClientId`: Your client ID
- `AzureAd__ClientSecret`: Your client secret
- `AzureAd__Domain`: Your tenant domain
- `AzureAd__RequiredRole`: Required role (MyGiftReg.Access)

## Step 8: Application Settings in Azure App Service

If deploying to Azure App Service:

1. Go to **App Service** > **Configuration** > **Application settings**
2. Add the environment variables:
   - `AzureAd__TenantId`
   - `AzureAd__ClientId`
   - `AzureAd__ClientSecret`
   - `AzureAd__Domain`

## Step 9: Testing the Configuration

1. Run the application locally: `dotnet run`
2. Navigate to `https://localhost:5001`
3. You should be redirected to Azure AD login
4. After login, you should be redirected back to the application
5. If you have the required role, you should see the main interface
6. If you don't have the role, you'll see an access denied message

## Role-Based Access Control

- **Authenticated users**: Can access the sign-in page
- **Users with MyGiftReg.Access role**: Can access the full application
- **Users without MyGiftReg.Access role**: Will see an access denied message

## Troubleshooting

### Common Issues:

1. **"AADSTS50011: The reply URL specified in the request does not match"**
   - Ensure the redirect URI in your app registration matches exactly

2. **"AADSTS50058: A silent login request was sent but no user is logged in"**
   - Clear browser cache and cookies, try again

3. **Users can sign in but can't access the application**
   - Check if the user is assigned the "MyGiftReg.Access" role
   - Verify role assignment in Azure Portal

4. **Role not appearing in tokens**
   - Ensure "roles" claim is included in the token configuration
   - Check that the app role is properly configured

### Debug Steps:

1. Check browser network tab for authentication errors
2. Review application logs for detailed error messages
3. Use Azure AD authentication tools to test token acquisition

## Security Considerations

- Use environment variables for production secrets
- Regularly rotate client secrets
- Monitor sign-in logs for unusual activity
- Implement conditional access policies if needed
- Consider multi-factor authentication requirements

## Next Steps

After successful configuration:

1. Test with multiple user accounts
2. Verify role-based access works correctly
3. Monitor application logs for authentication issues
4. Consider implementing additional security policies

For production deployment, ensure you:
- Use HTTPS for all communications
- Configure proper CORS settings
- Set up monitoring and alerting
- Implement proper backup strategies for user data
