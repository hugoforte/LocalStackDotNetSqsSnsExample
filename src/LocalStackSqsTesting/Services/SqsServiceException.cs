namespace LocalStackSqsTesting.Services;

/// <summary>
/// Exception thrown by SQS service operations
/// </summary>
public class SqsServiceException : Exception
{
    public string? Operation { get; }
    public string? QueueName { get; }

    public SqsServiceException(string message) : base(message)
    {
    }

    public SqsServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public SqsServiceException(string message, string operation, string? queueName = null) : base(message)
    {
        Operation = operation;
        QueueName = queueName;
    }

    public SqsServiceException(string message, Exception innerException, string operation, string? queueName = null) 
        : base(message, innerException)
    {
        Operation = operation;
        QueueName = queueName;
    }
}