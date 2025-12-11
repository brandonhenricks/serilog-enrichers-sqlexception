using System.Reflection;
using Microsoft.Data.SqlClient;
using Serilog.Core;
using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Enrichers;
using Serilog.Events;
using static Serilog.Enrichers.SqlException.Tests.TestHelpers;

namespace Serilog.Enrichers.SqlException.Tests;

public class OpenTelemetryIntegrationTests
{
    [Fact]
    public void Enrich_UsesOtelPropertyNames_WhenEnabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { UseOpenTelemetrySemantics = true };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = CreateSqlException(547, 0, 16, "TestServer", "FK violation", "sp_Test", 42);
        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "db.error.code" && p.Value.ToString() == "547");
        Assert.Contains(logEvent.Properties, p => p.Key == "db.error.state" && p.Value.ToString() == "0");
        Assert.Contains(logEvent.Properties, p => p.Key == "db.error.severity" && p.Value.ToString() == "16");
        Assert.Contains(logEvent.Properties, p => p.Key == "db.operation" && p.Value.ToString().Contains("sp_Test"));
        Assert.Contains(logEvent.Properties, p => p.Key == "server.address" && p.Value.ToString().Contains("TestServer"));
    }

    [Fact]
    public void Enrich_UsesCustomPrefix_WhenOtelDisabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { UseOpenTelemetrySemantics = false, PropertyPrefix = "Sql_" };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = CreateSqlException(547, 0, 16, "Server", "Error", "", 0);
        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "Sql_Number");
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "db.error.code");
    }

    [Fact]
    public void Enrich_MapsAllProperties_WithOtelSemantics()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { UseOpenTelemetrySemantics = true };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = CreateSqlException(1205, 13, 20, "TestServer", "Deadlock", "", 42);
        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "db.exception.sql");
        Assert.Contains(logEvent.Properties, p => p.Key == "db.error.count");
        Assert.Contains(logEvent.Properties, p => p.Key == "db.error.transient");
        Assert.Contains(logEvent.Properties, p => p.Key == "db.error.deadlock");
        Assert.Contains(logEvent.Properties, p => p.Key == "db.error.category");
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
