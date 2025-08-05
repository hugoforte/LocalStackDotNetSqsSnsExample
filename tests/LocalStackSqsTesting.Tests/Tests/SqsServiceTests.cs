using Amazon.SQS;
using Amazon.SQS.Model;
using LocalStackSqsTesting.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocalStackSqsTesting.Tests.Tests;

public class SqsServiceTests
{
    private readonly Mock<AmazonSQSClient> _mockSqsClient;
    private readonly Mock<ILogger<SqsService>> _mockLogger;
    private readonly SqsService _sqsService;

    public SqsServiceTests()
    {
        _mockSqsClient = new Mock<AmazonSQSClient>();
        _mockLogger = new Mock<ILogger<SqsService>>();
        _sqsService = new SqsService(_mockSqsClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateQueueAsync_WithValidQueueName_ReturnsQueueUrl()
    {
        // Arrange
        const string queueName = "test-queue";
        const string expectedQueueUrl = "http://localhost:4566/000000000000/test-queue";
        
        var expectedResponse = new CreateQueueResponse
        {
            QueueUrl = expectedQueueUrl
        };

        _mockSqsClient
            .Setup(x => x.CreateQueueAsync(It.IsAny<CreateQueueRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sqsService.CreateQueueAsync(queueName);

        // Assert
        Assert.Equal(expectedQueueUrl, result);
        
        _mockSqsClient.Verify(
            x => x.CreateQueueAsync(
                It.Is<CreateQueueRequest>(req => req.QueueName == queueName), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateQueueAsync_WithInvalidQueueName_ThrowsArgumentException(string invalidQueueName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sqsService.CreateQueueAsync(invalidQueueName));
        
        _mockSqsClient.Verify(
            x => x.CreateQueueAsync(It.IsAny<CreateQueueRequest>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task CreateQueueAsync_WithNullQueueName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sqsService.CreateQueueAsync(null!));
        
        _mockSqsClient.Verify(
            x => x.CreateQueueAsync(It.IsAny<CreateQueueRequest>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task CreateQueueAsync_WhenSqsThrowsException_ThrowsSqsServiceException()
    {
        // Arrange
        const string queueName = "test-queue";
        const string errorMessage = "Queue already exists";
        
        var sqsException = new AmazonSQSException(errorMessage);
        
        _mockSqsClient
            .Setup(x => x.CreateQueueAsync(It.IsAny<CreateQueueRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(sqsException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SqsServiceException>(() => _sqsService.CreateQueueAsync(queueName));
        
        Assert.Contains(queueName, exception.Message);
        Assert.Contains(errorMessage, exception.Message);
        Assert.Equal("CreateQueue", exception.Operation);
        Assert.Equal(queueName, exception.QueueName);
        Assert.Equal(sqsException, exception.InnerException);
    }

    [Fact]
    public async Task ListQueuesAsync_WhenSuccessful_ReturnsQueueUrls()
    {
        // Arrange
        var expectedQueueUrls = new List<string>
        {
            "http://localhost:4566/000000000000/queue1",
            "http://localhost:4566/000000000000/queue2"
        };
        
        var expectedResponse = new ListQueuesResponse
        {
            QueueUrls = expectedQueueUrls
        };

        _mockSqsClient
            .Setup(x => x.ListQueuesAsync(It.IsAny<ListQueuesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sqsService.ListQueuesAsync();

        // Assert
        Assert.Equal(expectedQueueUrls, result);
        
        _mockSqsClient.Verify(
            x => x.ListQueuesAsync(It.IsAny<ListQueuesRequest>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ListQueuesAsync_WhenSqsThrowsException_ThrowsSqsServiceException()
    {
        // Arrange
        const string errorMessage = "Access denied";
        var sqsException = new AmazonSQSException(errorMessage);
        
        _mockSqsClient
            .Setup(x => x.ListQueuesAsync(It.IsAny<ListQueuesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(sqsException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SqsServiceException>(() => _sqsService.ListQueuesAsync());
        
        Assert.Contains(errorMessage, exception.Message);
        Assert.Equal("ListQueues", exception.Operation);
        Assert.Equal(sqsException, exception.InnerException);
    }

    [Fact]
    public async Task DeleteQueueAsync_WithValidQueueUrl_CompletesSuccessfully()
    {
        // Arrange
        const string queueUrl = "http://localhost:4566/000000000000/test-queue";
        
        _mockSqsClient
            .Setup(x => x.DeleteQueueAsync(It.IsAny<DeleteQueueRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteQueueResponse());

        // Act
        await _sqsService.DeleteQueueAsync(queueUrl);

        // Assert
        _mockSqsClient.Verify(
            x => x.DeleteQueueAsync(
                It.Is<DeleteQueueRequest>(req => req.QueueUrl == queueUrl), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteQueueAsync_WithInvalidQueueUrl_ThrowsArgumentException(string invalidQueueUrl)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sqsService.DeleteQueueAsync(invalidQueueUrl));
        
        _mockSqsClient.Verify(
            x => x.DeleteQueueAsync(It.IsAny<DeleteQueueRequest>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task DeleteQueueAsync_WithNullQueueUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sqsService.DeleteQueueAsync(null!));
        
        _mockSqsClient.Verify(
            x => x.DeleteQueueAsync(It.IsAny<DeleteQueueRequest>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task DeleteQueueAsync_WhenSqsThrowsException_ThrowsSqsServiceException()
    {
        // Arrange
        const string queueUrl = "http://localhost:4566/000000000000/test-queue";
        const string errorMessage = "Queue not found";
        
        var sqsException = new AmazonSQSException(errorMessage);
        
        _mockSqsClient
            .Setup(x => x.DeleteQueueAsync(It.IsAny<DeleteQueueRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(sqsException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SqsServiceException>(() => _sqsService.DeleteQueueAsync(queueUrl));
        
        Assert.Contains(errorMessage, exception.Message);
        Assert.Equal("DeleteQueue", exception.Operation);
        Assert.Equal(sqsException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithNullSqsClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SqsService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SqsService(_mockSqsClient.Object, null!));
    }
}