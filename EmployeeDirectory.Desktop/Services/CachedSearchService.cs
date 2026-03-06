using EmployeeDirectory.Core.Models;
using EmployeeDirectory.Core.Services;
using EmployeeDirectory.Desktop.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Location = EmployeeDirectory.Core.Models.Location;

namespace EmployeeDirectory.Desktop.Services;

/// Desktop search implementation that searches the local SQLite cache
public class CachedSearchService : ISearchService
{
    private readonly LocalCacheContext _context;
    private readonly ILogger<CachedSearchService> _logger;

    public CachedSearchService(LocalCacheContext context, ILogger<CachedSearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SearchResults> SearchAllAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new SearchResults();

        _logger.LogDebug("Searching cache for: '{SearchTerm}'", searchTerm);

        try
        {
            var employees = await SearchEmployeesAsync(searchTerm);
            var departments = await SearchDepartmentsAsync(searchTerm);
            var locations = await SearchLocationsAsync(searchTerm);

            _logger.LogDebug("Found {Employees} employees, {Departments} departments, {Locations} locations",
                employees.Count, departments.Count, locations.Count);

            return new SearchResults
            {
                Employees = employees,
                Departments = departments,
                Locations = locations,
                SearchTerm = searchTerm
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search error for: '{SearchTerm}'", searchTerm);
            return new SearchResults { SearchTerm = searchTerm };
        }
    }

    private async Task<List<Employee>> SearchEmployeesAsync(string searchTerm)
    {
        var allEmployees = await _context.Employees
            .Include(e => e.EmpLocation)
            .Include(e => e.EmpDepartment)
            .Where(e => e.Active == true)
            .ToListAsync();

        return allEmployees.Where(e =>
            Contains(e.FirstName, searchTerm) ||
            Contains(e.LastName, searchTerm) ||
            Contains(e.Email, searchTerm) ||
            Contains(e.JobTitle, searchTerm) ||
            Contains(e.PhoneNumber, searchTerm) ||
            Contains(e.CellNumber, searchTerm) ||
            Contains(e.Extension, searchTerm) ||
            Contains(e.NetworkId, searchTerm)
        ).Take(20).ToList();
    }

    private async Task<List<Department>> SearchDepartmentsAsync(string searchTerm)
    {
        var allDepartments = await _context.Departments
            .Include(d => d.DeptLocation)
            .Where(d => d.Active == true)
            .ToListAsync();

        return allDepartments.Where(d =>
            Contains(d.DeptName, searchTerm) ||
            Contains(d.DeptManager, searchTerm) ||
            Contains(d.DeptPhone, searchTerm) ||
            Contains(d.DeptEmail, searchTerm)
        ).Take(20).ToList();
    }

    private async Task<List<Location>> SearchLocationsAsync(string searchTerm)
    {
        var allLocations = await _context.Locations
            .Where(l => l.Active == true)
            .ToListAsync();

        return allLocations.Where(l =>
            Contains(l.LocName, searchTerm) ||
            Contains(l.Address, searchTerm) ||
            Contains(l.City, searchTerm) ||
            Contains(l.State, searchTerm) ||
            Contains(l.Zipcode, searchTerm) ||
            Contains(l.PhoneNumber, searchTerm) ||
            Contains(l.AreaManager, searchTerm) ||
            Contains(l.StoreManager, searchTerm) ||
            (l.LocNum.HasValue && l.LocNum.ToString()!.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        ).Take(20).ToList();
    }

    private static bool Contains(string? value, string searchTerm)
        => value != null && value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}