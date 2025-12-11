using Serilog.Enrichers.SqlException.Models;

namespace Serilog.Enrichers.SqlException.Helpers;

/// <summary>
/// Provides categorization and classification for SQL Server errors.
/// </summary>
internal static class SqlErrorCategorizer
{
    private static readonly Dictionary<int, SqlErrorCategory> s_errorCategoryMap = new()
    {
        // Connectivity errors
        [-2] = SqlErrorCategory.Connectivity,
        [-1] = SqlErrorCategory.Connectivity,
        [4060] = SqlErrorCategory.Connectivity,
        [10053] = SqlErrorCategory.Connectivity,
        [10054] = SqlErrorCategory.Connectivity,
        [10060] = SqlErrorCategory.Connectivity,
        [10061] = SqlErrorCategory.Connectivity,
        [40143] = SqlErrorCategory.Connectivity,
        [40197] = SqlErrorCategory.Connectivity,
        [40501] = SqlErrorCategory.Connectivity,
        [40540] = SqlErrorCategory.Connectivity,
        [40613] = SqlErrorCategory.Connectivity,

        // Syntax errors
        [102] = SqlErrorCategory.Syntax,
        [156] = SqlErrorCategory.Syntax,
        [207] = SqlErrorCategory.Syntax,
        [208] = SqlErrorCategory.Syntax,
        [213] = SqlErrorCategory.Syntax,

        // Permission errors
        [229] = SqlErrorCategory.Permission,
        [230] = SqlErrorCategory.Permission,
        [262] = SqlErrorCategory.Permission,
        [297] = SqlErrorCategory.Permission,
        [18456] = SqlErrorCategory.Permission,

        // Constraint violations
        [547] = SqlErrorCategory.Constraint,
        [2601] = SqlErrorCategory.Constraint,
        [2627] = SqlErrorCategory.Constraint,
        [8152] = SqlErrorCategory.Constraint,

        // Resource errors
        [1205] = SqlErrorCategory.Resource,
        [1222] = SqlErrorCategory.Resource,
        [8645] = SqlErrorCategory.Resource,
        [8651] = SqlErrorCategory.Resource,
        [40544] = SqlErrorCategory.Resource,
        [40549] = SqlErrorCategory.Resource,
        [40550] = SqlErrorCategory.Resource,
        [40551] = SqlErrorCategory.Resource,
        [40552] = SqlErrorCategory.Resource,
        [40553] = SqlErrorCategory.Resource,

        // Corruption errors
        [823] = SqlErrorCategory.Corruption,
        [824] = SqlErrorCategory.Corruption,
        [825] = SqlErrorCategory.Corruption,

        // Concurrency errors
        [3960] = SqlErrorCategory.Concurrency,
        [3961] = SqlErrorCategory.Concurrency,
    };

    private static readonly Dictionary<int, SqlTimeoutType> s_timeoutTypeMap = new()
    {
        [-2] = SqlTimeoutType.Command,
        [-1] = SqlTimeoutType.Connection,
        [10060] = SqlTimeoutType.Network,
        [10061] = SqlTimeoutType.Network,
    };

    private static readonly HashSet<int> s_userErrorNumbers = new()
    {
        102, 156, 207, 208, 213,
        547, 2601, 2627, 8152,
        229, 230, 262, 297
    };

    /// <summary>
    /// Gets the error category for the specified error number.
    /// </summary>
    public static SqlErrorCategory GetCategory(int errorNumber)
    {
        return s_errorCategoryMap.TryGetValue(errorNumber, out var category)
            ? category
            : SqlErrorCategory.Unknown;
    }

    /// <summary>
    /// Gets the timeout type for the specified error number.
    /// </summary>
    public static SqlTimeoutType GetTimeoutType(int errorNumber)
    {
        return s_timeoutTypeMap.TryGetValue(errorNumber, out var type)
            ? type
            : SqlTimeoutType.Unknown;
    }

    /// <summary>
    /// Determines if the error number represents a timeout.
    /// </summary>
    public static bool IsTimeout(int errorNumber)
    {
        return s_timeoutTypeMap.ContainsKey(errorNumber);
    }

    /// <summary>
    /// Determines if the error number represents a deadlock.
    /// </summary>
    public static bool IsDeadlock(int errorNumber)
    {
        return errorNumber == 1205;
    }

    /// <summary>
    /// Determines if the error is typically caused by user action (vs system issue).
    /// </summary>
    public static bool IsUserError(int errorNumber)
    {
        return s_userErrorNumbers.Contains(errorNumber);
    }
}
