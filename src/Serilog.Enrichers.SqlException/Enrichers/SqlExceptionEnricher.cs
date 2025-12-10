using Serilog.Core;
using Serilog.Events;
using Microsoft.Data.SqlClient;

namespace Serilog.Enrichers.SqlException.Enrichers
{
    /// <summary>
    /// Enriches log events with SQL Server exception details.
    /// </summary>
    public class SqlExceptionEnricher : ILogEventEnricher
    {
        /// <summary>
        /// Enriches the log event with SQL exception properties if a SqlException is present.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">The property factory.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Exception == null)
            {
                return;
            }

            var exception = logEvent.Exception;

            var sqlException = GetSqlException(exception);

            if (sqlException == null)
            {
                return;
            }

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SqlException_IsSqlException", true));
            
            // Add properties from the first SqlError (if any errors exist)
            if (sqlException.Errors.Count > 0)
            {
                var firstError = sqlException.Errors[0];
                
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SqlException_Number", firstError.Number));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SqlException_State", firstError.State));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SqlException_Class", firstError.Class));
                
                if (!string.IsNullOrWhiteSpace(firstError.Procedure))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SqlException_Procedure", firstError.Procedure));
                }
                
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SqlException_Line", firstError.LineNumber));
                
                if (!string.IsNullOrWhiteSpace(firstError.Server))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SqlException_Server", firstError.Server));
                }
                
                if (!string.IsNullOrWhiteSpace(firstError.Message))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SqlException_Message", firstError.Message));
                }
            }
        }

        private static Microsoft.Data.SqlClient.SqlException? GetSqlException(Exception ex)
        {
            while (ex != null)
            {
                if (ex is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    return sqlEx;
                }

                ex = ex.InnerException;
            }
            
            return null;
        }
    }
}
