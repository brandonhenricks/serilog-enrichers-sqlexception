# Copilot Instructions for Serilog.Enrichers.SqlException

## Project Overview
This is a Serilog enricher library that extracts structured properties from `Microsoft.Data.SqlClient.SqlException` exceptions and adds them to log events. The enricher walks the exception chain to find SqlExceptions and exposes properties like error number, state, severity, procedure name, and line number.

**Target Framework**: .NET Standard 2.0 (library) / .NET 8.0 (tests)  
**SDK Version**: 8.0.0 (with latestMajor rollForward)

## Architecture

### Core Components
- **`SqlExceptionEnricher`** (`src/Serilog.Enrichers.SqlException/Enrichers/SqlExceptionEnricher.cs`): Implements `ILogEventEnricher` to parse SqlException details and add structured properties
- **`LoggerEnrichmentConfigurationExtensions`** (`src/Serilog.Enrichers.SqlException/Extensions/LoggerEnrichmentConfigurationExtensions.cs`): Provides fluent API extension method `WithSqlExceptionEnricher()`

### Property Extraction Pattern
The enricher extracts properties from the **first SqlError** in the SqlException's Errors collection:
- `SqlException_IsSqlException` (always true when enriched)
- `SqlException_Number` (error number)
- `SqlException_State` (error state)
- `SqlException_Class` (severity/class)
- `SqlException_Procedure` (only if non-empty)
- `SqlException_Line` (line number)
- `SqlException_Server` (only if non-empty)
- `SqlException_Message` (only if non-empty)

### Exception Chain Walking
`GetSqlException()` traverses `InnerException` chain with circular reference protection using `HashSet<Exception>`. This finds SqlExceptions wrapped in other exception types.

## Build & Package Management

### Centralized Package Management
Uses **Central Package Management** via `Directory.Packages.props`:
- All package versions defined centrally
- Projects reference packages WITHOUT version attributes
- Key dependencies: `Serilog` (≥2.12.0), `Microsoft.Data.SqlClient` (≥5.2.0)

### Shared Build Configuration
`Directory.Build.props` defines:
- **Nullable reference types enabled** (`<Nullable>enable</Nullable>`)
- **Warnings as errors for nullable** (`<WarningsAsErrors>nullable</WarningsAsErrors>`)
- XML documentation generation enabled
- Source Link configured for debugging NuGet packages
- Version prefix: 1.0.0

### Building
```powershell
dotnet build Serilog.Enrichers.SqlException.sln
```

### Testing
```powershell
dotnet test Serilog.Enrichers.SqlException.sln
```
Tests use xUnit with .NET 8.0. Test helper uses reflection to instantiate SqlException (not publicly constructible).

### Packaging
```powershell
dotnet pack src/Serilog.Enrichers.SqlException/Serilog.Enrichers.SqlException.csproj -c Release
```
Includes README.md in package. Symbols published as .snupkg.

## Code Conventions

### Namespace Structure
- Root namespace: `Serilog.Enrichers.SqlException`
- Sub-namespaces: `.Enrichers`, `.Extensions`

### Null Safety
- Nullable reference types enabled project-wide
- Nullable warnings treated as errors
- Use `?` suffix for nullable reference types
- Parameter null checks: `if (enrich == null) throw new ArgumentNullException(nameof(enrich));`

### Property Enrichment Pattern
Use `AddPropertyIfAbsent()` to avoid overwriting existing properties. Check for empty/whitespace strings before adding optional properties:
```csharp
if (!string.IsNullOrWhiteSpace(firstError.Procedure))
{
    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SqlException_Procedure", firstError.Procedure));
}
```

### XML Documentation
All public APIs must have XML doc comments with `<summary>`, `<param>`, and `<returns>` tags.

## Testing Patterns

### SqlException Instantiation
SqlException is internal/sealed, so tests use reflection to create instances:
1. Create `SqlErrorCollection` via `Activator.CreateInstance` with non-public constructor
2. Create `SqlError` with 8 parameters (number, state, class, server, message, procedure, line, innerException)
3. Add error to collection using non-public `Add` method
4. Invoke SqlException's non-public constructor with error collection

### Test Organization
- Test class name: `{ClassUnderTest}Tests`
- Arrange-Act-Assert pattern with clear comments
- Test method naming: `MethodName_ExpectedBehavior_Condition`
- Example: `Enrich_FindsSqlException_InInnerException`

### Common Test Scenarios
1. SqlException with all properties populated
2. No exception present
3. Non-SqlException in chain
4. SqlException as InnerException
5. SqlException with empty/null optional properties (procedure, server)

## Extension Method Pattern
Follow Serilog's extension method conventions:
- Extend `LoggerEnrichmentConfiguration`
- Return `LoggerConfiguration` for fluent chaining
- Null check parameters before use
- Usage: `.Enrich.WithSqlExceptionEnricher()`

## Common Tasks

### Adding New Properties
1. Update `SqlExceptionEnricher.Enrich()` to extract property from SqlError
2. Add corresponding test case in `SqlExceptionEnricherTests`
3. Update README.md to document the new property
4. Consider whether property should be conditional (check for null/empty)

### Modifying Exception Walking Logic
Changes to `GetSqlException()` must maintain circular reference protection. Add test cases for new exception chain scenarios.

### Version Updates
Update `<VersionPrefix>` in `Directory.Build.props` for releases. Follow semantic versioning.
