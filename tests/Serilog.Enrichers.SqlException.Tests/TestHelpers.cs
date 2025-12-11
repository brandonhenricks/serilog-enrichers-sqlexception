using Serilog.Core;
using Serilog.Events;

namespace Serilog.Enrichers.SqlException.Tests;

/// <summary>
/// Shared test helper utilities for creating test objects across test classes.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Creates a LogEvent with the specified exception for testing purposes.
    /// </summary>
    /// <param name="ex">The exception to associate with the log event.</param>
    /// <returns>A LogEvent configured for testing.</returns>
    public static LogEvent CreateLogEvent(Exception? ex)
    {
        return new LogEvent(DateTimeOffset.Now, LogEventLevel.Error, ex, MessageTemplate.Empty, Array.Empty<LogEventProperty>());
    }

    /// <summary>
    /// Simple implementation of ILogEventPropertyFactory for testing purposes.
    /// Wraps property values in ScalarValue without complex destructuring.
    /// </summary>
    public class SimplePropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            var propertyValue = new ScalarValue(value);
            return new LogEventProperty(name, propertyValue);
        }
    }
}
