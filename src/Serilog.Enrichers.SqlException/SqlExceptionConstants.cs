namespace Serilog.Enrichers.SqlException;

/// <summary>
/// Provides constants used throughout the SQL exception enricher.
/// </summary>
internal static class SqlExceptionConstants
{
    /// <summary>
    /// SQL Server severity level thresholds based on official documentation.
    /// Reference: https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-error-severities
    /// </summary>
    public static class SeverityThresholds
    {
        /// <summary>
        /// Maximum class value for Informational severity (Classes 1-10).
        /// Informational messages that return status information or report errors that are not severe.
        /// </summary>
        public const byte Informational = 10;

        /// <summary>
        /// Maximum class value for Warning severity (Classes 11-13).
        /// Warnings and issues that can be corrected by the user.
        /// </summary>
        public const byte Warning = 13;

        /// <summary>
        /// Maximum class value for Error severity (Classes 14-16).
        /// User errors that can be corrected by the user.
        /// </summary>
        public const byte Error = 16;

        /// <summary>
        /// Maximum class value for Severe severity (Classes 17-19).
        /// Software or hardware errors requiring administrator attention.
        /// </summary>
        public const byte Severe = 19;

        /// <summary>
        /// Maximum class value for Critical severity (Classes 20-24).
        /// System errors where connection is terminated.
        /// </summary>
        public const byte Critical = 24;

        /// <summary>
        /// Minimum class value requiring immediate attention (Class 20+).
        /// Indicates severe system problems.
        /// </summary>
        public const byte ImmediateAttentionRequired = 20;

        // Class 25 is Fatal (implicit from ranges above)
    }
}
