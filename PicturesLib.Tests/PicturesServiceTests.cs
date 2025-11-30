using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Xunit;
using WeatherLib.model;
using WeatherLib.service;

namespace WeatherLib.service
{
    public class WeatherServiceTest : IDisposable
    {
        private readonly string tempDir;

        public WeatherServiceTest()
        {
            tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        private string CreateWeatherJsonFile(CityWeather data, string? fileName = null)
        {
            fileName ??= Guid.NewGuid() + ".json";
            string filePath = Path.Combine(tempDir, fileName);
            var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower};
            File.WriteAllText(filePath, JsonSerializer.Serialize(data, options));
            return filePath;
        }

        private CityWeather CreateCityWeather(string cityName)
        {
            return Tests.ResidioReportServiceTests.CreateCityWeather(cityName, (new DateOnly(2024, 1, 1), 5.0, 15.0) );           
        }

        [Fact]
        public void GetCityWeather_ReturnsCorrectData()
        {
            var expected = CreateCityWeather("TestCity");                
            string filePath = CreateWeatherJsonFile(expected);

            var service = new WeatherService();
            var result = service.GetCityWeather(filePath);

            Assert.Equal(expected.Title, result.Title);
            Assert.Equal(expected.ConsolidatedWeather[0].TheTemp, result.ConsolidatedWeather[0].TheTemp);
            Assert.Equal(expected.ConsolidatedWeather[0].WeatherStateName, result.ConsolidatedWeather[0].WeatherStateName);
            Assert.Equal(expected.woeid, result.woeid);
        }

        [Fact]
        public void GetCityWeather_ThrowsOnEmptyFile()
        {
            string filePath = Path.Combine(tempDir, "empty.json");
            File.WriteAllText(filePath, "");

            var service = new WeatherService();
            Assert.ThrowsAny<Exception>(() => service.GetCityWeather(filePath));
        }

        [Fact]
        public void GetAllCityWeather_ReturnsAllCities()
        {
            var cities = new List<CityWeather> { CreateCityWeather("A"), CreateCityWeather("B") };
            cities.ForEach(city => CreateWeatherJsonFile(city, city.Title + ".json"));

            var service = new WeatherService();
            var results = service.GetAllCitiesWeather(tempDir);
            Assert.Equal(2, results.Count());
            Assert.Equal(1, results.Count(c => c.Title == "A"));
            Assert.Equal(1, results.Count(c => c.Title == "B"));
        }

        [Fact]
        public void GetAllCityWeather_EmptyDirectory_ReturnsEmptyList()
        {
            var service = new WeatherService();
            var results = service.GetAllCitiesWeather(tempDir);
            Assert.Empty(results);
        }
    }
}