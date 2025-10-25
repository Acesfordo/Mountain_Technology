using ProductionReadiness.Models;

namespace ProductionReadiness.Tests;

public class ProductionReadinessReportTests
{
    [Fact]
    public void ProductionReadinessReport_CreatesWithDefaultValues()
    {
        // Arrange & Act
        var report = new ProductionReadinessReport();

        // Assert
        Assert.True((DateTime.UtcNow - report.GeneratedAt).TotalSeconds < 1);
        Assert.Equal(string.Empty, report.Environment);
        Assert.False(report.IsProductionReady);
        Assert.NotNull(report.AzureServicesResults);
        Assert.Empty(report.AzureServicesResults);
        Assert.NotNull(report.DataPipelineResults);
        Assert.Empty(report.DataPipelineResults);
        Assert.NotNull(report.ConfigurationResults);
        Assert.Empty(report.ConfigurationResults);
        Assert.NotNull(report.Recommendations);
        Assert.Empty(report.Recommendations);
    }

    [Fact]
    public void ProductionReadinessReport_TotalChecks_ReturnsCorrectSum()
    {
        // Arrange
        var report = new ProductionReadinessReport();
        report.AzureServicesResults.Add(new AssessmentResult { CheckName = "Azure1" });
        report.AzureServicesResults.Add(new AssessmentResult { CheckName = "Azure2" });
        report.DataPipelineResults.Add(new AssessmentResult { CheckName = "DB1" });
        report.ConfigurationResults.Add(new AssessmentResult { CheckName = "Config1" });

        // Act
        var totalChecks = report.TotalChecks;

        // Assert
        Assert.Equal(4, totalChecks);
    }

    [Fact]
    public void ProductionReadinessReport_SuccessfulChecks_CountsCorrectly()
    {
        // Arrange
        var report = new ProductionReadinessReport();
        report.AzureServicesResults.Add(new AssessmentResult { Status = AssessmentStatus.Success });
        report.AzureServicesResults.Add(new AssessmentResult { Status = AssessmentStatus.Warning });
        report.DataPipelineResults.Add(new AssessmentResult { Status = AssessmentStatus.Success });
        report.ConfigurationResults.Add(new AssessmentResult { Status = AssessmentStatus.Failure });

        // Act
        var successfulChecks = report.SuccessfulChecks;

        // Assert
        Assert.Equal(2, successfulChecks);
    }

    [Fact]
    public void ProductionReadinessReport_FailedChecks_CountsCorrectly()
    {
        // Arrange
        var report = new ProductionReadinessReport();
        report.AzureServicesResults.Add(new AssessmentResult { Status = AssessmentStatus.Success });
        report.AzureServicesResults.Add(new AssessmentResult { Status = AssessmentStatus.Failure });
        report.DataPipelineResults.Add(new AssessmentResult { Status = AssessmentStatus.Failure });
        report.ConfigurationResults.Add(new AssessmentResult { Status = AssessmentStatus.Warning });

        // Act
        var failedChecks = report.FailedChecks;

        // Assert
        Assert.Equal(2, failedChecks);
    }

    [Fact]
    public void ProductionReadinessReport_WarningChecks_CountsCorrectly()
    {
        // Arrange
        var report = new ProductionReadinessReport();
        report.AzureServicesResults.Add(new AssessmentResult { Status = AssessmentStatus.Warning });
        report.AzureServicesResults.Add(new AssessmentResult { Status = AssessmentStatus.Warning });
        report.DataPipelineResults.Add(new AssessmentResult { Status = AssessmentStatus.Success });
        report.ConfigurationResults.Add(new AssessmentResult { Status = AssessmentStatus.Warning });

        // Act
        var warningChecks = report.WarningChecks;

        // Assert
        Assert.Equal(3, warningChecks);
    }

    [Fact]
    public void ProductionReadinessReport_CanAddRecommendations()
    {
        // Arrange
        var report = new ProductionReadinessReport();

        // Act
        report.Recommendations["critical"] = "Fix critical issues";
        report.Recommendations["warning"] = "Review warnings";

        // Assert
        Assert.Equal(2, report.Recommendations.Count);
        Assert.Equal("Fix critical issues", report.Recommendations["critical"]);
        Assert.Equal("Review warnings", report.Recommendations["warning"]);
    }
}
