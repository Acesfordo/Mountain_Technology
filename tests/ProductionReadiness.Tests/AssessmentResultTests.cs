using ProductionReadiness.Models;

namespace ProductionReadiness.Tests;

public class AssessmentResultTests
{
    [Fact]
    public void AssessmentResult_CreatesWithDefaultValues()
    {
        // Arrange & Act
        var result = new AssessmentResult();

        // Assert
        Assert.Equal(string.Empty, result.CheckName);
        Assert.Equal(AssessmentStatus.Success, result.Status);
        Assert.Equal(string.Empty, result.Message);
        Assert.NotNull(result.Metrics);
        Assert.Empty(result.Metrics);
        Assert.True((DateTime.UtcNow - result.Timestamp).TotalSeconds < 1);
    }

    [Fact]
    public void AssessmentResult_CanSetProperties()
    {
        // Arrange
        var result = new AssessmentResult();
        var timestamp = DateTime.UtcNow.AddHours(-1);
        var responseTime = TimeSpan.FromMilliseconds(250);

        // Act
        result.CheckName = "Test Check";
        result.Status = AssessmentStatus.Warning;
        result.Message = "Test message";
        result.Timestamp = timestamp;
        result.ResponseTime = responseTime;
        result.Metrics["TestMetric"] = 123;

        // Assert
        Assert.Equal("Test Check", result.CheckName);
        Assert.Equal(AssessmentStatus.Warning, result.Status);
        Assert.Equal("Test message", result.Message);
        Assert.Equal(timestamp, result.Timestamp);
        Assert.Equal(responseTime, result.ResponseTime);
        Assert.Single(result.Metrics);
        Assert.Equal(123, result.Metrics["TestMetric"]);
    }

    [Theory]
    [InlineData(AssessmentStatus.Success)]
    [InlineData(AssessmentStatus.Warning)]
    [InlineData(AssessmentStatus.Failure)]
    [InlineData(AssessmentStatus.NotApplicable)]
    public void AssessmentResult_SupportsAllStatusTypes(AssessmentStatus status)
    {
        // Arrange & Act
        var result = new AssessmentResult { Status = status };

        // Assert
        Assert.Equal(status, result.Status);
    }
}
