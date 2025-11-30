using WeatherLib.service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Invocation;
using PicturesLib.service;
using System.Threading;


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("PicturesConsole/appsettings.json", optional: false, reloadOnChange: true)   //must run from solution folder 
    .Build();

string? dataFolder = configuration["WeatherData:Folder"];
if (dataFolder == null)
{
    throw new Exception("Could not read data folder, is WeatherData:Folder set in appsettings.json?");
}

string? picturesFolder = configuration["PicturesData:Folder"];
if (picturesFolder == null)
{
    throw new Exception("Could not read pictures folder, is PicturesData:Folder set in appsettings.json?");
}

// Setup Dependency injection to enable future testability  
var serviceProvider = new ServiceCollection()
    .AddSingleton<IWeatherService, WeatherService>()
    .AddSingleton<IResidioReportService, ResidioReportService>()
    .BuildServiceProvider();

var reportOptions = new Option<string?>(new[] { "--file", "-f" }, "Output file path to save the weather report JSON");
var rootCommand = new RootCommand("Weather report console application");
rootCommand.AddOption(reportOptions);

// Default handler: generate weather report once and exit
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

// Command to run the thumbnail background service and keep the app running
var heightOption = new Option<int>(new[] {"--height", "-h"}, () => 290, "Thumbnail height in pixels");
var watchCommand = new Command("thumbnails", "Run the thumbnail processor as a background service");
watchCommand.AddOption(heightOption);
watchCommand.SetHandler(async (int height) =>
{
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            // Use the factory method to create a thumbnail-specific observer
            services.AddSingleton(sp => ThumbnailProcessorFactory.CreateThumbnailProcessor(new DirectoryInfo(picturesFolder), height));
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<FileObserverService>());
        })
        .Build();

    Console.WriteLine($"Starting thumbnail processor on '{picturesFolder}' with height={height}. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, heightOption);

rootCommand.AddCommand(watchCommand);

return await rootCommand.InvokeAsync(args);
