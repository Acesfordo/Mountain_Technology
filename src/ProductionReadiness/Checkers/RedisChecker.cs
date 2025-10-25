using Microsoft.Extensions.Logging;
using ProductionReadiness.Models;
using StackExchange.Redis;
using System.Diagnostics;

namespace ProductionReadiness.Checkers;

/// <summary>
/// Checks Redis cache connectivity and performance
/// </summary>
public class RedisChecker : IProductionReadinessChecker
{
    private readonly string? _connectionString;
    private readonly ILogger<RedisChecker>? _logger;

    public RedisChecker(string? connectionString, ILogger<RedisChecker>? logger = null)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<AssessmentResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var result = new AssessmentResult
        {
            CheckName = "Redis Cache Connectivity"
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                result.Status = AssessmentStatus.Warning;
                result.Message = "Redis connection string not configured.";
                result.Metrics["Configured"] = false;
                return result;
            }

            var redis = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            var db = redis.GetDatabase();
            
            // Perform a simple ping to test connectivity
            var pingResult = await db.PingAsync();
            
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            
            var endpoints = redis.GetEndPoints();
            if (endpoints.Length > 0)
            {
                var server = redis.GetServer(endpoints.First());
                var info = server.Info();
                
                // Memory info extraction is optional - skipping due to nullable complexity
            }
            
            result.Status = AssessmentStatus.Success;
            result.Message = $"Successfully connected to Redis. Ping latency: {pingResult.TotalMilliseconds:F2}ms";
            result.Metrics["Configured"] = true;
            result.Metrics["Accessible"] = true;
            result.Metrics["PingLatencyMs"] = pingResult.TotalMilliseconds;
            result.Metrics["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds;
            result.Metrics["Connected"] = redis.IsConnected;
            
            _logger?.LogInformation("Redis connectivity check passed. Latency: {Latency}ms", pingResult.TotalMilliseconds);
            
            await redis.CloseAsync();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = AssessmentStatus.Failure;
            result.Message = $"Redis connectivity check failed: {ex.Message}";
            result.Metrics["Configured"] = !string.IsNullOrWhiteSpace(_connectionString);
            result.Metrics["Accessible"] = false;
            _logger?.LogError(ex, "Redis connectivity check failed");
        }

        return result;
    }
}
