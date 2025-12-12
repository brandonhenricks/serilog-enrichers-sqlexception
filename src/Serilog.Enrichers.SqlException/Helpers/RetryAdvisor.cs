namespace Serilog.Enrichers.SqlException.Helpers;

/// <summary>
/// Provides transient error detection for SQL Server errors.
/// </summary>
internal static class RetryAdvisor
{
    /// <summary>
    /// Known transient error numbers that typically indicate temporary conditions.
    /// </summary>
    private static readonly HashSet<int> s_transientErrors = new()
    {
        // Deadlocks and lock timeouts
        1205,  // Deadlock victim
        1222,  // Lock timeout

        // Azure-specific transient errors
        40197, // Service busy
        40501, // Service busy - too many concurrent requests
        40613, // Database unavailable
        49918, // Insufficient resources
        49920, // Too many operations
        40143, // Connection initialization failed
        40540, // Service error

        // Connection issues
        -1,    // Connection timeout
        4060,  // Cannot open database
        10053, // Transport error
        10054, // Connection reset
        10060, // Network timeout
        10061, // Connection refused

        // Command timeouts
        -2,    // Command timeout
        8645,  // Memory timeout
    };

    /// <summary>
    /// Determines if the error represents a transient condition.
    /// </summary>
    public static bool ShouldRetry(int errorNumber) =>
        s_transientErrors.Contains(errorNumber);
}
