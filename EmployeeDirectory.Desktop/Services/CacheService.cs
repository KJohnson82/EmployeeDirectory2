using EmployeeDirectory.Core.Extensions;
using EmployeeDirectory.Core.Models;
using EmployeeDirectory.Desktop.Data;
using EmployeeDirectory.Desktop.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Location = EmployeeDirectory.Core.Models.Location;

namespace EmployeeDirectory.Desktop.Services
{
    public class CacheService : ICacheService
    {
        private readonly LocalCacheContext _context;
        private readonly ILogger<CacheService> _logger;

        public CacheService(LocalCacheContext context, ILogger<CacheService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SaveEmployeesAsync(List<Employee> employees)
        {
            _context.Employees.RemoveRange(_context.Employees);
            await _context.Employees.AddRangeAsync(employees);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved {Count} employees to cache", employees.Count);
        }

        public async Task SaveDepartmentsAsync(List<Department> departments)
        {
            _context.Departments.RemoveRange(_context.Departments);
            await _context.Departments.AddRangeAsync(departments);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved {Count} departments to cache", departments.Count);
        }

        public async Task SaveLocationsAsync(List<Location> locations)
        {
            _context.Locations.RemoveRange(_context.Locations);
            await _context.Locations.AddRangeAsync(locations);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved {Count} locations to cache", locations.Count);
        }

        public async Task ClearAllDataAsync()
        {
            _context.Employees.RemoveRange(_context.Employees);
            _context.Departments.RemoveRange(_context.Departments);
            _context.Locations.RemoveRange(_context.Locations);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleared all data from cache");
        }

        public async Task<DateTime?> GetLastSyncTimeAsync()
        {
            var metadata = await _context.SyncMetadata
                .OrderByDescending(sm => sm.LastSyncTime)
                .FirstOrDefaultAsync();

            return metadata?.LastSyncTime;
        }

        public async Task SetLastSyncTimeAsync(DateTime syncTime)
        {
            var metadata = new SyncMetadata
            {
                LastSyncTime = syncTime,
                EmployeeCount = await _context.Employees.CountAsync(),
                DepartmentCount = await _context.Departments.CountAsync(),
                LocationCount = await _context.Locations.CountAsync(),
                LastSyncSuccessful = true,
                LastSyncMessage = "Sync completed successfully"
            };

            await _context.SyncMetadata.AddAsync(metadata);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetCachedRecordCountAsync()
        {
            return await _context.Employees.CountAsync()
                 + await _context.Departments.CountAsync()
                 + await _context.Locations.CountAsync();
        }
    }
}
