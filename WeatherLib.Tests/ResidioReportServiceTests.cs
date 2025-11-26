using System.Text.Json;
using WeatherLib.model;
using WeatherLib.service;
using Xunit;

namespace WeatherLib.Tests;

public class ResidioReportServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ResidioReportService _service;

    public ResidioReportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _service = new ResidioReportService(new WeatherService());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private void CreateWeatherJsonFile(string fileName, CityWeather cityWeather)
    {
        string filePath = Path.Combine(_tempDir, fileName);
        var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower};
        string json = JsonSerializer.Serialize(cityWeather, options);
        File.WriteAllText(filePath, json);
    }

    public static CityWeather CreateCityWeather(string cityName, params (DateOnly date, double minTemp, double maxTemp)[] weatherData)
    {
        return new CityWeather
        {
            Title = cityName,
            ConsolidatedWeather = weatherData.Select(w => new ConsolidatedWeather
            {
                ApplicableDate = w.date,
                MinTemp = w.minTemp,
                MaxTemp = w.maxTemp,
                WeatherStateName = "Clear",
                WeatherStateAbbr = "c",
                WindDirectionCompass = "N",
                Created = DateTimeOffset.Now,
                TheTemp = (w.minTemp + w.maxTemp) / 2,
                WindSpeed = 5.0,
                WindDirection = 180.0,
                AirPressure = 1013.0,
                Humidity = 50,
                Visibility = 10.0,
                Predictability = 70
            }).ToList(),
            Time = DateTimeOffset.Now,
            SunRise = DateTimeOffset.Now,
            SunSet = DateTimeOffset.Now,
            TimezoneName = "UTC",
            Parent = new LocationParent
            {
                Title = "London",
                LocationType = "City",
                Woeid = 1,
                LattLong = "0,0"
            },
            Sources = new List<WeatherSource>(),
            LocationType = "City",
            woeid = 123,
            LattLong = "0,0",
            Timezone = "UTC"
        };
    }

    [Fact]
    public void GetWeatherReport_SingleCity_ReturnsCorrectReport()
    {
        var date1 = new DateOnly(2024, 1, 1);
        var cityWeather = CreateCityWeather("London", (date1, 5.0, 15.0));
        CreateWeatherJsonFile("london.json", cityWeather);

        var result = _service.GetWeatherReport(_tempDir);
        Assert.Single(result);  
        Assert.Equal("London", result[0].Title);
        Assert.Equal(date1, result[0].ApplicableDate);
        Assert.Equal(5.0, result[0].MinTemp);
        Assert.Equal(15.0, result[0].MaxTemp);
    }

    [Fact]
    public void GetWeatherReport_MultipleDates_ReturnsAllDatesOrdered()
    {
        var date1 = new DateOnly(2024, 1, 3);
        var date2 = new DateOnly(2024, 1, 1);
        var date3 = new DateOnly(2024, 1, 2);

        var london = CreateCityWeather("London",
            (date1, 10.0, 20.0), 
            (date2, 5.0, 15.0),
            (date3, 7.0, 17.0)
        );
        CreateWeatherJsonFile("london.json", london);

        var result = _service.GetWeatherReport(_tempDir);
        Assert.Equal(3, result.Count);
        Assert.Equal(date2, result[0].ApplicableDate); // Ordered by date
        Assert.Equal(date3, result[1].ApplicableDate);
        Assert.Equal(date1, result[2].ApplicableDate);
    }

    [Fact]
    public void GetWeatherReport_SameMinTempForDate_SelectsFirstEncountered()
    {
        var date = new DateOnly(2024, 1, 1);
        var london = CreateCityWeather("London", (date, 10.0, 20.0));
        var newYork = CreateCityWeather("New York", (date, 10.0, 18.0)); // Same min_temp

        CreateWeatherJsonFile("london.json", london);
        CreateWeatherJsonFile("newyork.json", newYork);

        var result = _service.GetWeatherReport(_tempDir);

        Assert.Single(result);
        Assert.Equal(10.0, result[0].MinTemp);
        Assert.True(result[0].Title == "London" || result[0].Title == "New York");
    }

    [Fact]
    public void SerializeWeatherReport_EmptyList_ReturnsEmptyJsonArray()
    {
        var reports = new List<ResidioCityWeatherReport>();
        var result = _service.SerializeWeatherReport(reports);
        Assert.NotNull(result);
        Assert.Contains("[]", result);
    }

    [Fact]
    public void GetWeatherReport_SimulatedScenario_ProcessesCorrectly()
    {
        var startDate = new DateOnly(2024, 3, 28);       
        
        var newYork = CreateCityWeather("New York",
            (startDate, 0.2, 6.0),      // Coldest on day 1
            (startDate.AddDays(1), 3.7, 14.1),
            (startDate.AddDays(2), 9.0, 17.1)  
        );

        var london = CreateCityWeather("London",
            (startDate, 3.7, 16.2),
            (startDate.AddDays(1), 2.6, 17.4),  // Coldest on day 2
            (startDate.AddDays(2), 3.8, 16.9)
        );
        
        var losAngeles = CreateCityWeather("Los Angeles",
            (startDate, 9.4, 21.3),
            (startDate.AddDays(1), 7.1, 22.2),
            (startDate.AddDays(2), 1.5, 25.0)  // Coldest on day 3
        );

        CreateWeatherJsonFile("losangeles.json", losAngeles);
        CreateWeatherJsonFile("newyork.json", newYork);
        CreateWeatherJsonFile("london.json", london);

        var result = _service.GetWeatherReport(_tempDir);

        Assert.Equal(3, result.Count);
        
        // Day 1: New York has lowest min_temp 
        Assert.Equal(startDate, result[0].ApplicableDate);
        Assert.Equal("New York", result[0].Title);
        Assert.Equal(0.2, result[0].MinTemp);
        
        // Day 2: London has lowest min_temp 
        Assert.Equal(startDate.AddDays(1), result[1].ApplicableDate);
        Assert.Equal("London", result[1].Title);
        Assert.Equal(2.6, result[1].MinTemp);
        
        // Day 3: Los Angeles has lowest min_temp 
        Assert.Equal(startDate.AddDays(2), result[2].ApplicableDate);
        Assert.Equal("Los Angeles", result[2].Title);
        Assert.Equal(1.5, result[2].MinTemp);
    }
}
