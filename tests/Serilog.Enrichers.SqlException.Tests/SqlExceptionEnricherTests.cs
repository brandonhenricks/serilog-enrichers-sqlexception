using System.Reflection;
using Microsoft.Data.SqlClient;
using Serilog.Core;
using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Enrichers;
using Serilog.Events;

namespace Serilog.Enrichers.SqlException.Tests;

public class SqlExceptionEnricherTests
{
    [Fact]
    public void Enrich_AddsProperties_ForSqlException()
    {
        // Arrange
        var sqlException = CreateSqlException(number: 1205, state: 13, errorClass: 20, procedure: "sp_TestProcedure", line: 42);
        var logEvent = CreateLogEvent(sqlException);
        var enricher = new SqlExceptionEnricher();

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsSqlException" && p.Value.ToString() == "True");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Number" && p.Value.ToString() == "1205");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_State" && p.Value.ToString() == "13");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Class" && p.Value.ToString() == "20");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Procedure" && p.Value.ToString().Contains("sp_TestProcedure"));
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Line" && p.Value.ToString() == "42");
    }

    [Fact]
    public void Enrich_DoesNothing_WhenNoException()
    {
        // Arrange
        var logEvent = CreateLogEvent(null);
        var enricher = new SqlExceptionEnricher();

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Empty(logEvent.Properties);
    }

    [Fact]
    public void Enrich_DoesNothing_WhenNoSqlExceptionInChain()
    {
        // Arrange
        var ex = new Exception("outer", new InvalidOperationException("inner"));
        var logEvent = CreateLogEvent(ex);
        var enricher = new SqlExceptionEnricher();

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Empty(logEvent.Properties);
    }

    [Fact]
    public void Enrich_FindsSqlException_InInnerException()
    {
        // Arrange
        var sqlException = CreateSqlException(number: 2627, state: 1, errorClass: 14, procedure: "", line: 1);
        var outerException = new InvalidOperationException("outer", sqlException);
        var logEvent = CreateLogEvent(outerException);
        var enricher = new SqlExceptionEnricher();

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsSqlException" && p.Value.ToString() == "True");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Number" && p.Value.ToString() == "2627");
    }

    [Fact]
    public void Enrich_AddsProperties_WithoutProcedure()
    {
        // Arrange
        var sqlException = CreateSqlException(number: 547, state: 0, errorClass: 16, procedure: "", line: 1);
        var logEvent = CreateLogEvent(sqlException);
        var enricher = new SqlExceptionEnricher();

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsSqlException" && p.Value.ToString() == "True");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Number" && p.Value.ToString() == "547");
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_Procedure");
    }

    private static LogEvent CreateLogEvent(Exception? ex)
    {
        return new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Error,
            ex,
            new MessageTemplate("test", []),
            new List<LogEventProperty>()); // IDE0300: No simplification needed here, already using collection initializer.
    }

    private static Microsoft.Data.SqlClient.SqlException CreateSqlException(int number, byte state, byte errorClass, string procedure, int line)
    {
        // SqlException cannot be directly instantiated, so we use reflection to create it
        var sqlExceptionType = typeof(Microsoft.Data.SqlClient.SqlException);
        var sqlErrorCollectionType = sqlExceptionType.Assembly.GetType("Microsoft.Data.SqlClient.SqlErrorCollection");
        var sqlErrorCollection = Activator.CreateInstance(sqlErrorCollectionType!, true);

        // Create SqlError with parameters: number, state, class, server, errorMessage, procedure, lineNumber, exception
        var sqlErrorType = sqlExceptionType.Assembly.GetType("Microsoft.Data.SqlClient.SqlError");
        var errorNumber = number;
        var errorState = state;
        var severity = errorClass;
        var server = "localhost";
        var errorMessage = "Test error message";
        var procedureName = procedure;
        var lineNumber = line;
        Exception innerException = new Exception("Inner");

        var sqlError = Activator.CreateInstance(
            sqlErrorType!,
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { errorNumber, errorState, severity, server, errorMessage, procedureName, lineNumber, innerException },
            null);

        var addMethod = sqlErrorCollectionType!.GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
        addMethod!.Invoke(sqlErrorCollection, new[] { sqlError });

        var ctor = sqlExceptionType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
        var sqlException = (Microsoft.Data.SqlClient.SqlException)ctor.Invoke(
            new object[] { "Test SQL Exception", sqlErrorCollection!, innerException, Guid.NewGuid() });

        return sqlException;
    }

    [Fact]
    public void Enrich_DoesNotAddProperties_WhenNoExceptionIsPresent()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var logEvent = CreateLogEvent(null);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.Empty(properties);
    }

    [Fact]
    public void Enrich_DoesNotAddProperties_WhenNonSqlExceptionIsPresent()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var exception = new InvalidOperationException("Test exception");
        var logEvent = CreateLogEvent(exception);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.Empty(properties);
    }

    [Fact]
    public void Enrich_DoesNotAddOptionalProperties_WhenEmpty()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlExceptionWithAllFields(
            number: 547,
            state: 0,
            errorClass: 16,
            server: "",
            errorMessage: "",
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.False(properties.ContainsKey("SqlException_Procedure"));
        Assert.False(properties.ContainsKey("SqlException_Server"));
        Assert.False(properties.ContainsKey("SqlException_Message"));
    }

    [Fact]
    public void Enrich_AddsErrorCount_WhenSqlExceptionPresent()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlExceptionWithMultipleErrors();
        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.True(properties.ContainsKey("SqlException_ErrorCount"));
        Assert.Equal("2", GetScalarValue(properties["SqlException_ErrorCount"]));
    }

    [Fact]
    public void Enrich_IncludesAllErrors_WhenOptionEnabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { IncludeAllErrors = true };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = CreateSqlExceptionWithMultipleErrors();
        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.True(properties.ContainsKey("SqlException_AllNumbers"));
        Assert.True(properties.ContainsKey("SqlException_AllStates"));
        Assert.True(properties.ContainsKey("SqlException_AllClasses"));
        Assert.True(properties.ContainsKey("SqlException_AllMessages"));
    }

    [Fact]
    public void Enrich_DoesNotIncludeAllErrors_WhenOptionDisabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { IncludeAllErrors = false };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = CreateSqlExceptionWithMultipleErrors();
        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.False(properties.ContainsKey("SqlException_AllNumbers"));
        Assert.False(properties.ContainsKey("SqlException_AllStates"));
        Assert.False(properties.ContainsKey("SqlException_AllClasses"));
    }

    [Fact]
    public void Enrich_DetectsTransientFailure_ForTimeoutError()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlExceptionWithAllFields(
            number: -2, // Timeout
            state: 0,
            errorClass: 11,
            server: "Server",
            errorMessage: "Timeout expired",
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.True(properties.ContainsKey("SqlException_IsTransient"));
        Assert.Equal("True", GetScalarValue(properties["SqlException_IsTransient"]));
    }

    [Fact]
    public void Enrich_DetectsTransientFailure_ForDeadlock()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlExceptionWithAllFields(
            number: 1205, // Deadlock
            state: 13,
            errorClass: 13,
            server: "Server",
            errorMessage: "Transaction was deadlocked",
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.True(properties.ContainsKey("SqlException_IsTransient"));
        Assert.Equal("True", GetScalarValue(properties["SqlException_IsTransient"]));
    }

    [Fact]
    public void Enrich_MarksNonTransient_ForConstraintViolation()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlExceptionWithAllFields(
            number: 547, // Constraint violation
            state: 0,
            errorClass: 16,
            server: "Server",
            errorMessage: "FK constraint violation",
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.True(properties.ContainsKey("SqlException_IsTransient"));
        Assert.Equal("False", GetScalarValue(properties["SqlException_IsTransient"]));
    }

    [Fact]
    public void Enrich_DoesNotDetectTransient_WhenOptionDisabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { DetectTransientFailures = false };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = CreateSqlExceptionWithAllFields(
            number: -2,
            state: 0,
            errorClass: 11,
            server: "Server",
            errorMessage: "Timeout",
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.False(properties.ContainsKey("SqlException_IsTransient"));
    }

    [Fact]
    public void Enrich_UsesCustomPropertyPrefix()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { PropertyPrefix = "Sql_" };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = CreateSqlExceptionWithAllFields(
            number: 547,
            state: 0,
            errorClass: 16,
            server: "Server",
            errorMessage: "Error",
            procedure: "",
            lineNumber: 1);

        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.True(properties.ContainsKey("Sql_IsSqlException"));
        Assert.True(properties.ContainsKey("Sql_Number"));
        Assert.False(properties.ContainsKey("SqlException_Number"));
    }

    [Fact]
    public void Enrich_IncludesClientConnectionId_WhenAvailable()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var connectionId = Guid.NewGuid();
        var sqlException = CreateSqlExceptionWithConnectionId(connectionId);
        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.True(properties.ContainsKey("SqlException_ClientConnectionId"));
    }

    private static Microsoft.Data.SqlClient.SqlException CreateSqlExceptionWithAllFields(
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
            return (Microsoft.Data.SqlClient.SqlException)fallbackCtor.Invoke(new object?[] { "Test SQL Exception", errorCollection, innerException, Guid.NewGuid() })!;
        }

        throw new InvalidOperationException("Could not find a suitable SqlException constructor.");
    }

    private static Microsoft.Data.SqlClient.SqlException CreateSqlExceptionWithMultipleErrors()
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

    private static Microsoft.Data.SqlClient.SqlException CreateSqlExceptionWithConnectionId(Guid connectionId)
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

    private static string GetScalarValue(LogEventPropertyValue value)
    {
        return value.ToString();
    }

    private class TestPropertyFactory : ILogEventPropertyFactory
    {
        private readonly Dictionary<string, LogEventPropertyValue> _properties;

        public TestPropertyFactory(Dictionary<string, LogEventPropertyValue> properties)
        {
            _properties = properties;
        }

        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            var propertyValue = new ScalarValue(value);
            _properties[name] = propertyValue;
            return new LogEventProperty(name, propertyValue);
        }
    }

    private class SimplePropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            // For test purposes, we just wrap values in ScalarValue
            // In production, Serilog's property factory handles complex destructuring
            var propertyValue = new ScalarValue(value);
            return new LogEventProperty(name, propertyValue);
        }
    }
}
