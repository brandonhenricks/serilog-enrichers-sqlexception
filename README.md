# Serilog.Enrichers.SqlException

[![NuGet](https://img.shields.io/nuget/v/Serilog.Enrichers.SqlException.svg)](https://www.nuget.org/packages/Serilog.Enrichers.SqlException/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A Serilog enricher that extracts structured properties from `Microsoft.Data.SqlClient.SqlException` and adds them to log events. Enhance observability and troubleshooting for .NET applications using SQL Server by capturing detailed exception metadata such as error number, state, severity, procedure name, and line number.

## Features

### Core Features
- **Automatic SqlException Detection**: Walks the exception chain to find `SqlException` instances, even when wrapped in other exception types
- **Structured Logging**: Extracts key properties as separate log event properties for filtering and querying
- **Circular Reference Protection**: Safely handles circular exception chains
- **Zero Configuration**: Works out of the box with a single fluent API call
- **Broad Compatibility**: Targets .NET Standard 2.0 for wide framework support

### Advanced Features (Phase 2)
- **Deadlock Detection**: Automatically identifies deadlock errors (1205) and extracts XML deadlock graphs
- **Timeout Classification**: Categorizes timeout errors by type (Command, Connection, Network)
- **Error Categorization**: Classifies errors into logical categories (Connectivity, Syntax, Permission, Constraint, Resource, Corruption, Concurrency)
- **User vs System Errors**: Distinguishes between user-caused errors and system issues
- **OpenTelemetry Integration**: Optional semantic convention property naming for OTel compatibility
- **Activity Event Emission**: Emit errors as OpenTelemetry ActivityEvents for distributed tracing

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

Add the enricher to your Serilog configuration:

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

Enable Phase 2 features with custom options:

```csharp
using Serilog;
using Serilog.Enrichers.SqlException.Configurations;

var options = new SqlExceptionEnricherOptions
{
    // Core options
    PropertyPrefix = "Sql",              // Default: "SqlException_"
    IncludeAllErrors = true,             // Enrich all errors in collection
    IncludeConnectionContext = true,     // Add server/database info
    DetectTransientFailures = true,      // Identify transient errors
    
    // Phase 2 options
    DetectDeadlocks = true,              // Detect deadlock errors
    IncludeDeadlockGraph = true,         // Extract XML deadlock graph
    ClassifyTimeouts = true,             // Classify timeout types
    CategorizeErrors = true,             // Categorize by error type
    UseOpenTelemetrySemantics = true,    // Use OTel property names
    EmitActivityEvents = true            // Emit as OTel ActivityEvents
};

Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher(options)
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();
```

## Enriched Properties

### Core Properties

When a `SqlException` is detected, the following properties are added to the log event:

| Property | Type | Description | Always Present |
|----------|------|-------------|----------------|
| `SqlException_IsSqlException` | `bool` | Indicates a SqlException was found | Yes |
| `SqlException_Number` | `int` | SQL Server error number | Yes |
| `SqlException_State` | `byte` | Error state | Yes |
| `SqlException_Class` | `byte` | Severity/class level (1-25) | Yes |
| `SqlException_Line` | `int` | Line number where error occurred | Yes |
| `SqlException_Procedure` | `string` | Stored procedure name | Conditional* |
| `SqlException_Server` | `string` | Server name | Conditional* |
| `SqlException_Message` | `string` | Error message | Conditional* |

\* *Only included if non-empty*

### Phase 2 Properties

Additional properties when advanced features are enabled:

| Property | Type | Description | Required Option |
|----------|------|-------------|-----------------|
| `SqlException_IsDeadlock` | `bool` | True if error 1205 (deadlock) | `DetectDeadlocks = true` |
| `SqlException_DeadlockGraph` | `string` | XML deadlock graph from message | `IncludeDeadlockGraph = true` |
| `SqlException_IsTimeout` | `bool` | True if timeout error | `ClassifyTimeouts = true` |
| `SqlException_TimeoutType` | `string` | Command/Connection/Network/Unknown | `ClassifyTimeouts = true` |
| `SqlException_ErrorCategory` | `string` | Connectivity/Syntax/Permission/Constraint/Resource/Corruption/Concurrency/Unknown | `CategorizeErrors = true` |
| `SqlException_IsUserError` | `bool` | True if user-caused (syntax, constraint violations, etc.) | `CategorizeErrors = true` |

### OpenTelemetry Semantic Conventions

When `UseOpenTelemetrySemantics = true`, properties use OTel naming:

| Standard Property | OpenTelemetry Property |
|-------------------|------------------------|
| `SqlException_Number` | `db.error.code` |
| `SqlException_State` | `db.error.state` |
| `SqlException_Class` | `db.error.severity` |
| `SqlException_Message` | `db.error.message` |
| `SqlException_Procedure` | `db.operation.name` |
| `SqlException_Server` | `server.address` |
| `SqlException_ErrorCategory` | `db.error.category` |
| `SqlException_IsTimeout` | `db.error.is_timeout` |
| `SqlException_TimeoutType` | `db.error.timeout_type` |
| `SqlException_IsDeadlock` | `db.error.is_deadlock` |

### Example Log Output

#### Basic Output
```json
{
  "@t": "2024-12-09T10:30:15.1234567Z",
  "@mt": "Database operation failed",
  "@l": "Error",
  "@x": "Microsoft.Data.SqlClient.SqlException...",
  "SqlException_IsSqlException": true,
  "SqlException_Number": 1205,
  "SqlException_State": 13,
  "SqlException_Class": 20,
  "SqlException_Procedure": "sp_UpdateInventory",
  "SqlException_Line": 42,
  "SqlException_Server": "sql-prod-01",
  "SqlException_Message": "Transaction was deadlocked on lock resources with another process"
}
```

#### With Phase 2 Features
```json
{
  "@t": "2024-12-09T10:30:15.1234567Z",
  "@mt": "Database operation failed",
  "@l": "Error",
  "@x": "Microsoft.Data.SqlClient.SqlException...",
  "SqlException_IsSqlException": true,
  "SqlException_Number": 1205,
  "SqlException_State": 13,
  "SqlException_Class": 20,
  "SqlException_Procedure": "sp_UpdateInventory",
  "SqlException_Line": 42,
  "SqlException_Server": "sql-prod-01",
  "SqlException_Message": "Transaction was deadlocked...",
  "SqlException_IsDeadlock": true,
  "SqlException_DeadlockGraph": "<deadlock-list>...</deadlock-list>",
  "SqlException_ErrorCategory": "Resource",
  "SqlException_IsUserError": false
}
```

#### With OpenTelemetry Semantics
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
  "db.error.is_deadlock": true,
  "db.error.category": "Resource",
  "error.type": "Microsoft.Data.SqlClient.SqlException"
}
```

## Configuration Options

The enricher can be customized via `SqlExceptionEnricherOptions`:

### Core Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `PropertyPrefix` | `string` | `"SqlException_"` | Prefix for all enriched properties |
| `IncludeAllErrors` | `bool` | `true` | Include all errors in Errors collection (not just first) |
| `IncludeConnectionContext` | `bool` | `true` | Include server and database connection info |
| `DetectTransientFailures` | `bool` | `true` | Identify transient errors suitable for retry |

### Phase 2 Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `DetectDeadlocks` | `bool` | `true` | Add `IsDeadlock` property for error 1205 |
| `IncludeDeadlockGraph` | `bool` | `true` | Extract XML deadlock graph from error message |
| `ClassifyTimeouts` | `bool` | `true` | Add timeout classification properties |
| `CategorizeErrors` | `bool` | `true` | Add error category and user error properties |
| `UseOpenTelemetrySemantics` | `bool` | `false` | Use OTel semantic convention property names |
| `EmitActivityEvents` | `bool` | `false` | Emit errors as OpenTelemetry ActivityEvents |

### Example: Full Configuration

```csharp
var options = new SqlExceptionEnricherOptions
{
    // Core options
    PropertyPrefix = "Db",                   // Properties like "Db_Number" instead of "SqlException_Number"
    IncludeAllErrors = true,                 // Include all errors, not just first
    IncludeConnectionContext = true,         // Add server/database info
    DetectTransientFailures = true,          // Mark transient errors
    
    // Phase 2 options
    DetectDeadlocks = true,                  // Detect deadlocks
    IncludeDeadlockGraph = true,             // Extract deadlock XML
    ClassifyTimeouts = true,                 // Classify timeout types
    CategorizeErrors = true,                 // Categorize all errors
    UseOpenTelemetrySemantics = false,       // Use custom prefix (not OTel names)
    EmitActivityEvents = true                // Emit to OTel Activity
};

Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher(options)
    .CreateLogger();
```

## How It Works

The enricher implements `ILogEventEnricher` and performs the following operations:

### Core Enrichment Process

1. **Exception Chain Traversal**: Walks through `InnerException` references to locate `SqlException` instances
2. **Circular Reference Protection**: Uses a `HashSet<Exception>` to detect and break circular chains
3. **Property Extraction**: Extracts properties from the first `SqlError` in the `SqlException.Errors` collection
4. **Conditional Enrichment**: Only adds properties if they contain meaningful values (non-null, non-empty)
5. **Safe Property Addition**: Uses `AddPropertyIfAbsent()` to avoid overwriting existing properties

### Phase 2 Enhanced Processing

When advanced features are enabled:

#### Deadlock Detection
- Checks if error number is 1205
- Extracts XML deadlock graph using regex pattern matching
- Graph format: `<deadlock-list>...</deadlock-list>` or `<deadlock>...</deadlock>`
- Useful for analyzing deadlock participants and resources

#### Timeout Classification
- Maps error numbers to timeout types:
  - `-2` → Command timeout
  - `-1` → Connection timeout  
  - `10060`, `10061` → Network timeout
- Enables filtering and alerting by timeout type

#### Error Categorization
- Maps 50+ error numbers to logical categories
- Identifies user-caused vs system errors
- Categories: Connectivity, Syntax, Permission, Constraint, Resource, Corruption, Concurrency, Unknown

#### OpenTelemetry Integration
- Translates 20+ properties to OTel semantic conventions
- Optionally emits errors as `ActivityEvent` instances on current `Activity`
- Supports distributed tracing and APM tools (Application Insights, Jaeger, Zipkin, etc.)

### Architecture

```
LogEvent with SqlException
    ↓
SqlExceptionEnricher.Enrich()
    ↓
├─ Find SqlException in chain
├─ Extract core properties (number, state, class, etc.)
├─ [Optional] Detect deadlock + extract graph
├─ [Optional] Classify timeout type
├─ [Optional] Categorize error type
├─ [Optional] Map to OTel semantic conventions
└─ [Optional] Emit ActivityEvent
    ↓
Enriched LogEvent with structured properties
```

## Advanced Usage

### Deadlock Detection and Analysis

Automatically detect deadlocks and extract the XML deadlock graph for detailed analysis:

```csharp
var options = new SqlExceptionEnricherOptions
{
    DetectDeadlocks = true,
    IncludeDeadlockGraph = true
};

Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher(options)
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

// When a deadlock occurs, logs will include:
// - SqlException_IsDeadlock: true
// - SqlException_DeadlockGraph: "<deadlock-list>...</deadlock-list>"
```

Query deadlocks in Seq:
```sql
SqlException_IsDeadlock = true
```

### Timeout Classification

Distinguish between different types of timeout errors:

```csharp
var options = new SqlExceptionEnricherOptions
{
    ClassifyTimeouts = true
};

// Timeout types detected:
// - Command: Error -2 (CommandTimeout exceeded)
// - Connection: Error -1 (ConnectionTimeout exceeded)  
// - Network: Errors 10060, 10061 (Network timeouts)
```

Query timeout errors by type:
```sql
-- All timeouts
SqlException_IsTimeout = true

-- Only command timeouts
SqlException_TimeoutType = 'Command'

-- Network-related timeouts
SqlException_TimeoutType = 'Network'
```

### Error Categorization

Automatically categorize errors for easier troubleshooting:

```csharp
var options = new SqlExceptionEnricherOptions
{
    CategorizeErrors = true
};

// Categories:
// - Connectivity: Network/connection issues
// - Syntax: SQL syntax errors
// - Permission: Authorization failures
// - Constraint: FK/PK/Unique violations
// - Resource: Deadlocks, memory, disk space
// - Corruption: Database corruption
// - Concurrency: Snapshot isolation conflicts
// - Unknown: Uncategorized errors
```

Query by category in Seq:
```sql
-- All constraint violations
SqlException_ErrorCategory = 'Constraint'

-- User-caused errors (syntax, constraints, permissions)
SqlException_IsUserError = true

-- System issues only
SqlException_IsUserError = false
```

### OpenTelemetry Integration

Use OpenTelemetry semantic conventions for consistent observability:

```csharp
var options = new SqlExceptionEnricherOptions
{
    UseOpenTelemetrySemantics = true,
    EmitActivityEvents = true  // Also emit as OTel ActivityEvents
};

Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher(options)
    .WriteTo.OpenTelemetry(opts => {
        opts.Endpoint = "http://localhost:4317";
    })
    .CreateLogger();

// Properties follow OTel conventions:
// - db.error.code (instead of SqlException_Number)
// - db.error.severity (instead of SqlException_Class)
// - server.address (instead of SqlException_Server)
// - db.operation (instead of SqlException_Procedure)
```

### Combining with Other Enrichers

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithThreadId()
    .Enrich.WithMachineName()
    .Enrich.WithSqlExceptionEnricher(new SqlExceptionEnricherOptions
    {
        DetectDeadlocks = true,
        ClassifyTimeouts = true,
        CategorizeErrors = true
    })
    .Enrich.FromLogContext()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();
```

### Filtering by SQL Error Number

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(le => 
            le.Properties.TryGetValue("SqlException_Number", out var number) &&
            number.ToString() == "1205") // Deadlock errors only
        .WriteTo.Seq("http://localhost:5341"))
    .CreateLogger();
