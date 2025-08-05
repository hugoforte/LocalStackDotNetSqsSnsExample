namespace LocalStackSqsTesting.Services;

/// <summary>
/// Interface for SQS service operations
/// </summary>
public interface ISqsService
{
    /// <summary>
    /// Creates a new SQS queue with the specified name
    /// </summary>
    /// <param name="queueName">The name of the queue to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The URL of the created queue</returns>
    Task<string> CreateQueueAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new SQS queue with the specified name and attributes
    /// </summary>
    /// <param name="queueName">The name of the queue to create</param>
    /// <param name="attributes">Queue attributes to set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The URL of the created queue</returns>
    Task<string> CreateQueueAsync(string queueName, Dictionary<string, string> attributes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available SQS queues
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of queue URLs</returns>
    Task<List<string>> ListQueuesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified SQS queue
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteQueueAsync(string queueUrl, CancellationToken cancellationToken = default);
}