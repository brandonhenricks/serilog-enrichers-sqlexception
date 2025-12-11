using Serilog.Core;
using Serilog.Events;
using System.Reflection;

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
    /// Creates a SqlException using reflection for testing purposes.
    /// </summary>
    public static Microsoft.Data.SqlClient.SqlException CreateSqlException(int number, byte state, byte errorClass, string server, string errorMessage, string procedure, int lineNumber)
    {
        var sqlExceptionType = typeof(Microsoft.Data.SqlClient.SqlException);
        var sqlErrorCollectionType = sqlExceptionType.Assembly.GetType("Microsoft.Data.SqlClient.SqlErrorCollection");
        var sqlErrorCollection = Activator.CreateInstance(sqlErrorCollectionType!, true);

        var sqlErrorType = sqlExceptionType.Assembly.GetType("Microsoft.Data.SqlClient.SqlError");
        Exception innerException = new Exception("Inner");

        var sqlError = Activator.CreateInstance(
            sqlErrorType!,
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { number, state, errorClass, server, errorMessage, procedure, lineNumber, innerException },
            null);

        var addMethod = sqlErrorCollectionType!.GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
        addMethod!.Invoke(sqlErrorCollection, new[] { sqlError });

        var ctor = sqlExceptionType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
        var sqlException = (Microsoft.Data.SqlClient.SqlException)ctor.Invoke(
            new object[] { errorMessage, sqlErrorCollection!, innerException, Guid.NewGuid() });

        return sqlException;
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
