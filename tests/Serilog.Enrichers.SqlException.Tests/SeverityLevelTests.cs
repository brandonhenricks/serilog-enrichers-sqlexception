using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Enrichers;

namespace Serilog.Enrichers.SqlException.Tests;

public class SeverityLevelTests
{
    [Theory]
    [InlineData(1, "Informational")]
    [InlineData(10, "Informational")]
    [InlineData(11, "Warning")]
    [InlineData(13, "Warning")]
    [InlineData(14, "Error")]
    [InlineData(16, "Error")]
    [InlineData(17, "Severe")]
    [InlineData(19, "Severe")]
    [InlineData(20, "Critical")]
    [InlineData(24, "Critical")]
    [InlineData(25, "Fatal")]
    public void Enrich_MapsSeverityLevel_Correctly(byte errorClass, string expectedSeverity)
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { IncludeSeverityLevel = true };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = TestHelpers.CreateSqlException(50000, 1, errorClass, "TestServer", "Test error", "", 0);
        var logEvent = TestHelpers.CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new TestHelpers.SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_SeverityLevel" && p.Value.ToString().Contains(expectedSeverity));
    }

    [Fact]
    public void Enrich_MarksHighSeverity_AsRequiringAttention()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { IncludeSeverityLevel = true };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = TestHelpers.CreateSqlException(50000, 1, 20, "TestServer", "Critical error", "", 0);
        var logEvent = TestHelpers.CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new TestHelpers.SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_RequiresImmediateAttention" && p.Value.ToString() == "True");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_SeverityLevel" && p.Value.ToString().Contains("Critical"));
    }

    [Fact]
    public void Enrich_DoesNotMarkLowSeverity_AsRequiringAttention()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { IncludeSeverityLevel = true };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = TestHelpers.CreateSqlException(50000, 1, 16, "TestServer", "User error", "", 0);
        var logEvent = TestHelpers.CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new TestHelpers.SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_RequiresImmediateAttention" && p.Value.ToString() == "False");
    }

    [Fact]
    public void Enrich_DoesNotAddSeverityLevel_WhenDisabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { IncludeSeverityLevel = false };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = TestHelpers.CreateSqlException(50000, 1, 20, "TestServer", "Critical error", "", 0);
        var logEvent = TestHelpers.CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new TestHelpers.SimplePropertyFactory());

        // Assert
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_SeverityLevel");
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_RequiresImmediateAttention");
    }
}
