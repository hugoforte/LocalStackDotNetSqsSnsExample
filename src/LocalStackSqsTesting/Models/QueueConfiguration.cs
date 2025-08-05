using System.ComponentModel.DataAnnotations;

namespace LocalStackSqsTesting.Models;

/// <summary>
/// Configuration model for SQS queue settings
/// </summary>
public class QueueConfiguration
{
    /// <summary>
    /// The name of the SQS queue
    /// </summary>
    [Required]
    [StringLength(80, MinimumLength = 1, ErrorMessage = "Queue name must be between 1 and 80 characters")]
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Queue attributes such as VisibilityTimeout, MessageRetentionPeriod, etc.
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();

    /// <summary>
    /// Tags to be applied to the queue
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// Validates the queue configuration
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(QueueName))
            return false;

        if (QueueName.Length > 80)
            return false;

        // Queue name validation according to AWS SQS rules
        if (!IsValidQueueName(QueueName))
            return false;

        return true;
    }

    /// <summary>
    /// Validates queue name according to AWS SQS naming rules
    /// </summary>
    private static bool IsValidQueueName(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            return false;

        // AWS SQS queue names can contain alphanumeric characters, hyphens, and underscores
        return queueName.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }
}