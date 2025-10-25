namespace ProductionReadiness.Models;

/// <summary>
/// Comprehensive production readiness report
/// </summary>
public class ProductionReadinessReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string Environment { get; set; } = string.Empty;
    public bool IsProductionReady { get; set; }
    public List<AssessmentResult> AzureServicesResults { get; set; } = new();
    public List<AssessmentResult> DataPipelineResults { get; set; } = new();
    public List<AssessmentResult> ConfigurationResults { get; set; } = new();
    public Dictionary<string, string> Recommendations { get; set; } = new();
    
    public int TotalChecks => 
        AzureServicesResults.Count + 
        DataPipelineResults.Count + 
        ConfigurationResults.Count;
    
    public int SuccessfulChecks =>
        AzureServicesResults.Count(r => r.Status == AssessmentStatus.Success) +
        DataPipelineResults.Count(r => r.Status == AssessmentStatus.Success) +
        ConfigurationResults.Count(r => r.Status == AssessmentStatus.Success);
    
    public int FailedChecks =>
        AzureServicesResults.Count(r => r.Status == AssessmentStatus.Failure) +
        DataPipelineResults.Count(r => r.Status == AssessmentStatus.Failure) +
        ConfigurationResults.Count(r => r.Status == AssessmentStatus.Failure);
    
    public int WarningChecks =>
        AzureServicesResults.Count(r => r.Status == AssessmentStatus.Warning) +
        DataPipelineResults.Count(r => r.Status == AssessmentStatus.Warning) +
        ConfigurationResults.Count(r => r.Status == AssessmentStatus.Warning);
}
