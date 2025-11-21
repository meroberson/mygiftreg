# Local Development Setup Guide

This guide will help you set up and run the MyGiftReg project locally for development.

## Prerequisites

Before running the project locally, ensure you have the following installed:

- **.NET 8.0 SDK** - Download from [Microsoft .NET](https://dotnet.microsoft.com/download)
- **Visual Studio 2022** or **Visual Studio Code** with C# extension
- **Git** for version control
- **Azure Storage Emulator** (for local Azure Table Storage development)

## Project Structure

The MyGiftReg solution consists of three main projects:

- **MyGiftReg.Backend** - Core business logic and data access
- **MyGiftReg.Frontend** - ASP.NET Core MVC web application
- **MyGiftReg.Tests** - Unit tests

## Prerequisites Installation

### 1. Install .NET 8.0 SDK

1. Download .NET 8.0 SDK from [Microsoft .NET](https://dotnet.microsoft.com/download)
2. Run the installer and follow the setup wizard
3. Verify installation by opening a command prompt and running:
   ```cmd
   dotnet --version
   ```

### 2. Install Azure Storage Emulator

For local development with Azure Table Storage:

1. Download **Azure Storage Emulator** from Microsoft
2. Install using the provided installer
3. Start the emulator before running the application

Alternatively, you can use **Azurite** (the cross-platform emulator):
```cmd
npm install -g azurite
azurite --silent --location ./azurite_data
```

## Configuration

### Development Settings

The application uses `appsettings.Development.json` for development configuration. Key settings include:

- **ConnectionStrings** - Database and storage connections
- **Logging** - Console and file logging configuration
- **ApplicationUrl** - Local development URL

### Azure Table Storage Configuration

For local development, configure the following in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "AzureTableStorage": "UseDevelopmentStorage=true"
  },
  "AzureTableConfig": {
    "TableServiceEndpoint": "http://127.0.0.1:10002/devstoreaccount1",
    "AccountName": "devstoreaccount1",
    "AccountKey": "Eby8vdM02xNOcqFe..."
  }
}
```

## Running the Application

### Using Visual Studio

1. **Clone the repository:**
   ```cmd
   git clone https://github.com/meroberson/mygiftreg.git
   cd mygiftreg
   ```

2. **Open the solution:**
   - Double-click `MyGiftReg.sln` or open in Visual Studio

3. **Start the Azure Storage Emulator** (if using local storage)

4. **Set startup project:**
   - Right-click the solution in Solution Explorer
   - Select "Set Startup Projects"
   - Choose "Multiple startup projects"
   - Set both `MyGiftReg.Frontend` and `MyGiftReg.Backend` to "Start"

5. **Build and run:**
   - Press `F5` or click "Start Debugging"

### Using Command Line

1. **Clone and navigate to the project:**
   ```cmd
   git clone https://github.com/meroberson/mygiftreg.git
   cd mygiftreg
   ```

2. **Restore dependencies:**
   ```cmd
   dotnet restore
   ```

3. **Start Azure Storage Emulator** (if needed)

4. **Run the application:**
   ```cmd
   dotnet run --project MyGiftReg.Frontend
   ```

   Or to run both backend and frontend:
   ```cmd
   # Terminal 1 - Backend
   dotnet run --project MyGiftReg.Backend
   
   # Terminal 2 - Frontend  
   dotnet run --project MyGiftReg.Frontend
   ```

5. **Access the application:**
   - Open your browser and navigate to `http://localhost:5000` or `https://localhost:5001`

## Testing

### Running Unit Tests

To run the test suite:

```cmd
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests for specific project
dotnet test MyGiftReg.Tests/MyGiftReg.Tests.csproj
```

### Test Coverage

View test coverage reports:

```cmd
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Development Workflow

### Making Changes

1. **Pull latest changes:**
   ```cmd
   git pull origin main
   ```

2. **Create a feature branch:**
   ```cmd
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes and test them locally**

4. **Run tests to ensure nothing is broken:**
   ```cmd
   dotnet test
   ```

5. **Commit and push your changes:**
   ```cmd
   git add .
   git commit -m "Your commit message"
   git push origin feature/your-feature-name
   ```

### Database/Storage Management

#### Azure Table Storage

- **Local Development:** Uses Azure Storage Emulator
- **Production:** Uses actual Azure Table Storage service

To clear local storage data:
1. Stop the application
2. Restart Azure Storage Emulator
3. Restart the application

## Troubleshooting

### Common Issues

**Port Already in Use:**
```cmd
# Find process using port 5000
netstat -ano | findstr :5000

# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F
```

**Azure Storage Emulator Issues:**
- Ensure the emulator is running as Administrator
- Check if port 10002 is available
- Try restarting the emulator

**Build Errors:**
```cmd
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

**Database Connection Issues:**
- Verify Azure Storage Emulator is running
- Check connection strings in `appsettings.Development.json`
- Ensure firewall isn't blocking connections

### Getting Help

1. Check the console output for specific error messages
2. Review the logs in the `logs` directory
3. Ensure all prerequisites are correctly installed
4. Check the GitHub repository for known issues

## Development Tips

### Useful Commands

```cmd
# Watch for changes and auto-rebuild
dotnet watch run --project MyGiftReg.Frontend

# Clean solution
dotnet clean

# Restore packages
dotnet restore

# Build solution
dotnet build

# Run with specific environment
dotnet run --project MyGiftReg.Frontend --environment Development
```

### Configuration Tips

- Use `appsettings.Development.json` for local development settings
- Environment variables override appsettings files
- Use user secrets for sensitive data in development:
  ```cmd
  dotnet user-secrets init --project MyGiftReg.Frontend
  dotnet user-secrets set "ConnectionStrings:AzureTableStorage" "YourConnectionString"
  ```

## Next Steps

After successfully running the project locally:

1. Explore the existing features
2. Review the API documentation
3. Check out the deployment guide for production setup
4. Consider contributing to the project

For more detailed information, see:
- [API Documentation](docs/api.md)
- [Deployment Guide](deployment/DEPLOYMENT.md)
- [Contributing Guidelines](CONTRIBUTING.md)
