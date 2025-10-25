using Microsoft.Extensions.Logging;
using Npgsql;
using ProductionReadiness.Models;
using System.Diagnostics;

namespace ProductionReadiness.Checkers;

/// <summary>
/// Checks PostgreSQL database connectivity and performance
/// </summary>
public class PostgresChecker : IProductionReadinessChecker
{
    private readonly string? _connectionString;
    private readonly string _databaseName;
    private readonly ILogger<PostgresChecker>? _logger;

    public PostgresChecker(string? connectionString, string databaseName = "postgres", ILogger<PostgresChecker>? logger = null)
    {
        _connectionString = connectionString;
        _databaseName = databaseName;
        _logger = logger;
    }

    public async Task<AssessmentResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var result = new AssessmentResult
        {
            CheckName = $"PostgreSQL Database Connectivity ({_databaseName})"
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                result.Status = AssessmentStatus.Warning;
                result.Message = "PostgreSQL connection string not configured.";
                result.Metrics["Configured"] = false;
                return result;
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Execute a simple query to test connectivity and performance
            await using var command = new NpgsqlCommand("SELECT version(), current_database(), pg_database_size(current_database())", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            string? version = null;
            string? database = null;
            long? databaseSize = null;
            
            if (await reader.ReadAsync(cancellationToken))
            {
                version = reader.GetString(0);
                database = reader.GetString(1);
                databaseSize = reader.GetInt64(2);
            }
            
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            
            result.Status = AssessmentStatus.Success;
            result.Message = $"Successfully connected to PostgreSQL database: {database}";
            result.Metrics["Configured"] = true;
            result.Metrics["Accessible"] = true;
            result.Metrics["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds;
            result.Metrics["Database"] = database ?? "unknown";
            result.Metrics["Version"] = version ?? "unknown";
            result.Metrics["DatabaseSizeBytes"] = databaseSize ?? 0;
            result.Metrics["DatabaseSizeMB"] = databaseSize.HasValue ? Math.Round(databaseSize.Value / (1024.0 * 1024.0), 2) : 0;
            
            _logger?.LogInformation("PostgreSQL connectivity check passed for database: {Database}", database);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = AssessmentStatus.Failure;
            result.Message = $"PostgreSQL connectivity check failed: {ex.Message}";
            result.Metrics["Configured"] = !string.IsNullOrWhiteSpace(_connectionString);
            result.Metrics["Accessible"] = false;
            _logger?.LogError(ex, "PostgreSQL connectivity check failed for {DatabaseName}", _databaseName);
        }

        return result;
    }
}
