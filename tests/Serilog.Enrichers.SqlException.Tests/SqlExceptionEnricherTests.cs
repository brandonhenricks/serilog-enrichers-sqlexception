using Serilog.Core;
using Serilog.Enrichers.SqlException.Enrichers;
using Serilog.Events;
using Microsoft.Data.SqlClient;
using System.Reflection;

namespace Serilog.Enrichers.SqlException.Tests
{
    public class SqlExceptionEnricherTests
    {
        [Fact]
        public void Enrich_AddsProperties_ForSqlException()
        {
            // Arrange
            var sqlException = CreateSqlException(number: 1205, state: 13, errorClass: 20, procedure: "sp_TestProcedure", line: 42);
            var logEvent = CreateLogEvent(sqlException);
            var enricher = new SqlExceptionEnricher();

            // Act
            enricher.Enrich(logEvent, new SimplePropertyFactory());

            // Assert
            Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsSqlException" && p.Value.ToString() == "True");
            Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Number" && p.Value.ToString() == "1205");
            Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_State" && p.Value.ToString() == "13");
            Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Class" && p.Value.ToString() == "20");
            Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Procedure" && p.Value.ToString().Contains("sp_TestProcedure"));
            Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Line" && p.Value.ToString() == "42");
        }

        [Fact]
        public void Enrich_DoesNothing_WhenNoException()
        {
            // Arrange
            var logEvent = CreateLogEvent(null);
            var enricher = new SqlExceptionEnricher();

            // Act
            enricher.Enrich(logEvent, new SimplePropertyFactory());

            // Assert
            Assert.Empty(logEvent.Properties);
        }

        [Fact]
        public void Enrich_DoesNothing_WhenNoSqlExceptionInChain()
        {
            // Arrange
            var ex = new Exception("outer", new InvalidOperationException("inner"));
            var logEvent = CreateLogEvent(ex);
            var enricher = new SqlExceptionEnricher();

            // Act
            enricher.Enrich(logEvent, new SimplePropertyFactory());

            // Assert
            Assert.Empty(logEvent.Properties);
        }

        [Fact]
        public void Enrich_FindsSqlException_InInnerException()
        {
            // Arrange
            var sqlException = CreateSqlException(number: 2627, state: 1, errorClass: 14, procedure: "", line: 1);
            var outerException = new InvalidOperationException("outer", sqlException);
            var logEvent = CreateLogEvent(outerException);
            var enricher = new SqlExceptionEnricher();

            // Act
            enricher.Enrich(logEvent, new SimplePropertyFactory());

            // Assert
            Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsSqlException" && p.Value.ToString() == "True");
            Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Number" && p.Value.ToString() == "2627");
        }

        [Fact]
        public void Enrich_AddsProperties_WithoutProcedure()
        {
            // Arrange
            var sqlException = CreateSqlException(number: 547, state: 0, errorClass: 16, procedure: "", line: 1);
            var logEvent = CreateLogEvent(sqlException);
            var enricher = new SqlExceptionEnricher();

            // Act
            enricher.Enrich(logEvent, new SimplePropertyFactory());

            // Assert
            Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_IsSqlException" && p.Value.ToString() == "True");
            Assert.Contains(logEvent.Properties, p => p.Key == "SqlException_Number" && p.Value.ToString() == "547");
            Assert.DoesNotContain(logEvent.Properties, p => p.Key == "SqlException_Procedure");
        }

        private static LogEvent CreateLogEvent(Exception? ex)
        {
            return new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Error,
                ex,
                new MessageTemplate("test", []),
                new List<LogEventProperty>());
        }

        private static Microsoft.Data.SqlClient.SqlException CreateSqlException(int number, byte state, byte errorClass, string procedure, int line)
        {
            // SqlException cannot be directly instantiated, so we use reflection to create it
            var sqlExceptionType = typeof(Microsoft.Data.SqlClient.SqlException);
            var sqlErrorCollectionType = sqlExceptionType.Assembly.GetType("Microsoft.Data.SqlClient.SqlErrorCollection");
            var sqlErrorCollection = Activator.CreateInstance(sqlErrorCollectionType!, true);
            
            // Create SqlError with parameters: number, state, class, server, errorMessage, procedure, lineNumber, exception
            var sqlErrorType = sqlExceptionType.Assembly.GetType("Microsoft.Data.SqlClient.SqlError");
            var errorNumber = number;
            var errorState = state;
            var severity = errorClass;
            var server = "localhost";
            var errorMessage = "Test error message";
            var procedureName = procedure;
            var lineNumber = line;
            Exception? innerException = null;
            
            var sqlError = Activator.CreateInstance(
                sqlErrorType!,
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new object[] { errorNumber, errorState, severity, server, errorMessage, procedureName, lineNumber, innerException! },
                null);

            var addMethod = sqlErrorCollectionType!.GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
            addMethod!.Invoke(sqlErrorCollection, new[] { sqlError });

            var ctor = sqlExceptionType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var sqlException = (Microsoft.Data.SqlClient.SqlException)ctor.Invoke(
                new object[] { "Test SQL Exception", sqlErrorCollection!, null!, Guid.NewGuid() });

            return sqlException;
        }

        private class SimplePropertyFactory : ILogEventPropertyFactory
        {
            public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            {
                // For test purposes, we just wrap values in ScalarValue
                // In production, Serilog's property factory handles complex destructuring
                var propertyValue = new ScalarValue(value);
                return new LogEventProperty(name, propertyValue);
            }
        }
    }
}
