using EmployeeDirectory.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeDirectory.Core.DTOs
{
    /// Data Transfer Object for Location - used for API communication
    public class LocationDto
    {
        public int Id { get; set; }
        public string LocName { get; set; } = string.Empty;
        public int? LocNum { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zipcode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? Email { get; set; }
        public string? Hours { get; set; }
        public Loctype Loctype { get; set; }
        public string? AreaManager { get; set; }
        public string? StoreManager { get; set; }
        public DateTime? RecordAdd { get; set; }
        public bool Active { get; set; }

        // Flattened related data
        public string LoctypeName => Loctype.GetDisplayName();

        public List<DepartmentDto> Departments { get; set; } = new List<DepartmentDto>();
        public List<EmployeeDto> Employees { get; set; } = new List<EmployeeDto>();
    }

}
