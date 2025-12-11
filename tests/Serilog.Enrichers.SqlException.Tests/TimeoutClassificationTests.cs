using System.Reflection;
using Microsoft.Data.SqlClient;
using Serilog.Core;
using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Enrichers;
using Serilog.Events;
using static Serilog.Enrichers.SqlException.Tests.TestHelpers;

namespace Serilog.Enrichers.SqlException.Tests;

public class TimeoutClassificationTests
{
    [Theory]
    [InlineData(-2, "Command")]
    [InlineData(-1, "Connection")]
    [InlineData(10060, "Network")]
    [InlineData(10061, "Network")]
    public void Enrich_ClassifiesTimeout_ByErrorNumber(int errorNumber, string expectedType)
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlException(errorNumber, 0, 11, "Server", "Timeout", "", 0);
        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsTimeout" && p.Value.ToString() == "True");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_TimeoutType" && p.Value.ToString().Contains(expectedType));
    }

    [Fact]
    public void Enrich_DoesNotClassifyTimeout_ForNonTimeoutError()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlException(547, 0, 16, "Server", "FK violation", "", 0);
        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsTimeout" && p.Value.ToString() == "False");
    }

    [Fact]
    public void Enrich_DoesNotClassifyTimeout_WhenDisabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { ClassifyTimeouts = false };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = CreateSqlException(-2, 0, 11, "Server", "Timeout", "", 0);
        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_IsTimeout");
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_TimeoutType");
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
