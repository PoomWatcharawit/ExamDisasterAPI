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

        public async Task<double> GetRainfallAsync(double lat, double lon)
        {
            string cacheKey = $"rain:{lat}:{lon}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null) return double.Parse(cached);

            var baseUrl = _config["ExternalApis:Flood:BaseUrl"];
            var apiKey = _config["ExternalApis:Flood:ApiKey"];
            var url = $"{baseUrl}?lat={lat}&lon={lon}&appid={apiKey}&units=metric";

            _logger.LogInformation("Calling OpenWeather API: {Url}", url);
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            double rainfall = doc.RootElement.TryGetProperty("rain", out var rainProp) && rainProp.TryGetProperty("1h", out var rain1h)
                ? rain1h.GetDouble() : 0.0;

            await _cache.SetStringAsync(
                cacheKey,
                rainfall.ToString(),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) }
            );
            return rainfall;
        }

        // 2. Earthquake Magnitude
        public async Task<double> GetEarthquakeMagnitudeAsync(double lat, double lon)
        {
            string cacheKey = $"quake:{lat}:{lon}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null) return double.Parse(cached);

            var baseUrl = _config["ExternalApis:Earthquake:BaseUrl"];
            var url = $"{baseUrl}?format=geojson&latitude={lat}&longitude={lon}&maxradiuskm=100&limit=1";
            _logger.LogInformation("Calling USGS API: {Url}", url);
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var features = doc.RootElement.GetProperty("features");
            double mag = features.GetArrayLength() > 0
                ? features[0].GetProperty("properties").GetProperty("mag").GetDouble()
                : 0.0;
            await _cache.SetStringAsync(cacheKey, mag.ToString(), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
            return mag;
        }

        // 3. Weather (for Wildfire)
        public async Task<(double temp, double humidity)> GetWeatherAsync(double lat, double lon)
        {
            string cacheKey = $"weather:{lat}:{lon}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                var parts = cached.Split(',');
                return (double.Parse(parts[0]), double.Parse(parts[1]));
            }

            var baseUrl = _config["ExternalApis:Flood:BaseUrl"];
            var apiKey = _config["ExternalApis:Flood:ApiKey"];
            var url = $"{baseUrl}?lat={lat}&lon={lon}&appid={apiKey}&units=metric";
            _logger.LogInformation("Calling OpenWeather API: {Url}", url);
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            double temp = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
            double humidity = doc.RootElement.GetProperty("main").GetProperty("humidity").GetDouble();
            await _cache.SetStringAsync(cacheKey, $"{temp},{humidity}", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
            return (temp, humidity);
        }

        // 4. Risk Score (cache by region and disaster type)
        public async Task<int> CalculateRiskScore(Regions region, string disasterType)
        {
            string cacheKey = $"risk:{region.RegionID}:{disasterType}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Cache hit for key {CacheKey} with value {Value}", cacheKey, cached);
                return int.Parse(cached);
            }

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

            await _cache.SetStringAsync(
                cacheKey,
                score.ToString(),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) }
            );
            _logger.LogInformation("Set cache for key {CacheKey} with value {Score}", cacheKey, score);

            return score;
        }

        public RiskLevel GetRiskLevel(int score)
        {
            if (score >= 75) return RiskLevel.High;
            if (score >= 50) return RiskLevel.Medium;
            return RiskLevel.Low;
        }
    }
}
