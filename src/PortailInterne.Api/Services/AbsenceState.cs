using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

/// <summary>Où en est une absence : ses heures effectives, et si elle est en cours (R24).</summary>
public record AbsenceStatus(TimeOnly Start, TimeOnly? End, bool InProgress, bool ReturnConfirmed);

public static class AbsenceState
{
	public static AbsenceStatus Compute(Absence absence, DateOnly today, TimeOnly now, TimeOnly? workdayEnd)
	{
		// 1. Le départ effectif : réel s'il existe, sinon prévu
		var start = (absence.StartedAt ?? absence.PlannedStart)!.Value;

		// 2. Retour cliqué : terminée, retour confirmé
		if (absence.EndedAt is not null)
			return new AbsenceStatus(start, absence.EndedAt, InProgress: false, ReturnConfirmed: true);

		// 3. La fin « de principe », sans retour cliqué :
		//    - parti avec « Je pars » : absent jusqu'à la fin de la journée (on attend son retour) ;
		//    - absence prévue jamais démarrée : elle suit le plan.
		//    Un jour non travaillé n'a pas de fin de journée : minuit (MaxValue).
		var endOfDay = workdayEnd ?? TimeOnly.MaxValue;
		var leftWithButton = absence.StartedAt is not null;
		var expectedEnd = leftWithButton ? endOfDay : absence.PlannedEnd ?? endOfDay;

		// 4. En cours : c'est aujourd'hui, le départ est passé, la fin de principe pas encore
		var inProgress = (today == absence.Date && start <= now && now < expectedEnd);
		if (inProgress)
			return new AbsenceStatus(start, End: null, InProgress: true, ReturnConfirmed: false);
		
		// 5. Terminée (ou pas encore commencée) : selon le plan, ou « retour non confirmé »
		//    si Sam est parti avec « Je pars » sans jamais cliquer « Je suis de retour »
		return new AbsenceStatus(start, expectedEnd, InProgress: false, ReturnConfirmed: !leftWithButton);
	}
}
