using System.Reflection;
using Serilog.Core;
using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Enrichers;
using Serilog.Events;
using static Serilog.Enrichers.SqlException.Tests.TestHelpers;

namespace Serilog.Enrichers.SqlException.Tests;

public class SqlExceptionEnricherTests
{
    [Fact]
    public void Enrich_AddsProperties_ForSqlException()
    {
        // Arrange
        var sqlException = CreateSqlException(
            number: 1205, 
            state: 13, 
            errorClass: 20, 
            server: "localhost",
            errorMessage: "Test error message",
            procedure: "sp_TestProcedure", 
            lineNumber: 42);
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
        var sqlException = CreateSqlException(
            number: 2627, 
            state: 1, 
            errorClass: 14, 
            server: "localhost",
            errorMessage: "Test error message",
            procedure: "", 
            lineNumber: 1);
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
        var sqlException = CreateSqlException(
            number: 547, 
            state: 0, 
            errorClass: 16, 
            server: "localhost",
            errorMessage: "Test error message",
            procedure: "", 
            lineNumber: 1);
        var logEvent = CreateLogEvent(sqlException);
        var enricher = new SqlExceptionEnricher();

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsSqlException" && p.Value.ToString() == "True");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Number" && p.Value.ToString() == "547");
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_Procedure");
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
        var sqlException = CreateSqlException(
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
        var sqlException = TestHelpers.CreateSqlExceptionWithMultipleErrors();
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
        var sqlException = TestHelpers.CreateSqlExceptionWithMultipleErrors();
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
        var sqlException = TestHelpers.CreateSqlExceptionWithMultipleErrors();
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
        var sqlException = CreateSqlException(
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
        var sqlException = CreateSqlException(
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
        var sqlException = CreateSqlException(
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
    public void Enrich_UsesCustomPropertyPrefix()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { PropertyPrefix = "Sql_" };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = CreateSqlException(
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
        var sqlException = TestHelpers.CreateSqlExceptionWithConnectionId(connectionId);
        var logEvent = CreateLogEvent(sqlException);
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        enricher.Enrich(logEvent, new TestPropertyFactory(properties));

        // Assert
        Assert.True(properties.ContainsKey("SqlException_ClientConnectionId"));
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
}
