namespace WeatherLib.model;
public class CityWeather
{
    public required List<ConsolidatedWeather> ConsolidatedWeather { get; set; }
    public required DateTimeOffset Time { get; set; }
    public required DateTimeOffset SunRise { get; set; }
    public required DateTimeOffset SunSet { get; set; }
    public required string TimezoneName { get; set; }
    public required LocationParent Parent { get; set; }
    public required List<WeatherSource> Sources { get; set; }
    public required string Title { get; set; }
    public required string LocationType { get; set; }
    public int woeid { get; set; }
    public required string LattLong { get; set; }
    public required string Timezone { get; set; }  
}


/*
  "consolidated_weather": [],
  "time": "2019-03-28T19:41:06.318115Z",
  "sun_rise": "2019-03-28T05:45:50.917439Z",
  "sun_set": "2019-03-28T18:26:31.803799Z",
  "timezone_name": "LMT",
  "parent": {},
  "sources": [],
  "title": "London",
  "location_type": "City",
  "woeid": 44418,
  "latt_long": "51.506321,-0.12714",
  "timezone": "Europe/London"
*/