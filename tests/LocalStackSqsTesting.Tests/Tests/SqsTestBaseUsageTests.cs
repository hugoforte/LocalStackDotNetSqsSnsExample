using LocalStackSqsTesting.Tests.Infrastructure;
using Xunit;

namespace LocalStackSqsTesting.Tests.Tests;

/// <summary>
/// Tests demonstrating the usage of SqsTestBase and its helper utilities
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "LocalStack")]
public class SqsTestBaseUsageTests : SqsTestBase
{
    public SqsTestBaseUsageTests(LocalStackFixture localStackFixture) : base(localStackFixture)
    {
    }

    [Fact]
    public async Task CreateTestQueueAsync_CreatesUniqueQueue_ReturnsValidQueueUrl()
    {
        // Act
        var queueUrl = await CreateTestQueueAsync("sample-queue");

        // Assert
        Assert.NotNull(queueUrl);
        Assert.NotEmpty(queueUrl);
        TestUtilities.AssertValidQueueUrl(queueUrl);
        
        // Verify the queue exists
        var exists = await VerifyQueueExistsAsync(queueUrl);
        Assert.True(exists);
        
        // Verify it's tracked for cleanup
        var createdQueues = GetCreatedQueues();
        Assert.Contains(queueUrl, createdQueues);
    }

    [Fact]
    public async Task CreateTestQueuesAsync_CreatesMultipleQueues_AllQueuesExist()
    {
        // Arrange
        var baseNames = new[] { "queue-1", "queue-2", "queue-3" };

        // Act
        var queueUrls = await CreateTestQueuesAsync(baseNames);

        // Assert
        Assert.Equal(3, queueUrls.Count);
        
        foreach (var baseName in baseNames)
        {
            Assert.True(queueUrls.ContainsKey(baseName));
            TestUtilities.AssertValidQueueUrl(queueUrls[baseName]);
        }

        // Verify all queues exist
        var existenceStatus = await VerifyQueuesExistAsync(queueUrls.Values);
        Assert.All(existenceStatus.Values, exists => Assert.True(exists));
        
        // Verify all are tracked for cleanup
        var createdQueues = GetCreatedQueues();
        Assert.Equal(3, createdQueues.Count);
    }

    [Fact]
    public async Task DeleteTestQueueAsync_RemovesQueueFromTracking()
    {
        // Arrange
        var queueUrl = await CreateTestQueueAsync("temp-queue");
        Assert.Contains(queueUrl, GetCreatedQueues());

        // Act
        await DeleteTestQueueAsync(queueUrl);

        // Assert
        var exists = await VerifyQueueExistsAsync(queueUrl);
        Assert.False(exists);
        
        // Verify it's no longer tracked
        var createdQueues = GetCreatedQueues();
        Assert.DoesNotContain(queueUrl, createdQueues);
    }

    [Fact]
    public async Task WaitForLocalStackReadyAsync_WhenContainerIsRunning_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await WaitForLocalStackReadyAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task UniqueQueueNaming_MultipleTestInstances_GeneratesDifferentNames()
    {
        // Act
        var queue1 = await CreateTestQueueAsync("test");
        var queue2 = await CreateTestQueueAsync("test");

        // Assert
        Assert.NotEqual(queue1, queue2);
        
        var name1 = TestUtilities.ExtractQueueNameFromUrl(queue1);
        var name2 = TestUtilities.ExtractQueueNameFromUrl(queue2);
        Assert.NotEqual(name1, name2);
        
        // Both should exist
        Assert.True(await VerifyQueueExistsAsync(queue1));
        Assert.True(await VerifyQueueExistsAsync(queue2));
    }
}