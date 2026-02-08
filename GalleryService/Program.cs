using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.CommandLine;
using System.CommandLine.Invocation;
using GalleryLib.service;
using GalleryLib.model;
using System.Threading;
using GalleryLib.model.configuration;
using GalleryLib.service.thumbnail;
using GalleryLib.service.album;
using GalleryLib.service.fileProcessor;
using GalleryLib.service.database;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var basePath = AppContext.BaseDirectory;

Console.WriteLine($"Running |{environment}| workload from {basePath} ");

var configuration = new ConfigurationBuilder()
    //.SetBasePath(Directory.GetCurrentDirectory())  // use current directory when running in development
    .SetBasePath(basePath)   // use assembly base path when running as published app
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

// Setup Dependency injection to enable future testability  
var serviceProvider = new ServiceCollection()
    .Configure<PicturesDataConfiguration>(configuration.GetSection(PicturesDataConfiguration.SectionName))
    .Configure<DatabaseConfiguration>(configuration.GetSection(DatabaseConfiguration.SectionName))
    .BuildServiceProvider();

var picturesConfig = serviceProvider.GetRequiredService<IOptions<PicturesDataConfiguration>>().Value;
picturesConfig.ApplyEnvironmentOverrides();
var dbConfig = serviceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
// Console.WriteLine($"appsettings.json exists: {File.Exists(Path.Combine(basePath, "appsettings.json"))}");
// Console.WriteLine($"appsettings.{environment}.json exists: {File.Exists(Path.Combine(basePath, $"appsettings.{environment}.json"))}");
// Console.WriteLine($"Connection String: '{dbConfig.ToConnectionString()}'");
// Console.WriteLine("Press Enter to continue...");
// Console.ReadLine(); // Press Enter to continue...

