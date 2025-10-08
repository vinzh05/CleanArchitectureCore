using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Messaging.Configuration
{
    /// <summary>
    /// Strongly-typed configuration for Outbox pattern.
    /// Controls message publishing behavior and performance.
    /// </summary>
    public class OutboxSettings
    {
        public const string SectionName = "Outbox";

        [Range(1, 300)]
        public int PollIntervalSeconds { get; set; } = 5;

        [Range(1, 1000)]
        public int BatchSize { get; set; } = 50;

        [Range(1, 32)]
        public int MaxDegreeOfParallelism { get; set; } = 4;

        [Range(1, 100)]
        public int MaxRetryCount { get; set; } = 10;

        [Range(1, 3600)]
        public int RetryDelaySeconds { get; set; } = 60;

        public bool EnableCircuitBreaker { get; set; } = true;

        [Range(1, 100)]
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        [Range(10, 3600)]
        public int CircuitBreakerResetTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Enable idempotency tracking to prevent duplicate processing.
        /// </summary>
        public bool EnableIdempotency { get; set; } = true;

        /// <summary>
        /// Time to keep processed message IDs in cache (in hours).
        /// </summary>
        [Range(1, 720)]
        public int IdempotencyCacheHours { get; set; } = 24;
    }
}
