using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Phase 3.1, étape 3 : coordonnées personnelles et contacts d'urgence dans des tables à part (R28),
/// et code postal validé selon le pays (Canada, États-Unis, France ; texte libre ailleurs).
/// </summary>
public class PrivateInfoTests
{
	// ─── PostalCodeRules.Normalize ─────────────────────────────────────────

	[Theory]
	[InlineData("H2X 1Y4", "CA", "H2X 1Y4")]
	[InlineData("h2x1y4", "CA", "H2X 1Y4")]
	[InlineData("  k1a 0b1 ", "CA", "K1A 0B1")]
	[InlineData("90210", "US", "90210")]
	[InlineData("90210-1234", "US", "90210-1234")]
	[InlineData("90210 1234", "US", "90210-1234")]
	[InlineData("902101234", "US", "90210-1234")]
	[InlineData("75001", "FR", "75001")]
	[InlineData(" 97400 ", "FR", "97400")]
	[InlineData("sw1a 1aa", "GB", "SW1A 1AA")] // pays sans format connu : texte libre, nettoyé
	public void Un_code_valide_est_mis_en_forme(string input, string country, string expected)
	{
		Assert.Equal((expected, null), PostalCodeRules.Normalize(input, country));
	}

	[Theory]
	[InlineData("H2X-1Y4", "CA")]
	[InlineData("H2X 1Y", "CA")]
	[InlineData("D2X 1Y4", "CA")] // D n'est jamais permis
	[InlineData("W2X 1Y4", "CA")] // W jamais en 1re position
	[InlineData("H2O 1Y4", "CA")] // O jamais permis
	[InlineData("H2X 1Y4", "US")]
	[InlineData("9021", "US")]
	[InlineData("90210-123", "US")]
	[InlineData("7500", "FR")]
	[InlineData("75 001", "FR")]
	public void Un_code_invalide_donne_un_message(string input, string country)
	{
		Assert.Equal((null, "Code postal invalide pour ce pays."), PostalCodeRules.Normalize(input, country));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Sans_code_ni_code_ni_erreur(string? input)
	{
		Assert.Equal((null, null), PostalCodeRules.Normalize(input, "CA"));
	}

	// ─── Tables ────────────────────────────────────────────────────────────

	[Fact]
	public void Un_employe_a_au_plus_une_ligne_de_coordonnees_et_deux_contacts()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;

		api.WithDb(db =>
		{
			db.EmployeePrivateInfos.Add(new EmployeePrivateInfo { EmployeeId = team.Sam.Id, City = "Montréal", UpdatedAt = DateTime.UtcNow });
			db.EmergencyContacts.Add(Contact(team.Sam.Id, 1));
			db.EmergencyContacts.Add(Contact(team.Sam.Id, 2));
		});

		Assert.Throws<DbUpdateException>(() => api.WithDb(db => db.EmergencyContacts.Add(Contact(team.Sam.Id, 3)))); // contrainte : 1 ou 2
		Assert.Throws<DbUpdateException>(() => api.WithDb(db => db.EmergencyContacts.Add(Contact(team.Sam.Id, 2)))); // index unique
		Assert.Throws<DbUpdateException>(() => api.WithDb(db => db.EmergencyContacts.Add(Contact(team.Sam.Id, 0))));
		Assert.Throws<DbUpdateException>(() => api.WithDb(db =>
			db.EmployeePrivateInfos.Add(new EmployeePrivateInfo { EmployeeId = team.Sam.Id, UpdatedAt = DateTime.UtcNow })));
		api.WithDb(db =>
		{
			Assert.Equal([1, 2], db.EmergencyContacts.Where(c => c.EmployeeId == team.Sam.Id).OrderBy(c => c.SortOrder).Select(c => c.SortOrder).ToList());
			Assert.Equal("Montréal", db.EmployeePrivateInfos.Single().City);
		});
	}

	[Fact]
	public void Deux_employes_peuvent_avoir_chacun_leur_contact_1()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;

		api.WithDb(db =>
		{
			db.EmergencyContacts.Add(Contact(team.Sam.Id, 1));
			db.EmergencyContacts.Add(Contact(team.Jane.Id, 1));
		});

		api.WithDb(db => Assert.Equal(2, db.EmergencyContacts.Count()));
	}

	[Fact]
	public void Supprimer_un_employe_supprime_ses_coordonnees_et_ses_contacts()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var nouveauId = 0;
		api.WithDb(db =>
		{
			var nouveau = new Employee
			{
				FirstName = "Nora",
				LastName = "Test",
				Email = "nora@test.local",
				Departement = Departement.TI,
				AnnualLeaveDays = 20,
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				SiteId = 1,
			};
			db.Employees.Add(nouveau);
			db.SaveChanges();
			nouveauId = nouveau.Id;
			db.EmployeePrivateInfos.Add(new EmployeePrivateInfo { EmployeeId = nouveauId, City = "Laval", UpdatedAt = DateTime.UtcNow });
			db.EmergencyContacts.Add(Contact(nouveauId, 1));
			db.EmergencyContacts.Add(Contact(team.Sam.Id, 1)); // celui d'un autre reste
		});

		api.WithDb(db => db.Employees.Remove(db.Employees.Find(nouveauId)!));

		api.WithDb(db =>
		{
			Assert.False(db.EmployeePrivateInfos.Any());
			Assert.Equal(team.Sam.Id, db.EmergencyContacts.Single().EmployeeId);
		});
	}

	private static EmergencyContact Contact(int employeeId, int sortOrder) => new()
	{
		EmployeeId = employeeId,
		Name = "Contact " + sortOrder,
		Relationship = "Conjoint",
		PhoneNumber = "+15145550000",
		SortOrder = sortOrder,
	};
}
