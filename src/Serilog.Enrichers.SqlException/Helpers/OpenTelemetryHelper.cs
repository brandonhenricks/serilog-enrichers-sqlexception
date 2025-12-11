namespace Serilog.Enrichers.SqlException.Helpers;

/// <summary>
/// Provides OpenTelemetry semantic convention mappings for SQL exception properties.
/// </summary>
internal static class OpenTelemetryHelper
{
    /// <summary>
    /// Gets the OpenTelemetry-compliant property name for a given semantic.
    /// </summary>
    public static string GetOtelPropertyName(string semantic)
    {
        return semantic switch
        {
            "IsSqlException" => "db.exception.sql",
            "Number" => "db.error.code",
            "State" => "db.error.state",
            "Class" => "db.error.severity",
            "Message" => "exception.message",
            "Procedure" => "db.operation",
            "Server" => "server.address",
            "Database" => "db.name",
            "DataSource" => "server.address",
            "ClientConnectionId" => "db.client.connection.id",
            "IsTransient" => "db.error.transient",
            "IsDeadlock" => "db.error.deadlock",
            "IsTimeout" => "db.error.timeout",
            "TimeoutType" => "db.error.timeout.type",
            "ErrorCategory" => "db.error.category",
            "IsUserError" => "db.error.user_caused",
            "IsSystemError" => "db.error.system_caused",
            "DeadlockGraph" => "db.deadlock.graph",
            "ErrorCount" => "db.error.count",
            "AllNumbers" => "db.error.all_codes",
            "AllStates" => "db.error.all_states",
            "AllClasses" => "db.error.all_severities",
            "AllMessages" => "db.error.all_messages",
            "Line" => "db.error.line",
            "ConnectionTimeout" => "db.connection.timeout",
            "ShouldRetry" => "db.error.retry.recommended",
            "RetryStrategy" => "db.error.retry.strategy",
            "SuggestedRetryDelay" => "db.error.retry.delay",
            "MaxRetries" => "db.error.retry.max_attempts",
            "RetryReason" => "db.error.retry.reason",
            "SeverityLevel" => "db.error.severity.level",
            "RequiresImmediateAttention" => "db.error.critical",
            _ => $"db.{semantic.ToLowerInvariant()}"
        };
    }
}
