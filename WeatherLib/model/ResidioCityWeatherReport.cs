namespace WeatherLib.model;

public class ResidioCityWeatherReport
{
    public required DateOnly ApplicableDate { get; set; }
    public double MinTemp { get; set; }
    public double MaxTemp { get; set; }
    public required string Title { get; set; }
}