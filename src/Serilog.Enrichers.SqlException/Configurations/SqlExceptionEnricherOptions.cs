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
    /// <remarks>
    /// This property is obsolete. Use <see cref="ProvideRetryGuidance"/> instead,
    /// which provides more comprehensive retry recommendations including strategy,
    /// delay, and reasoning. The SqlException_IsTransient property will be set based
    /// on SqlException_ShouldRetry when ProvideRetryGuidance is enabled.
    /// </remarks>
    [Obsolete("Use ProvideRetryGuidance instead for comprehensive retry recommendations. This property will be removed in a future version.")]
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

    /// <summary>
    /// Gets or sets a value indicating whether to provide retry guidance properties
    /// including retry strategy, delay, and reasoning.
    /// Default is <c>true</c>.
    /// </summary>
    public bool ProvideRetryGuidance { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include human-readable severity levels
    /// in addition to SQL Server Class values.
    /// Default is <c>true</c>.
    /// </summary>
    public bool IncludeSeverityLevel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable diagnostic logging.
    /// When enabled, the enricher will call the DiagnosticLogger action with debug information.
    /// Default is <c>false</c>.
    /// </summary>
    public bool EnableDiagnostics { get; set; } = false;

    /// <summary>
    /// Gets or sets an optional diagnostic logger action.
    /// Called when EnableDiagnostics is true to provide troubleshooting information.
    /// </summary>
    public Action<string>? DiagnosticLogger { get; set; }

    /// <summary>
    /// Validates the configuration options for consistency.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (IncludeDeadlockGraph && !DetectDeadlocks)
        {
            throw new InvalidOperationException(
                "IncludeDeadlockGraph requires DetectDeadlocks to be enabled. " +
                "Set DetectDeadlocks = true or disable IncludeDeadlockGraph.");
        }

        if (string.IsNullOrWhiteSpace(PropertyPrefix))
        {
            throw new ArgumentException(
                "PropertyPrefix cannot be null or whitespace. " +
                "Use empty string for no prefix or provide a valid prefix.",
                nameof(PropertyPrefix));
        }

        if (EmitActivityEvents && DiagnosticLogger != null)
        {
            DiagnosticLogger("ActivityEvents emission is enabled - ensure System.Diagnostics.Activity is available");
        }
    }
}
