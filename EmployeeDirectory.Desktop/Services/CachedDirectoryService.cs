using EmployeeDirectory.Core.DTOs;
using EmployeeDirectory.Core.Enums;
using EmployeeDirectory.Core.Extensions;
using EmployeeDirectory.Core.Services;
using EmployeeDirectory.Desktop.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeDirectory.Desktop.Services;

/// IDirectoryService implementation that reads from local SQLite cache
public class CachedDirectoryService : IDirectoryService
{
    private readonly LocalCacheContext _context;

    public CachedDirectoryService(LocalCacheContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync()
    {
        var departments = await _context.Departments
            .Include(d => d.DeptLocation)
            .Include(d => d.Employees)
            .Where(d => d.Active == true)
            .OrderBy(d => d.DeptName)
            .AsNoTracking()
            .ToListAsync();

        return departments.Select(d => d.ToDto()).ToList();
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
    {
        var department = await _context.Departments
            .Include(d => d.DeptLocation)
            .Include(d => d.Employees)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        return department?.ToDto();
    }

    public async Task<DepartmentDto?> GetDepartmentWithEmployeesAsync(int departmentId)
    {
        var department = await _context.Departments
            .Include(d => d.DeptLocation)
            .Include(d => d.Employees)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == departmentId);

        return department?.ToDto();
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.EmpLocation)
            .Include(e => e.EmpDepartment)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        return employee?.ToDto();
    }

    public async Task<List<EmployeeDto>> GetEmployeesAsync()
    {
        var employees = await _context.Employees
            .Include(e => e.EmpLocation)
            .Include(e => e.EmpDepartment)
            .Where(e => e.Active == true)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .AsNoTracking()
            .ToListAsync();

        return employees.Select(e => e.ToDto()).ToList();
    }

    public async Task<List<EmployeeDto>> GetEmployeesByDepartmentAsync(int departmentId)
    {
        var employees = await _context.Employees
            .Include(e => e.EmpLocation)
            .Include(e => e.EmpDepartment)
            .Where(e => e.Active == true && e.Department == departmentId)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .AsNoTracking()
            .ToListAsync();

        return employees.Select(e => e.ToDto()).ToList();
    }

    public async Task<List<EmployeeDto>> GetEmployeesByLocationAsync(int locationId)
    {
        var employees = await _context.Employees
            .Include(e => e.EmpLocation)
            .Include(e => e.EmpDepartment)
            .Where(e => e.Active == true && e.Location == locationId)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .AsNoTracking()
            .ToListAsync();

        return employees.Select(e => e.ToDto()).ToList();
    }

    public async Task<List<EmployeeDto>> GetEmployeesByLocationAndDepartmentAsync(int locationId, int departmentId)
    {
        var employees = await _context.Employees
            .Include(e => e.EmpLocation)
            .Include(e => e.EmpDepartment)
            .Where(e => e.Active == true && e.Location == locationId && e.Department == departmentId)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .AsNoTracking()
            .ToListAsync();

        return employees.Select(e => e.ToDto()).ToList();
    }

    public async Task<List<LocationDto>> GetLocationsAsync()
    {
        var locations = await _context.Locations
            .Include(l => l.Departments)
            .Include(l => l.Employees)
            .Where(l => l.Active == true)
            .OrderBy(l => l.LocName)
            .AsNoTracking()
            .ToListAsync();

        return locations.Select(l => l.ToDto()).ToList();
    }

    public async Task<LocationDto?> GetLocationByIdAsync(int id)
    {
        var location = await _context.Locations
            .Include(l => l.Departments)
            .Include(l => l.Employees)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        return location?.ToDto();
    }

    public async Task<LocationDto?> GetLocationWithDepartmentsAsync(int locationId)
    {
        var location = await _context.Locations
            .Include(l => l.Departments)
            .Include(l => l.Employees)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId);

        return location?.ToDto(includeRelated: true);
    }

    public async Task<List<LocationDto>> GetLocationsByTypeAsync(Loctype loctype)
    {
        var locations = await _context.Locations
            .Include(l => l.Departments)
            .Include(l => l.Employees)
            .Where(l => l.Active == true && l.Loctype == loctype)
            .OrderBy(l => l.LocName)
            .AsNoTracking()
            .ToListAsync();

        return locations.Select(l => l.ToDto(includeRelated: true)).ToList();
    }

    public async Task<LocationDto?> GetLocationByIdAsync(Loctype loctype, int id)
    {
        var location = await _context.Locations
            .Include(l => l.Departments)
            .Include(l => l.Employees)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && l.Loctype == loctype);

        return location?.ToDto(includeRelated: true);
    }
}