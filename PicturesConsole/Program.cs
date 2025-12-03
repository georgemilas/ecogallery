using WeatherLib.service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.CommandLine;
using System.CommandLine.Invocation;
using PicturesLib.service;
using PicturesLib.model;
using System.Threading;
using PicturesLib.model.configuration;
using PicturesLib.service.thumbnail;


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("PicturesConsole/appsettings.json", optional: false, reloadOnChange: true)   //must run from solution folder 
    .Build();

// Setup Dependency injection to enable future testability  
var serviceProvider = new ServiceCollection()
    .Configure<PicturesDataConfiguration>(configuration.GetSection(PicturesDataConfiguration.SectionName))
//    .AddSingleton<IWeatherService, WeatherService>()
//    .AddSingleton<IResidioReportService, ResidioReportService>()
    .BuildServiceProvider();

var picturesConfig = serviceProvider.GetRequiredService<IOptions<PicturesDataConfiguration>>().Value;
if (string.IsNullOrWhiteSpace(picturesConfig.Folder))
{
    throw new InvalidOperationException("PicturesData:Folder is required in appsettings.json");
}

var rootCommand = new RootCommand("Pictures background services console application");

// Command to run the thumbnail background service and keep the app running
var watchCommand = new Command("thumbnails", "Run the thumbnail processor as a background service");
var watchFolderOption = new Option<string>(new[] {"--folder", "-f"}, () => picturesConfig.Folder, "Pictures folder path");
var watchHeightOption = new Option<int>(new[] {"--height", "-h"}, () => 290, "Thumbnail height in pixels");
watchCommand.AddOption(watchFolderOption);
watchCommand.AddOption(watchHeightOption);
watchCommand.SetHandler(async (string folder, int height) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            // Use the factory method to create a thumbnail-specific observer
            services.AddSingleton<IHostedService>(sp => ThumbnailProcessor.CreateProcessor(picturesConfig, height));
        })
        .Build();

    Console.WriteLine($"Starting thumbnail processor on '{picturesConfig.Folder}' with height={height}. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, watchFolderOption, watchHeightOption);
rootCommand.AddCommand(watchCommand);


// Command to run the thumbnail cleanup background service and keep the app running
var cleanupCommand = new Command("cleanup", "Run the thumbnail cleanup as a background service");
var cleanupFolderOption = new Option<string>(new[] {"--folder", "-f"}, () => picturesConfig.Folder, "Pictures folder path");
var cleanupHeightOption = new Option<int>(new[] {"--height", "-h"}, () => 290, "Thumbnail height in pixels");
cleanupCommand.AddOption(cleanupFolderOption);
cleanupCommand.AddOption(cleanupHeightOption);
cleanupCommand.SetHandler(async (string folder, int height) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(sp => ThumbnailCleanup.CreateProcessor(picturesConfig, height));
        })
        .Build();

    Console.WriteLine($"Starting thumbnail cleanup processor for pictures on '{picturesConfig.Folder}' with height={height}. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, cleanupFolderOption, cleanupHeightOption);
rootCommand.AddCommand(cleanupCommand);





return await rootCommand.InvokeAsync(args);
