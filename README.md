# Serilog.Enrichers.SqlException

[![NuGet](https://img.shields.io/nuget/v/Serilog.Enrichers.SqlException.svg)](https://www.nuget.org/packages/Serilog.Enrichers.SqlException/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A Serilog enricher that extracts structured properties from `Microsoft.Data.SqlClient.SqlException` and adds them to log events. Enhance observability and troubleshooting for .NET applications using SQL Server by capturing detailed exception metadata such as error number, state, severity, procedure name, and line number.

## Features

- **Automatic SqlException Detection**: Walks the exception chain to find `SqlException` instances, even when wrapped in other exception types
- **Structured Logging**: Extracts key properties as separate log event properties for filtering and querying
- **Circular Reference Protection**: Safely handles circular exception chains
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

## Enriched Properties

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

### Example Log Output

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

## How It Works

The enricher implements `ILogEventEnricher` and performs the following operations:

1. **Exception Chain Traversal**: Walks through `InnerException` references to locate `SqlException` instances
2. **Circular Reference Protection**: Uses a `HashSet<Exception>` to detect and break circular chains
3. **Property Extraction**: Extracts properties from the first `SqlError` in the `SqlException.Errors` collection
4. **Conditional Enrichment**: Only adds properties if they contain meaningful values (non-null, non-empty)
5. **Safe Property Addition**: Uses `AddPropertyIfAbsent()` to avoid overwriting existing properties

## Advanced Usage

### Combining with Other Enrichers

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithThreadId()
    .Enrich.WithMachineName()
    .Enrich.WithSqlExceptionEnricher()
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

### Querying Enriched Logs in Seq

```sql
-- Find all deadlock errors
SqlException_Number = 1205

-- Find errors in specific stored procedure
SqlException_Procedure = 'sp_UpdateInventory'

-- Find high-severity errors (16+)
SqlException_Class >= 16
```

## Common SQL Error Numbers

| Error | Description |
|-------|-------------|
| 1205 | Deadlock victim |
| 2627 | Unique constraint violation |
| 547 | Foreign key constraint violation |
| 515 | Cannot insert NULL |
| 8152 | String or binary data truncated |

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

- SqlException with all properties populated
- SqlException as inner exception
- Non-SqlException in chain
- Empty/null optional properties
- Circular exception reference protection

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
