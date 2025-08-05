using LocalStackSqsTesting.Tests.Infrastructure;
using Amazon.SQS.Model;
using Xunit;

namespace LocalStackSqsTesting.Tests.Infrastructure;

/// <summary>
/// Tests for LocalStackFixture to verify container management functionality
/// </summary>
public class LocalStackFixtureTests : IClassFixture<LocalStackFixture>
{
    private readonly LocalStackFixture _fixture;

    public LocalStackFixtureTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "LocalStack")]
    public void LocalStackFixture_ShouldInitializeSuccessfully()
    {
        // Arrange & Act - fixture is initialized by xUnit
        
        // Assert
        Assert.NotNull(_fixture.SqsClient);
        Assert.NotNull(_fixture.LocalStackEndpoint);
        Assert.True(_fixture.IsRunning);
        Assert.Contains("localhost:4566", _fixture.LocalStackEndpoint);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "LocalStack")]
    public async Task LocalStackFixture_SqsClient_ShouldBeConfiguredCorrectly()
    {
        // Arrange
        var sqsClient = _fixture.SqsClient;

        // Act - Try to list queues (this will verify the client is properly configured)
        var response = await sqsClient.ListQueuesAsync(new ListQueuesRequest());

        // Assert
        Assert.NotNull(response);
        // QueueUrls can be null when there are no queues, so we just verify the response is not null
        // This confirms the client is properly configured and can communicate with LocalStack
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "LocalStack")]
    public async Task LocalStackFixture_ShouldAllowQueueOperations()
    {
        // Arrange
        var sqsClient = _fixture.SqsClient;
        var queueName = $"test-queue-{Guid.NewGuid():N}";

        try
        {
            // Act - Create a queue
            var createResponse = await sqsClient.CreateQueueAsync(queueName);
            
            // Assert
            Assert.NotNull(createResponse.QueueUrl);
            Assert.Contains(queueName, createResponse.QueueUrl);

            // Verify queue exists by listing queues
            var listResponse = await sqsClient.ListQueuesAsync(new ListQueuesRequest());
            Assert.NotNull(listResponse.QueueUrls);
            Assert.Contains(createResponse.QueueUrl, listResponse.QueueUrls);
        }
        finally
        {
            // Cleanup - Delete the test queue
            try
            {
                var listResponse = await sqsClient.ListQueuesAsync(new ListQueuesRequest());
                var testQueueUrl = listResponse.QueueUrls?.FirstOrDefault(url => url.Contains(queueName));
                if (testQueueUrl != null)
                {
                    await sqsClient.DeleteQueueAsync(testQueueUrl);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}