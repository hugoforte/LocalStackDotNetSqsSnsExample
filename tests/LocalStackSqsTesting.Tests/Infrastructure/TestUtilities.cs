using Amazon.SQS;
using Amazon.SQS.Model;
using LocalStackSqsTesting.Models;

namespace LocalStackSqsTesting.Tests.Infrastructure;

/// <summary>
/// Utility class providing helper methods for SQS testing
/// </summary>
public static class TestUtilities
{
    /// <summary>
    /// Creates a QueueConfiguration for testing with default values
    /// </summary>
    /// <param name="queueName">Name of the queue</param>
    /// <param name="attributes">Optional queue attributes</param>
    /// <param name="tags">Optional queue tags</param>
    /// <returns>Configured QueueConfiguration instance</returns>
    public static QueueConfiguration CreateTestQueueConfiguration(
        string queueName,
        Dictionary<string, string>? attributes = null,
        Dictionary<string, string>? tags = null)
    {
        return new QueueConfiguration
        {
            QueueName = queueName,
            Attributes = attributes ?? new Dictionary<string, string>(),
            Tags = tags ?? new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// Creates a QueueConfiguration with common test attributes
    /// </summary>
    /// <param name="queueName">Name of the queue</param>
    /// <param name="visibilityTimeoutSeconds">Visibility timeout in seconds (default: 30)</param>
    /// <param name="messageRetentionPeriodSeconds">Message retention period in seconds (default: 1209600 = 14 days)</param>
    /// <returns>Configured QueueConfiguration instance</returns>
    public static QueueConfiguration CreateTestQueueConfigurationWithAttributes(
        string queueName,
        int visibilityTimeoutSeconds = 30,
        int messageRetentionPeriodSeconds = 1209600)
    {
        var attributes = new Dictionary<string, string>
        {
            [QueueAttributeName.VisibilityTimeout] = visibilityTimeoutSeconds.ToString(),
            [QueueAttributeName.MessageRetentionPeriod] = messageRetentionPeriodSeconds.ToString()
        };

        return CreateTestQueueConfiguration(queueName, attributes);
    }

    /// <summary>
    /// Extracts the queue name from a queue URL
    /// </summary>
    /// <param name="queueUrl">The queue URL</param>
    /// <returns>The queue name</returns>
    public static string ExtractQueueNameFromUrl(string queueUrl)
    {
        if (string.IsNullOrWhiteSpace(queueUrl))
            throw new ArgumentException("Queue URL cannot be null or empty", nameof(queueUrl));

        // Queue URL format: http://localhost:4566/000000000000/queue-name
        var uri = new Uri(queueUrl);
        var segments = uri.Segments;
        
        if (segments.Length < 3)
            throw new ArgumentException($"Invalid queue URL format: {queueUrl}", nameof(queueUrl));

        return segments[^1]; // Last segment is the queue name
    }

    /// <summary>
    /// Validates that a queue name follows SQS naming conventions
    /// </summary>
    /// <param name="queueName">The queue name to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidQueueName(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            return false;

        // SQS queue name requirements:
        // - 1-80 characters
        // - Alphanumeric characters, hyphens, and underscores only
        // - Cannot start or end with hyphen or underscore
        if (queueName.Length > 80)
            return false;

        if (queueName.StartsWith('-') || queueName.StartsWith('_') ||
            queueName.EndsWith('-') || queueName.EndsWith('_'))
            return false;

        return System.Text.RegularExpressions.Regex.IsMatch(queueName, @"^[a-zA-Z0-9\-_]+$");
    }

    /// <summary>
    /// Generates a collection of test queue names with different patterns
    /// </summary>
    /// <param name="basePrefix">Base prefix for all queue names</param>
    /// <param name="count">Number of queue names to generate</param>
    /// <returns>Collection of unique queue names</returns>
    public static IEnumerable<string> GenerateTestQueueNames(string basePrefix, int count)
    {
        if (string.IsNullOrWhiteSpace(basePrefix))
            throw new ArgumentException("Base prefix cannot be null or empty", nameof(basePrefix));

        if (count <= 0)
            throw new ArgumentException("Count must be greater than zero", nameof(count));

        var names = new List<string>();
        for (int i = 1; i <= count; i++)
        {
            names.Add($"{basePrefix}-{i:D3}");
        }

        return names;
    }

    /// <summary>
    /// Creates a test-specific LocalStackSettings with optimized values for testing
    /// </summary>
    /// <param name="startupTimeoutMinutes">Startup timeout in minutes (default: 2)</param>
    /// <returns>Configured LocalStackSettings instance</returns>
    public static LocalStackSettings CreateTestLocalStackSettings(int startupTimeoutMinutes = 2)
    {
        return new LocalStackSettings
        {
            StartupTimeout = TimeSpan.FromMinutes(startupTimeoutMinutes),
            // Use latest stable image for testing
            Image = "localstack/localstack:latest",
            // Only enable SQS service for faster startup
            Services = new[] { "sqs" }
        };
    }

    /// <summary>
    /// Waits for a condition to be met with exponential backoff
    /// </summary>
    /// <param name="condition">The condition to check</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="initialDelay">Initial delay between checks</param>
    /// <param name="maxDelay">Maximum delay between checks</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if condition was met, false if timeout occurred</returns>
    public static async Task<bool> WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        CancellationToken cancellationToken = default)
    {
        initialDelay ??= TimeSpan.FromMilliseconds(100);
        maxDelay ??= TimeSpan.FromSeconds(5);

        var startTime = DateTime.UtcNow;
        var currentDelay = initialDelay.Value;

        while (DateTime.UtcNow - startTime < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await condition())
                return true;

            await Task.Delay(currentDelay, cancellationToken);

            // Exponential backoff with max limit
            currentDelay = TimeSpan.FromMilliseconds(Math.Min(
                currentDelay.TotalMilliseconds * 1.5,
                maxDelay.Value.TotalMilliseconds));
        }

