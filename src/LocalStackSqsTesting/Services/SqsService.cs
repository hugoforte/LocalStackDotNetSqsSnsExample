using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;

namespace LocalStackSqsTesting.Services;

/// <summary>
/// Service for SQS operations using AmazonSQSClient
/// </summary>
public class SqsService : ISqsService
{
    private readonly AmazonSQSClient _sqsClient;
    private readonly ILogger<SqsService> _logger;

    public SqsService(AmazonSQSClient sqsClient, ILogger<SqsService> logger)
    {
        _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> CreateQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        return await CreateQueueAsync(queueName, new Dictionary<string, string>(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> CreateQueueAsync(string queueName, Dictionary<string, string> attributes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));
        }

        attributes ??= new Dictionary<string, string>();

        _logger.LogInformation("Creating SQS queue: {QueueName} with {AttributeCount} attributes", 
            queueName, attributes.Count);

        try
        {
            var request = new CreateQueueRequest
            {
                QueueName = queueName,
                Attributes = attributes
            };

            var response = await _sqsClient.CreateQueueAsync(request, cancellationToken);
            
            _logger.LogInformation("Successfully created SQS queue: {QueueName} with URL: {QueueUrl}", 
                queueName, response.QueueUrl);

            return response.QueueUrl;
        }
        catch (AmazonSQSException ex)
        {
            _logger.LogError(ex, "Failed to create SQS queue: {QueueName}. Error: {ErrorMessage}", 
                queueName, ex.Message);
            
            throw new SqsServiceException(
                $"Failed to create queue '{queueName}': {ex.Message}", 
                ex, 
                "CreateQueue", 
                queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating SQS queue: {QueueName}", queueName);
            
            throw new SqsServiceException(
                $"Unexpected error creating queue '{queueName}': {ex.Message}", 
                ex, 
                "CreateQueue", 
                queueName);
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> ListQueuesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing all SQS queues");

        try
        {
            var request = new ListQueuesRequest();
            var response = await _sqsClient.ListQueuesAsync(request, cancellationToken);
            
            _logger.LogInformation("Successfully listed {QueueCount} SQS queues", response.QueueUrls.Count);

            return response.QueueUrls;
        }
        catch (AmazonSQSException ex)
        {
            _logger.LogError(ex, "Failed to list SQS queues. Error: {ErrorMessage}", ex.Message);
            
            throw new SqsServiceException(
                $"Failed to list queues: {ex.Message}", 
                ex, 
                "ListQueues");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing SQS queues");
            
            throw new SqsServiceException(
                $"Unexpected error listing queues: {ex.Message}", 
                ex, 
                "ListQueues");
        }
    }

    /// <inheritdoc />
    public async Task DeleteQueueAsync(string queueUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            throw new ArgumentException("Queue URL cannot be null or empty", nameof(queueUrl));
        }

        _logger.LogInformation("Deleting SQS queue: {QueueUrl}", queueUrl);

        try
        {
            var request = new DeleteQueueRequest
            {
                QueueUrl = queueUrl
            };

            await _sqsClient.DeleteQueueAsync(request, cancellationToken);
            
            _logger.LogInformation("Successfully deleted SQS queue: {QueueUrl}", queueUrl);
        }
        catch (AmazonSQSException ex)
        {
            _logger.LogError(ex, "Failed to delete SQS queue: {QueueUrl}. Error: {ErrorMessage}", 
                queueUrl, ex.Message);
            
            throw new SqsServiceException(
                $"Failed to delete queue '{queueUrl}': {ex.Message}", 
                ex, 
                "DeleteQueue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting SQS queue: {QueueUrl}", queueUrl);
            
            throw new SqsServiceException(
                $"Unexpected error deleting queue '{queueUrl}': {ex.Message}", 
                ex, 
                "DeleteQueue");
        }
    }
}