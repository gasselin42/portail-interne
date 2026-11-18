using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public class ProfileService
{
	private readonly AppDbContext _db;

	public ProfileService(AppDbContext db)
	{
		_db = db;
	}

	public async Task<string?> ApplyAsync(Employee employee, UpdateProfileRequest request)
	{
		var details = request.Details!;
		
		var preferredName = string.IsNullOrWhiteSpace(request.PreferredName) ? null : request.PreferredName.Trim();

		var (codes, languageError) = LanguageLabels.Normalize(request.Languages);

		if (languageError is not null)
			return languageError;

		var country = string.IsNullOrWhiteSpace(details.Country) ? null : details.Country.Trim().ToUpperInvariant();

		if (country is not null && !PhoneNumberRules.IsKnownCountry(country))
			return "Pays inconnu. Utilise un code à deux lettres, par exemple « CA » ou « FR ».";

		var line1 = string.IsNullOrWhiteSpace(details.AddressLine1) ? null : details.AddressLine1.Trim();
		var line2 = string.IsNullOrWhiteSpace(details.AddressLine2) ? null : details.AddressLine2.Trim();
		var city = string.IsNullOrWhiteSpace(details.City) ? null : details.City.Trim();
		var region = string.IsNullOrWhiteSpace(details.Region) ? null : details.Region.Trim();
		var (postal, postalError) = PostalCodeRules.Normalize(details.PostalCode, country ?? "");

		if (postalError is not null)
			return postalError;

		string?[] address = [line1, line2, city, region, postal, country];

		if (address.Any(f => f is not null))
		{
			var missing = new (string? Value, string Label)[] { (line1, "adresse"), (city, "ville"), (country, "pays") }
				.Where(f => f.Value is null)
				.Select(f => f.Label)
				.ToList();

			if (missing.Count > 0)
				return $"Adresse personnelle incomplète : {string.Join(", ", missing)} manquant.";
		}

		var (cellphone, phoneError) = PhoneNumberRules.Normalize(details.PersonalMobilePhone, country ?? employee.Site.CountryCode);

		if (phoneError is not null)
			return "Numéro de cellulaire personnel invalide pour ce pays.";

		var email = string.IsNullOrWhiteSpace(details.PersonalEmail) ? null : details.PersonalEmail.Trim();

		if (email is not null && new EmailAddressAttribute().IsValid(email) == false)
			return "Email personnel invalide.";

		var birthdayError = BirthdayRules.Validate(details.BirthMonth, details.BirthDay);

		if (birthdayError is not null)
			return birthdayError;

		var contacts = new List<EmergencyContact>();
		for (var i = 0; i < request.EmergencyContacts.Count; i++)
		{
			var c = request.EmergencyContacts[i];
			var (number, error) = PhoneNumberRules.Normalize(c.PhoneNumber, employee.Site.CountryCode);
			if (error is not null)
				return $"Contact d'urgence {i + 1} : {error}";

			contacts.Add(new EmergencyContact { Employee = employee, Name = c.Name.Trim(), Relationship = c.Relationship.Trim(), PhoneNumber = number!, SortOrder = i + 1 });
		}

		var privateInfos = await _db.EmployeePrivateInfos.FirstOrDefaultAsync(e => e.Employee == employee) ?? _db.EmployeePrivateInfos.Add(new EmployeePrivateInfo() { Employee = employee }).Entity;

		privateInfos.PersonalMobilePhone = cellphone;
		privateInfos.PersonalEmail = email;
		privateInfos.AddressLine1 = line1;
		privateInfos.AddressLine2 = line2;
		privateInfos.City = city;
		privateInfos.Region = region;
		privateInfos.PostalCode = postal;
		privateInfos.Country = country;
		privateInfos.BirthDay = details.BirthDay;
		privateInfos.BirthMonth = details.BirthMonth;
		privateInfos.ShowBirthday = details.BirthDay is not null && details.BirthMonth is not null && details.ShowBirthday;
		privateInfos.UpdatedAt = DateTime.UtcNow;
		
		employee.PreferredName = preferredName;
		
		employee.Languages.RemoveAll(l => !codes.Contains(l.LanguageCode));

		foreach (var code in codes.Where(c => !employee.Languages.Any(l => l.LanguageCode == c)))
			employee.Languages.Add(new EmployeeLanguage { LanguageCode = code });

		_db.EmergencyContacts.RemoveRange(await _db.EmergencyContacts.Where(c => c.Employee == employee).ToListAsync());
		_db.EmergencyContacts.AddRange(contacts);

		return null;
	}
}