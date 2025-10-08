using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Messaging.Configuration
{
    /// <summary>
    /// Strongly-typed configuration for RabbitMQ connection settings.
    /// Provides validation and centralized configuration management.
    /// </summary>
    public class RabbitMqSettings
    {
        public const string SectionName = "RabbitMq";

        [Required]
        public string Host { get; set; } = "localhost";

        [Required]
        public string Username { get; set; } = "guest";

        [Required]
        public string Password { get; set; } = "guest";

        [Range(1, 10000)]
        public int Port { get; set; } = 5672;

        public string VirtualHost { get; set; } = "/";

        [Range(1, 1000)]
        public ushort PrefetchCount { get; set; } = 16;

        public bool UseSSL { get; set; } = false;

        public RetrySettings Retry { get; set; } = new();

        public ConnectionSettings Connection { get; set; } = new();

        public Dictionary<string, ExchangeSettings> Exchanges { get; set; } = new();
    }

    /// <summary>
    /// Configuration for retry behavior.
    /// </summary>
    public class RetrySettings
    {
        [Range(0, 20)]
        public int RetryCount { get; set; } = 5;

        [Range(1, 300)]
        public int IntervalSeconds { get; set; } = 5;

        public bool UseExponentialBackoff { get; set; } = true;

        [Range(1, 3600)]
        public int MaxIntervalSeconds { get; set; } = 300;
    }

    /// <summary>
    /// Configuration for connection pooling and health.
    /// </summary>
    public class ConnectionSettings
    {
        [Range(5, 300)]
        public int RequestedHeartbeat { get; set; } = 60;

        [Range(5, 120)]
        public int RequestedConnectionTimeout { get; set; } = 30;

        [Range(1, 10)]
        public int MaxConnectionRetries { get; set; } = 3;
    }

    /// <summary>
    /// Configuration for individual exchange and queue settings.
    /// </summary>
    public class ExchangeSettings
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = "topic"; // fanout, direct, topic, headers

        [Required]
        public string Queue { get; set; } = string.Empty;

        public string RoutingKey { get; set; } = string.Empty;

        public bool Durable { get; set; } = true;

        public bool AutoDelete { get; set; } = false;

        public QueueSettings QueueSettings { get; set; } = new();
    }

    /// <summary>
    /// Advanced queue settings for performance tuning.
    /// </summary>
    public class QueueSettings
    {
        public bool Exclusive { get; set; } = false;

        public Dictionary<string, object> Arguments { get; set; } = new();

        public DeadLetterSettings? DeadLetter { get; set; }
    }

    /// <summary>
    /// Dead letter queue configuration for failed messages.
    /// </summary>
    public class DeadLetterSettings
    {
        [Required]
        public string ExchangeName { get; set; } = string.Empty;

        public string RoutingKey { get; set; } = string.Empty;
    }
}
