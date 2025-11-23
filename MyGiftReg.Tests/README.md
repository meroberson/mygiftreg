# MyGiftReg.Backend Test Suite

This comprehensive test suite uses Azurite to emulate Azure Table Storage, providing integration tests that validate the complete backend functionality including CRUD operations, error handling, and concurrency scenarios.

## Test Structure

### Infrastructure
- **AzuriteTestBase.cs**: Base class for all integration tests that manages Azurite lifecycle and provides test database setup/teardown
- Provides automated setup and teardown of Azurite emulator
- Creates and manages Azure Table Storage tables for testing
- Handles connection strings and table client configuration

### Test Categories

#### 1. Repository Integration Tests (`EventRepositoryIntegrationTests.cs`)
Direct testing of repository layer with real Azure Table Storage:
- **Create**: Valid event creation, duplicate event handling
- **Read**: Retrieve existing and non-existing events
- **Update**: Update existing events, handle non-existent updates
- **Delete**: Delete existing events, handle non-existent deletions
- **Query**: Get all events, existence checks

#### 2. Service Integration Tests (`EventServiceIntegrationTests.cs`)
End-to-end testing of service layer with real storage:
- **Service CRUD Operations**: Complete event lifecycle via service layer
- **Input Validation**: Empty/null user IDs, empty event names
- **Business Rules**: Duplicate prevention, data consistency
- **Error Handling**: Validation exceptions, not found exceptions
- **Service-Repository Consistency**: Verify data flow between layers

#### 3. Concurrency Tests (`EventConcurrencyTests.cs`)
Advanced testing of Azure Table Storage concurrency features:
- **ETag Management**: Proper ETag handling during updates
- **Concurrent Operations**: Multiple simultaneous reads/writes
- **Stale Updates**: Handling of outdated ETag scenarios
- **Data Integrity**: Ensuring consistency under concurrent access
- **Optimistic Locking**: Validation of Azure's ETag-based concurrency control

#### 4. Unit Tests (Existing)
- **Mock-based Tests**: `EventServiceTests.cs` using Moq for isolated unit testing
- **Validation Focus**: Business logic testing without external dependencies
- **Fast Execution**: Quick-running tests for development feedback

## Prerequisites

### Required Tools
1. **Azurite**: Local Azure storage emulator
   ```bash
   npm install -g azurite
   ```

2. **.NET 8.0**: Latest .NET runtime and SDK
   ```bash
   dotnet --version
   ```

### Package Dependencies
The test project includes all necessary Azure SDK packages:
- Azure.Data.Tables (Azure Table Storage)
- Azure.Storage.Blobs, Azure.Storage.Queues
- Azure.Core.TestFramework
- Microsoft.Extensions.Hosting

## Running Tests

### Prerequisites Check
```bash
# Verify Azurite is available
azurite --version

# Verify .NET installation
dotnet --version

# Restore packages
dotnet restore MyGiftReg.Tests/MyGiftReg.Tests.csproj
```

### Run All Tests
```bash
# Run all tests in the project
dotnet test MyGiftReg.Tests/MyGiftReg.Tests.csproj

# Run with verbose output
dotnet test MyGiftReg.Tests/MyGiftReg.Tests.csproj --verbosity normal

# Run with detailed logs
dotnet test MyGiftReg.Tests/MyGiftReg.Tests.csproj --verbosity detailed
```

### Run Specific Test Categories
```bash
# Run only integration tests
dotnet test MyGiftReg.Tests/MyGiftReg.Tests.csproj --filter Category=Integration

# Run only concurrency tests
dotnet test MyGiftReg.Tests/MyGiftReg.Tests.csproj --filter Category=Concurrency

# Run only repository tests
dotnet test MyGiftReg.Tests/MyGiftReg.Tests.csproj --filter EventRepositoryIntegrationTests

# Run only service tests
dotnet test MyGiftReg.Tests/MyGiftReg.Tests.csproj --filter EventServiceIntegrationTests
```

