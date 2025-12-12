using Serilog.Enrichers.SqlException.Helpers;

namespace Serilog.Enrichers.SqlException.Tests;

/// <summary>
/// Tests for the RetryAdvisor class transient error detection logic.
/// </summary>
public class RetryAdvisorTests
{
    [Theory]
    [InlineData(1205, true)]  // Deadlock victim
    [InlineData(1222, true)]  // Lock timeout
    public void ShouldRetry_ReturnsTrue_ForDeadlockAndLockTimeoutErrors(int errorNumber, bool expected)
    {
        // Act
        var result = RetryAdvisor.ShouldRetry(errorNumber);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(40197, true)] // Service busy
    [InlineData(40501, true)] // Service busy - too many concurrent requests
    [InlineData(40613, true)] // Database unavailable
    [InlineData(49918, true)] // Insufficient resources
    [InlineData(49920, true)] // Too many operations
    [InlineData(40143, true)] // Connection initialization failed
    [InlineData(40540, true)] // Service error
    public void ShouldRetry_ReturnsTrue_ForAzureTransientErrors(int errorNumber, bool expected)
    {
        // Act
        var result = RetryAdvisor.ShouldRetry(errorNumber);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-1, true)]    // Connection timeout
    [InlineData(4060, true)]  // Cannot open database
    [InlineData(10053, true)] // Transport error
    [InlineData(10054, true)] // Connection reset
    [InlineData(10060, true)] // Network timeout
    [InlineData(10061, true)] // Connection refused
    public void ShouldRetry_ReturnsTrue_ForConnectionErrors(int errorNumber, bool expected)
    {
        // Act
        var result = RetryAdvisor.ShouldRetry(errorNumber);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-2, true)]    // Command timeout
    [InlineData(8645, true)]  // Memory timeout
    public void ShouldRetry_ReturnsTrue_ForCommandTimeoutErrors(int errorNumber, bool expected)
    {
        // Act
        var result = RetryAdvisor.ShouldRetry(errorNumber);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, false)]      // No error
    [InlineData(102, false)]    // Syntax error
    [InlineData(229, false)]    // Permission denied
    [InlineData(547, false)]    // Constraint violation
    [InlineData(2627, false)]   // Primary key violation
    [InlineData(8152, false)]   // String data truncation
    [InlineData(50000, false)]  // User-defined error
    [InlineData(99999, false)]  // Unknown error
    public void ShouldRetry_ReturnsFalse_ForNonTransientErrors(int errorNumber, bool expected)
    {
        // Act
        var result = RetryAdvisor.ShouldRetry(errorNumber);

        // Assert
        Assert.Equal(expected, result);
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
