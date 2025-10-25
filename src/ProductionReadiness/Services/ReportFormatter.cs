using ProductionReadiness.Models;
using System.Text;

namespace ProductionReadiness.Services;

/// <summary>
/// Formats production readiness reports for display
/// </summary>
public static class ReportFormatter
{
    public static string FormatReport(ProductionReadinessReport report)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("          PRODUCTION READINESS ASSESSMENT REPORT");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Generated At: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine($"Environment:  {report.Environment}");
        sb.AppendLine($"Status:       {(report.IsProductionReady ? "✓ READY FOR PRODUCTION" : "✗ NOT READY FOR PRODUCTION")}");
        sb.AppendLine();
        sb.AppendLine($"Summary:      {report.SuccessfulChecks}/{report.TotalChecks} checks passed");
        sb.AppendLine($"              {report.FailedChecks} failed, {report.WarningChecks} warnings");
        sb.AppendLine();

        // Azure Services Section
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("AZURE WEB SERVICES ACCESSIBILITY");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        FormatResultSection(sb, report.AzureServicesResults);
        sb.AppendLine();

        // Data Pipeline Section
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("DATA PIPELINE PERFORMANCE");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        FormatResultSection(sb, report.DataPipelineResults);
        sb.AppendLine();

        // Configuration Section
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("CONFIGURATION VALIDATION");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        FormatResultSection(sb, report.ConfigurationResults);
        sb.AppendLine();

        // Recommendations Section
        if (report.Recommendations.Any())
        {
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("RECOMMENDATIONS");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            foreach (var rec in report.Recommendations.OrderBy(r => r.Key))
            {
                sb.AppendLine($"• {rec.Value}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        
        return sb.ToString();
    }

    private static void FormatResultSection(StringBuilder sb, List<AssessmentResult> results)
    {
        foreach (var result in results)
        {
            var statusIcon = result.Status switch
            {
                AssessmentStatus.Success => "✓",
                AssessmentStatus.Warning => "⚠",
                AssessmentStatus.Failure => "✗",
                _ => "○"
            };

            sb.AppendLine($"{statusIcon} {result.CheckName}");
            sb.AppendLine($"  Status: {result.Status}");
            sb.AppendLine($"  {result.Message}");
            
            if (result.ResponseTime.HasValue)
            {
                sb.AppendLine($"  Response Time: {result.ResponseTime.Value.TotalMilliseconds:F2}ms");
            }

            if (result.Metrics.Any())
            {
                sb.AppendLine("  Metrics:");
                foreach (var metric in result.Metrics.OrderBy(m => m.Key))
                {
                    sb.AppendLine($"    - {metric.Key}: {metric.Value}");
                }
            }
            
            sb.AppendLine();
        }
    }

    public static string FormatJson(ProductionReadinessReport report)
    {
        return System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
