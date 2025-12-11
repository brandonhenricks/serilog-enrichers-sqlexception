using System.Collections;
using Microsoft.Data.SqlClient;
using Serilog.Core;
using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Helpers;
using Serilog.Enrichers.SqlException.Models;
using Serilog.Events;

namespace Serilog.Enrichers.SqlException.Enrichers;

/// <summary>
/// Enriches log events with SQL Server exception details.
/// </summary>
public class SqlExceptionEnricher : ILogEventEnricher
{
    private readonly SqlExceptionEnricherOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlExceptionEnricher"/> class with default options.
    /// </summary>
    public SqlExceptionEnricher()
        : this(new SqlExceptionEnricherOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlExceptionEnricher"/> class with the specified options.
    /// </summary>
    /// <param name="options">The enricher configuration options.</param>
    public SqlExceptionEnricher(SqlExceptionEnricherOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        if (_options.EnableDiagnostics)
        {
            _options.DiagnosticLogger?.Invoke("SqlExceptionEnricher initialized with configured options");
        }
    }

    /// <summary>
    /// Enriches the log event with SQL Server exception details if a SQL exception is found in the exception chain.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent?.Exception == null)
        {
            return;
        }

        var sqlException = GetSqlException(logEvent.Exception);

        if (sqlException == null || sqlException.Errors.Count == 0)
        {
            if (_options.EnableDiagnostics)
            {
                _options.DiagnosticLogger?.Invoke("No SqlException found in exception chain");
            }
            return;
        }

        if (_options.EnableDiagnostics)
        {
            _options.DiagnosticLogger?.Invoke($"SqlException found with {sqlException.Errors.Count} error(s)");
        }

        // Add marker property
        AddProperty(logEvent, propertyFactory, "IsSqlException", true);

        // Add error count
        AddProperty(logEvent, propertyFactory, "ErrorCount", sqlException.Errors.Count);

        // Enrich with first error details
        var firstError = sqlException.Errors[0];

        EnrichWithSqlError(logEvent, propertyFactory, firstError, string.Empty);

        // Optionally enrich with all errors
        if (_options.IncludeAllErrors && sqlException.Errors.Count > 1)
        {
            EnrichWithAllErrors(logEvent, propertyFactory, sqlException);
        }

        // Add connection context if enabled
        if (_options.IncludeConnectionContext)
        {
            EnrichWithConnectionContext(logEvent, propertyFactory, sqlException);
        }

        // Detect transient failures if enabled (legacy - use ProvideRetryGuidance for comprehensive info)
#pragma warning disable CS0618 // Type or member is obsolete
        if (_options.DetectTransientFailures)
        {
            // Delegate to RetryAdvisor - single source of truth for retry logic
            var isTransient = RetryAdvisor.ShouldRetry(firstError.Number);
            AddProperty(logEvent, propertyFactory, "IsTransient", isTransient);
        }
#pragma warning restore CS0618 // Type or member is obsolete

        // Detect deadlocks if enabled
        if (_options.DetectDeadlocks)
        {
            EnrichWithDeadlockDetection(logEvent, propertyFactory, firstError);
        }

        // Classify timeouts if enabled
        if (_options.ClassifyTimeouts)
        {
            EnrichWithTimeoutClassification(logEvent, propertyFactory, firstError);
        }

        // Categorize errors if enabled
        if (_options.CategorizeErrors)
        {
            EnrichWithErrorCategorization(logEvent, propertyFactory, firstError);
        }

        // Provide retry guidance if enabled
        if (_options.ProvideRetryGuidance)
        {
            EnrichWithRetryGuidance(logEvent, propertyFactory, firstError);
        }

        // Include severity level if enabled
        if (_options.IncludeSeverityLevel)
        {
            EnrichWithSeverityLevel(logEvent, propertyFactory, firstError);
        }

        // Emit OpenTelemetry events if enabled
        EmitOpenTelemetryEvent(sqlException, firstError);

        if (_options.EnableDiagnostics)
        {
            _options.DiagnosticLogger?.Invoke($"Enrichment complete - {logEvent.Properties.Count} total properties");
        }
    }

    private void EnrichWithSqlError(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, SqlError error, string suffix)
    {
        AddProperty(logEvent, propertyFactory, $"Number{suffix}", error.Number);
        AddProperty(logEvent, propertyFactory, $"State{suffix}", error.State);
        AddProperty(logEvent, propertyFactory, $"Class{suffix}", error.Class);
        AddProperty(logEvent, propertyFactory, $"Line{suffix}", error.LineNumber);

        if (!string.IsNullOrWhiteSpace(error.Procedure))
        {
            AddProperty(logEvent, propertyFactory, $"Procedure{suffix}", error.Procedure);
        }

        if (!string.IsNullOrWhiteSpace(error.Server))
        {
            AddProperty(logEvent, propertyFactory, $"Server{suffix}", error.Server);
        }

        if (!string.IsNullOrWhiteSpace(error.Message))
        {
            AddProperty(logEvent, propertyFactory, $"Message{suffix}", error.Message);
        }
    }

    private void EnrichWithAllErrors(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, Microsoft.Data.SqlClient.SqlException sqlException)
    {
        var errorNumbers = new List<int>();
        var errorMessages = new List<string>();
        var errorStates = new List<byte>();
        var errorClasses = new List<byte>();

        foreach (Microsoft.Data.SqlClient.SqlError error in sqlException.Errors)
        {
            errorNumbers.Add(error.Number);
            errorStates.Add(error.State);
            errorClasses.Add(error.Class);

            if (!string.IsNullOrWhiteSpace(error.Message))
            {
                errorMessages.Add(error.Message);
            }
        }

        AddProperty(logEvent, propertyFactory, "AllNumbers", errorNumbers);
        AddProperty(logEvent, propertyFactory, "AllStates", errorStates);
        AddProperty(logEvent, propertyFactory, "AllClasses", errorClasses);

        if (errorMessages.Count > 0)
        {
            AddProperty(logEvent, propertyFactory, "AllMessages", errorMessages);
        }
    }

