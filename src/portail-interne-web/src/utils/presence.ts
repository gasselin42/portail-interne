import { DaySource, type EffectiveDay } from "../api/calendar"
import { formatTime } from "./date"

export function dayStatus(day: EffectiveDay, now: string): string {
	const absence = day.absences.find((a) => a.inProgress)

	if (absence)
		return `Absent depuis ${formatTime(absence.start)}`

	if (day.source === DaySource.Conge)
		return "En congé aujourd'hui"

	if (day.source === DaySource.Ferie)
		return `Férié : ${day.holidayName}`

	if (day.source === DaySource.NonDefini)
		return "Horaire non défini"

	if (day.startTime === null)
		return "Pas de travail aujourd'hui"

	if (now < day.startTime)
		return `Ta journée commence à ${formatTime(day.startTime)}`

	if (now < day.endTime!)
		return `Au travail jusqu'à ${formatTime(day.endTime!)}`

	return "Ta journée est terminée"
}

export function daySchedule(day: EffectiveDay): string {
	if (day.source === DaySource.Conge)
		return "Congé"

	if (day.source === DaySource.Ferie)
		return `${day.holidayName}`

	if (day.source === DaySource.NonDefini)
		return "Non défini"

	if (day.startTime === null)
		return "Non travaillé"

	return `${formatTime(day.startTime)} – ${formatTime(day.endTime!)}${day.source === DaySource.Ajustement ? " (ajusté)" : ""}`
}
