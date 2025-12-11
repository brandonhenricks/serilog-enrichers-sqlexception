using Serilog.Enrichers.SqlException.Configurations;
using Serilog.Enrichers.SqlException.Enrichers;

namespace Serilog.Enrichers.SqlException.Tests;

public class ConfigurationValidationTests
{
    [Fact]
    public void Constructor_ThrowsException_WhenPropertyPrefixIsEmpty()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions
        {
            PropertyPrefix = ""
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new SqlExceptionEnricher(options));
        Assert.Contains("PropertyPrefix", exception.Message);
    }

    [Fact]
    public void Constructor_ThrowsException_WhenPropertyPrefixIsWhitespace()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions
        {
            PropertyPrefix = "   "
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new SqlExceptionEnricher(options));
        Assert.Contains("PropertyPrefix", exception.Message);
    }

    [Fact]
    public void Constructor_Succeeds_WithValidConfiguration()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions
        {
            DetectDeadlocks = true,
            PropertyPrefix = "Sql_"
        };

        // Act & Assert - should not throw
        var enricher = new SqlExceptionEnricher(options);
        Assert.NotNull(enricher);
    }

    [Fact]
    public void Constructor_Succeeds_WhenDeadlockDetectionDisabled()
    {
        // Arrange
        var options = new SqlExceptionEnricherOptions
        {
            DetectDeadlocks = false
        };

        // Act & Assert - should not throw
        var enricher = new SqlExceptionEnricher(options);
        Assert.NotNull(enricher);
    }
}
