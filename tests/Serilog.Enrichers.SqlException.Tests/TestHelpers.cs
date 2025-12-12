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
        var (errorCollectionType, errorCollection) = CreateErrorCollection();
        var sqlErrorType = GetSqlErrorType();
        var innerException = new Exception("Inner");

        var sqlError = CreateSqlError(sqlErrorType, number, state, errorClass, server, errorMessage, procedure, lineNumber, innerException);
        AddErrorToCollection(errorCollectionType, errorCollection, sqlError);

        return CreateSqlExceptionFromCollection(errorCollectionType, errorCollection, innerException);
    }

    /// <summary>
    /// Creates a SqlException with multiple errors for testing purposes.
    /// </summary>
    public static Microsoft.Data.SqlClient.SqlException CreateSqlExceptionWithMultipleErrors()
    {
        var (errorCollectionType, errorCollection) = CreateErrorCollection();
        var sqlErrorType = GetSqlErrorType();

        var innerException1 = new Exception("Inner1");
        var innerException2 = new Exception("Inner2");

        var sqlError1 = CreateSqlError(sqlErrorType, 547, 0, 16, "Server1", "FK violation", "proc1", 10, innerException1);
        var sqlError2 = CreateSqlError(sqlErrorType, 2627, 1, 14, "Server2", "PK violation", "proc2", 20, innerException2);

        AddErrorToCollection(errorCollectionType, errorCollection, sqlError1);
        AddErrorToCollection(errorCollectionType, errorCollection, sqlError2);

        return CreateSqlExceptionFromCollection(errorCollectionType, errorCollection, innerException1);
    }

    /// <summary>
    /// Creates a SqlException with a specific client connection ID for testing purposes.
    /// </summary>
    public static Microsoft.Data.SqlClient.SqlException CreateSqlExceptionWithConnectionId(Guid connectionId)
    {
        var (errorCollectionType, errorCollection) = CreateErrorCollection();
        var sqlErrorType = GetSqlErrorType();
        var innerException = new Exception("Inner");

        var sqlError = CreateSqlError(sqlErrorType, 547, 0, 16, "Server", "Error", "", 1, innerException);
        AddErrorToCollection(errorCollectionType, errorCollection, sqlError);

        // Try the 3-parameter constructor with connection ID
        var constructor = typeof(Microsoft.Data.SqlClient.SqlException).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { errorCollectionType, typeof(string), typeof(Guid) },
            null);

        if (constructor != null)
        {
            return (Microsoft.Data.SqlClient.SqlException)constructor.Invoke(new object?[] { errorCollection, null, connectionId })!;
        }

        // Fall back to 4-parameter constructor with connection ID
        var fallbackCtor = FindFallbackConstructor(errorCollectionType);
        if (fallbackCtor != null)
        {
            return (Microsoft.Data.SqlClient.SqlException)fallbackCtor.Invoke(new object?[] { "Test SQL Exception", errorCollection, innerException, connectionId })!;
        }

        throw new InvalidOperationException("Could not find a suitable SqlException constructor.");
    }

    private static (Type errorCollectionType, object errorCollection) CreateErrorCollection()
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

        return (errorCollectionType, errorCollection);
    }

    private static Type GetSqlErrorType()
    {
        return typeof(Microsoft.Data.SqlClient.SqlException)
            .Assembly
            .GetType("Microsoft.Data.SqlClient.SqlError")!;
    }

    private static object CreateSqlError(Type sqlErrorType, int number, byte state, byte errorClass, string server, string errorMessage, string procedure, int lineNumber, Exception innerException)
    {
        return Activator.CreateInstance(
            sqlErrorType,
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { number, state, errorClass, server, errorMessage, procedure, lineNumber, innerException },
            null)!;
    }

    private static void AddErrorToCollection(Type errorCollectionType, object errorCollection, object sqlError)
    {
        var addMethod = errorCollectionType.GetMethod(
            "Add",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        addMethod.Invoke(errorCollection, new[] { sqlError });
    }

    private static Microsoft.Data.SqlClient.SqlException CreateSqlExceptionFromCollection(Type errorCollectionType, object errorCollection, Exception innerException)
    {
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
        var fallbackCtor = FindFallbackConstructor(errorCollectionType);
        if (fallbackCtor != null)
        {
            return (Microsoft.Data.SqlClient.SqlException)fallbackCtor.Invoke(new object?[] { "Test SQL Exception", errorCollection, innerException, Guid.NewGuid() })!;
        }

        throw new InvalidOperationException("Could not find a suitable SqlException constructor.");
    }

    private static ConstructorInfo? FindFallbackConstructor(Type errorCollectionType)
    {
        return typeof(Microsoft.Data.SqlClient.SqlException).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 4
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == errorCollectionType
                    && parameters[2].ParameterType == typeof(Exception)
                    && parameters[3].ParameterType == typeof(Guid);
            });
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