```

### Alerting on Critical Errors

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher(new SqlExceptionEnricherOptions
    {
        CategorizeErrors = true
    })
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(le =>
            le.Properties.TryGetValue("SqlException_ErrorCategory", out var category) &&
            (category.ToString() == "\"Corruption\"" || category.ToString() == "\"Resource\""))
        .WriteTo.Email(
            fromEmail: "alerts@company.com",
            toEmail: "dba@company.com",
            mailServer: "smtp.company.com"))
    .CreateLogger();
```

### Querying Enriched Logs in Seq

```sql
-- Find all deadlock errors
SqlException_Number = 1205
SqlException_IsDeadlock = true

-- Find errors in specific stored procedure
SqlException_Procedure = 'sp_UpdateInventory'

-- Find high-severity errors (16+)
SqlException_Class >= 16

-- Find all timeout errors
SqlException_IsTimeout = true

-- Find constraint violations
SqlException_ErrorCategory = 'Constraint'

-- Find user errors vs system errors
SqlException_IsUserError = true
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

The test suite uses xUnit and includes scenarios for:

### Phase 1 Tests
- SqlException with all properties populated
- SqlException as inner exception
- Non-SqlException in chain
- Empty/null optional properties
- Circular exception reference protection
- All errors collection enrichment
- Connection context enrichment
- Transient failure detection

### Phase 2 Tests
- **Deadlock Detection**: Error 1205 detection, XML graph extraction, configuration options
- **Timeout Classification**: Command/Connection/Network timeout detection, configuration options
- **Error Categorization**: All 7 categories, user vs system errors, unknown error handling
- **OpenTelemetry Integration**: Semantic convention property naming, custom prefix fallback

Run tests with code coverage:

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
