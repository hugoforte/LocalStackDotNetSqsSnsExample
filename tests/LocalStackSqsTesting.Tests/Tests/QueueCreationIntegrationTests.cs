using Amazon.SQS;
using LocalStackSqsTesting.Tests.Infrastructure;
using Xunit;

namespace LocalStackSqsTesting.Tests.Tests;

/// <summary>
/// Integration tests for SQS queue creation functionality using LocalStack
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "LocalStack")]
public class QueueCreationIntegrationTests : SqsTestBase
{
    public QueueCreationIntegrationTests(LocalStackFixture localStackFixture) 
        : base(localStackFixture)
    {
    }

    [Fact]
    public async Task CreateQueueAsync_WithValidQueueName_ReturnsValidQueueUrl()
    {
        // Arrange
        const string queueName = "basic-test-queue";

        // Act
        var queueUrl = await CreateTestQueueAsync(queueName);

        // Assert
        Assert.NotNull(queueUrl);
        Assert.NotEmpty(queueUrl);
        Assert.Contains(queueName, queueUrl);
        Assert.StartsWith("http://", queueUrl);
        
        // Verify the queue URL follows the expected LocalStack format
        // LocalStack can use different URL formats, so we'll check for a valid URL structure
        Assert.True(Uri.IsWellFormedUriString(queueUrl, UriKind.Absolute), "Queue URL should be a valid absolute URI");
    }

    [Fact]
    public async Task CreateQueueAsync_WithValidQueueName_QueueCanBeVerified()
    {
        // Arrange
        const string queueName = "verification-test-queue";

        // Act
        var queueUrl = await CreateTestQueueAsync(queueName);

        // Assert - Verify the queue exists by listing queues
        var queueExists = await VerifyQueueExistsAsync(queueUrl);
        Assert.True(queueExists, $"Queue {queueUrl} should exist after creation");
    }

    [Fact]
    public async Task CreateQueueAsync_WithCustomAttributes_ReturnsValidQueueUrl()
    {
        // Arrange
        const string queueName = "custom-attributes-queue";
        var attributes = new Dictionary<string, string>
        {
            { "VisibilityTimeout", "300" },
            { "MessageRetentionPeriod", "1209600" }, // 14 days
            { "DelaySeconds", "5" }
        };

        // Act
        var queueUrl = await SqsService.CreateQueueAsync(queueName, attributes);

        // Assert
        Assert.NotNull(queueUrl);
        Assert.NotEmpty(queueUrl);
        Assert.Contains(queueName, queueUrl);
        Assert.StartsWith("http://", queueUrl);
        
        // Track the queue for cleanup
        await CreateTestQueueAsync(queueName); // This will create a duplicate but ensures cleanup
        
        // Verify the queue exists
        var queueExists = await VerifyQueueExistsAsync(queueUrl);
        Assert.True(queueExists, $"Queue {queueUrl} with custom attributes should exist after creation");
    }

    [Fact]
    public async Task CreateQueueAsync_WithEmptyAttributes_ReturnsValidQueueUrl()
    {
        // Arrange
        const string queueName = "empty-attributes-queue";
        var emptyAttributes = new Dictionary<string, string>();

        // Act
        var queueUrl = await SqsService.CreateQueueAsync(queueName, emptyAttributes);

        // Assert
        Assert.NotNull(queueUrl);
        Assert.NotEmpty(queueUrl);
        Assert.Contains(queueName, queueUrl);
        
        // Track the queue for cleanup
        await CreateTestQueueAsync(queueName); // This will create a duplicate but ensures cleanup
        
        // Verify the queue exists
        var queueExists = await VerifyQueueExistsAsync(queueUrl);
        Assert.True(queueExists, $"Queue {queueUrl} with empty attributes should exist after creation");
    }

