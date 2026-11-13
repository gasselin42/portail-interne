import { DaySource, type CalendarAbsence, type EffectiveDay } from "../api/calendar"
import { LeaveStatus } from "../api/leaves"
import { CalendarDensity, CalendarHourRange, type CalendarDensityId, type CalendarHourRangeId } from "../api/preferences"
import { formatTime } from "./date"

export const HOUR_HEIGHTS: Record<CalendarDensityId, number> = {
	[CalendarDensity.Normal]: 48,
	[CalendarDensity.Compact]: 32,
	[CalendarDensity.Airy]: 72,
} // pixels par heure

export function timeToPx(time: string, startHour: number, density: CalendarDensityId): number {
	return minutesToPx(toMinutes(time), startHour, density)
}

export function minutesToPx(minutes: number, startHour: number, density: CalendarDensityId): number {
	return minutes * HOUR_HEIGHTS[density] / 60 - startHour * HOUR_HEIGHTS[density]
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

export function hourRange(days: EffectiveDay[], range: CalendarHourRangeId, slices: { from: number, to: number }[]): { start: number, end: number } {
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

	for (const slice of slices) {
		minutes.push(slice.from, slice.to)
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

/** Range les éléments qui se chevauchent côte à côte : sous-colonne et nombre de sous-colonnes du groupe. */
export function layoutOverlaps<T extends { from: number; to: number }>(items: T[]): (T & { column: number; columns: number })[] {
	const sorted = [...items].sort((a, b) => a.from - b.from || b.to - a.to) // En cas d'égalité des from, = 0, donc on passe au 2e critère
	const result: (T & { column: number; columns: number })[] = []

	// Un "groupe", c'est une suite de meetings reliés par des chevauchements
	// Exemple : Des meetings de 9h à 10h, de 9h30 à 11h et de 10h30 à 12h forment un seul groupe de 9h à 12h,
	//			 ne prenant que 2 colonnes, comme le 1er et le 3e meeting ne se chevauchent pas
	let group: (T & { column: number; columns: number })[] = []
	let columnEnds: number[] = [] // fin du dernier élément de chaque sous-colonne
	let groupEnd = -1

	function closeGroup() {
		for (const item of group) item.columns = columnEnds.length
		result.push(...group)
		group = []
		columnEnds = []
	}

	for (const item of sorted) {
		// Commence après tout le groupe : le groupe est terminé
		if (item.from >= groupEnd) closeGroup()

		// Première sous-colonne libérée à temps, sinon une nouvelle
		let column = columnEnds.findIndex((end) => end <= item.from)
		if (column === -1) {
			column = columnEnds.length
			columnEnds.push(item.to)
		} else {
			columnEnds[column] = item.to
		}

		group.push({ ...item, column, columns: 0 })
		groupEnd = Math.max(groupEnd, item.to)
	}
	closeGroup()

	return result
}
