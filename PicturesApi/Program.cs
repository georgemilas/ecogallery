using System.Text.Json;
using WeatherLib.service;
using PicturesLib.model.configuration;
using PicturesLib.repository;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
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

// Register PicturesDataConfiguration as a singleton for repositories that need it directly
builder.Services.AddSingleton(sp => 
{
    var config = new PicturesDataConfiguration();
    builder.Configuration.GetSection(PicturesDataConfiguration.SectionName).Bind(config);
    return config;
});

// Register business logic services
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IResidioReportService, ResidioReportService>();  
builder.Services.AddScoped<AlbumRepository>();  
builder.Services.AddScoped<AlbumImageRepository>();

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

