namespace Serilog.Enrichers.SqlException.Models;

/// <summary>
/// Categorizes SQL timeout errors by type.
/// </summary>
public enum SqlTimeoutType
{
    /// <summary>
    /// Unknown or unclassified timeout.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Connection timeout - failed to establish connection.
    /// </summary>
    Connection = 1,

    /// <summary>
    /// Command timeout - query execution exceeded timeout.
    /// </summary>
    Command = 2,

    /// <summary>
    /// Network timeout - network layer failure.
    /// </summary>
    Network = 3
}
