using Serilog.Configuration;
using Serilog.Enrichers.SqlException.Enrichers;

namespace Serilog.Enrichers.SqlException.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="LoggerEnrichmentConfiguration"/> to add SQL exception enrichers.
    /// </summary>
    public static class LoggerEnrichmentConfigurationExtensions
    {
        /// <summary>
        /// Enriches log events with SQL Server exception details using <see cref="SqlExceptionEnricher"/>.
        /// </summary>
        /// <param name="enrich">The logger enrichment configuration.</param>
        /// <returns>The logger configuration, enriched with SQL Server exception details.</returns>
        public static LoggerConfiguration WithSqlExceptionEnricher(this LoggerEnrichmentConfiguration enrich)
        {
            if (enrich == null) throw new ArgumentNullException(nameof(enrich));

            return enrich.With(new SqlExceptionEnricher());
        }
    }
}