    private void EnrichWithConnectionContext(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, Microsoft.Data.SqlClient.SqlException sqlException)
    {
        if (sqlException.Data is IDictionary data)
        {
            if (data["DataSource"] is string dataSource)
            {
                AddProperty(logEvent, propertyFactory, "DataSource", dataSource);
            }

            if (data["Database"] is string database)
            {
                AddProperty(logEvent, propertyFactory, "Database", database);
            }

            if (data["ConnectionTimeout"] is int timeout)
            {
                AddProperty(logEvent, propertyFactory, "ConnectionTimeout", timeout);
            }
        }

        // ClientConnectionId is available on SqlException directly
        if (sqlException.ClientConnectionId != Guid.Empty)
        {
            AddProperty(logEvent, propertyFactory, "ClientConnectionId", sqlException.ClientConnectionId);
        }
    }

    private void EnrichWithDeadlockDetection(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, SqlError error)
    {
        var isDeadlock = SqlErrorCategorizer.IsDeadlock(error.Number);
        AddProperty(logEvent, propertyFactory, "IsDeadlock", isDeadlock);
    }

    private void EnrichWithTimeoutClassification(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, SqlError error)
    {
        var isTimeout = SqlErrorCategorizer.IsTimeout(error.Number);
        AddProperty(logEvent, propertyFactory, "IsTimeout", isTimeout);

        if (isTimeout)
        {
            var timeoutType = SqlErrorCategorizer.GetTimeoutType(error.Number);
            AddProperty(logEvent, propertyFactory, "TimeoutType", timeoutType.ToString());
        }
    }

    private void EnrichWithErrorCategorization(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, SqlError error)
    {
        var category = SqlErrorCategorizer.GetCategory(error.Number);
        AddProperty(logEvent, propertyFactory, "ErrorCategory", category.ToString());

        var isUserError = SqlErrorCategorizer.IsUserError(error.Number);
        AddProperty(logEvent, propertyFactory, "IsUserError", isUserError);
        AddProperty(logEvent, propertyFactory, "IsSystemError", !isUserError);
    }

    private void EnrichWithRetryGuidance(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, SqlError error)
    {
        var shouldRetry = RetryAdvisor.ShouldRetry(error.Number);
        AddProperty(logEvent, propertyFactory, "ShouldRetry", shouldRetry);
        AddProperty(logEvent, propertyFactory, "RetryStrategy", RetryAdvisor.GetRetryStrategy(error.Number));
        AddProperty(logEvent, propertyFactory, "SuggestedRetryDelay", RetryAdvisor.GetSuggestedDelay(error.Number));
        AddProperty(logEvent, propertyFactory, "MaxRetries", RetryAdvisor.GetMaxRetries(error.Number));

        var reason = RetryAdvisor.GetRetryReason(error.Number);
        if (!string.IsNullOrWhiteSpace(reason))
        {
            AddProperty(logEvent, propertyFactory, "RetryReason", reason);
        }

        if (_options.EnableDiagnostics)
        {
            _options.DiagnosticLogger?.Invoke($"Retry guidance: {(shouldRetry ? "Retry recommended" : "Do not retry")} - {reason}");
        }
    }

    private void EnrichWithSeverityLevel(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, SqlError error)
    {
        var severityLevel = error.Class switch
        {
            <= SqlExceptionConstants.SeverityThresholds.Informational => SqlSeverityLevel.Informational,
            <= SqlExceptionConstants.SeverityThresholds.Warning => SqlSeverityLevel.Warning,
            <= SqlExceptionConstants.SeverityThresholds.Error => SqlSeverityLevel.Error,
            <= SqlExceptionConstants.SeverityThresholds.Severe => SqlSeverityLevel.Severe,
            <= SqlExceptionConstants.SeverityThresholds.Critical => SqlSeverityLevel.Critical,
            _ => SqlSeverityLevel.Fatal
        };

        AddProperty(logEvent, propertyFactory, "SeverityLevel", severityLevel.ToString());
        
        // Class 20+ indicates severe system problems requiring immediate attention
        AddProperty(logEvent, propertyFactory, "RequiresImmediateAttention", 
            error.Class >= SqlExceptionConstants.SeverityThresholds.ImmediateAttentionRequired);
    }

    private void EmitOpenTelemetryEvent(Microsoft.Data.SqlClient.SqlException sqlException, SqlError firstError)
    {
        if (_options.EmitActivityEvents)
        {
            OpenTelemetryHelper.EmitActivityEvent(sqlException, firstError);

            if (_options.EnableDiagnostics)
            {
                _options.DiagnosticLogger?.Invoke("ActivityEvent emitted for OpenTelemetry");
            }
        }
    }

    private void AddProperty(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, string name, object value)
    {
        var propertyName = _options.UseOpenTelemetrySemantics
            ? OpenTelemetryHelper.GetOtelPropertyName(name)
            : $"{_options.PropertyPrefix}{name}";

        var property = propertyFactory.CreateProperty(propertyName, value);
        logEvent.AddPropertyIfAbsent(property);
    }

    private static Microsoft.Data.SqlClient.SqlException? GetSqlException(Exception exception)
    {
        var visited = new HashSet<Exception>();
        var current = exception;

        while (current != null)
        {
            if (!visited.Add(current))
            {
                // Circular reference detected
                break;
            }

            if (current is Microsoft.Data.SqlClient.SqlException sqlException)
            {
                return sqlException;
            }

            current = current.InnerException;
        }

        return null;
    }
}
