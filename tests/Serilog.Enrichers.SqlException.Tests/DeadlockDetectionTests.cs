using System.Reflection;
using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Enrichers;
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
    public void Enrich_DoesNotAddDeadlockGraph_BecauseNotAvailableInException()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlException(
            number: 1205,
            state: 13,
            errorClass: 13,
            server: "TestServer",
            errorMessage: "Transaction was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction.",
            procedure: "",
            lineNumber: 0);

        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        // Deadlock graphs are not included in SqlException messages
        // They are only available in SQL Server error log when trace flags 1204/1222 are enabled
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
