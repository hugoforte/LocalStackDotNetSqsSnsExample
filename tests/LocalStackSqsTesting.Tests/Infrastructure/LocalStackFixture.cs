using Amazon.SQS;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using LocalStackSqsTesting.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LocalStackSqsTesting.Tests.Infrastructure;

/// <summary>
/// Test fixture for managing LocalStack container lifecycle
/// Implements IAsyncLifetime for proper setup and cleanup
/// </summary>
public class LocalStackFixture : ILocalStackFixture
{
    private readonly LocalStackSettings _settings;
    private readonly ILogger<LocalStackFixture> _logger;
    private IContainer? _container;
    private AmazonSQSClient? _sqsClient;

    /// <summary>
    /// Gets the configured AmazonSQSClient for LocalStack
    /// </summary>
    public AmazonSQSClient SqsClient => _sqsClient ?? throw new InvalidOperationException("LocalStack container is not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Gets the LocalStack endpoint URL
    /// </summary>
    public string LocalStackEndpoint => _settings.GetEndpointUrl();

    /// <summary>
    /// Gets whether the LocalStack container is running
    /// </summary>
    public bool IsRunning => _container?.State == TestcontainersStates.Running;

    public LocalStackFixture()
    {
        _settings = new LocalStackSettings();
        
        // Create null logger for testing (can be replaced with actual logger in production)
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<LocalStackFixture>.Instance;
    }

    /// <summary>
    /// Initializes the LocalStack container and SQS client
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting LocalStack container initialization...");
            
            // Build and start the LocalStack container
            await StartLocalStackContainerAsync();
            
            // Wait for LocalStack to be ready
            await WaitForLocalStackReadyAsync();
            
            // Configure SQS client
            ConfigureSqsClient();
            
            _logger.LogInformation("LocalStack container initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize LocalStack container");
            await DisposeAsync(); // Cleanup on failure
            
            // Check if this is a Docker-related issue
            if (IsDockerRelatedError(ex))
            {
                throw new DockerNotAvailableException("Docker is not available or not running. Please ensure Docker Desktop is installed and running.", ex);
            }
            
            throw new LocalStackInitializationException("Failed to initialize LocalStack container", ex);
        }
    }

    /// <summary>
    /// Disposes the LocalStack container and cleans up resources
    /// </summary>
    public async Task DisposeAsync()
    {
        try
        {
            _logger.LogInformation("Disposing LocalStack container...");
            
            // Dispose SQS client
            _sqsClient?.Dispose();
            _sqsClient = null;
            
            // Stop and dispose container
            if (_container != null)
            {
                await _container.StopAsync();
                await _container.DisposeAsync();
                _container = null;
            }
            
            _logger.LogInformation("LocalStack container disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error occurred while disposing LocalStack container");
            // Don't throw exceptions during cleanup
        }
    }



    /// <summary>
    /// Starts the LocalStack container with proper configuration
    /// </summary>
    private async Task StartLocalStackContainerAsync()
    {
        _logger.LogDebug("Building LocalStack container...");
        
        var containerBuilder = new ContainerBuilder()
            .WithImage(_settings.Image)
            .WithPortBinding(4566, true) // Use dynamic port allocation to avoid conflicts
            .WithEnvironment("SERVICES", _settings.GetServicesString())
            .WithEnvironment("DEBUG", "1")
            .WithEnvironment("DATA_DIR", "/tmp/localstack/data")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r
                    .ForPort(4566)
                    .ForPath("/_localstack/health")
                    .ForStatusCode(System.Net.HttpStatusCode.OK)))
            .WithStartupCallback((container, ct) =>
            {
                _logger.LogInformation("LocalStack container started with ID: {ContainerId}", container.Id);
                return Task.CompletedTask;
            });

        // Add any additional environment variables
        foreach (var envVar in _settings.EnvironmentVariables)
        {
            containerBuilder = containerBuilder.WithEnvironment(envVar.Key, envVar.Value);
        }

        _container = containerBuilder.Build();
        
        _logger.LogInformation("Starting LocalStack container...");
        await _container.StartAsync();
        
        // Update the settings with the actual mapped port
        var mappedPort = _container.GetMappedPublicPort(4566);
        _settings.Port = mappedPort;
        _logger.LogInformation("LocalStack container started on port {Port}", mappedPort);
    }

    /// <summary>
    /// Waits for LocalStack to be fully ready and responsive
    /// </summary>
    private async Task WaitForLocalStackReadyAsync()
    {
        _logger.LogDebug("Waiting for LocalStack to be ready...");
        
        var timeout = _settings.StartupTimeout;
        var startTime = DateTime.UtcNow;
        var httpClient = new HttpClient();
        
        try
        {
            while (DateTime.UtcNow - startTime < timeout)
            {
                try
                {
                    var healthUrl = $"{LocalStackEndpoint}/_localstack/health";
                    var response = await httpClient.GetAsync(healthUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug("LocalStack health check response: {Content}", content);
                        
                        // Check if SQS service is available
                        if (content.Contains("\"sqs\": \"available\"") || content.Contains("\"sqs\": \"running\""))
                        {
                            _logger.LogInformation("LocalStack is ready and SQS service is available");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Health check failed, retrying...");
                }
                
                await Task.Delay(1000); // Wait 1 second before retry
            }
            
            throw new TimeoutException($"LocalStack did not become ready within {timeout}");
        }
        finally
        {
            httpClient.Dispose();
        }
    }

    /// <summary>
    /// Configures the AmazonSQSClient for LocalStack endpoint
    /// </summary>
    private void ConfigureSqsClient()
    {
        _logger.LogDebug("Configuring SQS client for LocalStack endpoint: {Endpoint}", LocalStackEndpoint);
        
        var config = new AmazonSQSConfig
        {
            ServiceURL = LocalStackEndpoint,
            UseHttp = _settings.UseHttp,
            AuthenticationRegion = _settings.Region
        };

        // Use dummy credentials for LocalStack
        _sqsClient = new AmazonSQSClient("dummy", "dummy", config);
        
        _logger.LogInformation("SQS client configured successfully for LocalStack");
    }

    /// <summary>
    /// Determines if an exception is related to Docker availability issues
    /// </summary>
    private static bool IsDockerRelatedError(Exception ex)
    {
        // Check the exception type and message for Docker-related indicators
        var exceptionType = ex.GetType().Name;
        var message = ex.Message;
        
        // Common Docker-related exception types
        if (exceptionType.Contains("Docker", StringComparison.OrdinalIgnoreCase))
            return true;
            
        // Common Docker-related error messages
        var dockerErrorIndicators = new[]
        {
            "docker",
            "container",
            "daemon",
            "docker desktop",
            "docker engine",
            "npipe://./pipe/docker_engine",
            "port is already allocated",
            "failed to set up container networking"
        };
        
        return dockerErrorIndicators.Any(indicator => 
            message.Contains(indicator, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Exception thrown when LocalStack initialization fails
/// </summary>
public class LocalStackInitializationException : Exception
{
    public LocalStackInitializationException(string message) : base(message) { }
    public LocalStackInitializationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when Docker is not available
/// </summary>
public class DockerNotAvailableException : Exception
{
    public DockerNotAvailableException(string message) : base(message) { }
    public DockerNotAvailableException(string message, Exception innerException) : base(message, innerException) { }
}