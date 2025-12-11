namespace Serilog.Enrichers.SqlException.Helpers;

/// <summary>
/// Provides retry guidance for SQL Server errors based on error characteristics.
/// </summary>
internal static class RetryAdvisor
{
    private static readonly Dictionary<int, RetryGuidance> s_retryGuidance = new()
    {
        // Transient - safe to retry with exponential backoff
        [1205] = new(true, "Exponential", "100ms", 3, "Deadlock victim - transient conflict, safe to retry"),
        [1222] = new(true, "Exponential", "200ms", 3, "Lock timeout - retry with exponential backoff"),
        [40197] = new(true, "Exponential", "1s", 3, "Azure service busy - retry with backoff"),
        [40501] = new(true, "Exponential", "2s", 3, "Azure service busy - too many concurrent requests"),
        [40613] = new(true, "Exponential", "1s", 3, "Azure database unavailable - temporary issue"),
        [49918] = new(true, "Exponential", "2s", 2, "Insufficient resources - retry with backoff"),
        [49920] = new(true, "Exponential", "2s", 2, "Too many operations - retry with backoff"),
        
        // Connection issues - retry with linear backoff
        [-1] = new(true, "Linear", "5s", 2, "Connection timeout - network issue, retry with delay"),
        [4060] = new(true, "Linear", "3s", 2, "Cannot open database - may be starting up"),
        [10053] = new(true, "Linear", "2s", 2, "Transport error - network instability"),
        [10054] = new(true, "Linear", "2s", 2, "Connection reset - network issue"),
        [10060] = new(true, "Linear", "3s", 2, "Network timeout - connectivity issue"),
        [10061] = new(true, "Linear", "3s", 2, "Connection refused - service may be restarting"),
        [40143] = new(true, "Linear", "3s", 2, "Connection initialization failed - retry"),
        [40540] = new(true, "Linear", "5s", 2, "Azure service error - temporary unavailability"),
        
        // Command timeouts - retry once with caution
        [-2] = new(true, "Linear", "10s", 1, "Command timeout - optimize query or increase timeout, then retry"),
        [8645] = new(true, "Linear", "5s", 1, "Memory timeout - retry once after delay"),
        
        // User errors - do not retry
        [102] = new(false, "None", "0ms", 0, "Syntax error - fix SQL statement"),
        [156] = new(false, "None", "0ms", 0, "Syntax error near keyword - fix SQL"),
        [207] = new(false, "None", "0ms", 0, "Invalid column name - fix query"),
        [208] = new(false, "None", "0ms", 0, "Invalid object name - verify table/view exists"),
        [213] = new(false, "None", "0ms", 0, "Column mismatch - fix INSERT statement"),
        [229] = new(false, "None", "0ms", 0, "Permission denied - grant necessary permissions"),
        [230] = new(false, "None", "0ms", 0, "Execute permission denied - grant EXECUTE permission"),
        [262] = new(false, "None", "0ms", 0, "Permission denied - check user permissions"),
        [297] = new(false, "None", "0ms", 0, "Permission denied - insufficient privileges"),
        [18456] = new(false, "None", "0ms", 0, "Login failed - check credentials"),
        [547] = new(false, "None", "0ms", 0, "Foreign key violation - verify related data exists"),
        [2601] = new(false, "None", "0ms", 0, "Duplicate key in unique index - check data uniqueness"),
        [2627] = new(false, "None", "0ms", 0, "Primary key violation - check for duplicate values"),
        [8152] = new(false, "None", "0ms", 0, "String truncation - reduce data length or increase column size"),
        
        // System errors - do not retry
        [823] = new(false, "None", "0ms", 0, "I/O error - investigate storage subsystem"),
        [824] = new(false, "None", "0ms", 0, "Consistency error - run DBCC CHECKDB"),
        [825] = new(false, "None", "0ms", 0, "Read retry - investigate disk issues"),
    };

    /// <summary>
    /// Determines if the error is safe to retry.
    /// </summary>
    public static bool ShouldRetry(int errorNumber) =>
        s_retryGuidance.TryGetValue(errorNumber, out var guidance) && guidance.ShouldRetry;

    /// <summary>
    /// Gets the recommended retry strategy for the error.
    /// </summary>
    public static string GetRetryStrategy(int errorNumber) =>
        s_retryGuidance.TryGetValue(errorNumber, out var guidance) ? guidance.Strategy : "None";

    /// <summary>
    /// Gets the suggested initial delay before retry.
    /// </summary>
    public static string GetSuggestedDelay(int errorNumber) =>
        s_retryGuidance.TryGetValue(errorNumber, out var guidance) ? guidance.SuggestedDelay : "0ms";

    /// <summary>
    /// Gets the recommended maximum number of retry attempts.
    /// </summary>
    public static int GetMaxRetries(int errorNumber) =>
        s_retryGuidance.TryGetValue(errorNumber, out var guidance) ? guidance.MaxRetries : 0;

    /// <summary>
    /// Gets a human-readable explanation of the retry recommendation.
    /// </summary>
    public static string GetRetryReason(int errorNumber) =>
        s_retryGuidance.TryGetValue(errorNumber, out var guidance) ? guidance.Reason : string.Empty;

    private readonly struct RetryGuidance
    {
        public RetryGuidance(bool shouldRetry, string strategy, string suggestedDelay, int maxRetries, string reason)
        {
            ShouldRetry = shouldRetry;
            Strategy = strategy;
            SuggestedDelay = suggestedDelay;
            MaxRetries = maxRetries;
            Reason = reason;
        }

        public bool ShouldRetry { get; }
        public string Strategy { get; }
        public string SuggestedDelay { get; }
        public int MaxRetries { get; }
        public string Reason { get; }
    }
}
