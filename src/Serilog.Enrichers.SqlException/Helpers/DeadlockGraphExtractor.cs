using System.Text.RegularExpressions;

namespace Serilog.Enrichers.SqlException.Helpers;

/// <summary>
/// Extracts deadlock graph XML from SQL Server error messages.
/// </summary>
internal static class DeadlockGraphExtractor
{
    private static readonly Regex s_deadlockGraphPattern = new(
        @"<deadlock-list>.*?</deadlock-list>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Attempts to extract the deadlock graph XML from an error message.
    /// </summary>
    /// <param name="errorMessage">The SQL Server error message.</param>
    /// <param name="graph">The extracted deadlock graph XML, or null if not found.</param>
    /// <returns>True if a deadlock graph was found; otherwise, false.</returns>
    public static bool TryExtractGraph(string? errorMessage, out string? graph)
    {
        graph = null;

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return false;
        }

        var match = s_deadlockGraphPattern.Match(errorMessage);
        if (match.Success)
        {
            graph = match.Value;
            return true;
        }

        return false;
    }
}
