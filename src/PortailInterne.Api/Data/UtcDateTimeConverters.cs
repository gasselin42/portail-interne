using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PortailInterne.Api.Data;

/// <summary>
/// Les DateTime sont enregistrés en UTC, mais SQLite ne garde pas le « Kind » :
/// à la lecture, on les remarque UTC pour que le JSON finisse par « Z ».
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
	public UtcDateTimeConverter()
		: base(
			v => v, // écriture
			v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) // lecture
	{ // Tout le travail est fait par la base
	}
}

/// <summary>La même chose pour les DateTime facultatifs.</summary>
public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
	public NullableUtcDateTimeConverter()
		: base(
			v => v,
			v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
	{
	}
}
