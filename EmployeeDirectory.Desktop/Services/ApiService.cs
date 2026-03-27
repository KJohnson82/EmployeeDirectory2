using EmployeeDirectory.Core.DTOs;
using EmployeeDirectory.Desktop.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeDirectory.Desktop.Services
{
    public class ApiService : IApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ApiService> _logger;

        public ApiService(
        IHttpClientFactory httpClientFactory,
        ILogger<ApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<DirectorySyncDto> GetFullDirectoryAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("EmployeeDirectoryApi");
                var response = await client.GetAsync("/api/directory/sync");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var directoryData = System.Text.Json.JsonSerializer.Deserialize<DirectorySyncDto>(content, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return directoryData ?? new DirectorySyncDto();
                }
                else
                {
                    _logger.LogError("API returned error: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    return new DirectorySyncDto();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling API");
                return new DirectorySyncDto();
            }
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("EmployeeDirectoryApi");
                var response = await client.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during API health check");
                return false;
            }
        }

        public string GetApiUrl()
        {
            var client = _httpClientFactory.CreateClient("EmployeeDirectoryApi");
            return client.BaseAddress?.ToString() ?? "Unknown";
        }
    }
}
