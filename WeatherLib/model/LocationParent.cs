namespace WeatherLib.model;

public class LocationParent
{
    public required string Title { get; set; }
    public required string LocationType { get; set; }
    public int Woeid { get; set; }
    public required string LattLong { get; set; }
}


/*
"title": "England",
"location_type": "Region / State / Province",
"woeid": 24554868,
"latt_long": "52.883560,-1.974060"
*/