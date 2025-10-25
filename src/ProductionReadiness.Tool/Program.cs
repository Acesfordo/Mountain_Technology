using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductionReadiness.Services;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Setup dependency injection
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

try
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     Production Readiness Assessment Tool - eShop             ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();

    // Create and run the assessment service
    var assessmentService = new ProductionReadinessService(configuration, 
        serviceProvider.GetRequiredService<ILogger<ProductionReadinessService>>());
    
    var report = await assessmentService.RunAssessmentAsync();

    // Display the report
    Console.WriteLine(ReportFormatter.FormatReport(report));

    // Optionally save to file
    var outputFormat = Environment.GetEnvironmentVariable("REPORT_FORMAT") ?? "text";
    var outputFile = Environment.GetEnvironmentVariable("REPORT_OUTPUT_FILE");

    if (!string.IsNullOrWhiteSpace(outputFile))
    {
        var content = outputFormat.ToLower() switch
        {
            "json" => ReportFormatter.FormatJson(report),
            _ => ReportFormatter.FormatReport(report)
        };

        await File.WriteAllTextAsync(outputFile, content);
        Console.WriteLine($"Report saved to: {outputFile}");
    }

    // Exit with appropriate code
    Environment.ExitCode = report.IsProductionReady ? 0 : 1;
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error during production readiness assessment");
    Console.WriteLine($"ERROR: {ex.Message}");
    Environment.ExitCode = 2;
}
