# Serilog.Enrichers.SqlException

[![NuGet](https://img.shields.io/nuget/v/Serilog.Enrichers.SqlException.svg)](https://www.nuget.org/packages/Serilog.Enrichers.SqlException/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A Serilog enricher that extracts structured properties from `Microsoft.Data.SqlClient.SqlException` and adds them to log events. Enhance observability and troubleshooting for .NET applications using SQL Server by capturing detailed exception metadata.

## Features

- **Automatic SqlException Detection**: Walks the exception chain to find `SqlException` instances, even when wrapped in other exception types
- **Structured Logging**: Extracts key properties as separate log event properties for filtering and querying
- **Transient Error Detection**: Identifies transient errors (deadlocks, timeouts, network issues) that typically warrant retry
- **Deadlock Detection**: Automatically identifies deadlock errors (error number 1205)
- **Timeout Classification**: Categorizes timeout errors by type (Command, Connection, Network)
- **Error Categorization**: Classifies errors into logical categories (Connectivity, Syntax, Permission, Constraint, Resource, Corruption, Concurrency)
- **Severity Levels**: Human-readable severity classification (Informational → Fatal) mapped from SQL Server Class values
- **OpenTelemetry Integration**: Optional semantic convention property naming for distributed tracing compatibility
- **Configuration Validation**: Prevents invalid option combinations at initialization time
- **Zero Configuration**: Works out of the box with a single fluent API call
- **Broad Compatibility**: Targets .NET Standard 2.0 for wide framework support

## Installation

Install via NuGet Package Manager:

```powershell
Install-Package Serilog.Enrichers.SqlException
```

Or via .NET CLI:

```bash
dotnet add package Serilog.Enrichers.SqlException
```

## Quick Start

### Basic Usage

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher()
    .WriteTo.Console()
    .CreateLogger();

try
{
    // Your SQL operations
}
catch (Exception ex)
{
    Log.Error(ex, "Database operation failed");
}
```

### Advanced Configuration

```csharp
using Serilog;
using Serilog.Enrichers.SqlException.Configurations;

var options = new SqlExceptionEnricherOptions
{
    PropertyPrefix = "Sql",                  // Default: "SqlException_"
    IncludeAllErrors = true,                 // Enrich all errors in collection
    IncludeConnectionContext = true,         // Add server/database info
    DetectDeadlocks = true,                  // Detect deadlock errors (1205)
    ClassifyTimeouts = true,                 // Classify timeout types
    CategorizeErrors = true,                 // Categorize by error type
    IncludeSeverityLevel = true,             // Add human-readable severity
    UseOpenTelemetrySemantics = true,        // Use OTel property names
};

Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher(options)
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();
```

## Enriched Properties

### Core Properties

| Property | Type | Description | Always Present |
|----------|------|-------------|----------------|
| `SqlException_IsSqlException` | `bool` | Indicates a SqlException was found | Yes |
| `SqlException_Number` | `int` | SQL Server error number | Yes |
| `SqlException_State` | `byte` | Error state | Yes |
| `SqlException_Class` | `byte` | Severity/class level (1-25) | Yes |
| `SqlException_Line` | `int` | Line number where error occurred | Yes |
| `SqlException_IsTransient` | `bool` | True if error is potentially transient (retriable) | Yes |
| `SqlException_Procedure` | `string` | Stored procedure name | Conditional* |
| `SqlException_Server` | `string` | Server name | Conditional* |
| `SqlException_Message` | `string` | Error message | Conditional* |

\* *Only included if non-empty*

### Extended Properties

| Property | Type | Description | Required Option |
|----------|------|-------------|-----------------|
| `SqlException_IsDeadlock` | `bool` | True if error 1205 (deadlock) | `DetectDeadlocks` |
| `SqlException_IsTimeout` | `bool` | True if timeout error | `ClassifyTimeouts` |
| `SqlException_TimeoutType` | `string` | Command/Connection/Network/Unknown | `ClassifyTimeouts` |
| `SqlException_ErrorCategory` | `string` | Connectivity/Syntax/Permission/Constraint/Resource/Corruption/Concurrency/Unknown | `CategorizeErrors` |
| `SqlException_IsUserError` | `bool` | True if user-caused (syntax, constraint violations, etc.) | `CategorizeErrors` |
| `SqlException_SeverityLevel` | `string` | Informational/Warning/Error/Severe/Critical/Fatal | `IncludeSeverityLevel` |
| `SqlException_RequiresImmediateAttention` | `bool` | True for Class ≥ 20 (severe system errors) | `IncludeSeverityLevel` |

### OpenTelemetry Semantic Conventions

When `UseOpenTelemetrySemantics = true`, properties use OTel naming:

| Standard Property | OpenTelemetry Property |
|-------------------|------------------------|
| `SqlException_Number` | `db.error.code` |
| `SqlException_State` | `db.error.state` |
| `SqlException_Class` | `db.error.severity` |
| `SqlException_Message` | `exception.message` |
| `SqlException_Procedure` | `db.operation` |
| `SqlException_Server` | `server.address` |
| `SqlException_ErrorCategory` | `db.error.category` |
| `SqlException_IsTimeout` | `db.error.timeout` |
| `SqlException_TimeoutType` | `db.error.timeout.type` |
| `SqlException_IsDeadlock` | `db.error.deadlock` |
| `SqlException_IsTransient` | `db.error.transient` |
| `SqlException_SeverityLevel` | `db.error.severity.level` |
| `SqlException_RequiresImmediateAttention` | `db.error.critical` |

### Example Log Output

**Basic Output:**
```json
{
  "@t": "2024-12-09T10:30:15.1234567Z",
  "@mt": "Database operation failed",
  "@l": "Error",
  "SqlException_Number": 1205,
  "SqlException_State": 13,
  "SqlException_Class": 20,
  "SqlException_Procedure": "sp_UpdateInventory",
  "SqlException_Line": 42,
  "SqlException_IsTransient": true
}
```

**With Advanced Features:**
```json
{
  "@t": "2024-12-09T10:30:15.1234567Z",
  "@mt": "Database operation failed",
  "@l": "Error",
  "SqlException_Number": 1205,
  "SqlException_IsTransient": true,
  "SqlException_IsDeadlock": true,
  "SqlException_ErrorCategory": "Resource",
  "SqlException_IsUserError": false,
  "SqlException_SeverityLevel": "Critical"
}
```

**With OpenTelemetry Semantics:**
```json
{
  "@t": "2024-12-09T10:30:15.1234567Z",
  "@mt": "Database operation failed",
  "@l": "Error",
  "db.error.code": 1205,
  "db.error.state": 13,
  "db.error.severity": 20,
  "db.operation": "sp_UpdateInventory",
  "server.address": "sql-prod-01",
  "db.error.transient": true,
  "db.error.deadlock": true,
  "db.error.category": "Resource"
}
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `PropertyPrefix` | `string` | `"SqlException_"` | Prefix for all enriched properties |
| `IncludeAllErrors` | `bool` | `true` | Include all errors in Errors collection (not just first) |
| `IncludeConnectionContext` | `bool` | `true` | Include server and database connection info |
| `DetectDeadlocks` | `bool` | `true` | Add `IsDeadlock` property for error 1205 |
| `ClassifyTimeouts` | `bool` | `true` | Classify timeout errors by type |
| `CategorizeErrors` | `bool` | `true` | Add error category and user error properties |
| `IncludeSeverityLevel` | `bool` | `true` | Add human-readable severity level classification |
| `UseOpenTelemetrySemantics` | `bool` | `false` | Use OTel semantic convention property names |

## How It Works

The enricher implements `ILogEventEnricher` and performs the following operations:

1. **Exception Chain Traversal**: Walks through `InnerException` references to locate `SqlException` instances
2. **Circular Reference Protection**: Uses a `HashSet<Exception>` to detect and break circular chains
3. **Property Extraction**: Extracts properties from `SqlError` instances in the `SqlException.Errors` collection
4. **Transient Error Detection**: Identifies transient errors based on error number (deadlocks, timeouts, network issues)
5. **Deadlock Detection**: Identifies error 1205 and flags it as a deadlock
6. **Timeout Classification**: Maps error numbers to timeout types (Command: -2, Connection: -1, Network: 10060/10061)
7. **Error Categorization**: Maps 50+ error numbers to logical categories and user/system classification
8. **Severity Mapping**: Converts SQL Server Class values (1-25) to human-readable severity levels
9. **OpenTelemetry Integration**: Optionally translates properties to OTel semantic conventions
10. **Safe Property Addition**: Uses `AddPropertyIfAbsent()` to avoid overwriting existing properties

> **Note on Deadlock Graphs**: SQL Server does not include deadlock graph XML in the `SqlException` error message. Deadlock graphs are only available in the SQL Server error log when trace flags 1204 or 1222 are enabled. This enricher detects deadlock errors (1205) but cannot extract the graph data from the exception itself.

## Usage Examples

### Deadlock Detection

```csharp
var options = new SqlExceptionEnricherOptions
{
    DetectDeadlocks = true
};

Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher(options)
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

// When a deadlock (error 1205) occurs, logs will include:
// - SqlException_IsDeadlock: true
// Note: Deadlock graphs are not available in SqlException.
// Enable SQL Server trace flags 1204 or 1222 to write graphs to the error log.
```

### Transient Error Detection

```csharp
// Route transient errors to a separate sink for retry handling
Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(le =>
            le.Properties.TryGetValue("SqlException_IsTransient", out var isTransient) &&
            isTransient.ToString() == "True")
        .WriteTo.File("logs/transient-errors.txt"))
    .WriteTo.Console()
    .CreateLogger();

try
{
    await ExecuteDatabaseOperation();
}
catch (Exception ex)
{
    // Transient errors are automatically logged to transient-errors.txt
    // where your retry logic can monitor and process them
    Log.Error(ex, "Database operation failed");
}
```

### Alert on Critical Errors

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher(new SqlExceptionEnricherOptions
    {
        IncludeSeverityLevel = true
    })
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(le =>
            le.Properties.TryGetValue("SqlException_RequiresImmediateAttention", out var attention) &&
            attention.ToString() == "True")
        .WriteTo.Email(
            fromEmail: "alerts@company.com",
            toEmail: "dba@company.com",
            mailServer: "smtp.company.com"))
    .CreateLogger();
```

### Querying Logs in Seq

```sql
-- Find all deadlock errors
SqlException_IsDeadlock = true

-- Find errors in specific stored procedure
SqlException_Procedure = 'sp_UpdateInventory'

-- Find retryable errors
SqlException_IsTransient = true

-- Find critical/fatal errors
SqlException_SeverityLevel in ('Critical', 'Fatal')

-- Find user-caused errors
SqlException_IsUserError = true

-- Find constraint violations
SqlException_ErrorCategory = 'Constraint'
```

## Common SQL Error Numbers

| Error | Category | Description | User Error |
|-------|----------|-------------|------------|
| **Connectivity** | | | |
| -2 | Connectivity | Command timeout | No |
| -1 | Connectivity | Connection timeout | No |
| 4060 | Connectivity | Cannot open database | No |
| 10053 | Connectivity | Transport-level error | No |
| 10060 | Connectivity | Network timeout | No |
| **Syntax** | | | |
| 102 | Syntax | Incorrect syntax near | Yes |
| 156 | Syntax | Incorrect syntax near keyword | Yes |
| 207 | Syntax | Invalid column name | Yes |
| 208 | Syntax | Invalid object name | Yes |
| **Permissions** | | | |
| 229 | Permission | SELECT permission denied | Yes |
| 230 | Permission | EXECUTE permission denied | Yes |
| 262 | Permission | Database permission denied | Yes |
| 18456 | Permission | Login failed | Yes |
| **Constraints** | | | |
| 547 | Constraint | Foreign key violation | Yes |
| 2601 | Constraint | Duplicate key (unique index) | Yes |
| 2627 | Constraint | Primary key violation | Yes |
| 8152 | Constraint | String/binary data truncated | Yes |
| **Resource** | | | |
| 1205 | Resource | Deadlock victim | No |
| 1222 | Resource | Lock request timeout | No |
| 8645 | Resource | Timeout waiting for memory | No |
| **Corruption** | | | |
| 823 | Corruption | I/O error during read | No |
| 824 | Corruption | Logical consistency error | No |
| 825 | Corruption | Read-retry required | No |

## Requirements

- **.NET Standard 2.0+** (compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+)
- **Serilog ≥ 2.12.0**
- **Microsoft.Data.SqlClient ≥ 5.2.0**

## Building from Source

```powershell
# Clone the repository
git clone https://github.com/brandonhenricks/serilog-enrichers-sqlexception.git
cd serilog-enrichers-sqlexception

# Restore dependencies
dotnet restore

# Build the solution
dotnet build -c Release

# Run tests
dotnet test

# Create NuGet package
dotnet pack -c Release
```

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes with clear messages
4. Add or update tests as needed
5. Ensure all tests pass (`dotnet test`)
6. Submit a pull request

## Testing

The test suite uses xUnit and covers:

- SqlException detection in exception chains
- Property extraction and enrichment
- Deadlock detection (error 1205)
- Timeout classification (Command/Connection/Network)
- Error categorization (7 categories, user vs system errors)
- Transient error detection
- Severity level mapping (6 levels: Informational → Fatal)
- OpenTelemetry semantic conventions
- Configuration validation
- Circular exception reference protection

Run tests:

```powershell
dotnet test
```

Run with code coverage:

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## License

This project is licensed under the [MIT License](LICENSE).

## Acknowledgments

Built with [Serilog](https://serilog.net/) - flexible, structured logging for .NET.

## Support

- **Issues**: [GitHub Issues](https://github.com/brandonhenricks/serilog-enrichers-sqlexception/issues)
- **Documentation**: [Serilog Documentation](https://github.com/serilog/serilog/wiki)
- **NuGet**: [Serilog.Enrichers.SqlException](https://www.nuget.org/packages/Serilog.Enrichers.SqlException/)

---

Made with ❤️ by [Brandon Henricks](https://github.com/brandonhenricks)
