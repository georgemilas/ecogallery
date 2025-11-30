using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using WeatherLib.model;
using WeatherLib.service;

namespace WeatherApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/pictures")]
public class PicturesController : ControllerBase
{
    private readonly IResidioReportService _reportService;
    private readonly IConfiguration _configuration;

    public PicturesController(IResidioReportService reportService, IConfiguration configuration)
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
