---
name: Configuration / Integration issue
about: Report problems setting up or integrating the enricher
title: '[CONFIG] '
labels: 'configuration'
assignees: ''

---

**Describe the configuration issue**
A clear and concise description of the problem you're experiencing during setup or integration.

**Your Serilog configuration**
Please provide your complete Serilog configuration (sanitize any sensitive information):
```csharp
// Example:
Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher()
    .WriteTo.Console()
    .CreateLogger();
```

**Environment:**
 - .NET Version: [e.g. .NET 8.0, .NET Framework 4.8]
 - Serilog Version: [e.g. 2.12.0]
 - Serilog.Enrichers.SqlException Version: [e.g. 1.0.0]
 - Microsoft.Data.SqlClient Version: [e.g. 5.2.0]
 - Other relevant Serilog packages: [e.g. Serilog.Sinks.Console 4.1.0]

**What you expected**
A clear description of what you expected to happen with your configuration.

**What actually happened**
What actually happened when you tried to use this configuration.

**Error messages or logs**
If applicable, include any error messages, stack traces, or log output:
```
Paste error messages here
```

**Additional context**
Add any other context about the configuration issue here.
