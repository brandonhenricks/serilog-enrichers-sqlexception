namespace Serilog.Enrichers.SqlException.Configurations;

/// <summary>
/// Provides configuration options for <see cref="Enrichers.SqlExceptionEnricher"/>.
/// </summary>
public class SqlExceptionEnricherOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to include all errors from the <c>SqlException.Errors</c> collection.
    /// Default is <c>true</c>.
    /// </summary>
    public bool IncludeAllErrors { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include connection context details
    /// such as database name, data source, and connection timeout.
    /// Default is <c>true</c>.
    /// </summary>
    public bool IncludeConnectionContext { get; set; } = true;

    /// <summary>
    /// Gets or sets the property name prefix for enriched properties.
    /// Default is <c>SqlException_</c>.
    /// </summary>
    public string PropertyPrefix { get; set; } = "SqlException_";

    /// <summary>
    /// Gets or sets a value indicating whether to detect and flag deadlock errors specifically.
    /// When enabled, adds SqlException_IsDeadlock property for error number 1205.
    /// Default is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Note: Deadlock graphs are not included in SqlException error messages by default.
    /// SQL Server writes deadlock graphs to the error log when trace flags 1204 or 1222 are enabled,
    /// but these are not accessible through the SqlException object.
    /// </remarks>
    public bool DetectDeadlocks { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to classify timeout errors by type
    /// (Connection, Command, Network).
    /// Default is <c>true</c>.
    /// </summary>
    public bool ClassifyTimeouts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to categorize errors by operational type
    /// (Connectivity, Syntax, Permission, Constraint, Resource, Corruption, Concurrency).
    /// Default is <c>true</c>.
    /// </summary>
    public bool CategorizeErrors { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use OpenTelemetry semantic conventions
    /// for property names instead of the custom PropertyPrefix.
    /// When enabled, uses standard OTel attributes like 'db.system', 'error.type', etc.
    /// Default is <c>false</c>.
    /// </summary>
    public bool UseOpenTelemetrySemantics { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to include human-readable severity levels
    /// in addition to SQL Server Class values.
    /// Default is <c>true</c>.
    /// </summary>
    public bool IncludeSeverityLevel { get; set; } = true;

    /// <summary>
    /// Validates the configuration options for consistency.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <see cref="PropertyPrefix"/> is null or whitespace.</exception>
    public void Validate()
    {
        if (PropertyPrefix == null || (PropertyPrefix.Length > 0 && string.IsNullOrWhiteSpace(PropertyPrefix)))
        {
            throw new ArgumentException(
                "PropertyPrefix cannot be null or whitespace. Use empty string for no prefix or provide a valid prefix.",
                nameof(PropertyPrefix));
        }
    }
}
