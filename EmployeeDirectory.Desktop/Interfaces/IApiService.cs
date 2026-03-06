using EmployeeDirectory.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmployeeDirectory.Desktop.Interfaces
{
    public interface IApiService
    {
        Task<DirectorySyncDto> GetFullDirectoryAsync();

        Task<bool> HealthCheckAsync();

        string GetApiUrl();

    }
}
