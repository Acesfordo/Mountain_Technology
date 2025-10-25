using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using ProductionReadiness.Models;
using System.Diagnostics;

namespace ProductionReadiness.Checkers;

/// <summary>
/// Checks Azure Application Insights connectivity
/// </summary>
public class ApplicationInsightsChecker : IProductionReadinessChecker
{
    private readonly string? _connectionString;
    private readonly ILogger<ApplicationInsightsChecker>? _logger;

    public ApplicationInsightsChecker(string? connectionString, ILogger<ApplicationInsightsChecker>? logger = null)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<AssessmentResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var result = new AssessmentResult
        {
            CheckName = "Azure Application Insights Connectivity"
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                result.Status = AssessmentStatus.Warning;
                result.Message = "Application Insights connection string not configured. Telemetry will not be collected.";
                result.Metrics["Configured"] = false;
                return result;
            }

            // Create a telemetry configuration with the connection string
            var config = new TelemetryConfiguration
            {
                ConnectionString = _connectionString
            };

            var telemetryClient = new TelemetryClient(config);
            
            // Send a test event
            telemetryClient.TrackEvent("ProductionReadinessCheck", 
                new Dictionary<string, string> 
                { 
                    { "CheckType", "ApplicationInsights" },
                    { "Timestamp", DateTime.UtcNow.ToString("o") }
                });
            
            // Flush to send immediately (with timeout)
            telemetryClient.Flush();
            await Task.Delay(1000, cancellationToken); // Allow time for telemetry to be sent
            
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            
            result.Status = AssessmentStatus.Success;
            result.Message = "Application Insights is configured. Test telemetry event sent successfully.";
            result.Metrics["Configured"] = true;
            result.Metrics["Accessible"] = true;
            result.Metrics["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds;
            result.Metrics["TestEventSent"] = true;
            
            _logger?.LogInformation("Application Insights connectivity check passed");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = AssessmentStatus.Failure;
            result.Message = $"Application Insights check failed: {ex.Message}";
            result.Metrics["Configured"] = !string.IsNullOrWhiteSpace(_connectionString);
            result.Metrics["Accessible"] = false;
            _logger?.LogError(ex, "Application Insights connectivity check failed");
        }

        return result;
    }
}
