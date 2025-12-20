using System.Text.Json;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on all network interfaces
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5001); // Listen on all interfaces on port 5001 that is localhost, 127.0.0.1, 10.0.0.240 etc.
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {   
        // policy.WithOrigins("http://localhost:3000", "http://10.0.0.240:3000", 
        //                     "http://127.0.0.1:3000", "http://gm-pictures.ddns.net:3000",
        //                     "http://172.29.80.1:3000")
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

// Register configuration
builder.Services.Configure<PicturesDataConfiguration>(builder.Configuration.GetSection(PicturesDataConfiguration.SectionName));
builder.Services.Configure<DatabaseConfiguration>(builder.Configuration.GetSection(DatabaseConfiguration.SectionName));

// Register PicturesDataConfiguration as a singleton for repositories that need it directly
builder.Services.AddSingleton(sp => 
{
    var config = new PicturesDataConfiguration();
    builder.Configuration.GetSection(PicturesDataConfiguration.SectionName).Bind(config);
    return config;
});

builder.Services.AddSingleton(sp =>
{
    var config = new DatabaseConfiguration();
    builder.Configuration.GetSection(DatabaseConfiguration.SectionName).Bind(config);
    return config;
});

// Register HttpContextAccessor for accessing request information
builder.Services.AddHttpContextAccessor();

// Register business logic services
builder.Services.AddScoped<AlbumRepository>();  
builder.Services.AddScoped<AlbumImageRepository>();
builder.Services.AddScoped<GalleryApi.service.AlbumsService>();
builder.Services.AddScoped<GalleryApi.service.VirtualAlbumsService>();

// Swagger/OpenAPI https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "gmpictures api",
        Version = "v1.0"
    });
});

var app = builder.Build();

// Serve static files from the pictures directory
var picturesPath = builder.Configuration["PicturesData:Folder"];
if (!string.IsNullOrEmpty(picturesPath) && Directory.Exists(picturesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(picturesPath),
        RequestPath = "/pictures"
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "gmpictures api v1");
    });
}

app.UseCors("AllowFrontend");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();

