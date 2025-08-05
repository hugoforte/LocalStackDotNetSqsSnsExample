using Amazon.SQS;
using Xunit;

namespace LocalStackSqsTesting.Tests.Infrastructure;

/// <summary>
/// Interface for LocalStack test fixture
/// </summary>
public interface ILocalStackFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the configured AmazonSQSClient for LocalStack
    /// </summary>
    AmazonSQSClient SqsClient { get; }

    /// <summary>
    /// Gets the LocalStack endpoint URL
    /// </summary>
    string LocalStackEndpoint { get; }

    /// <summary>
    /// Gets whether the LocalStack container is running
    /// </summary>
    bool IsRunning { get; }
}