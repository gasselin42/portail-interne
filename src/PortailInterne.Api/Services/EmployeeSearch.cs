using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public static class EmployeeSearch
{
	public static IQueryable<Employee> Apply(IQueryable<Employee> query, string? search)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            string key = TextKey.From(search);
			string emailSearch = search.Trim().ToLowerInvariant();

           	var departements = DepartementLabels.All
				.Where(d => TextKey.From(d.Value).Contains(key) || TextKey.From(d.Key.ToString()) == key)
				.Select(d => d.Key)
				.ToList();

            query = query.Where(e =>
                e.FirstNameKey.Contains(key)
                || e.LastNameKey.Contains(key)
                || e.JobTitleKey.Contains(key)
                || e.Email.Contains(emailSearch)
                || departements.Contains(e.Departement)
				|| (e.FirstNameKey + " " + e.LastNameKey).Contains(key)
				|| (e.LastNameKey + " " + e.FirstNameKey).Contains(key));
        }

        return query;
    }
}