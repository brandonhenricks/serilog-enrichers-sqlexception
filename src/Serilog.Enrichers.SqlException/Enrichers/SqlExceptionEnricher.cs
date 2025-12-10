using System.Collections;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Serilog.Core;
using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Events;

namespace Serilog.Enrichers.SqlException.Enrichers;

/// <summary>
/// Enriches log events with SQL Server exception details.
/// </summary>
public class SqlExceptionEnricher : ILogEventEnricher
{
    private readonly SqlExceptionEnricherOptions _options;

    // Transient error numbers based on Microsoft's recommended retry logic
    // https://learn.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues
    private static readonly HashSet<int> s_transientErrorNumbers = new HashSet<int>
    {
        -2,     // Timeout
        -1,     // Connection timeout
        40197,  // Service error processing request
        40501,  // Service is busy
        40613,  // Database unavailable
        49918,  // Cannot process request. Not enough resources
        49919,  // Cannot process create or update request
        49920,  // Cannot process request. Too many operations in progress
        4060,   // Cannot open database
        40143,  // Connection could not be initialized
        40540,  // Service has encountered an error
        10053,  // Transport-level error
        10054,  // Transport-level error (connection reset by peer)
        10060,  // Network or instance-specific error
        10061,  // Network or instance-specific error
        40544,  // Database has reached its size quota
        40549,  // Session terminated (long transaction)
        40550,  // Session terminated (excessive lock acquisition)
        40551,  // Session terminated (excessive TEMPDB usage)
        40552,  // Session terminated (excessive transaction log space)
        40553,  // Session terminated (excessive memory usage)
        1205    // Deadlock victim (often transient)
    };

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
            return;
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

        // Detect transient failures if enabled
        if (_options.DetectTransientFailures)
        {
            var isTransient = s_transientErrorNumbers.Contains(firstError.Number);
            AddProperty(logEvent, propertyFactory, "IsTransient", isTransient);
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

    private void AddProperty(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, string name, object value)
    {
        var propertyName = $"{_options.PropertyPrefix}{name}";
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
