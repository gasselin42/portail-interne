using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Employee ---
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.JobTitle).HasMaxLength(150);
            entity.Property(e => e.PhoneNumber).HasMaxLength(30);

            // Relation 1-1 : un employé a 0 ou 1 compte
            entity.HasOne(e => e.UserAccount) // <Employee> a un UserAccount
                  .WithOne(u => u.Employee) // et ce UserAccount a un Employee
                  .HasForeignKey<UserAccount>(u => u.EmployeeId) // Qui a la clé étrangère? C'est UserAccount avec EmployeeId
                  .OnDelete(DeleteBehavior.Cascade); // Si tu supprimes Employee, tu supprimes UserAccount
        });

        // --- UserAccount ---
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.EmployeeId).IsUnique(); // 1 compte max par employé
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.PasswordHash).HasMaxLength(500);
        });

        // --- UserAccount ---
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.ReviewedBy)
                .WithMany()
                .HasForeignKey(l => l.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}