    [Fact]
    public async Task CreateQueueAsync_WithNullAttributes_ReturnsValidQueueUrl()
    {
        // Arrange
        const string queueName = "null-attributes-queue";

        // Act
        var queueUrl = await SqsService.CreateQueueAsync(queueName, null!);

        // Assert
        Assert.NotNull(queueUrl);
        Assert.NotEmpty(queueUrl);
        Assert.Contains(queueName, queueUrl);
        
        // Track the queue for cleanup
        await CreateTestQueueAsync(queueName); // This will create a duplicate but ensures cleanup
        
        // Verify the queue exists
        var queueExists = await VerifyQueueExistsAsync(queueUrl);
        Assert.True(queueExists, $"Queue {queueUrl} with null attributes should exist after creation");
    }

    [Fact]
    public async Task CreateQueueAsync_MultipleQueues_AllQueuesCanBeVerified()
    {
        // Arrange
        var queueNames = new[] { "multi-queue-1", "multi-queue-2", "multi-queue-3" };

        // Act
        var createdQueues = await CreateTestQueuesAsync(queueNames);

        // Assert
        Assert.Equal(queueNames.Length, createdQueues.Count);
        
        foreach (var queueName in queueNames)
        {
            Assert.True(createdQueues.ContainsKey(queueName), $"Queue {queueName} should be in created queues");
            
            var queueUrl = createdQueues[queueName];
            Assert.NotNull(queueUrl);
            Assert.NotEmpty(queueUrl);
            Assert.Contains(queueName, queueUrl);
        }

        // Verify all queues exist
        var verificationResults = await VerifyQueuesExistAsync(createdQueues.Values);
        
        foreach (var (queueUrl, exists) in verificationResults)
        {
            Assert.True(exists, $"Queue {queueUrl} should exist after creation");
        }
    }

    [Fact]
    public async Task CreateQueueAsync_UsingDirectAmazonSQSClient_ReturnsValidQueueUrl()
    {
        // Arrange
        const string queueName = "direct-client-queue";
        var uniqueQueueName = $"test-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{queueName}";

        // Act - Use the AmazonSQSClient directly (as per requirement 3.1)
        var createRequest = new Amazon.SQS.Model.CreateQueueRequest
        {
            QueueName = uniqueQueueName
        };
        
        var response = await SqsClient.CreateQueueAsync(createRequest);

        // Assert
        Assert.NotNull(response.QueueUrl);
        Assert.NotEmpty(response.QueueUrl);
        Assert.Contains(uniqueQueueName, response.QueueUrl);
        
        // Verify the queue exists by listing queues
        var listResponse = await SqsClient.ListQueuesAsync(new Amazon.SQS.Model.ListQueuesRequest());
        Assert.Contains(response.QueueUrl, listResponse.QueueUrls);
        
        // Cleanup - delete the queue
        await SqsClient.DeleteQueueAsync(new Amazon.SQS.Model.DeleteQueueRequest
        {
            QueueUrl = response.QueueUrl
        });
    }

    [Fact]
    public async Task CreateQueueAsync_WithCustomAttributesUsingDirectClient_ReturnsValidQueueUrl()
    {
        // Arrange
        const string queueName = "direct-client-custom-attrs-queue";
        var uniqueQueueName = $"test-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{queueName}";
        
        var attributes = new Dictionary<string, string>
        {
            { "VisibilityTimeout", "600" },
            { "MessageRetentionPeriod", "345600" } // 4 days
        };

        // Act - Use the AmazonSQSClient directly with custom attributes
        var createRequest = new Amazon.SQS.Model.CreateQueueRequest
        {
            QueueName = uniqueQueueName,
            Attributes = attributes
        };
        
        var response = await SqsClient.CreateQueueAsync(createRequest);

        // Assert
        Assert.NotNull(response.QueueUrl);
        Assert.NotEmpty(response.QueueUrl);
        Assert.Contains(uniqueQueueName, response.QueueUrl);
        
        // Verify the queue exists by listing queues
        var listResponse = await SqsClient.ListQueuesAsync(new Amazon.SQS.Model.ListQueuesRequest());
        Assert.Contains(response.QueueUrl, listResponse.QueueUrls);
        
        // Cleanup - delete the queue
        await SqsClient.DeleteQueueAsync(new Amazon.SQS.Model.DeleteQueueRequest
        {
            QueueUrl = response.QueueUrl
        });
    }
}