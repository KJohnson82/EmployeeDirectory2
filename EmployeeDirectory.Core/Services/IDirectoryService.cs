using EmployeeDirectory.Core.DTOs;
using EmployeeDirectory.Core.Enums;

namespace EmployeeDirectory.Core.Services;

public interface IDirectoryService
{
    Task<List<DepartmentDto>> GetDepartmentsAsync();
    Task<DepartmentDto?> GetDepartmentByIdAsync(int id);
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
    Task<LocationDto?> GetLocationWithDepartmentsAsync(int locationId);
    Task<DepartmentDto?> GetDepartmentWithEmployeesAsync(int departmentId);
    Task<LocationDto?> GetLocationByIdAsync(int id);
    Task<List<LocationDto>> GetLocationsAsync();
    Task<List<EmployeeDto>> GetEmployeesAsync();
    Task<List<EmployeeDto>> GetEmployeesByDepartmentAsync(int departmentId);
    Task<List<EmployeeDto>> GetEmployeesByLocationAsync(int locationId);
    Task<List<EmployeeDto>> GetEmployeesByLocationAndDepartmentAsync(int locationId, int departmentId);
    Task<List<LocationDto>> GetLocationsByTypeAsync(Loctype loctype);
    Task<LocationDto?> GetLocationByIdAsync(Loctype loctype, int id);
}