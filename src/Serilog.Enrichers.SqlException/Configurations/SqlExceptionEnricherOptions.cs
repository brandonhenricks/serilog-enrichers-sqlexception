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
    /// Gets or sets a value indicating whether to detect and flag transient failures
    /// that are typically retry-eligible.
    /// Default is <c>true</c>.
    /// </summary>
    public bool DetectTransientFailures { get; set; } = true;

    /// <summary>
    /// Gets or sets the property name prefix for enriched properties.
    /// Default is <c>SqlException_</c>.
    /// </summary>
    public string PropertyPrefix { get; set; } = "SqlException_";
}
