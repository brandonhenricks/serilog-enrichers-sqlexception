---
name: Bug report
about: Create a report to help us improve
title: ''
labels: ''
assignees: ''

---

**Describe the bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Go to '...'
2. Click on '....'
3. Scroll down to '....'
4. See error

**Expected behavior**
A clear and concise description of what you expected to happen.

**Environment (please complete the following information):**
 - OS: [e.g. Windows 11, Ubuntu 22.04]
 - .NET Version: [e.g. .NET 8.0, .NET Framework 4.8]
 - Serilog Version: [e.g. 2.12.0]
 - Serilog.Enrichers.SqlException Version: [e.g. 1.0.0]
 - Microsoft.Data.SqlClient Version: [e.g. 5.2.0]

**Configuration**
Please provide your Serilog configuration (sanitize any sensitive information):
```csharp
// Example:
Log.Logger = new LoggerConfiguration()
    .Enrich.WithSqlExceptionEnricher()
    .WriteTo.Console()
    .CreateLogger();
```

**Additional context**
Add any other context about the problem here (stack traces, log output, etc.).
