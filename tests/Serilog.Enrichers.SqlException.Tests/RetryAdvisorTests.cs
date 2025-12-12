using Serilog.Enrichers.SqlException.Helpers;

namespace Serilog.Enrichers.SqlException.Tests;

/// <summary>
/// Tests for the RetryAdvisor class transient error detection logic.
/// </summary>
public class RetryAdvisorTests
{
    [Theory]
    [InlineData(1205)]  // Deadlock victim
    [InlineData(1222)]  // Lock timeout
    public void ShouldRetry_ReturnsTrue_ForDeadlockAndLockTimeoutErrors(int errorNumber)
    {
        // Act
        var result = RetryAdvisor.ShouldRetry(errorNumber);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(40197)] // Service busy
    [InlineData(40501)] // Service busy - too many concurrent requests
    [InlineData(40613)] // Database unavailable
    [InlineData(49918)] // Insufficient resources
    [InlineData(49920)] // Too many operations
    [InlineData(40143)] // Connection initialization failed
    [InlineData(40540)] // Service error
    public void ShouldRetry_ReturnsTrue_ForAzureTransientErrors(int errorNumber)
    {
        // Act
        var result = RetryAdvisor.ShouldRetry(errorNumber);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(-1)]    // Connection timeout
    [InlineData(4060)]  // Cannot open database
    [InlineData(10053)] // Transport error
    [InlineData(10054)] // Connection reset
    [InlineData(10060)] // Network timeout
    [InlineData(10061)] // Connection refused
    public void ShouldRetry_ReturnsTrue_ForConnectionErrors(int errorNumber)
    {
        // Act
        var result = RetryAdvisor.ShouldRetry(errorNumber);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(-2)]    // Command timeout
    [InlineData(8645)]  // Memory timeout
    public void ShouldRetry_ReturnsTrue_ForCommandTimeoutErrors(int errorNumber)
    {
        // Act
        var result = RetryAdvisor.ShouldRetry(errorNumber);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0)]      // No error
    [InlineData(102)]    // Syntax error
    [InlineData(229)]    // Permission denied
    [InlineData(547)]    // Constraint violation
    [InlineData(2627)]   // Primary key violation
    [InlineData(8152)]   // String data truncation
    [InlineData(50000)]  // User-defined error
    [InlineData(99999)]  // Unknown error
    public void ShouldRetry_ReturnsFalse_ForNonTransientErrors(int errorNumber)
    {
        // Act
        var result = RetryAdvisor.ShouldRetry(errorNumber);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRetry_IsConsistent_ForSameErrorNumber()
    {
        // Arrange
        const int transientError = 1205;
        const int nonTransientError = 2627;

        // Act
        var result1 = RetryAdvisor.ShouldRetry(transientError);
        var result2 = RetryAdvisor.ShouldRetry(transientError);
        var result3 = RetryAdvisor.ShouldRetry(nonTransientError);
        var result4 = RetryAdvisor.ShouldRetry(nonTransientError);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.False(result3);
        Assert.False(result4);
    }
}
