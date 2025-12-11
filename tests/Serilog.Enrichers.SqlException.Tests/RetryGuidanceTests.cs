using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Enrichers;

namespace Serilog.Enrichers.SqlException.Tests;

public class RetryGuidanceTests
{
    [Fact]
    public void Enrich_AddsRetryGuidance_ForDeadlock()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { ProvideRetryGuidance = true };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = TestHelpers.CreateSqlException(1205, 13, 13, "TestServer", "Deadlock", "", 0);
        var logEvent = TestHelpers.CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new TestHelpers.SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_ShouldRetry" && p.Value.ToString() == "True");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_RetryStrategy" && p.Value.ToString().Contains("Exponential"));
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_SuggestedRetryDelay" && p.Value.ToString().Contains("100ms"));
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_MaxRetries" && p.Value.ToString() == "3");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_RetryReason");
    }

    [Fact]
    public void Enrich_RecommendsNoRetry_ForPrimaryKeyViolation()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { ProvideRetryGuidance = true };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = TestHelpers.CreateSqlException(2627, 1, 14, "TestServer", "PK violation", "", 0);
        var logEvent = TestHelpers.CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new TestHelpers.SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_ShouldRetry" && p.Value.ToString() == "False");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_RetryStrategy" && p.Value.ToString().Contains("None"));
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_MaxRetries" && p.Value.ToString() == "0");
    }

    [Fact]
    public void Enrich_RecommendsRetry_ForConnectionTimeout()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { ProvideRetryGuidance = true };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = TestHelpers.CreateSqlException(-1, 0, 20, "TestServer", "Connection timeout", "", 0);
        var logEvent = TestHelpers.CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new TestHelpers.SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_ShouldRetry" && p.Value.ToString() == "True");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_RetryStrategy" && p.Value.ToString().Contains("Linear"));
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_SuggestedRetryDelay" && p.Value.ToString().Contains("5s"));
    }

    [Fact]
    public void Enrich_DoesNotAddRetryGuidance_WhenDisabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { ProvideRetryGuidance = false };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = TestHelpers.CreateSqlException(1205, 13, 13, "TestServer", "Deadlock", "", 0);
        var logEvent = TestHelpers.CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new TestHelpers.SimplePropertyFactory());

        // Assert
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_ShouldRetry");
        Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_RetryStrategy");
    }

    [Theory]
    [InlineData(-2, "Command timeout")]
    [InlineData(1222, "Lock timeout")]
    [InlineData(40197, "Azure busy")]
    public void Enrich_AddsRetryGuidance_ForTransientErrors(int errorNumber, string description)
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions { ProvideRetryGuidance = true };
        var enricher = new SqlExceptionEnricher(options);
        var sqlException = TestHelpers.CreateSqlException(errorNumber, 0, 20, "TestServer", description, "", 0);
        var logEvent = TestHelpers.CreateLogEvent(sqlException);

        // Act
        enricher.Enrich(logEvent, new TestHelpers.SimplePropertyFactory());

        // Assert
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_ShouldRetry" && p.Value.ToString() == "True");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_RetryStrategy");
        Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_MaxRetries");
    }
}
