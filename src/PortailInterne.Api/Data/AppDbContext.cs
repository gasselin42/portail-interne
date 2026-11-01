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
	public DbSet<Holiday> Holidays => Set<Holiday>();
	public DbSet<ScheduleAdjustment> ScheduleAdjustments => Set<ScheduleAdjustment>();
	public DbSet<Absence> Absences => Set<Absence>();
	public DbSet<EmployeePreferences> EmployeePreferences => Set<EmployeePreferences>();
	public DbSet<Meeting> Meetings => Set<Meeting>();
	public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();
	public DbSet<Notification> Notifications => Set<Notification>();

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

        // --- LeaveRequest ---
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

		// --- ScheduleAdjustment ---
        modelBuilder.Entity<ScheduleAdjustment>(entity =>
        {
            entity.HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ReviewedBy)
                .WithMany()
                .HasForeignKey(a => a.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);

			entity.HasIndex(a => new { a.EmployeeId, a.Date })
				.IsUnique()
				.HasFilter("Status IN (0, 1)"); // EnAttente, Approuve
        });

		// --- Absence ---
        modelBuilder.Entity<Absence>(entity =>
        {
            entity.HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ReviewedBy)
                .WithMany()
                .HasForeignKey(a => a.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);

			entity.HasIndex(a => new { a.EmployeeId, a.Date });
        });

		// --- EmployeePreferences ---
		modelBuilder.Entity<EmployeePreferences>(entity =>
		{
			entity.HasKey(p => p.EmployeeId);
			
			entity.HasOne(p => p.Employee)
				.WithOne()
				.HasForeignKey<EmployeePreferences>(p => p.EmployeeId)
				.OnDelete(DeleteBehavior.Cascade);
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

		// --- Holidays ---
		modelBuilder.Entity<Holiday>(entity =>
		{
			entity.HasOne(h => h.Site)
				.WithMany()
				.HasForeignKey(h => h.SiteId)
				.OnDelete(DeleteBehavior.Restrict);
			
			entity.HasIndex(h => new { h.SiteId, h.Date }).IsUnique();

			entity.HasData([
				new Holiday { Id = 1, SiteId = 1, Date = new DateOnly(2026, 01, 01), Name = "Jour de l'An" },
				new Holiday { Id = 2, SiteId = 1, Date = new DateOnly(2026, 04, 03), Name = "Vendredi saint" },
				new Holiday { Id = 3, SiteId = 1, Date = new DateOnly(2026, 04, 06), Name = "Lundi de Pâques" },
				new Holiday { Id = 4, SiteId = 1, Date = new DateOnly(2026, 05, 18), Name = "Journée nationale des patriotes" },
				new Holiday { Id = 5, SiteId = 1, Date = new DateOnly(2026, 06, 24), Name = "Fête nationale" },
				new Holiday { Id = 6, SiteId = 1, Date = new DateOnly(2026, 07, 01), Name = "Fête du Canada" },
				new Holiday { Id = 7, SiteId = 1, Date = new DateOnly(2026, 09, 07), Name = "Fête du Travail" },
				new Holiday { Id = 8, SiteId = 1, Date = new DateOnly(2026, 10, 12), Name = "Action de grâce" },
				new Holiday { Id = 9, SiteId = 1, Date = new DateOnly(2026, 12, 25), Name = "Noël" },
				new Holiday { Id = 10, SiteId = 1, Date = new DateOnly(2027, 01, 01), Name = "Jour de l'An" },
				new Holiday { Id = 11, SiteId = 1, Date = new DateOnly(2027, 03, 26), Name = "Vendredi saint" },
				new Holiday { Id = 12, SiteId = 1, Date = new DateOnly(2027, 03, 29), Name = "Lundi de Pâques" },
				new Holiday { Id = 13, SiteId = 1, Date = new DateOnly(2027, 05, 24), Name = "Journée nationale des patriotes" },
				new Holiday { Id = 14, SiteId = 1, Date = new DateOnly(2027, 06, 24), Name = "Fête nationale" },
				new Holiday { Id = 15, SiteId = 1, Date = new DateOnly(2027, 07, 01), Name = "Fête du Canada" },
				new Holiday { Id = 16, SiteId = 1, Date = new DateOnly(2027, 09, 06), Name = "Fête du Travail" },
				new Holiday { Id = 17, SiteId = 1, Date = new DateOnly(2027, 10, 11), Name = "Action de grâce" },
				new Holiday { Id = 18, SiteId = 1, Date = new DateOnly(2027, 12, 25), Name = "Noël" },
			]);
		});

		// --- Meetings ---
		modelBuilder.Entity<Meeting>(entity =>
		{
			entity.Property(m => m.Title).HasMaxLength(200);
			entity.Property(m => m.Description).HasMaxLength(2000);
			entity.HasIndex(m => new { m.StartAt, m.EndAt });

			entity.Property(m => m.StartAt)
				.HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

			entity.Property(m => m.EndAt)
				.HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

			entity.HasOne(m => m.Organizer)
                .WithMany()
                .HasForeignKey(m => m.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
		});

		// --- MeetingParticipants ---
		modelBuilder.Entity<MeetingParticipant>(entity =>
		{
			entity.Property(p => p.Color).HasMaxLength(20);
			entity.HasIndex(p => p.EmployeeId );

			entity.HasKey(p => new { p.MeetingId, p.EmployeeId });

			entity.HasOne(p => p.Meeting)
				.WithMany(m => m.Participants)
				.HasForeignKey(p => p.MeetingId)
				.OnDelete(DeleteBehavior.Cascade);
			
			entity.HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
		});

		// --- Notifications ---
		modelBuilder.Entity<Notification>(entity =>
		{
			entity.Property(n => n.Message).HasMaxLength(500);
			entity.Property(n => n.Link).HasMaxLength(200);

			entity.Property(n => n.CreatedAt)
				.HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

			entity.Property(n => n.ReadAt)
				.HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
		
			entity.HasOne(n => n.Recipient)
				.WithMany()
				.HasForeignKey(n => n.RecipientId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasIndex(n => new { n.RecipientId, n.ReadAt });
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