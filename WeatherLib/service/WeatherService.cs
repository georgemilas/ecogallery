using System.Text.Json;
using WeatherLib.model;

namespace WeatherLib.service;

public interface IWeatherService
{
    /// <summary>
    /// Returns weather data for all city files found in the given folder path
    /// </summary>
    IEnumerable<CityWeather> GetAllCitiesWeather(string jsonFilesPath);

    /// <summary>
    /// Returns weather data for the given city file
    /// </summary>
    CityWeather GetCityWeather(string jsonFilePath);
    
}

public class WeatherService : IWeatherService
{

    public IEnumerable<CityWeather> GetAllCitiesWeather(string jsonFilesPath)
    {
        DirectoryInfo dir = new DirectoryInfo(jsonFilesPath);
        FileInfo[] files = dir.GetFiles();
        return files.Select(f => GetCityWeather(f)).ToList();
    }

    public CityWeather GetCityWeather(string jsonFilePath)
    {
        return GetCityWeather(new FileInfo(jsonFilePath));
    }
    private CityWeather GetCityWeather(FileInfo jsonFilePath)
    {
        string json = File.ReadAllText(jsonFilePath.FullName);

        //let it throw if it can't parse
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        CityWeather? data = JsonSerializer.Deserialize<CityWeather>(json, options);
        if (data == null)
        {
            throw new Exception("Could not find json weather data, is the file empty?");
        }
        return data;
    }




}
