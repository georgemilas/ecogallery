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
using PicturesLib.service.album;


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("PicturesConsole/appsettings.json", optional: false, reloadOnChange: true)   //must run from solution folder 
    .Build();

// Setup Dependency injection to enable future testability  
var serviceProvider = new ServiceCollection()
    .Configure<PicturesDataConfiguration>(configuration.GetSection(PicturesDataConfiguration.SectionName))
    .BuildServiceProvider();

var picturesConfig = serviceProvider.GetRequiredService<IOptions<PicturesDataConfiguration>>().Value;
if (string.IsNullOrWhiteSpace(picturesConfig.Folder))
{
    throw new InvalidOperationException("PicturesData:Folder is required in appsettings.json");
}

var rootCommand = new RootCommand("Pictures background services console application");
var folderOption = new Option<string>(new[] {"--folder", "-f"}, () => picturesConfig.Folder, "Pictures folder path");
var heightOption = new Option<int>(new[] {"--height", "-h"}, () => 290, "Thumbnail height in pixels");
var nonParallelOption = new Option<bool>(new[] {"--nonparallel", "-np"}, "Run thumbnail processing in non-parallel mode");
var parallelDegreeOption = new Option<int>(new[] {"--parallel", "-p"}, () => Environment.ProcessorCount, "Degree of parallelism for thumbnail processing");

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the thumbnail background service and keep the app running
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var thumbCommand = new Command("thumbnails", "Run the thumbnail building processor as a background service");
thumbCommand.AddOption(folderOption);
thumbCommand.AddOption(heightOption);
thumbCommand.AddOption(nonParallelOption);
thumbCommand.AddOption(parallelDegreeOption);
thumbCommand.SetHandler(async (string folder, int height, bool nonParallel, int parallelDegree) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            if (nonParallel)
            {
                services.AddSingleton<IHostedService>(sp => ThumbnailProcessor.CreateProcessorNotParallel(picturesConfig, height));
            }
            else
            {
                services.AddSingleton<IHostedService>(sp => ThumbnailProcessor.CreateProcessor(picturesConfig, height, parallelDegree));
            }
        })
        .Build();

    Console.WriteLine($"Starting thumbnail processor on '{picturesConfig.Folder}' with height={height}. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, heightOption, nonParallelOption, parallelDegreeOption);
rootCommand.AddCommand(thumbCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the thumbnail cleanup background service and keep the app running
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var cleanupCommand = new Command("cleanup", "Run the thumbnail cleanup processor as a background service");
cleanupCommand.AddOption(folderOption);
cleanupCommand.AddOption(heightOption);
cleanupCommand.AddOption(nonParallelOption);
cleanupCommand.AddOption(parallelDegreeOption);
cleanupCommand.SetHandler(async (string folder, int height, bool nonParallel, int parallelDegree) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            if (nonParallel)
            {
                services.AddSingleton<IHostedService>(sp => ThumbnailCleanup.CreateProcessorNotParallel(picturesConfig, height));
            }
            else
            {
                services.AddSingleton<IHostedService>(sp => ThumbnailCleanup.CreateProcessor(picturesConfig, height, parallelDegree));
            }
            
        })
        .Build();

    Console.WriteLine($"Starting thumbnail cleanup processor for pictures on '{picturesConfig.Folder}' with height={height}. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, heightOption, nonParallelOption, parallelDegreeOption);
rootCommand.AddCommand(cleanupCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the album building background service and keep the app running
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var albumCommand = new Command("album", "Run the album building processor as a background service");
albumCommand.AddOption(folderOption);
albumCommand.AddOption(nonParallelOption);
albumCommand.AddOption(parallelDegreeOption);
albumCommand.SetHandler(async (string folder, bool nonParallel, int parallelDegree) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            if (nonParallel)
            {
                services.AddSingleton<IHostedService>(sp => AlbumProcessor.CreateProcessorNotParallel(picturesConfig));
            }
            else
            {
                services.AddSingleton<IHostedService>(sp => AlbumProcessor.CreateProcessor(picturesConfig, parallelDegree));
            }            
        })
        .Build();

    Console.WriteLine($"Starting album buiding processor on '{picturesConfig.Folder}'. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, nonParallelOption, parallelDegreeOption);
rootCommand.AddCommand(albumCommand);



return await rootCommand.InvokeAsync(args);
