using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Data;

public class DbSeeder
{
    public static void Seed(AppDbContext db)
	{
		if (!db.UserAccounts.Any())
        {
            var passwords = new PasswordService();

            var adminEmployee = new Employee
            {
                FirstName = "Alex",
                LastName = "Admin",
                Email = "admin@portail.local",
                JobTitle = "Administrateur système",
                Departement = Departement.TI,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UserAccount = new UserAccount
                {
                    Email = "admin@portail.local",
                    PasswordHash = passwords.Hash("Admin123!"),
                    Role = Role.Admin,
                    MustChangePassword = false, // pratique pour tester plus tard
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var samEmployee = new Employee
            {
                FirstName = "Sam",
                LastName = "Tremblay",
                Email = "sam@portail.local",
                JobTitle = "Analyste",
                Departement = Departement.RH,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UserAccount = new UserAccount
                {
                    Email = "sam@portail.local",
                    PasswordHash = passwords.Hash("Sam123!"),
                    Role = Role.Employee,
                    MustChangePassword = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            db.Employees.AddRange(adminEmployee, samEmployee);
            db.SaveChanges();
        }

        if (!db.LeaveRequests.Any())
        {
            var admin = db.Employees.First(e => e.Email == "admin@portail.local");
            var sam = db.Employees.First(e => e.Email == "sam@portail.local");

            if (sam.ManagerId is null)
                sam.ManagerId = admin.Id;

            db.LeaveRequests.AddRange(
                new LeaveRequest
                {
                    EmployeeId = sam.Id,
                    StartDate = new DateTime(2026, 10, 6),
                    EndDate = new DateTime(2026, 10, 8),
                    Type = LeaveType.Conge,
                    Status = LeaveStatus.EnAttente,
                    Reason = "Vacances",
                    CreatedAt = DateTime.UtcNow
                },
                new LeaveRequest
                {
                    EmployeeId = sam.Id,
                    StartDate = new DateTime(2026, 10, 13),
                    EndDate = new DateTime(2026, 10, 13),
                    Type = LeaveType.Maladie,
                    Status = LeaveStatus.Approuve,
                    Reason = "Rendez-vous",
                    ReviewedById = admin.Id,
                    ReviewedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });

            db.SaveChanges();
        }
    }
}

