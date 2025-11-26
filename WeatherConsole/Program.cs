using WeatherLib.service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("WeatherConsole/appsettings.json", optional: false, reloadOnChange: true)   //must run from solution folder 
    .Build();

string? dataFolder = configuration["WeatherData:Folder"];
if (dataFolder == null)
{
    throw new Exception("Could not read data folder, is WeatherData:Folder set in appsettings.json?");
}

// Setup Dependency injection to enable future testability  
var serviceProvider = new ServiceCollection()
    .AddSingleton<IWeatherService, WeatherService>()
    .AddSingleton<IResidioReportService, ResidioReportService>()
    .BuildServiceProvider();

var reportOptions = new Option<string?>(new[] { "--file", "-f" }, "Output file path to save the weather report JSON");
var rootCommand = new RootCommand("Weather report console application");
rootCommand.AddOption(reportOptions);

rootCommand.SetHandler((string? filePath) =>
{
    var service = serviceProvider.GetRequiredService<IResidioReportService>();
    var report = service.GetWeatherReport(dataFolder);
    var json = service.SerializeWeatherReport(report);

    if (!string.IsNullOrWhiteSpace(filePath))
    {
        File.WriteAllText(filePath, json);
        Console.WriteLine($"Weather report saved to: {filePath}");
    }
    else
    {
        Console.WriteLine(json);
    }
}, reportOptions);

return await rootCommand.InvokeAsync(args);

