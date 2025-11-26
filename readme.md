
## Definition and requirements
See the problem definition [here](./CodingProblem.docx)

## Solution structure
Since this was a take home problem I decided to create a more realistic solution, including a business layer, tests and also a web api for deplyment using containerization and to showcase a few aspects like contract versioning and dependency injection for flexible testing etc.  

The output of the solution is a json with an array containing data objects of the following structure with the names and values matching the input json as requested in requirements. `"The names and values of the JSON fields must match exactly the corresponding names and values in your output report"`

```json
 {
    "applicable_date": "2019-03-29",
    "min_temp": 2.6300000000000003,
    "max_temp": 17.42,
    "title": "London"
  }
```

The solution folder consists of 4 projects and the ["`data`"](./data/) folder, where the json files for the weather data is stored:
1) WeatherLib - This is a library containing the main business logic for the solution. 
    * model folder:  This folder contains classes for the data model used by the json weather data as well as the output model for the result
    * service folder: 
        * ResideoReportService.cs file: This file contains the main business logic of creating a data structure for the cities with the minimum temperature for each of the days (GetWeatherReport method)
            * The algorithm consists of one loop over the json files and building a dictionary containing the city with the minimum temperature as the loop progresses. This way the solution is O(n). The ordering of the days is a bit more computationaly intensive using Linq and coverting to a list from a dictionary. This could be optimized by using an ordered or even a sorted dictionary to begin with and thus eliminating the need for the aditional linq operation. I decided to keep the linq in the solution as sometimes having independent steps could be more beneficial allowing for building composable systems and more flexibility in swaping components.       
        * WeatherService.cs: This class is resposible for creating the data model used by the business logic in ResidioReportService  
2) WeatherConsole - This projects outputs the json report either to the screen directly or to a file, based on command line arguments
	```powershell
	cd Resideo
    dotnet run --project WeatherConsole
    dotnet run --project WeatherConsole --file report.json
    ```
3) WeatherApi - A versioned Web API (v1) to expose the data at "/api/v1/weather" and also to run as a docker container
    ```powershell
	cd Resideo
    dotnet run --project WeatherApi
    # Swagger UI: http://localhost:5001/swagger
    # Endpoint:   http://localhost:5001/api/v1/weather
    ```

4) WeatherLib.Tests - this projects contains the tests to ensure correctness in the business services  
    * WeatherService
        * GetCityWeather_ReturnsCorrectData - test parsing and creating DTO works as expected
        * GetCityWeather_ThrowsOnEmptyFile - make sure we handle empty json file
        * GetAllCityWeather_ReturnsAllCities - ensure we handle all json files in the data folder
        * GetAllCityWeather_EmptyDirectory_ReturnsEmptyList - handle no json files
    * ResideoReportService
        * GetWeatherReport_SingleCity_ReturnsCorrectReport - handles only one file for one city 
        * GetWeatherReport_MultipleDates_ReturnsAllDatesOrdered - ensure proper ascending order
        * GetWeatherReport_SameMinTempForDate_SelectsFirstEncountered - handle multiple cities having the same minimum
        * SerializeWeatherReport_EmptyList_ReturnsEmptyJsonArray - handle no files
        * GetWeatherReport_SimulatedScenario_ProcessesCorrectly - business case scenario acceptance test 

    ```powershell
    cd Resideo
	dotnet test
    ```

## Run webapi using docker

Build and run API with an attached data volume for where the json files are stored:

```powershell
cd Resideo
docker build -t weather-api:latest .
docker run -d -p 8080:8080 -v ${PWD}\data:/app/data -e WEATHERDATA__FOLDER=/app/data --name weather-api weather-api:latest
```


## Commands to run the projects
You can run the following commands using a powershell terminal.

| Command | Description |
|---------|---------|
| cd Resideo | Go to the solution folder 
| dotnet run --project WeatherConsole | Print report to console |
| dotnet run --project WeatherConsole --file report.json | Save report to file |
| dotnet run --project WeatherApi | Run web API locally |
| dotnet test | Execute tests |
| docker run -d -p 8080:8080 ...  | Run webapi in a container - see above for full command |


## Additional information
The solution above is targeting .NET 8 and if you aren't sure you have it or if there are issues building the projects, you can use the following command on a Windows machine in a powershell connsole or download and install from [Microsoft official .NET site](https://dotnet.microsoft.com/download/dotnet/8.0) 

```powershell
winget install Microsoft.DotNet.SDK.8
````
