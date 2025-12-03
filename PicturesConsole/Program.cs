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

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the thumbnail background service and keep the app running
var thumbCommand = new Command("thumbnails", "Run the thumbnail building processor as a background service");
var thumbFolderOption = new Option<string>(new[] {"--folder", "-f"}, () => picturesConfig.Folder, "Pictures folder path");
var thumbHeightOption = new Option<int>(new[] {"--height", "-h"}, () => 290, "Thumbnail height in pixels");
thumbCommand.AddOption(thumbFolderOption);
thumbCommand.AddOption(thumbHeightOption);
thumbCommand.SetHandler(async (string folder, int height) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(sp => ThumbnailProcessor.CreateProcessor(picturesConfig, height));
        })
        .Build();

    Console.WriteLine($"Starting thumbnail processor on '{picturesConfig.Folder}' with height={height}. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, thumbFolderOption, thumbHeightOption);
rootCommand.AddCommand(thumbCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the thumbnail cleanup background service and keep the app running
var cleanupCommand = new Command("cleanup", "Run the thumbnail cleanup processor as a background service");
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


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the album building background service and keep the app running
var albumCommand = new Command("album", "Run the album building processor as a background service");
var albumFolderOption = new Option<string>(new[] {"--folder", "-f"}, () => picturesConfig.Folder, "Pictures folder path");
albumCommand.AddOption(albumFolderOption);
albumCommand.SetHandler(async (string folder) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(sp => AlbumProcessor.CreateProcessor(picturesConfig));
        })
        .Build();

    Console.WriteLine($"Starting album buiding processor on '{picturesConfig.Folder}'. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, albumFolderOption);
rootCommand.AddCommand(albumCommand);



return await rootCommand.InvokeAsync(args);
