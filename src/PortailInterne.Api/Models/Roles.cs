namespace PortailInterne.Api.Models;

/// <summary>Valeurs pour [Authorize(Roles = …)].</summary>
public static class Roles
{
	public const string Admins = "Admin,GlobalAdmin";
	public const string GlobalAdmin = "GlobalAdmin";
}
