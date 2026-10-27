import { DaySource, type CalendarAbsence, type EffectiveDay } from "../api/calendar"
import { LeaveStatus } from "../api/leaves"
import { CalendarHourRange, type CalendarHourRangeId } from "../api/preferences"
import { formatTime } from "./date"

export const HOUR_HEIGHT = 48 // pixels par heure

export function timeToPx(time: string, startHour: number): number {
	return toMinutes(time) * (HOUR_HEIGHT / 60) - startHour * HOUR_HEIGHT
}

function toMinutes(time: string): number {
	const [h, m] = time.split(":").map(Number)
	return (h * 60 + m)
}

export function dayNote(day: EffectiveDay): string | null {
	if (day.source === DaySource.Ajustement) {
		if (day.startTime && day.endTime)
			return `Ajusté · ${formatTime(day.startTime)} – ${formatTime(day.endTime)}`
		return "Journée retirée"
	}

	const pending = day.pendingAdjustment
	if (pending && pending.startTime && pending.endTime)
		return `Ajustement demandé · ${formatTime(pending.startTime)} – ${formatTime(pending.endTime)}`

	return null
}

export function hourRange(days: EffectiveDay[], range: CalendarHourRangeId): { start: number, end: number } {
	if (range === CalendarHourRange.FullDay)
		return { start: 0, end: 24 }

	const minutes: number[] = []

	function add(time: string | null) {
		if (time)
			minutes.push(toMinutes(time))
	}

	for (const day of days) {
		add(day.startTime)
		add(day.endTime)
		add(day.pendingAdjustment?.startTime ?? null)
		add(day.pendingAdjustment?.endTime ?? null)

		for (const absence of [...day.absences, ...day.pendingAbsences]) {
			add(absence.start)
			add(absence.end)
			add(absence.plannedStart)
			add(absence.plannedEnd)
		}
	}
	
	if (minutes.length === 0)
		return { start: 7, end: 19}
	
	return {
		start: Math.max(0, Math.floor(Math.min(...minutes) / 60) - 1),
		end: Math.min(24, Math.ceil(Math.max(...minutes) / 60) + 1),
	}
}

export function absenceTitle(absence: CalendarAbsence): string {
	const label = absence.status === LeaveStatus.EnAttente ? "Absence demandée" : "Absence"
	
	let result = absence.end === null
		? `Absent depuis ${formatTime(absence.start)}`
		: `${label} · ${formatTime(absence.start)} – ${formatTime(absence.end)}`

	if (absence.reason)
		result += ` · ${absence.reason}`

	return result
}
