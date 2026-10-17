using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>
/// La vraie API, démarrée en mémoire pour un test :
/// base SQLite neuve (en mémoire), horloge fixe, équipe de test connue.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
	/// <summary>« Maintenant » pour tous les tests : mercredi 14 octobre 2026, midi à Toronto.</summary>
	public static readonly DateTimeOffset Now = new(2026, 10, 14, 16, 0, 0, TimeSpan.Zero);
	public static readonly DateOnly Today = new(2026, 10, 14);

	public const string Password = "Test123!";

	// La base en mémoire n'existe que tant que cette connexion reste ouverte.
	private readonly SqliteConnection _connection = new("Data Source=:memory:");

	public TestTeam Team { get; private set; } = null!;

	public ApiFactory()
	{
		_connection.Open();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		// Ni seed de développement, ni Swagger, ni secrets utilisateur.
		builder.UseEnvironment("Testing");
		// UseSetting : lu dès le démarrage de Program (vérification de la clé JWT).
		builder.UseSetting("Jwt:Key", "cle-reservee-aux-tests-automatises-0123456789");

		builder.ConfigureServices(services =>
		{
			services.RemoveAll<DbContextOptions<AppDbContext>>();
			services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
			services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

			services.RemoveAll<TimeProvider>();
			services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
		});
	}

	/// <summary>Crée le schéma (migrations) et l'équipe de test. À appeler au début de chaque test.</summary>
	public ApiFactory Start()
	{
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		db.Database.Migrate();
		Team = TestTeam.Seed(db);
		return this;
	}

	/// <summary>Un client HTTP connecté avec le compte donné (token JWT réel).</summary>
	public async Task<HttpClient> LoginAsync(string email)
	{
		var client = CreateClient();
		var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
		res.EnsureSuccessStatusCode();

		var login = await res.Content.ReadFromJsonAsync<LoginResponse>();
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
		return client;
	}

	/// <summary>Accès direct à la base, pour préparer un cas que l'API refuserait de créer.</summary>
	public void WithDb(Action<AppDbContext> action)
	{
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		action(db);
		db.SaveChanges();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing)
			_connection.Dispose();
	}
}

/// <summary>Horloge figée : « aujourd'hui » ne change jamais pendant les tests.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
	public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>
/// Alex (Admin) ; Mark (manager de Sam) ; Sam ; Jane (dans aucune équipe).
/// Tous avec le mot de passe <see cref="ApiFactory.Password"/>, sans changement forcé.
/// </summary>
public sealed record TestTeam(Employee Alex, Employee Mark, Employee Sam, Employee Jane)
{
	public static TestTeam Seed(AppDbContext db)
	{
		var passwords = new PasswordService();

		Employee Create(string firstName, string email, Role role) => new()
		{
			FirstName = firstName,
			LastName = "Test",
			Email = email,
			Departement = Departement.TI,
			AnnualLeaveDays = 20,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			UserAccount = new UserAccount
			{
				Email = email,
				PasswordHash = passwords.Hash(ApiFactory.Password),
				Role = role,
				MustChangePassword = false,
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
			},
		};

		var alex = Create("Alex", "alex@test.local", Role.Admin);
		var mark = Create("Mark", "mark@test.local", Role.Employee);
		var sam = Create("Sam", "sam@test.local", Role.Employee);
		var jane = Create("Jane", "jane@test.local", Role.Employee);

		db.Employees.AddRange(alex, mark, sam, jane);
		db.SaveChanges();

		sam.ManagerId = mark.Id;
		db.SaveChanges();

		return new TestTeam(alex, mark, sam, jane);
	}
}
