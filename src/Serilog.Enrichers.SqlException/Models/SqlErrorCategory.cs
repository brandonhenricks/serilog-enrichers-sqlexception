namespace Serilog.Enrichers.SqlException.Models;

/// <summary>
/// Categorizes SQL Server errors by operational type.
/// </summary>
public enum SqlErrorCategory
{
    /// <summary>
    /// Unknown or uncategorized error.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Network, connection, or connectivity failures.
    /// </summary>
    Connectivity = 1,

    /// <summary>
    /// SQL syntax errors.
    /// </summary>
    Syntax = 2,

    /// <summary>
    /// Permission or authentication errors.
    /// </summary>
    Permission = 3,

    /// <summary>
    /// Constraint violations (PK, FK, CHECK, UNIQUE).
    /// </summary>
    Constraint = 4,

    /// <summary>
    /// Resource-related errors (locks, deadlocks, timeouts, memory).
    /// </summary>
    Resource = 5,

    /// <summary>
    /// Database or data corruption.
    /// </summary>
    Corruption = 6,

    /// <summary>
    /// Concurrency or optimistic locking failures.
    /// </summary>
    Concurrency = 7
}
