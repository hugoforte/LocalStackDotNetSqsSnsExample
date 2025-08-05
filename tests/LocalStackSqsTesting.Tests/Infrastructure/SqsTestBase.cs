using Amazon.SQS;
using LocalStackSqsTesting.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LocalStackSqsTesting.Tests.Infrastructure;

/// <summary>
/// Base class for SQS integration tests providing common setup and helper utilities
/// </summary>
public abstract class SqsTestBase : IClassFixture<LocalStackFixture>, IAsyncDisposable
{
    protected readonly LocalStackFixture LocalStackFixture;
    protected readonly AmazonSQSClient SqsClient;
    protected readonly ISqsService SqsService;
    protected readonly ILogger<SqsService> Logger;
    
    private readonly List<string> _createdQueueUrls = new();
    private readonly string _testInstanceId;

    protected SqsTestBase(LocalStackFixture localStackFixture)
    {
        LocalStackFixture = localStackFixture ?? throw new ArgumentNullException(nameof(localStackFixture));
        SqsClient = localStackFixture.SqsClient;
        
        // Create logger for SqsService
        Logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SqsService>.Instance;
        
        // Create SqsService instance
        SqsService = new SqsService(SqsClient, Logger);
        
        // Generate unique test instance ID for queue naming
        _testInstanceId = GenerateTestInstanceId();
    }

    /// <summary>
    /// Creates a queue with a unique name for test isolation
    /// The queue will be automatically cleaned up after the test
    /// </summary>
    /// <param name="baseName">Base name for the queue (will be prefixed with test-specific identifier)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The URL of the created queue</returns>
    protected async Task<string> CreateTestQueueAsync(string baseName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(baseName))
            throw new ArgumentException("Base name cannot be null or empty", nameof(baseName));

        var uniqueQueueName = GenerateUniqueQueueName(baseName);
        var queueUrl = await SqsService.CreateQueueAsync(uniqueQueueName, cancellationToken);
        
        // Track the queue for cleanup
        _createdQueueUrls.Add(queueUrl);
        
        return queueUrl;
    }

    /// <summary>
    /// Creates multiple queues with unique names for test isolation
    /// All queues will be automatically cleaned up after the test
    /// </summary>
    /// <param name="baseNames">Base names for the queues</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping base names to queue URLs</returns>
    protected async Task<Dictionary<string, string>> CreateTestQueuesAsync(IEnumerable<string> baseNames, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>();
        
        foreach (var baseName in baseNames)
        {
            var queueUrl = await CreateTestQueueAsync(baseName, cancellationToken);
            result[baseName] = queueUrl;
        }
        
        return result;
    }

    /// <summary>
    /// Verifies that a queue exists by checking if it appears in the queue list
    /// </summary>
    /// <param name="queueUrl">The queue URL to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the queue exists, false otherwise</returns>
    protected async Task<bool> VerifyQueueExistsAsync(string queueUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queueUrl))
            throw new ArgumentException("Queue URL cannot be null or empty", nameof(queueUrl));

        var queues = await SqsService.ListQueuesAsync(cancellationToken);
        return queues.Contains(queueUrl);
    }

    /// <summary>
    /// Verifies that multiple queues exist
    /// </summary>
    /// <param name="queueUrls">The queue URLs to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping queue URLs to their existence status</returns>
    protected async Task<Dictionary<string, bool>> VerifyQueuesExistAsync(IEnumerable<string> queueUrls, CancellationToken cancellationToken = default)
    {
        var allQueues = await SqsService.ListQueuesAsync(cancellationToken);
        var result = new Dictionary<string, bool>();
        
        foreach (var queueUrl in queueUrls)
        {
            result[queueUrl] = allQueues.Contains(queueUrl);
        }
        
        return result;
    }

    /// <summary>
    /// Manually deletes a test queue (useful for testing cleanup scenarios)
    /// </summary>
    /// <param name="queueUrl">The queue URL to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected async Task DeleteTestQueueAsync(string queueUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queueUrl))
            throw new ArgumentException("Queue URL cannot be null or empty", nameof(queueUrl));

        await SqsService.DeleteQueueAsync(queueUrl, cancellationToken);
        
        // Remove from tracking list
        _createdQueueUrls.Remove(queueUrl);
    }

    /// <summary>
    /// Gets all queues created during this test instance
    /// </summary>
    /// <returns>List of queue URLs created during this test</returns>
    protected IReadOnlyList<string> GetCreatedQueues()
    {
        return _createdQueueUrls.AsReadOnly();
    }

    /// <summary>
    /// Waits for LocalStack to be ready (useful for tests that need to ensure container is fully initialized)
    /// </summary>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected async Task WaitForLocalStackReadyAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (LocalStackFixture.IsRunning)
            {
                try
                {
                    // Try a simple operation to verify SQS is responsive
                    await SqsService.ListQueuesAsync(cancellationToken);
                    return;
                }
                catch
                {
                    // Continue waiting if operation fails
                }
            }
            
            await Task.Delay(1000, cancellationToken);
        }
        
        throw new TimeoutException($"LocalStack was not ready within {timeout}");
    }

    /// <summary>
    /// Generates a unique queue name for test isolation
    /// </summary>
    /// <param name="baseName">Base name for the queue</param>
    /// <returns>Unique queue name</returns>
    private string GenerateUniqueQueueName(string baseName)
    {
        // Sanitize base name to ensure it's valid for SQS queue names
        var sanitizedBaseName = SanitizeQueueName(baseName);
        
        // Create unique name with test instance ID and timestamp
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"test-{_testInstanceId}-{sanitizedBaseName}-{timestamp}";
    }

    /// <summary>
    /// Generates a unique test instance ID
    /// </summary>
    /// <returns>Unique test instance identifier</returns>
    private static string GenerateTestInstanceId()
    {
        // Use a combination of timestamp and random value for uniqueness
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(1000, 9999);
        return $"{timestamp}-{random}";
    }

    /// <summary>
    /// Sanitizes a queue name to ensure it meets SQS naming requirements
    /// </summary>
    /// <param name="name">Original name</param>
    /// <returns>Sanitized name</returns>
    private static string SanitizeQueueName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "default";

        // SQS queue names can only contain alphanumeric characters, hyphens, and underscores
        // and must be 1-80 characters long
        var sanitized = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9\-_]", "-");
        
        // Remove consecutive hyphens and trim
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"-+", "-");
        sanitized = sanitized.Trim('-');
        
        // Ensure it's not empty and not too long
        if (string.IsNullOrEmpty(sanitized))
            sanitized = "default";
        
        if (sanitized.Length > 30) // Leave room for prefixes and suffixes
            sanitized = sanitized[..30];
            
        return sanitized;
    }

    /// <summary>
    /// Cleans up all created queues and resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // Clean up all created queues
        var cleanupTasks = _createdQueueUrls.Select(async queueUrl =>
        {
            try
            {
                await SqsService.DeleteQueueAsync(queueUrl);
            }
            catch
            {
                // Ignore cleanup errors to prevent test failures
                // In a real scenario, you might want to log these errors
            }
        });

        await Task.WhenAll(cleanupTasks);
        _createdQueueUrls.Clear();
        
        GC.SuppressFinalize(this);
    }
}