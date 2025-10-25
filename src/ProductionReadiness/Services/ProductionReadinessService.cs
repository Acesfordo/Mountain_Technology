using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductionReadiness.Checkers;
using ProductionReadiness.Models;

namespace ProductionReadiness.Services;

/// <summary>
/// Main service for running production readiness assessments
/// </summary>
public class ProductionReadinessService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProductionReadinessService> _logger;

    public ProductionReadinessService(IConfiguration configuration, ILogger<ProductionReadinessService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Runs a comprehensive production readiness assessment
    /// </summary>
    public async Task<ProductionReadinessReport> RunAssessmentAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting production readiness assessment...");

        var report = new ProductionReadinessReport
        {
            Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown"
        };

        // Check Azure Services
        await CheckAzureServicesAsync(report, cancellationToken);

        // Check Data Pipeline Performance
        await CheckDataPipelinePerformanceAsync(report, cancellationToken);

        // Check Configuration
        CheckConfiguration(report);

        // Determine if production ready
        report.IsProductionReady = DetermineProductionReadiness(report);

        // Generate recommendations
        GenerateRecommendations(report);

        _logger.LogInformation("Production readiness assessment completed. Status: {Status}", 
            report.IsProductionReady ? "READY" : "NOT READY");

        return report;
    }

    private async Task CheckAzureServicesAsync(ProductionReadinessReport report, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking Azure services accessibility...");

        // Check Application Insights
        var appInsightsConnectionString = _configuration.GetConnectionString("AppInsights");
        var appInsightsChecker = new ApplicationInsightsChecker(appInsightsConnectionString, 
            _logger as ILogger<ApplicationInsightsChecker>);
        report.AzureServicesResults.Add(await appInsightsChecker.CheckAsync(cancellationToken));

        // Check Service Bus
        var serviceBusConnectionString = _configuration.GetConnectionString("EventBus");
        var serviceBusChecker = new ServiceBusChecker(serviceBusConnectionString, 
            _logger as ILogger<ServiceBusChecker>);
        report.AzureServicesResults.Add(await serviceBusChecker.CheckAsync(cancellationToken));

        // Check Redis
        var redisConnectionString = _configuration.GetConnectionString("redis");
        var redisChecker = new RedisChecker(redisConnectionString, 
            _logger as ILogger<RedisChecker>);
        report.AzureServicesResults.Add(await redisChecker.CheckAsync(cancellationToken));
    }

    private async Task CheckDataPipelinePerformanceAsync(ProductionReadinessReport report, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking data pipeline performance...");

        // Check database connectivity and performance
        var databases = new[] { "catalogdb", "orderingdb", "identitydb", "webhooksdb" };
        
        foreach (var dbName in databases)
        {
            var connectionString = _configuration.GetConnectionString(dbName);
            var postgresChecker = new PostgresChecker(connectionString, dbName, 
                _logger as ILogger<PostgresChecker>);
            report.DataPipelineResults.Add(await postgresChecker.CheckAsync(cancellationToken));
        }

        // Add pipeline performance assessment result
        var pipelineResult = new AssessmentResult
        {
            CheckName = "Data Pipeline Performance Assessment",
            Status = AssessmentStatus.Success,
            Message = "Data pipeline performance metrics collected successfully"
        };

        // Calculate aggregate metrics
        var successfulDbs = report.DataPipelineResults.Count(r => r.Status == AssessmentStatus.Success);
        var totalDbs = databases.Length;
        var responseTimes = report.DataPipelineResults
            .Where(r => r.ResponseTime.HasValue)
            .Select(r => r.ResponseTime!.Value.TotalMilliseconds)
            .ToList();
        var avgResponseTime = responseTimes.Any() ? responseTimes.Average() : 0;

        pipelineResult.Metrics["DatabasesChecked"] = totalDbs;
        pipelineResult.Metrics["SuccessfulConnections"] = successfulDbs;
        pipelineResult.Metrics["AverageResponseTimeMs"] = Math.Round(avgResponseTime, 2);
        pipelineResult.Metrics["SuccessRate"] = Math.Round((double)successfulDbs / totalDbs * 100, 2);

        if (successfulDbs < totalDbs)
        {
            pipelineResult.Status = AssessmentStatus.Warning;
            pipelineResult.Message = $"Only {successfulDbs} out of {totalDbs} databases are accessible";
        }

        report.DataPipelineResults.Add(pipelineResult);
    }

    private void CheckConfiguration(ProductionReadinessReport report)
    {
        _logger.LogInformation("Checking configuration...");

        var configChecks = new[]
        {
            ("AppInsights", _configuration.GetConnectionString("AppInsights"), "Azure Application Insights monitoring"),
            ("EventBus", _configuration.GetConnectionString("EventBus"), "Azure Service Bus event messaging"),
            ("redis", _configuration.GetConnectionString("redis"), "Redis cache"),
            ("catalogdb", _configuration.GetConnectionString("catalogdb"), "Catalog database"),
            ("orderingdb", _configuration.GetConnectionString("orderingdb"), "Ordering database")
        };

        foreach (var (key, value, description) in configChecks)
        {
            var result = new AssessmentResult
            {
                CheckName = $"Configuration: {key}",
                Metrics = new Dictionary<string, object>
                {
                    { "Key", key },
                    { "Description", description }
                }
            };

            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Status = AssessmentStatus.Success;
                result.Message = $"{description} is configured";
                result.Metrics["Configured"] = true;
            }
            else
            {
                result.Status = AssessmentStatus.Warning;
                result.Message = $"{description} is not configured";
                result.Metrics["Configured"] = false;
            }

            report.ConfigurationResults.Add(result);
        }
    }

    private bool DetermineProductionReadiness(ProductionReadinessReport report)
    {
        // Production ready if:
        // 1. No critical failures in Azure services
        // 2. At least 75% of data pipeline checks pass
        // 3. Critical configurations are present

        var criticalAzureFailures = report.AzureServicesResults
            .Count(r => r.Status == AssessmentStatus.Failure && 
                       (r.CheckName.Contains("Service Bus") || r.CheckName.Contains("Database")));

        var pipelineSuccessRate = report.DataPipelineResults.Any() 
            ? (double)report.DataPipelineResults.Count(r => r.Status == AssessmentStatus.Success) / report.DataPipelineResults.Count
            : 0;

        var criticalConfigsMissing = report.ConfigurationResults
            .Count(r => r.Status == AssessmentStatus.Failure);

        return criticalAzureFailures == 0 && 
               pipelineSuccessRate >= 0.75 && 
               criticalConfigsMissing == 0;
    }

    private void GenerateRecommendations(ProductionReadinessReport report)
    {
        if (report.FailedChecks > 0)
        {
            report.Recommendations["critical"] = $"Address {report.FailedChecks} failed check(s) before deploying to production";
        }

        if (report.WarningChecks > 0)
        {
            report.Recommendations["warning"] = $"Review {report.WarningChecks} warning(s) for optimal production configuration";
        }

        var unconfiguredServices = report.AzureServicesResults
            .Where(r => r.Status == AssessmentStatus.Warning && r.Message.Contains("not configured"))
            .ToList();

        if (unconfiguredServices.Any())
        {
            report.Recommendations["azure_services"] = 
                $"Configure Azure services: {string.Join(", ", unconfiguredServices.Select(s => s.CheckName))}";
        }

        var slowDatabases = report.DataPipelineResults
            .Where(r => r.ResponseTime.HasValue && r.ResponseTime.Value.TotalMilliseconds > 1000)
            .ToList();

        if (slowDatabases.Any())
        {
            report.Recommendations["performance"] = 
                $"Investigate slow database responses (>1s): {string.Join(", ", slowDatabases.Select(d => d.CheckName))}";
        }

        if (report.IsProductionReady)
        {
            report.Recommendations["status"] = "✓ All critical systems are operational and ready for production deployment";
        }
        else
        {
            report.Recommendations["status"] = "✗ System is NOT ready for production. Address critical issues before deployment";
        }
    }
}
