using System.Reflection;
using Microsoft.Data.SqlClient;
using Serilog.Core;
using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Enrichers;
using Serilog.Events;
using static Serilog.Enrichers.SqlException.Tests.TestHelpers;

namespace Serilog.Enrichers.SqlException.Tests;

public class ErrorCategorizationTests
{
    [Theory]
    [InlineData(547, "Constraint", true)]
    [InlineData(2627, "Constraint", true)]
    [InlineData(102, "Syntax", true)]
    [InlineData(1205, "Resource", false)]
    [InlineData(-2, "Connectivity", false)]
    [InlineData(229, "Permission", true)]
    public void Enrich_CategorizesError_Correctly(int errorNumber, string expectedCategory, bool expectedIsUserError)
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlException(errorNumber, 0, 16, "Server", "Error", "", 0);
        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_ErrorCategory" && p.Value.ToString().Contains(expectedCategory));
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsUserError" && p.Value.ToString() == expectedIsUserError.ToString());
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsSystemError" && p.Value.ToString() == (!expectedIsUserError).ToString());
    }

    [Fact]
    public void Enrich_DoesNotCategorize_WhenDisabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { CategorizeErrors = false };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = CreateSqlException(547, 0, 16, "Server", "Error", "", 0);
        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_ErrorCategory");
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_IsUserError");
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_IsSystemError");
    }

    [Fact]
    public void Enrich_CategorizesUnknownError_AsUnknown()
    {
        // Arrange
        var enricher = new SqlExceptionEnricher();
        var sqlException = CreateSqlException(99999, 0, 16, "Server", "Unknown error", "", 0);
        var logEvent = CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_ErrorCategory" && p.Value.ToString().Contains("Unknown"));
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
