using System.Reflection;
using Microsoft.Data.SqlClient;
using Serilog.Core;
using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Enrichers;
using Serilog.Events;
using static Serilog.Enrichers.SqlException.Tests.TestHelpers;

namespace Serilog.Enrichers.SqlException.Tests;

public class DeadlockDetectionTests
{
    [Fact]
    public void Enrich_DetectsDeadlock_WhenErrorNumber1205()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlException(
            number: 1205,
            state: 13,
            errorClass: 13,
            server: "TestServer",
            errorMessage: "Transaction was deadlocked",
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsDeadlock" && p.Value.ToString() == "True");
    }

    [Fact]
    public void Enrich_DoesNotDetectDeadlock_ForNonDeadlockError()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlException(
            number: 547,
            state: 0,
            errorClass: 16,
            server: "TestServer",
            errorMessage: "FK constraint violation",
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsDeadlock" && p.Value.ToString() == "False");
    }

    [Fact]
    public void Enrich_ExtractsDeadlockGraph_WhenPresent()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var deadlockMessage = "Transaction was deadlocked. <deadlock-list><deadlock victim=\"process123\"><resource-list><exchangeEvent/></resource-list></deadlock></deadlock-list>";
        var sqlException = CreateSqlException(
            number: 1205,
            state: 13,
            errorClass: 13,
            server: "TestServer",
            errorMessage: deadlockMessage,
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_DeadlockGraph" && p.Value.ToString().Contains("deadlock-list"));
    }

    [Fact]
    public void Enrich_DoesNotAddDeadlockGraph_WhenDisabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { IncludeDeadlockGraph = false };
        var enricher = new SqlExceptionEnricher(options);
        var deadlockMessage = "Transaction was deadlocked. <deadlock-list><deadlock victim=\"process123\"></deadlock></deadlock-list>";
        var sqlException = CreateSqlException(
            number: 1205,
            state: 13,
            errorClass: 13,
            server: "TestServer",
            errorMessage: deadlockMessage,
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_DeadlockGraph");
    }

    [Fact]
    public void Enrich_DoesNotAddDeadlockGraph_WhenNotInMessage()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlException(
            number: 1205,
            state: 13,
            errorClass: 13,
            server: "TestServer",
            errorMessage: "Transaction was deadlocked on lock resources",
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_DeadlockGraph");
    }

    [Fact]
    public void Enrich_ExtractsDeadlockGraph_WithAttributes()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var deadlockMessage = "Transaction was deadlocked. <deadlock-list id=\"123\" xmlns=\"http://example.com\"><deadlock victim=\"process123\"><resource-list><exchangeEvent/></resource-list></deadlock></deadlock-list>";
        var sqlException = CreateSqlException(
            number: 1205,
            state: 13,
            errorClass: 13,
            server: "TestServer",
            errorMessage: deadlockMessage,
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_DeadlockGraph" && p.Value.ToString().Contains("deadlock-list"));
        var graphProperty = logEvent.Properties.First(p => p.Key == "SqlException_DeadlockGraph");
        // The value is stored as a ScalarValue, so check for the attribute in the string representation
        Assert.Contains("id=", graphProperty.Value.ToString());
        Assert.Contains("123", graphProperty.Value.ToString());
    }

    [Fact]
    public void Enrich_DoesNotExtractDeadlockGraph_WhenMalformed()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var deadlockMessage = "Transaction was deadlocked. <deadlock-list><deadlock victim=\"process123\"><resource-list><exchangeEvent></resource-list></deadlock></deadlock-list>";
        var sqlException = CreateSqlException(
            number: 1205,
            state: 13,
            errorClass: 13,
            server: "TestServer",
            errorMessage: deadlockMessage,
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        // Should not extract because exchangeEvent tag is not self-closing and doesn't have a closing tag
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_DeadlockGraph");
    }

    [Fact]
    public void Enrich_ExtractsDeadlockGraph_WithNamespace()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var deadlockMessage = "Deadlock detected. <deadlock-list xmlns=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\"><deadlock victim=\"process123\"></deadlock></deadlock-list>";
        var sqlException = CreateSqlException(
            number: 1205,
            state: 13,
            errorClass: 13,
            server: "TestServer",
            errorMessage: deadlockMessage,
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_DeadlockGraph" && p.Value.ToString().Contains("xmlns"));
    }

    [Fact]
    public void Enrich_DoesNotExtractDeadlockGraph_WhenUnclosedTag()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var deadlockMessage = "Transaction was deadlocked. <deadlock-list><deadlock victim=\"process123\">";
        var sqlException = CreateSqlException(
            number: 1205,
            state: 13,
            errorClass: 13,
            server: "TestServer",
            errorMessage: deadlockMessage,
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        // Should not extract because the XML is incomplete
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_DeadlockGraph");
    }

    private static Microsoft.Data.SqlClient.SqlException CreateSqlException(int number, byte state, byte errorClass, string server, string errorMessage, string procedure, int lineNumber)
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
}
