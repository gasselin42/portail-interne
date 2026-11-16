using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;
using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;
	private readonly AccessScope _scope;
	private readonly SiteClock _clock;

    public EmployeesController(AppDbContext db, AccessScope scope, SiteClock clock)
    {
        _db = db;
		_scope = scope;
		_clock = clock;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmployeeListItemResponse>>> List([FromQuery] string? search, [FromQuery] int? siteId, [FromQuery] string? language)
    {
        var query = _db.Employees
            .AsNoTracking()
            .Where(e => e.IsActive);

        query = EmployeeSearch.Apply(query, search);

		if (siteId != null)
			query = query.Where(e => e.SiteId == siteId);

		if (!string.IsNullOrWhiteSpace(language))
		{
			(List<string> codes, string? error) = LanguageLabels.Normalize([language]);
			if (error is not null)
				return BadRequest(new { message = error });
			query = query.Where(e => e.Languages.Any(l => l.LanguageCode == codes[0]));
		}

        var employees = await query
            .OrderBy(e => e.LastNameKey)
            .ThenBy(e => e.DisplayFirstNameKey)
			.ThenBy(e => e.Id)
            .Select(e => new EmployeeListItemResponse
            {
                Id = e.Id,
                FirstName = e.FirstName,
				PreferredName = e.PreferredName,
                LastName = e.LastName,
                Email = e.Email,
				SiteName = e.Site.Name,
                JobTitle = e.JobTitle,
                Departement = e.Departement,
				OfficeLocation = e.OfficeLocation,
                WorkPhone = e.WorkPhone,
				PhoneExtension = e.PhoneExtension,
				SiteMainPhoneNumber = e.Site.MainPhoneNumber,
                PhotoUrl = e.PhotoFileName == null ? null : PhotoStorage.UrlFor(e.PhotoFileName),
				HireDate = e.HireDate,
				Languages = e.Languages.Select(l => l.LanguageCode).Order().ToList()
            })
            .ToListAsync();

        return Ok(employees);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDetailResponse>> GetEmployeeById(int id)
    {
        var employee = await _db.Employees.AsNoTracking().Include(e => e.Site).Include(e => e.Languages).FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null || !employee.IsActive)
            return NotFound();

        var employeeDetails = new EmployeeDetailResponse
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
			PreferredName = employee.PreferredName,
            LastName = employee.LastName,
            Email = employee.Email,
			SiteName = employee.Site.Name,
            JobTitle = employee.JobTitle,
            Departement = employee.Departement,
			OfficeLocation = employee.OfficeLocation,
            WorkPhone = employee.WorkPhone,
			PhoneExtension = employee.PhoneExtension,
			SiteMainPhoneNumber = employee.Site.MainPhoneNumber,
            ManagerId = employee.ManagerId,
			CanManage = await _scope.CanManageAsync(employee),
            PhotoUrl = employee.PhotoFileName == null
                ? null
                : PhotoStorage.UrlFor(employee.PhotoFileName),
			HireDate = employee.HireDate,
			Languages = employee.Languages.Select(l => l.LanguageCode).Order().ToList()
        };

        if (employee.ManagerId is not null)
        {
            var manager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employee.ManagerId);

            if (manager is not null)
            {
                employeeDetails.ManagerFirstName = manager.FirstName;
                employeeDetails.ManagerLastName = manager.LastName;
            }
        }

        return Ok(employeeDetails);
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<EmployeeLookupResponse>> Lookup(
        [FromQuery] string? search,
        [FromQuery] int limit = 10,
        [FromQuery] int? excludeTeamOf = null,
		[FromQuery] int? siteId = null
		)
    {
        limit = Math.Clamp(limit, 1, 50);

        var query = _db.Employees
            .AsNoTracking()
            .Where(e => e.IsActive);

        query = EmployeeSearch.Apply(query, search);

		if (siteId != null)
			query = query.Where(e => e.SiteId == siteId);

        if (excludeTeamOf is > 0)
        {
            var team = await GetTeamIdsAsync(excludeTeamOf.Value);
            query = query.Where(e => !team.Contains(e.Id));
        }

        var items = await query
            .OrderBy(e => e.LastNameKey)
            .ThenBy(e => e.DisplayFirstNameKey)
            .ThenBy(e => e.Id)
            .Take(limit + 1)
            .Select(e => new EmployeeLookupItem
            {
                Id = e.Id,
                FirstName = e.FirstName,
				PreferredName = e.PreferredName,
                LastName = e.LastName,
                JobTitle = e.JobTitle,
                Departement = e.Departement,
            })
            .ToListAsync();

        var hasMore = items.Count > limit;
        if (hasMore) items.RemoveAt(items.Count - 1);

        return Ok(new EmployeeLookupResponse { Items = items, HasMore = hasMore });
    }

	[HttpGet("birthdays")]
	public async Task<ActionResult<List<BirthdayResponse>>> GetBirthdays()
	{
		DateOnly today = await _clock.TodayForAsync(_scope.CallerEmployeeId);
		DateOnly monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
		int siteId = await _scope.CallerSiteIdAsync();

		var birthdays = await _db.EmployeePrivateInfos
			.AsNoTracking()
			.Include(e => e.Employee)
			.Where(e => e.ShowBirthday && e.BirthMonth != null && e.BirthDay != null
				&& e.Employee.IsActive && e.Employee.SiteId == siteId)
			.ToListAsync();

		var week = Enumerable.Range(0, 7).Select(i => monday.AddDays(i)).ToList();
		
		return birthdays
			.Select(b => new
			{
				Info = b,
				Date = week.FirstOrDefault(d => BirthdayRules.OccursOn(b.BirthMonth!.Value, b.BirthDay!.Value, d)),
			})
			.Where(x => x.Date != default)
			.OrderBy(x => x.Date)
			.ThenBy(x => x.Info.Employee.LastNameKey)
			.ThenBy(x => x.Info.Employee.DisplayFirstNameKey)
			.Select(x => new BirthdayResponse
			{
				EmployeeId = x.Info.EmployeeId,
				FirstName = x.Info.Employee.PreferredName ?? x.Info.Employee.FirstName,
				LastName = x.Info.Employee.LastName,
				PhotoUrl = x.Info.Employee.PhotoFileName == null ? null : PhotoStorage.UrlFor(x.Info.Employee.PhotoFileName),
				Date = x.Date,
			})
			.ToList();
	}

	[HttpGet("{id:int}/private")]
	public async Task<ActionResult<PrivateInfoResponse>> GetPrivateInfos(int id)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
            return NotFound();

		bool canSeeAll = employee.Id == _scope.CallerEmployeeId || await _scope.CanAdministerSiteAsync(employee.SiteId);
		bool isDirectManager = employee.ManagerId == _scope.CallerEmployeeId;

		if (!canSeeAll && (!employee.IsActive || !isDirectManager))
			return NotFound();
	
		PrivateDetailsResponse? details = null;
		if (canSeeAll)
		{
			details = await _db.EmployeePrivateInfos
				.Where(p => p.EmployeeId == id)
				.Select(p => new PrivateDetailsResponse
				{
					PersonalMobilePhone = p.PersonalMobilePhone,
					PersonalEmail = p.PersonalEmail,
					AddressLine1 = p.AddressLine1,
					AddressLine2 = p.AddressLine2,
					City = p.City,
					Region = p.Region,
					PostalCode = p.PostalCode,
					Country = p.Country,
					BirthMonth = p.BirthMonth,
					BirthDay = p.BirthDay,
					ShowBirthday = p.ShowBirthday
				})
				.FirstOrDefaultAsync()
				?? new PrivateDetailsResponse();
		}

		var contacts = await _db.EmergencyContacts
			.Where(c => c.EmployeeId == id)
			.Select(c => new EmergencyContactResponse
			{
				Name = c.Name,
				Relationship = c.Relationship,
				PhoneNumber = c.PhoneNumber,
				SortOrder = c.SortOrder
			})
			.OrderBy(c => c.SortOrder)
			.ToListAsync();

		return Ok(new PrivateInfoResponse { Details = details, EmergencyContacts = contacts });
	}

	[HttpPut("{id:int}/profile")]
	public async Task<IActionResult> ModifyProfile(int id, [FromBody] UpdateProfileRequest request)
	{
		var employee = await _db.Employees.Include(e => e.Site).Include(e => e.Languages).FirstOrDefaultAsync(e => e.Id == id);

		if (employee is null || !employee.IsActive)
			return NotFound();

		if (id != _scope.CallerEmployeeId && !await _scope.CanAdministerSiteAsync(employee.SiteId))
			return NotFound();

		var details = request.Details!;
		
		var preferredName = string.IsNullOrWhiteSpace(request.PreferredName) ? null : request.PreferredName.Trim();

		var (codes, languageError) = LanguageLabels.Normalize(request.Languages);

		if (languageError is not null)
			return BadRequest(new { message = languageError });

		var country = string.IsNullOrWhiteSpace(details.Country) ? null : details.Country.Trim().ToUpperInvariant();

		if (country is not null && !PhoneNumberRules.IsKnownCountry(country))
			return BadRequest(new { message = "Pays inconnu. Utilise un code à deux lettres, par exemple « CA » ou « FR »." });

		var line1 = string.IsNullOrWhiteSpace(details.AddressLine1) ? null : details.AddressLine1.Trim();
		var line2 = string.IsNullOrWhiteSpace(details.AddressLine2) ? null : details.AddressLine2.Trim();
		var city = string.IsNullOrWhiteSpace(details.City) ? null : details.City.Trim();
		var region = string.IsNullOrWhiteSpace(details.Region) ? null : details.Region.Trim();
		var (postal, postalError) = PostalCodeRules.Normalize(details.PostalCode, country ?? "");

		if (postalError is not null)
			return BadRequest(new { message = postalError });

		string?[] address = [line1, line2, city, region, postal, country];

		if (address.Any(f => f is not null))
		{
			var missing = new (string? Value, string Label)[] { (line1, "adresse"), (city, "ville"), (country, "pays") }
				.Where(f => f.Value is null)
				.Select(f => f.Label)
				.ToList();

			if (missing.Count > 0)
				return BadRequest(new { message = $"Adresse personnelle incomplète : {string.Join(", ", missing)} manquant." });
		}

		var (cellphone, phoneError) = PhoneNumberRules.Normalize(details.PersonalMobilePhone, country ?? employee.Site.CountryCode);

		if (phoneError is not null)
			return BadRequest(new { message = "Numéro de cellulaire personnel invalide pour ce pays." });

		var email = string.IsNullOrWhiteSpace(details.PersonalEmail) ? null : details.PersonalEmail.Trim();

		if (email is not null && new EmailAddressAttribute().IsValid(email) == false)
			return BadRequest(new { message = "Email personnel invalide." });

		var birthdayError = BirthdayRules.Validate(details.BirthMonth, details.BirthDay);

		if (birthdayError is not null)
			return BadRequest(new { message = birthdayError });

		var contacts = new List<EmergencyContact>();
		for (var i = 0; i < request.EmergencyContacts.Count; i++)
		{
			var c = request.EmergencyContacts[i];
			var (number, error) = PhoneNumberRules.Normalize(c.PhoneNumber, employee.Site.CountryCode);
			if (error is not null)
				return BadRequest(new { message = $"Contact d'urgence {i + 1} : {error}" });

			contacts.Add(new EmergencyContact { EmployeeId = employee.Id, Name = c.Name.Trim(), Relationship = c.Relationship.Trim(), PhoneNumber = number!, SortOrder = i + 1 });
		}

		var privateInfos = await _db.EmployeePrivateInfos.FirstOrDefaultAsync(e => e.EmployeeId == employee.Id) ?? _db.EmployeePrivateInfos.Add(new EmployeePrivateInfo() { EmployeeId = employee.Id }).Entity;

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

		_db.EmergencyContacts.RemoveRange(await _db.EmergencyContacts.Where(c => c.EmployeeId == employee.Id).ToListAsync());
		_db.EmergencyContacts.AddRange(contacts);

		await _db.SaveChangesAsync();

		return NoContent();
	}

    private async Task<HashSet<int>> GetTeamIdsAsync(int rootId)
    {
        // Toutes les paires (Id, ManagerId), y compris les employés INACTIFS :
        // un inactif peut avoir des subordonnés actifs, la chaîne doit passer par lui.
        var links = await _db.Employees
            .AsNoTracking()
            .Select(e => new { e.Id, e.ManagerId })
            .ToListAsync();

        // Pour chaque manager, la liste de ses subordonnés directs.
        var reportsByManager = links
            .Where(l => l.ManagerId is not null)
            .ToLookup(l => l.ManagerId!.Value, l => l.Id);

        // Parcours en largeur; le HashSet évite de tourner en rond si la base contenait une boucle.
        var team = new HashSet<int> { rootId };

        var queue = new Queue<int>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var subordinates = reportsByManager[id];
            foreach (var sub in subordinates)
            {
                if (team.Add(sub))
                    queue.Enqueue(sub);
            }
        }

        return team;
    }
}