        return false;
    }

    /// <summary>
    /// Retries an operation with exponential backoff
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to retry</param>
    /// <param name="maxAttempts">Maximum number of attempts</param>
    /// <param name="initialDelay">Initial delay between attempts</param>
    /// <param name="maxDelay">Maximum delay between attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    /// <exception cref="InvalidOperationException">Thrown when all retry attempts are exhausted</exception>
    public static async Task<T> RetryWithBackoffAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        CancellationToken cancellationToken = default)
    {
        initialDelay ??= TimeSpan.FromMilliseconds(100);
        maxDelay ??= TimeSpan.FromSeconds(5);

        var currentDelay = initialDelay.Value;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == maxAttempts)
                    break;

                await Task.Delay(currentDelay, cancellationToken);

                // Exponential backoff with max limit
                currentDelay = TimeSpan.FromMilliseconds(Math.Min(
                    currentDelay.TotalMilliseconds * 2,
                    maxDelay.Value.TotalMilliseconds));
            }
        }

        throw new InvalidOperationException(
            $"Operation failed after {maxAttempts} attempts", lastException);
    }

    /// <summary>
    /// Asserts that a queue URL has the expected format
    /// </summary>
    /// <param name="queueUrl">The queue URL to validate</param>
    /// <param name="expectedQueueName">Expected queue name (optional)</param>
    /// <exception cref="ArgumentException">Thrown when queue URL format is invalid</exception>
    public static void AssertValidQueueUrl(string queueUrl, string? expectedQueueName = null)
    {
        if (string.IsNullOrWhiteSpace(queueUrl))
            throw new ArgumentException("Queue URL cannot be null or empty");

        if (!Uri.TryCreate(queueUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Queue URL is not a valid URI: {queueUrl}");

        // Check that it looks like a LocalStack SQS URL
        if (!queueUrl.Contains("localhost") && !queueUrl.Contains("127.0.0.1"))
            throw new ArgumentException($"Queue URL does not appear to be a LocalStack URL: {queueUrl}");

        if (expectedQueueName != null)
        {
            var actualQueueName = ExtractQueueNameFromUrl(queueUrl);
            if (actualQueueName != expectedQueueName)
                throw new ArgumentException(
                    $"Queue URL contains unexpected queue name. Expected: {expectedQueueName}, Actual: {actualQueueName}");
        }
    }
}