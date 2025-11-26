using System.Text.Json;
using WeatherLib.model;

namespace WeatherLib.service;

public interface IResidioReportService
{
    /// <summary>
    /// Find the city with the minimum temperature for each day across all cities and all days 
    /// </summary>
    List<ResidioCityWeatherReport> GetWeatherReport(string jsonFilesPath);

    /// <summary>
    /// Returns pretty printed json weather report 
    /// </summary>
    string SerializeWeatherReport(List<ResidioCityWeatherReport> weatherReport);
}

public class ResidioReportService : IResidioReportService
{
    private readonly IWeatherService _weatherService;
    public ResidioReportService(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }
    
    public List<ResidioCityWeatherReport> GetWeatherReport(string jsonFilesPath)
    {
        Dictionary<DateOnly, ResidioCityWeatherReport> reportData = new Dictionary<DateOnly, ResidioCityWeatherReport>();

        var allCitiesWeather = _weatherService.GetAllCitiesWeather(jsonFilesPath);

        foreach (var cityWeather in allCitiesWeather)
        {
            string cityName = cityWeather.Title;
            foreach (var cw in cityWeather.ConsolidatedWeather)
            {
                if ((reportData.ContainsKey(cw.ApplicableDate) && reportData[cw.ApplicableDate].MinTemp > cw.MinTemp) ||
                    !reportData.ContainsKey(cw.ApplicableDate))
                {
                    reportData[cw.ApplicableDate] = new ResidioCityWeatherReport
                    {
                        Title = cityName,
                        ApplicableDate = cw.ApplicableDate,
                        MinTemp = cw.MinTemp,
                        MaxTemp = cw.MaxTemp
                    };
                }
            }
        }

        return reportData.OrderBy(r => r.Value.ApplicableDate).ToList().ConvertAll(r => r.Value);
    }    
    
    public string SerializeWeatherReport(List<ResidioCityWeatherReport> weatherReport)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true  // pretty print since this is for human consumption
        };
        return JsonSerializer.Serialize(weatherReport, options);        
    }



}