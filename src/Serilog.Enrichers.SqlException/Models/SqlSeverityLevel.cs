namespace Serilog.Enrichers.SqlException.Models;

/// <summary>
/// Represents human-readable severity levels for SQL Server errors.
/// Maps SQL Server Class (1-25) to intuitive severity categories.
/// </summary>
public enum SqlSeverityLevel
{
    /// <summary>
    /// Informational message (Class 1-10).
    /// No error condition.
    /// </summary>
    Informational = 0,

    /// <summary>
    /// Warning message (Class 11-13).
    /// User-correctable issues.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error (Class 14-16).
    /// User-correctable errors, application-level issues.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Severe error (Class 17-19).
    /// System-level errors, often transient.
    /// </summary>
    Severe = 3,

    /// <summary>
    /// Critical error (Class 20-24).
    /// Connection-breaking errors, requires reconnection.
    /// </summary>
    Critical = 4,

    /// <summary>
    /// Fatal error (Class 25).
    /// System-wide fatal condition.
    /// </summary>
    Fatal = 5
}
