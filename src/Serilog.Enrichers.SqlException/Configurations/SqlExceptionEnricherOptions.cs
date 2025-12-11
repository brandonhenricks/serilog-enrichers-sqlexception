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

    /// <summary>
    /// Gets or sets a value indicating whether to detect and flag deadlock errors specifically.
    /// When enabled, adds SqlException_IsDeadlock property and attempts to extract deadlock graph.
    /// Default is <c>true</c>.
    /// </summary>
    public bool DetectDeadlocks { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include the deadlock graph XML in enriched properties.
    /// Only applies when DetectDeadlocks is true and a deadlock graph is present in the error message.
    /// Default is <c>true</c>.
    /// </summary>
    public bool IncludeDeadlockGraph { get; set; } = true;

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
    /// Gets or sets a value indicating whether to emit OpenTelemetry ActivityEvents
    /// for each SqlException encountered. Requires System.Diagnostics.DiagnosticSource.
    /// Default is <c>false</c>.
    /// </summary>
    public bool EmitActivityEvents { get; set; } = false;
}
