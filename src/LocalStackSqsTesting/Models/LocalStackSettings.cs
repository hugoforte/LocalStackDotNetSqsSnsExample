using System.ComponentModel.DataAnnotations;

namespace LocalStackSqsTesting.Models;

/// <summary>
/// Configuration settings for LocalStack container management
/// </summary>
public class LocalStackSettings
{
    /// <summary>
    /// Docker image to use for LocalStack container
    /// </summary>
    public string Image { get; set; } = "localstack/localstack:latest";

    /// <summary>
    /// Port to expose LocalStack services on
    /// </summary>
    [Range(1024, 65535, ErrorMessage = "Port must be between 1024 and 65535")]
    public int Port { get; set; } = 4566;

    /// <summary>
    /// AWS services to enable in LocalStack
    /// </summary>
    public string[] Services { get; set; } = { "sqs" };

    /// <summary>
    /// Maximum time to wait for LocalStack container to start
    /// </summary>
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Environment variables to pass to the LocalStack container
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new()
    {
        { "DEBUG", "1" },
        { "DATA_DIR", "/tmp/localstack/data" }
    };

    /// <summary>
    /// AWS region to use for LocalStack
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Whether to use HTTP instead of HTTPS for LocalStack endpoint
    /// </summary>
    public bool UseHttp { get; set; } = true;

    /// <summary>
    /// Validates the LocalStack settings
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Image))
            return false;

        if (Port < 1024 || Port > 65535)
            return false;

        if (Services == null || Services.Length == 0)
            return false;

        if (StartupTimeout <= TimeSpan.Zero)
            return false;

        if (string.IsNullOrWhiteSpace(Region))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the LocalStack endpoint URL
    /// </summary>
    /// <returns>The endpoint URL</returns>
    public string GetEndpointUrl()
    {
        var protocol = UseHttp ? "http" : "https";
        return $"{protocol}://localhost:{Port}";
    }

    /// <summary>
    /// Gets the services as a comma-separated string for LocalStack environment
    /// </summary>
    /// <returns>Comma-separated services string</returns>
    public string GetServicesString()
    {
        return string.Join(",", Services);
    }
}