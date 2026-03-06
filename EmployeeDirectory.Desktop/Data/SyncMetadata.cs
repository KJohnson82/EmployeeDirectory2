using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeDirectory.Desktop.Data;

/// Tracks sync history for the local cache
public class SyncMetadata
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public DateTime LastSyncTime { get; set; }
    public int EmployeeCount { get; set; }
    public int DepartmentCount { get; set; }
    public int LocationCount { get; set; }
    public bool LastSyncSuccessful { get; set; }
    public string? LastSyncMessage { get; set; }
}