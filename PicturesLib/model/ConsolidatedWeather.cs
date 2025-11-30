namespace WeatherLib.model;

public class ConsolidatedWeather
{
    public long Id { get; set; }
    public required string WeatherStateName { get; set; }
    public required string WeatherStateAbbr { get; set; }
    public required string WindDirectionCompass { get; set; }
    public required DateTimeOffset Created { get; set; }
    public required DateOnly ApplicableDate { get; set; }
    public double MinTemp { get; set; }
    public double MaxTemp { get; set; }
    public double TheTemp { get; set; }
    public double WindSpeed { get; set; }
    public double WindDirection { get; set; }
    public double AirPressure { get; set; }
    public int Humidity { get; set; }
    public double Visibility { get; set; }
    public int Predictability { get; set; }
}

/*
"id": 5838918358401024,
"weather_state_name": "Light Cloud",
"weather_state_abbr": "lc",
"wind_direction_compass": "S",
"created": "2019-03-28T16:44:44.722535Z",
"applicable_date": "2019-03-28",
"min_temp": 0.195,
"max_temp": 5.97,
"the_temp": 5.29,
"wind_speed": 6.176262557117103,
"wind_direction": 173.70645757364878,
"air_pressure": 1030.8200000000002,
"humidity": 72,
"visibility": 15.190972222222221,
"predictability": 70
*/