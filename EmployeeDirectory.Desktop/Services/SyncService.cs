using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using EmployeeDirectory.Desktop.Interfaces;
using EmployeeDirectory.Core.DTOs;
using EmployeeDirectory.Core.Models;
using EmployeeDirectory.Core.Extensions;

namespace EmployeeDirectory.Desktop.Services
{
    public class SyncService : ISyncService
    {
        private readonly IApiService _apiService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<SyncService> _logger;

        private DateTime? _lastSyncTime;
        private bool _isSyncing;

        public DateTime? LastSyncTime => _lastSyncTime;
        public bool IsSyncing => _isSyncing;

        public SyncService(
            IApiService apiService,
            ICacheService cacheService,
            ILogger<SyncService> logger)
        {
            _apiService = apiService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<SyncResult> SyncOnLaunchAsync()
        {
            var result = new SyncResult
            {
                SyncTime = DateTime.UtcNow
            };
            _isSyncing = true;
            try
            {
                result.ApiWasAvailable = await _apiService.HealthCheckAsync();
                if (!result.ApiWasAvailable)
                {
                    result.Success = false;
                    result.Message = "API is not available. Showing cached data.";
                    return result;
                }
                var directoryData = await _apiService.GetFullDirectoryAsync();
                var employees = directoryData.Employees.Select(dto => dto.ToModel()).ToList();
                var departments = directoryData.Departments.Select(dto => dto.ToModel()).ToList();
                var locations = directoryData.Locations.Select(dto => dto.ToModel()).ToList();

                await _cacheService.ClearAllDataAsync();
                await _cacheService.SaveEmployeesAsync(employees);
                await _cacheService.SaveDepartmentsAsync(departments);
                await _cacheService.SaveLocationsAsync(locations);
                _lastSyncTime = DateTime.UtcNow;
                result.Success = true;
                result.Message = "Data synced successfully.";
                result.EmployeesUpdated = directoryData.Employees.Count;
                result.DepartmentsUpdated = directoryData.Departments.Count;
                result.LocationsUpdated = directoryData.Locations.Count;

                return new SyncResult
                {
                    Success = true,
                    Message = "Data synced successfully.",
                    EmployeesUpdated = directoryData.Employees.Count,
                    DepartmentsUpdated = directoryData.Departments.Count,
                    LocationsUpdated = directoryData.Locations.Count,
                    SyncTime = _lastSyncTime.Value,
                    ApiWasAvailable = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync");
                result.Success = false;
                result.Message = "An error occurred during sync. Showing cached data.";
            }
            finally
            {
                _isSyncing = false;
            }
            return result;
        }

        public async Task<bool> IsApiAvailableAsync()
        {
            return await _apiService.HealthCheckAsync();
        }
    }
}