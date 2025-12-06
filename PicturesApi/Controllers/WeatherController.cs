using Microsoft.AspNetCore.Mvc;
using WeatherLib.model;
using WeatherLib.service;

namespace WeatherApi.Controllers;

[ApiController]
[Route("api/v1/weather")]
public class WeatherController : ControllerBase
{
    private readonly IResidioReportService _reportService;
    private readonly IConfiguration _configuration;

    public WeatherController(IResidioReportService reportService, IConfiguration configuration)
    {
        _reportService = reportService;
        _configuration = configuration;
    }

    [HttpGet]
    public ActionResult<List<ResidioCityWeatherReport>> Get()
    {
        string? dataFolder = _configuration["WeatherData:Folder"];
        if (string.IsNullOrWhiteSpace(dataFolder))
        {
            return Problem("Could not read data folder, is WeatherData:Folder set in appsettings.json?", statusCode: 500);
        }

        var report = _reportService.GetWeatherReport(dataFolder);
        return Ok(report);
    }
}
