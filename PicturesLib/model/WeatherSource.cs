namespace WeatherLib.model;

public class WeatherSource
{
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string Url { get; set; }
    public int CrawlRate { get; set; }
}

/*
"title": "BBC",
"slug": "bbc",
"url": "http://www.bbc.co.uk/weather/",
"crawl_rate": 180
*/