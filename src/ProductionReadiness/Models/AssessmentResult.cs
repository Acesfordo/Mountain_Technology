namespace ProductionReadiness.Models;

/// <summary>
/// Represents the result of a production readiness assessment check
/// </summary>
public class AssessmentResult
{
    public string CheckName { get; set; } = string.Empty;
    public AssessmentStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Metrics { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan? ResponseTime { get; set; }
}

public enum AssessmentStatus
{
    Success,
    Warning,
    Failure,
    NotApplicable
}
