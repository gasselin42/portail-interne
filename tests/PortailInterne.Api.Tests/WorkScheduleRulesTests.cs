using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>Règle R11 : un horaire type valide (un bloc par jour, sans passer minuit).</summary>
public class WorkScheduleRulesTests
{
	[Fact]
	public void Un_horaire_lundi_au_vendredi_est_valide()
	{
		var days = new[]
		{
			Day(DayOfWeek.Monday, "08:00", "17:00", 30),
			Day(DayOfWeek.Tuesday, "08:00", "17:00", 30),
			Day(DayOfWeek.Wednesday, "08:00", "17:00", 30),
			Day(DayOfWeek.Thursday, "08:00", "17:00", 30),
			Day(DayOfWeek.Friday, "08:00", "12:00", 0),
		};

		Assert.Null(WorkScheduleRules.Validate(days));
	}

	[Fact]
	public void Un_horaire_sans_jour_travaille_est_refuse()
	{
		Assert.Equal(
			"Un horaire doit avoir au moins un jour travaillé.",
			WorkScheduleRules.Validate([]));
	}

	[Fact]
	public void Un_jour_en_double_est_refuse()
	{
		var days = new[]
		{
			Day(DayOfWeek.Monday, "08:00", "12:00", 0),
			Day(DayOfWeek.Monday, "13:00", "17:00", 0),
		};

		Assert.Equal("Lundi apparaît deux fois.", WorkScheduleRules.Validate(days));
	}

	[Theory]
	[InlineData("17:00", "08:00")] // fin avant le début (quart de nuit, exclu par R11)
	[InlineData("08:00", "08:00")] // journée de 0 minute
	public void La_fin_doit_etre_apres_le_debut(string start, string end)
	{
		var days = new[] { Day(DayOfWeek.Tuesday, start, end, 0) };

		Assert.Equal("Mardi : la fin doit être après le début.", WorkScheduleRules.Validate(days));
	}

	[Fact]
	public void Une_pause_negative_est_refusee()
	{
		var days = new[] { Day(DayOfWeek.Wednesday, "08:00", "17:00", -15) };

		Assert.Equal("Mercredi : la pause ne peut pas être négative.", WorkScheduleRules.Validate(days));
	}

	[Theory]
	[InlineData(240)] // pause aussi longue que la journée (8 h à 12 h)
	[InlineData(300)] // pause plus longue que la journée
	public void La_pause_doit_etre_plus_courte_que_la_journee(int breakMinutes)
	{
		var days = new[] { Day(DayOfWeek.Thursday, "08:00", "12:00", breakMinutes) };

		Assert.Equal("Jeudi : la pause doit être plus courte que la journée.", WorkScheduleRules.Validate(days));
	}

	[Fact]
	public void Une_pause_d_une_minute_de_moins_que_la_journee_est_acceptee()
	{
		var days = new[] { Day(DayOfWeek.Friday, "08:00", "12:00", 239) };

		Assert.Null(WorkScheduleRules.Validate(days));
	}

	private static WorkScheduleDay Day(DayOfWeek day, string start, string end, int breakMinutes) => new()
	{
		DayOfWeek = day,
		StartTime = TimeOnly.Parse(start),
		EndTime = TimeOnly.Parse(end),
		BreakMinutes = breakMinutes,
	};
}
