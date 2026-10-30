import { daySchedule } from "../../utils/presence"
import { DaySource, type EffectiveDay } from "../../api/calendar"
import { absenceTitle } from "../../utils/timeGrid"
import { formatTime } from "../../utils/date"

/** La couleur de la ligne principale, selon la source du jour. */
function scheduleClass(day: EffectiveDay): string {
	if (day.source === DaySource.Conge)
		return "inline-block rounded-full bg-sky-100 px-2 py-0.5 text-xs font-medium text-sky-800"

	if (day.source === DaySource.Ferie)
		return "text-sm font-medium text-violet-700"

	if (day.startTime === null)
		return "text-sm text-slate-400"

	return "text-sm font-medium text-slate-900"
}

export function TeamDayCell({ day }: { day: EffectiveDay}) {
	return (
		<>
			<p>
				<span className={scheduleClass(day)}>{daySchedule(day)}</span>
			</p>
			{day.absences.map((absence) => (
				<p
					key={absence.id}
					className={`mt-1 text-xs ${absence.inProgress ? "font-semibold text-amber-800" : "text-amber-700"}`}
				>
					{absenceTitle(absence)}
				</p>
			))}
			{day.pendingAbsences.map((absence) => (
				<p
					key={absence.id}
					className={`mt-1 text-xs border border-dashed border-amber-300 rounded px-1 ${absence.inProgress ? "font-semibold text-amber-800" : "text-amber-700"}`}
				>
					{absenceTitle(absence)}
				</p>
			))}
			{day.pendingAdjustment && day.pendingAdjustment.startTime && day.pendingAdjustment.endTime &&
				<p className="mt-1 text-xs text-slate-500 rounded border border-dashed border-slate-200 px-1">
					Demandé · {formatTime(day.pendingAdjustment.startTime)} – {formatTime(day.pendingAdjustment.endTime)}
				</p>
			}
		</>
	)
}