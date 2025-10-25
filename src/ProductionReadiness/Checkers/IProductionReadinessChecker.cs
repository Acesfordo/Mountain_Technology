using ProductionReadiness.Models;

namespace ProductionReadiness.Checkers;

/// <summary>
/// Interface for implementing various production readiness checks
/// </summary>
public interface IProductionReadinessChecker
{
    Task<AssessmentResult> CheckAsync(CancellationToken cancellationToken = default);
}
