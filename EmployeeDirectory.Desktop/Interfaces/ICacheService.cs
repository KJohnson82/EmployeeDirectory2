using EmployeeDirectory.Core.Models;
using Location = EmployeeDirectory.Core.Models.Location;

namespace EmployeeDirectory.Desktop.Interfaces;

/// Service for writing sync data to local SQLite cache
public interface ICacheService
{
    // Write operations
    Task SaveEmployeesAsync(List<Employee> employees);
    Task SaveDepartmentsAsync(List<Department> departments);
    Task SaveLocationsAsync(List<Location> locations);
    Task ClearAllDataAsync();

    // Metadata
    Task<DateTime?> GetLastSyncTimeAsync();
    Task SetLastSyncTimeAsync(DateTime syncTime);
    Task<int> GetCachedRecordCountAsync();
}