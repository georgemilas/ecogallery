FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first (restore layer caching)
COPY WeatherApi/WeatherApi.csproj WeatherApi/
COPY WeatherLib/WeatherLib.csproj WeatherLib/
RUN dotnet restore WeatherApi/WeatherApi.csproj

# Copy remaining source including data folder used at runtime
COPY . .
WORKDIR /src/WeatherApi
RUN dotnet publish WeatherApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=build /app/publish .

# Copy data folder relative to working directory so appsettings.json configuration works 
COPY data ./data

# Set environment variables so configuration resolves absolute path if needed
ENV WEATHERDATA__FOLDER=./data

ENTRYPOINT ["dotnet", "WeatherApi.dll"]