### Run Tests with Coverage
```bash
# Install coverlet collector (already included in project)
dotnet test MyGiftReg.Tests/MyGiftReg.Tests.csproj --collect:"XPlat Code Coverage"

# Generate coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

## Test Data Management

### Test Isolation
- Each test class gets a fresh Azurite instance
- Tables are created automatically for each test run
- Automatic cleanup after each test completes
- Test data is isolated per test method

### Test Naming Conventions
- **Repository Tests**: `[Operation]_[Condition]_[ExpectedResult]`
  - Example: `CreateEventAsync_ValidEvent_ReturnsCreatedEvent`
  
- **Service Tests**: `[Operation]_[Condition]_[ExpectedResult]`
  - Example: `CreateEventAsync_DuplicateEvent_ThrowsValidationException`
  
- **Concurrency Tests**: `[Scenario]_[Behavior]`
  - Example: `MultipleSimultaneousReads_ReturnConsistentData`

## Configuration

### Connection String
Tests use the default Azurite connection string:
```csharp
"UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1"
```

### Table Names
- `EventTable`: Event entities storage
- `GiftListTable`: Gift list entities storage  
- `GiftItemTable`: Gift item entities storage

### Ports (Default)
- Blob service: 10000
- Queue service: 10001  
- Table service: 10002

## Troubleshooting

### Common Issues

#### Azurite Not Found
```bash
# Install Azurite globally
npm install -g azurite

# Verify installation
azurite --version
```

#### Port Conflicts
If ports are in use, modify the AzuriteTestBase.cs configuration:
```csharp
Arguments = "--location .\\AzuriteData --blobPort 10010 --queuePort 10011 --tablePort 10012"
```

#### Permission Issues (Linux/Mac)
```bash
# Kill existing Azurite processes
pkill azurite

# Or on Windows
taskkill /IM azurite.exe /F
```

#### Test Timeouts
Integration tests may take longer due to Azurite startup. Adjust timeout in test settings if needed.

## Test Coverage Goals

### Repository Layer
- [x] CRUD operations for all entities
- [x] Error handling and edge cases
- [x] Azure Table Storage integration
- [x] ETag handling and optimistic locking

### Service Layer  
- [x] Input validation and business rules
- [x] Error propagation and exception handling
- [x] Data consistency and integrity
- [x] Service-repository communication

### Concurrency & Performance
- [x] Concurrent read operations
- [x] Concurrent write operations
- [x] ETag conflict resolution
- [x] Data consistency under load

### Future Enhancements
- [ ] GiftList and GiftItem integration tests
- [ ] Performance and load testing
- [ ] Relationship integrity tests
- [ ] Advanced concurrency scenarios

## Development Workflow

### Running Tests During Development
```bash
# Quick feedback loop
dotnet test MyGiftReg.Tests/MyGiftReg.Tests.csproj --filter "EventServiceIntegrationTests" --verbosity minimal

# Watch mode (with dotnet watch)
dotnet watch test MyGiftReg.Tests/MyGiftReg.Tests.csproj --filter "EventRepositoryIntegrationTests"
```

### Test-Driven Development
1. Write failing integration test
2. Implement minimal code to pass
3. Refactor while keeping tests green
4. Add edge cases and error scenarios

### CI/CD Integration
The test suite is designed to run in CI/CD environments:
- Self-contained Azurite setup
- Automatic resource cleanup
- Clear test result reporting
- No external dependencies required

## Best Practices

### Test Organization
- Group related tests in the same file
- Use descriptive test method names
- Follow AAA pattern (Arrange, Act, Assert)
- Keep tests independent and isolated

### Test Data
- Use meaningful test data that reveals intent
- Avoid magic numbers and strings
- Create test utilities for common scenarios
- Ensure deterministic test results

### Performance
- Minimize test execution time
- Use parallel test execution when safe
- Clean up resources promptly
- Avoid unnecessary Azurite restarts

This test suite provides comprehensive validation of the MyGiftReg.Backend functionality while maintaining fast execution times and reliable results through the use of Azurite for local Azure storage emulation.
