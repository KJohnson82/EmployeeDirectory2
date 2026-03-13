using EmployeeDirectory.Core.Enums;
using EmployeeDirectory.Core.Models;
using Microsoft.EntityFrameworkCore;
using Location = EmployeeDirectory.Core.Models.Location;

namespace EmployeeDirectory.Desktop.Data;

/// Local SQLite cache database context for EmployeeDirectory.Desktop
/// This is separate from AppDbContext — it's a local read-only cache
public class LocalCacheContext : DbContext
{
    public LocalCacheContext(DbContextOptions<LocalCacheContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<SyncMetadata> SyncMetadata { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Location>(entity =>
        {
            entity.Property(e => e.Loctype)
                .HasConversion<int>()
                .IsRequired();
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasOne(d => d.DeptLocation)
                .WithMany(l => l.Departments)
                .HasForeignKey(d => d.Location)
                .IsRequired(false);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasOne(e => e.EmpDepartment)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.Department)
                .IsRequired(false);

            entity.HasOne(e => e.EmpLocation)
                .WithMany(l => l.Employees)
                .HasForeignKey(e => e.Location)
                .IsRequired(false);
        });

        modelBuilder.Entity<SyncMetadata>(entity =>
        {
            entity.ToTable("SyncMetadata");
        });
    }
}