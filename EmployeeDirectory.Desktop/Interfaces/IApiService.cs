using EmployeeDirectory.Core.DTOs;

namespace EmployeeDirectory.Desktop.Interfaces
{
    public interface IApiService
    {
        Task<DirectorySyncDto> GetFullDirectoryAsync();

        Task<bool> HealthCheckAsync();

        string GetApiUrl();

    }
}
