using System.Reflection;
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
    /// Creates a SqlException using reflection for testing purposes.
    /// </summary>
    public static Microsoft.Data.SqlClient.SqlException CreateSqlException(
        int number, 
        byte state, 
        byte errorClass, 
        string server, 
        string errorMessage, 
        string procedure, 
        int lineNumber)
    {
        var errorCollectionType = typeof(Microsoft.Data.SqlClient.SqlException)
            .Assembly
            .GetType("Microsoft.Data.SqlClient.SqlErrorCollection")!;

        var errorCollection = Activator.CreateInstance(
            errorCollectionType,
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            null,
            null)!;

        var sqlErrorType = typeof(Microsoft.Data.SqlClient.SqlException)
            .Assembly
            .GetType("Microsoft.Data.SqlClient.SqlError")!;

        var innerException = new Exception("Inner");

        var sqlError = Activator.CreateInstance(
            sqlErrorType,
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { number, state, errorClass, server, errorMessage, procedure, lineNumber, innerException },
            null)!;

        var addMethod = errorCollectionType.GetMethod(
            "Add",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        addMethod.Invoke(errorCollection, new[] { sqlError });

        // Try the 3-parameter constructor first
        var constructor = typeof(Microsoft.Data.SqlClient.SqlException).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { errorCollectionType, typeof(string), typeof(Guid) },
            null);

        if (constructor != null)
        {
            return (Microsoft.Data.SqlClient.SqlException)constructor.Invoke(new object?[] { errorCollection, null, Guid.Empty })!;
        }

        // Fall back to 4-parameter constructor
        var fallbackCtor = typeof(Microsoft.Data.SqlClient.SqlException).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 4
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == errorCollectionType
                    && parameters[2].ParameterType == typeof(Exception)
                    && parameters[3].ParameterType == typeof(Guid);
            });

        if (fallbackCtor != null)
        {
            return (Microsoft.Data.SqlClient.SqlException)fallbackCtor.Invoke(new object?[] { "Test SQL Exception", errorCollection, innerException, Guid.NewGuid() })!;
        }

        throw new InvalidOperationException("Could not find a suitable SqlException constructor.");
    }

    /// <summary>
    /// Creates a SqlException with multiple errors for testing purposes.
    /// </summary>
    public static Microsoft.Data.SqlClient.SqlException CreateSqlExceptionWithMultipleErrors()
    {
        var errorCollectionType = typeof(Microsoft.Data.SqlClient.SqlException)
            .Assembly
            .GetType("Microsoft.Data.SqlClient.SqlErrorCollection")!;

        var errorCollection = Activator.CreateInstance(
            errorCollectionType,
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            null,
            null)!;

        var sqlErrorType = typeof(Microsoft.Data.SqlClient.SqlException)
            .Assembly
            .GetType("Microsoft.Data.SqlClient.SqlError")!;

        var innerException1 = new Exception("Inner1");
        var innerException2 = new Exception("Inner2");

        var sqlError1 = Activator.CreateInstance(
            sqlErrorType,
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { 547, (byte)0, (byte)16, "Server1", "FK violation", "proc1", 10, innerException1 },
            null)!;

        var sqlError2 = Activator.CreateInstance(
            sqlErrorType,
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { 2627, (byte)1, (byte)14, "Server2", "PK violation", "proc2", 20, innerException2 },
            null)!;

        var addMethod = errorCollectionType.GetMethod(
            "Add",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        addMethod.Invoke(errorCollection, new[] { sqlError1 });
        addMethod.Invoke(errorCollection, new[] { sqlError2 });

        var constructor = typeof(Microsoft.Data.SqlClient.SqlException).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { errorCollectionType, typeof(string), typeof(Guid) },
            null);

        if (constructor != null)
        {
            return (Microsoft.Data.SqlClient.SqlException)constructor.Invoke(new object?[] { errorCollection, null, Guid.Empty })!;
        }

        var fallbackCtor = typeof(Microsoft.Data.SqlClient.SqlException).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 4
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == errorCollectionType
                    && parameters[2].ParameterType == typeof(Exception)
                    && parameters[3].ParameterType == typeof(Guid);
            });

        if (fallbackCtor != null)
        {
            return (Microsoft.Data.SqlClient.SqlException)fallbackCtor.Invoke(new object?[] { "Test SQL Exception", errorCollection, innerException1, Guid.NewGuid() })!;
        }

        throw new InvalidOperationException("Could not find a suitable SqlException constructor.");
    }

    /// <summary>
    /// Creates a SqlException with a specific client connection ID for testing purposes.
    /// </summary>
    public static Microsoft.Data.SqlClient.SqlException CreateSqlExceptionWithConnectionId(Guid connectionId)
    {
        var errorCollectionType = typeof(Microsoft.Data.SqlClient.SqlException)
            .Assembly
            .GetType("Microsoft.Data.SqlClient.SqlErrorCollection")!;

        var errorCollection = Activator.CreateInstance(
            errorCollectionType,
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            null,
            null)!;

        var sqlErrorType = typeof(Microsoft.Data.SqlClient.SqlException)
            .Assembly
            .GetType("Microsoft.Data.SqlClient.SqlError")!;

        var innerException = new Exception("Inner");

        var sqlError = Activator.CreateInstance(
            sqlErrorType,
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { 547, (byte)0, (byte)16, "Server", "Error", "", 1, innerException },
            null)!;

        var addMethod = errorCollectionType.GetMethod(
            "Add",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        addMethod.Invoke(errorCollection, new[] { sqlError });

        var constructor = typeof(Microsoft.Data.SqlClient.SqlException).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { errorCollectionType, typeof(string), typeof(Guid) },
            null);

        if (constructor != null)
        {
            return (Microsoft.Data.SqlClient.SqlException)constructor.Invoke(new object?[] { errorCollection, null, connectionId })!;
        }

        var fallbackCtor = typeof(Microsoft.Data.SqlClient.SqlException).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 4
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == errorCollectionType
                    && parameters[2].ParameterType == typeof(Exception)
                    && parameters[3].ParameterType == typeof(Guid);
            });

        if (fallbackCtor != null)
        {
            return (Microsoft.Data.SqlClient.SqlException)fallbackCtor.Invoke(new object?[] { "Test SQL Exception", errorCollection, innerException, connectionId })!;
        }

        throw new InvalidOperationException("Could not find a suitable SqlException constructor.");
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
