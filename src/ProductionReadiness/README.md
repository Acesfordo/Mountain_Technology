# Production Readiness Assessment Tool

## Overview

This tool provides a comprehensive production readiness assessment for the eShop application. It evaluates:

1. **Azure Web Services Accessibility** - Checks connectivity to Azure Application Insights, Service Bus, Redis, and PostgreSQL databases
2. **Data Pipeline Performance** - Assesses database connectivity, response times, and pipeline health
3. **Configuration Validation** - Verifies that critical configuration settings are properly configured

## Features

- ✓ Azure Application Insights connectivity check
- ✓ Azure Service Bus connectivity and accessibility
- ✓ Redis cache connectivity and performance metrics
- ✓ PostgreSQL database connectivity for all databases (catalog, ordering, identity, webhooks)
- ✓ Data pipeline performance assessment with aggregate metrics
- ✓ Configuration validation for all critical services
- ✓ Detailed production readiness report with recommendations
- ✓ Support for both text and JSON output formats
- ✓ Exit codes for CI/CD integration (0 = ready, 1 = not ready, 2 = error)

## Usage

### Run the Assessment Tool

```bash
cd src/ProductionReadiness.Tool
dotnet run
```

### Configuration

The tool reads connection strings and configuration from:

1. **appsettings.json** - Local configuration file
2. **Environment Variables** - Override settings via environment variables

#### Required Connection Strings

Configure these connection strings in `appsettings.json` or as environment variables:

```json
{
  "ConnectionStrings": {
    "AppInsights": "InstrumentationKey=xxxx;IngestionEndpoint=https://xxxx",
    "EventBus": "<namespace>.servicebus.windows.net",
    "redis": "localhost:6379",
    "catalogdb": "Host=localhost;Database=catalogdb;Username=postgres;Password=xxx",
    "orderingdb": "Host=localhost;Database=orderingdb;Username=postgres;Password=xxx",
    "identitydb": "Host=localhost;Database=identitydb;Username=postgres;Password=xxx",
    "webhooksdb": "Host=localhost;Database=webhooksdb;Username=postgres;Password=xxx"
  }
}
```

### Environment Variables

You can override configuration using environment variables:

```bash
export ConnectionStrings__AppInsights="InstrumentationKey=xxxx"
export ConnectionStrings__EventBus="<namespace>.servicebus.windows.net"
export ConnectionStrings__redis="localhost:6379"
```

### Output Options

#### Text Format (Default)

```bash
dotnet run
```

#### JSON Format

```bash
export REPORT_FORMAT=json
dotnet run
```

#### Save to File

```bash
export REPORT_OUTPUT_FILE=/tmp/production-readiness-report.txt
dotnet run
```

## Report Interpretation

### Production Ready Status

The tool determines production readiness based on:

- **No critical Azure service failures** - Service Bus and databases must be accessible
- **At least 75% of data pipeline checks pass** - Most databases must be reachable
- **No critical configuration missing** - Essential connection strings must be configured

### Assessment Status Levels

- **✓ Success** - Check passed successfully
- **⚠ Warning** - Check passed with warnings (e.g., service not configured)
- **✗ Failure** - Check failed critically
- **○ Not Applicable** - Check does not apply to current environment

### Sample Output

```
═══════════════════════════════════════════════════════════════
          PRODUCTION READINESS ASSESSMENT REPORT
═══════════════════════════════════════════════════════════════

Generated At: 2025-10-25 14:30:00 UTC
Environment:  Production
Status:       ✓ READY FOR PRODUCTION

Summary:      12/12 checks passed
              0 failed, 0 warnings

───────────────────────────────────────────────────────────────
AZURE WEB SERVICES ACCESSIBILITY
───────────────────────────────────────────────────────────────
✓ Azure Application Insights Connectivity
  Status: Success
  Application Insights is configured. Test telemetry event sent successfully.
  Response Time: 245.32ms
  Metrics:
    - Accessible: True
    - Configured: True
    - ResponseTimeMs: 245
    - TestEventSent: True

✓ Azure Service Bus Connectivity
  Status: Success
  Successfully connected to Service Bus namespace
  Response Time: 187.54ms
  Metrics:
    - Accessible: True
    - Configured: True
    - ResponseTimeMs: 187

...
```

## Integration with CI/CD

The tool returns appropriate exit codes for CI/CD integration:

- **0** - Production ready, all checks passed
- **1** - Not production ready, has failures
- **2** - Fatal error during assessment

### Example GitHub Actions

```yaml
- name: Run Production Readiness Assessment
  run: |
    cd src/ProductionReadiness.Tool
    dotnet run
  env:
    ConnectionStrings__AppInsights: ${{ secrets.APP_INSIGHTS_CONNECTION_STRING }}
    ConnectionStrings__EventBus: ${{ secrets.SERVICE_BUS_CONNECTION_STRING }}
    REPORT_OUTPUT_FILE: production-readiness-report.txt
```

## Library Usage

You can also use the `ProductionReadiness` library directly in your code:

```csharp
using ProductionReadiness.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var configuration = ...; // Your IConfiguration
var logger = ...; // Your ILogger

var service = new ProductionReadinessService(configuration, logger);
var report = await service.RunAssessmentAsync();

if (report.IsProductionReady)
{
    Console.WriteLine("System is ready for production!");
}
else
{
    Console.WriteLine($"System has {report.FailedChecks} failed checks");
}
```

## Troubleshooting

### Connection String Issues

If you see warnings about services not being configured:
- Verify connection strings are properly set in `appsettings.json`
- Ensure environment variables are properly formatted (use `__` for nested config keys)
- Check for typos in connection string keys

### Azure Service Authentication Failures

If Azure services fail to authenticate:
- Verify your connection strings are correct and not expired
- Ensure managed identities or service principals have appropriate permissions
- Check firewall rules allow connections from your environment

### Database Connection Failures

If database connections fail:
- Verify PostgreSQL is running and accessible
- Check credentials are correct
- Ensure databases exist and are properly initialized
- Verify network connectivity and firewall rules

## Development

### Building the Project

```bash
cd src/ProductionReadiness
dotnet build
```

### Running Tests

```bash
cd tests/ProductionReadiness.Tests
dotnet test
```

## License

This tool is part of the eShop reference application and follows the same license.
