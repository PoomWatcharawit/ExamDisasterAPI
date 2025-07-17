using DisasterAPI.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DisasterAPI.Services
{
    public class DisasterServices
    {
        private readonly HttpClient _httpClient;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _config;
        private readonly ILogger<DisasterServices> _logger;

        public DisasterServices(HttpClient httpClient, IDistributedCache cache, IConfiguration config, ILogger<DisasterServices> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _config = config;
            _logger = logger;
        }

        public async Task<int> CalculateRiskScore(Regions region, string disasterType)
        {
            int score = 0;
            if (disasterType == "Flood")
            {
                double rainfall = await GetRainfallAsync(region.LocationCoordinates.Latitude, region.LocationCoordinates.Longitude);
                if (rainfall > 50) score = 90;
                else if (rainfall > 20) score = 60;
                else score = 30;
            }
            else if (disasterType == "Earthquake")
            {
                double mag = await GetEarthquakeMagnitudeAsync(region.LocationCoordinates.Latitude, region.LocationCoordinates.Longitude);
                if (mag > 5.0) score = 90;
                else if (mag > 3.0) score = 60;
                else score = 30;
            }
            else if (disasterType == "Wildfire")
            {
                var (temp, humidity) = await GetWeatherAsync(region.LocationCoordinates.Latitude, region.LocationCoordinates.Longitude);
                if (temp > 35 && humidity < 30) score = 90;
                else if (temp > 30 && humidity < 40) score = 60;
                else score = 30;
            }

            return score;
        }

        public async Task<double> GetRainfallAsync(double lat, double lon)
        {
            var baseUrl = _config["ExternalApis:Flood:BaseUrl"];
            var apiKey = _config["ExternalApis:Flood:ApiKey"];
            var url = $"{baseUrl}?lat={lat}&lon={lon}&appid={apiKey}&units=metric";

            _logger.LogInformation("Calling OpenWeather API: {Url}", url);
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            double rainfall = doc.RootElement.TryGetProperty("rain", out var rainProp) && rainProp.TryGetProperty("1h", out var rain1h)
                ? rain1h.GetDouble() : 0.0;

            return rainfall;
        }

        public async Task<double> GetEarthquakeMagnitudeAsync(double lat, double lon)
        {
            var baseUrl = _config["ExternalApis:Earthquake:BaseUrl"];
            var url = $"{baseUrl}?format=geojson&latitude={lat}&longitude={lon}&maxradiuskm=100&limit=1";
            _logger.LogInformation("Calling USGS API: {Url}", url);
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var features = doc.RootElement.GetProperty("features");
            double mag = features.GetArrayLength() > 0
                ? features[0].GetProperty("properties").GetProperty("mag").GetDouble()
                : 0.0;

            return mag;
        }

        public async Task<(double temp, double humidity)> GetWeatherAsync(double lat, double lon)
        {

            var baseUrl = _config["ExternalApis:Flood:BaseUrl"];
            var apiKey = _config["ExternalApis:Flood:ApiKey"];
            var url = $"{baseUrl}?lat={lat}&lon={lon}&appid={apiKey}&units=metric";
            _logger.LogInformation("Calling OpenWeather API: {Url}", url);
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            double temp = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
            double humidity = doc.RootElement.GetProperty("main").GetProperty("humidity").GetDouble();

            return (temp, humidity);
        }

        public RiskLevel GetRiskLevel(int score)
        {
            if (score >= 75) return RiskLevel.High;
            if (score >= 50) return RiskLevel.Medium;
            return RiskLevel.Low;
        }
    }
}
