using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
	public DbSet<Site> Sites => Set<Site>();
	public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
	public DbSet<WorkScheduleDay> WorkScheduleDays => Set<WorkScheduleDay>();

	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
		SavingChanges += (_, _) => UpdateSearchKeys();
	}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Employee ---
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
			entity.HasIndex(e => new { e.LastNameKey, e.FirstNameKey });
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.JobTitle).HasMaxLength(150);
            entity.Property(e => e.PhoneNumber).HasMaxLength(30);
			entity.Property(e => e.AnnualLeaveDays).HasDefaultValue(20);
			entity.Property(e => e.SiteId).HasDefaultValue(1);

            // Relation 1-1 : un employé a 0 ou 1 compte
            entity.HasOne(e => e.UserAccount) // <Employee> a un UserAccount
                  .WithOne(u => u.Employee) // et ce UserAccount a un Employee
                  .HasForeignKey<UserAccount>(u => u.EmployeeId) // Qui a la clé étrangère? C'est UserAccount avec EmployeeId
                  .OnDelete(DeleteBehavior.Cascade); // Si tu supprimes Employee, tu supprimes UserAccount

			entity.HasOne(e => e.Site)
					.WithMany()
					.HasForeignKey(e => e.SiteId)
					.OnDelete(DeleteBehavior.Restrict);
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

			entity.HasOne(l => l.CreatedBy)
                .WithMany()
                .HasForeignKey(l => l.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.ReviewedBy)
                .WithMany()
                .HasForeignKey(l => l.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

		// --- Site ---
		modelBuilder.Entity<Site>(entity =>
		{
			entity.HasData(new Site { Id = 1, Name = "Montréal", TimeZoneId = "America/Toronto" });
		});

		// --- WorkSchedule ---
		modelBuilder.Entity<WorkSchedule>(entity =>
		{
			entity.HasMany(s => s.Days)
				.WithOne()
				.HasForeignKey(s => s.WorkScheduleId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
			
			entity.HasIndex(s => new { s.EmployeeId, s.EffectiveFrom }).IsUnique();
		});

		// --- WorkScheduleDay ---
		modelBuilder.Entity<WorkScheduleDay>(entity =>
		{
			entity.HasKey(d => new { d.WorkScheduleId, d.DayOfWeek });
		});
    }

	private void UpdateSearchKeys()
	{
		foreach (var entry in ChangeTracker.Entries<Employee>())
		{
			if (entry.State is not (EntityState.Added or EntityState.Modified))
				continue;

			var employee = entry.Entity;

			employee.FirstNameKey = TextKey.From(employee.FirstName);
			employee.LastNameKey = TextKey.From(employee.LastName);
			employee.JobTitleKey = TextKey.From(employee.JobTitle);
		}
	}
}