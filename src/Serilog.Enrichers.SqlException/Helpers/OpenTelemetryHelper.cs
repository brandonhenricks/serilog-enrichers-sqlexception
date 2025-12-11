using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace Serilog.Enrichers.SqlException.Helpers;

/// <summary>
/// Provides OpenTelemetry integration for SQL exceptions.
/// </summary>
internal static class OpenTelemetryHelper
{
    private const string ActivitySourceName = "Serilog.Enrichers.SqlException";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName, "1.0.0");

    /// <summary>
    /// Emits an OpenTelemetry ActivityEvent for a SQL exception.
    /// </summary>
    public static void EmitActivityEvent(Microsoft.Data.SqlClient.SqlException sqlException, Microsoft.Data.SqlClient.SqlError firstError)
    {
        var activity = Activity.Current;
        if (activity == null)
        {
            return;
        }

        var tags = new ActivityTagsCollection
        {
            { "db.system", "mssql" },
            { "error.type", "sql_exception" },
            { "db.error.code", firstError.Number },
            { "db.error.state", firstError.State },
            { "db.error.severity", firstError.Class }
        };

        if (!string.IsNullOrWhiteSpace(firstError.Server))
        {
            tags.Add("server.address", firstError.Server);
        }

        if (!string.IsNullOrWhiteSpace(firstError.Procedure))
        {
            tags.Add("db.operation", firstError.Procedure);
        }

        if (!string.IsNullOrWhiteSpace(firstError.Message))
        {
            tags.Add("exception.message", firstError.Message);
        }

        if (sqlException.ClientConnectionId != Guid.Empty)
        {
            tags.Add("db.client.connection.id", sqlException.ClientConnectionId.ToString());
        }

        var activityEvent = new ActivityEvent("db.sql_exception", tags: tags);
        activity.AddEvent(activityEvent);
    }

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
            _ => $"db.{semantic.ToLowerInvariant()}"
        };
    }
}
