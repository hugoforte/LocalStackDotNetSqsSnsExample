# Design Document

## Overview

This design outlines a .NET solution that integrates LocalStack for testing Amazon SQS functionality. The solution uses Docker to manage LocalStack containers, xUnit for testing framework, and Testcontainers.NET for container lifecycle management. The architecture ensures isolated testing environments and proper resource cleanup.

## Architecture

The solution follows a standard .NET solution structure with clear separation between the main application logic and test infrastructure:

```
LocalStackSqsTesting/
├── src/
│   └── LocalStackSqsTesting/
│       ├── LocalStackSqsTesting.csproj
│       ├── Services/
│       │   └── SqsService.cs
│       └── Models/
│           └── QueueConfiguration.cs
├── tests/
│   └── LocalStackSqsTesting.Tests/
│       ├── LocalStackSqsTesting.Tests.csproj
│       ├── Infrastructure/
│       │   ├── LocalStackFixture.cs
│       │   └── SqsTestBase.cs
│       └── Tests/
│           └── SqsServiceTests.cs
├── LocalStackSqsTesting.sln
└── docker-compose.yml
```

## Components and Interfaces

### Core Components

1. **SqsService**: Main service class that wraps AmazonSQSClient functionality
   - Handles queue creation, deletion, and listing
   - Configured to work with both LocalStack and real AWS
   - Implements proper error handling and logging

2. **LocalStackFixture**: Test infrastructure component using IAsyncLifetime
   - Manages LocalStack container lifecycle
   - Provides configured AmazonSQSClient instances
   - Handles container startup, health checks, and cleanup

3. **SqsTestBase**: Base class for SQS-related tests
   - Provides common test setup and teardown
   - Offers helper methods for queue operations
   - Ensures test isolation through unique queue naming

### Key Interfaces

```csharp
public interface ISqsService
{
    Task<string> CreateQueueAsync(string queueName, CancellationToken cancellationToken = default);
    Task<List<string>> ListQueuesAsync(CancellationToken cancellationToken = default);
    Task DeleteQueueAsync(string queueUrl, CancellationToken cancellationToken = default);
}

public interface ILocalStackFixture : IAsyncLifetime
{
    AmazonSQSClient SqsClient { get; }
    string LocalStackEndpoint { get; }
}
```

## Data Models

### QueueConfiguration
```csharp
public class QueueConfiguration
{
    public string QueueName { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
}
```

### LocalStackSettings
```csharp
public class LocalStackSettings
{
    public string Image { get; set; } = "localstack/localstack:latest";
    public int Port { get; set; } = 4566;
    public string[] Services { get; set; } = { "sqs" };
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
```

## Error Handling

### Exception Strategy
- **LocalStackException**: Custom exception for LocalStack-specific errors
- **SqsServiceException**: Wraps AWS SDK exceptions with additional context
- **ContainerStartupException**: Handles Docker container startup failures

### Error Scenarios
1. **Docker Not Available**: Clear error message with setup instructions
2. **LocalStack Startup Failure**: Retry logic with exponential backoff
3. **SQS Operation Failures**: Detailed error messages with operation context
4. **Network Connectivity Issues**: Timeout handling and retry mechanisms

## Testing Strategy

### Test Organization
- **Unit Tests**: Test SqsService logic with mocked AmazonSQSClient
- **Integration Tests**: Test against LocalStack container
- **Container Tests**: Verify LocalStack container management

### Test Isolation
- Each test class gets its own LocalStack container instance
- Queue names include test-specific prefixes to avoid conflicts
- Automatic cleanup of created resources after each test

### Test Categories
```csharp
[Trait("Category", "Integration")]
[Trait("Category", "LocalStack")]
public class SqsServiceIntegrationTests : IClassFixture<LocalStackFixture>
```

### Container Management
Using Testcontainers.NET for reliable container lifecycle:
- Automatic port allocation to avoid conflicts
- Health check verification before tests run
- Proper cleanup even if tests fail
- Support for parallel test execution

## Configuration

### LocalStack Container Configuration
```yaml
# docker-compose.yml for manual testing
version: '3.8'
services:
  localstack:
    image: localstack/localstack:latest
    ports:
      - "4566:4566"
    environment:
      - SERVICES=sqs
      - DEBUG=1
      - DATA_DIR=/tmp/localstack/data
    volumes:
      - localstack-data:/tmp/localstack
volumes:
  localstack-data:
```

### AmazonSQSClient Configuration
```csharp
var config = new AmazonSQSConfig
{
    ServiceURL = "http://localhost:4566",
    UseHttp = true,
    AuthenticationRegion = "us-east-1"
};

var client = new AmazonSQSClient("dummy", "dummy", config);
```

## Dependencies

### NuGet Packages
- **AWSSDK.SQS**: AWS SDK for SQS operations
- **Testcontainers**: Container management for tests
- **xunit**: Testing framework
- **xunit.runner.visualstudio**: Test runner integration
- **Microsoft.Extensions.Logging**: Logging infrastructure
- **Microsoft.Extensions.Configuration**: Configuration management

### Docker Requirements
- Docker Desktop or Docker Engine
- LocalStack image (automatically pulled)
- Sufficient system resources for container execution

## Security Considerations

### Credentials
- Use dummy credentials for LocalStack (no real AWS access)
- Ensure no real AWS credentials are accidentally used in tests
- Environment variable isolation for test runs

### Network Security
- LocalStack containers only expose necessary ports
- No external network access required for tests
- Isolated container networks for parallel test execution