var rootCommand = new RootCommand("Pictures background services console application");
var folderOption = new Option<string>(new[] {"--folder", "-f"}, () => picturesConfig.Folder, "Pictures folder path");
var yamlFileOption = new Option<string?>(new[] {"--yaml", "-y"}, () => null, "YAML file path");
var heightsOption = new Option<int[]>(new[] {"--height", "-h"}, () => new[] { 400, 1440 }, "Thumbnail heights in pixels (can specify multiple)")  //{ 400, 1080, 1440 }
{
    AllowMultipleArgumentsPerToken = true
};
var databaseNameOption = new Option<string>(new[] {"--database", "-d"}, () => dbConfig.Database, "Database name to create");
var parallelDegreeOption = new Option<int>(new[] {"--parallel", "-p"}, () => Environment.ProcessorCount, "Degree of parallelism for thumbnail processing");
var isPlanOption = new Option<string>(new[] {"--plan", "-pl"}, () => "yes", "Run the process in plan mode only");
var passwordOption = new Option<string>(new[] {"--password", "-pw"}, () => "admin123", "Admin user password");
var logIfProcessed = new Option<bool>(new[] {"--log", "-l"}, () => false, "Log details to output");
var reprocessMetadataOption = new Option<bool>(new[] {"--reprocess", "-r"}, () => false, "Reprocess metadata");
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the thumbnail background service and keep the app running
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// thumb   400
/// HD      1080
/// UHD     1440
/// 4K      2160
/// 5K      2880 
/// 8K      4320
var thumbCommand = new Command("thumbnails", "Run the thumbnail building processor as a background service");
thumbCommand.AddOption(folderOption);
thumbCommand.AddOption(heightsOption);
thumbCommand.AddOption(parallelDegreeOption);
thumbCommand.SetHandler(async (string folder, int[] heights, int parallelDegree) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(sp => MultipleThumbnailsProcessor.CreateProcessor(picturesConfig, heights, parallelDegree));            
        })
        .Build();

    Console.WriteLine($"Starting thumbnail processor on '{picturesConfig.Folder}' with heights=[{string.Join(", ", heights)}]. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, heightsOption, parallelDegreeOption);
rootCommand.AddCommand(thumbCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the thumbnail cleanup background service and keep the app running
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var cleanupCommand = new Command("cleanup", "Run the cleanup processor (thumbnails and db) as a background service");
cleanupCommand.AddOption(folderOption);
cleanupCommand.AddOption(heightsOption);
cleanupCommand.AddOption(parallelDegreeOption);
cleanupCommand.AddOption(databaseNameOption);
cleanupCommand.AddOption(isPlanOption);
cleanupCommand.AddOption(logIfProcessed);

cleanupCommand.SetHandler(async (string folder, int[] heights, int parallelDegree, string databaseName, string isPlan, bool logIfProcessed) =>
{
    picturesConfig.Folder = folder;
    dbConfig.Database = databaseName;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            if (heights.Length == 0)
            {
                heights = new[] { 400, 1440 };
            }
            
            bool planMode = isPlan.Equals("yes", StringComparison.OrdinalIgnoreCase);            
            foreach(var h in heights)
            {
                services.AddSingleton<IHostedService>(sp => ThumbnailCleanupProcessor.CreateProcessor(picturesConfig, h, parallelDegree, planMode, logIfProcessed));
            }
            services.AddSingleton<IHostedService>(sp => DbCleanupProcessor.CreateProcessor(picturesConfig, dbConfig, parallelDegree, planMode, logIfProcessed));
            
            Console.WriteLine("Configured services.");

        })
        .Build();

    Console.WriteLine($"Starting cleanup processor on {dbConfig.Database}/'{picturesConfig.Folder}' with heights=[{string.Join(", ", heights)}]. Press Ctrl+C to stop.");
    // Console.WriteLine("Press Enter to continue...");
    // Console.ReadLine();    
    await host.RunAsync(cts.Token);
}, folderOption, heightsOption, parallelDegreeOption, databaseNameOption, isPlanOption, logIfProcessed);
rootCommand.AddCommand(cleanupCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the album building background service along with image exif extraction functionality 
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var imageExifCommand = new Command("db", "Run the db sync processor as a background service");
imageExifCommand.AddOption(folderOption);
imageExifCommand.AddOption(parallelDegreeOption);
imageExifCommand.AddOption(databaseNameOption);
imageExifCommand.AddOption(reprocessMetadataOption);

imageExifCommand.SetHandler(async (string folder, int parallelDegree, string databaseName, bool reprocess) =>
{
    picturesConfig.Folder = folder;
    dbConfig.Database = databaseName;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };        

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(sp => DbSyncProcessor.CreateProcessor(picturesConfig, dbConfig, parallelDegree, reprocess));                        
        })
        .Build();

    Console.WriteLine($"Starting db sync processor on {dbConfig.Database}/'{picturesConfig.Folder}'. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, parallelDegreeOption, databaseNameOption, reprocessMetadataOption);
rootCommand.AddCommand(imageExifCommand);



//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run load virtual albums from a yaml file  
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var valbumCommand = new Command("valbum", "Load virtual albums from a YAML file");
valbumCommand.AddOption(folderOption);
valbumCommand.AddOption(yamlFileOption);
valbumCommand.AddOption(databaseNameOption);
valbumCommand.SetHandler(async (string folder, string? yamlFilePath, string databaseName) =>
{
    if (string.IsNullOrWhiteSpace(yamlFilePath) || !File.Exists(yamlFilePath))
    {
        Console.WriteLine("YAML file path is required and must exist.");
        return;
    }
    picturesConfig.Folder = folder;
    dbConfig.Database = databaseName;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(sp => new VirtualAlbumLoaderService(
                sp.GetRequiredService<IHostApplicationLifetime>(),
                picturesConfig,
                dbConfig,
                new FileInfo(yamlFilePath!)
            ));                        
        })
        .Build();

    Console.WriteLine($"Starting virtual album loader on {dbConfig.Database}/'{yamlFilePath}'. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, yamlFileOption, databaseNameOption);
rootCommand.AddCommand(valbumCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the db album building + metadata extraction + thumbnail building  
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var syncCommand = new Command("sync", "Run the sync processor (thumbnails and db)  as a background service");
syncCommand.AddOption(folderOption);
syncCommand.AddOption(parallelDegreeOption);
syncCommand.AddOption(databaseNameOption);
syncCommand.AddOption(reprocessMetadataOption);
syncCommand.SetHandler(async (string folder, int parallelDegree, string databaseName, bool reprocess) =>
{
    picturesConfig.Folder = folder;
    dbConfig.Database = databaseName;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            //could add these as 2 separate services.AddSingleton() but because of the possible of console feadback overlap
            //we are combining them into a single processor
            var processors = new List<IFileProcessor> { 
                new DbSyncProcessor(picturesConfig, dbConfig, reprocess), 
                new MultipleThumbnailsProcessor(picturesConfig, new[] { 400, 1440 })  //{ 400, 1080, 1440 }
            };

            services.AddSingleton<IHostedService>(sp => CombinedProcessor.CreateProcessor(processors, picturesConfig, parallelDegree));                        
        })
        .Build();

    Console.WriteLine($"Starting sync processor on {dbConfig.Database}/'{picturesConfig.Folder}'. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, parallelDegreeOption, databaseNameOption, reprocessMetadataOption);
rootCommand.AddCommand(syncCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to create database schema from SQL files
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var createDbCommand = new Command("create-db", "Create database schema from SQL files in GalleryLib/db folder");
createDbCommand.AddOption(databaseNameOption);
createDbCommand.AddOption(passwordOption);
createDbCommand.SetHandler(async (string databaseName, string password) =>
{
    dbConfig.Database = databaseName;
    Console.WriteLine($"Starting database creation for '{dbConfig.Database}'. Press Ctrl+C to stop.");
    var createDatabaseService = new CreateDatabaseService(dbConfig);
    var success = await createDatabaseService.CreateDatabaseAsync(dbConfig.Database, password);
    
    if (success)
    {
        Console.WriteLine("Database creation completed successfully!");
        Environment.Exit(0);
    }
    else
    {
        Console.WriteLine("Database creation failed!");
        Environment.Exit(1);
    }
}, databaseNameOption, passwordOption);
rootCommand.AddCommand(createDbCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the face detection background service and keep the app running
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var faceCommand = new Command("face", "Run the face detection processor as a background service");
faceCommand.AddOption(folderOption);
faceCommand.AddOption(parallelDegreeOption);
faceCommand.AddOption(databaseNameOption);
faceCommand.AddOption(isPlanOption);
faceCommand.AddOption(logIfProcessed);
faceCommand.SetHandler(async (string folder, int parallelDegree, string databaseName, string isPlan, bool logIfProcessed) =>
{
    picturesConfig.Folder = folder;
    dbConfig.Database = databaseName;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            bool planMode = isPlan.Equals("yes", StringComparison.OrdinalIgnoreCase);            
            services.AddSingleton<IHostedService>(sp => FaceDetectionProcessor.CreateProcessor(picturesConfig, dbConfig, parallelDegree, planMode, logIfProcessed));
            
            Console.WriteLine("Configured services.");

        })
        .Build();

    Console.WriteLine($"Starting face detection processor on {dbConfig.Database}/'{picturesConfig.Folder}'. Press Ctrl+C to stop.");
    // Console.WriteLine("Press Enter to continue...");
    // Console.ReadLine();    
    await host.RunAsync(cts.Token);
}, folderOption, parallelDegreeOption, databaseNameOption, isPlanOption, logIfProcessed);
rootCommand.AddCommand(faceCommand);






return await rootCommand.InvokeAsync(args);
