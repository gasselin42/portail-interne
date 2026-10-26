import type { WorkScheduleDay, WorkScheduleResponse } from "../api/schedules"

export type DayDraft = { dayOfWeek: number; worked: boolean; startTime: string; endTime: string; breakMinutes: number }

export const WEEK_ORDER = [1, 2, 3, 4, 5, 6, 0]

export const DAY_LABELS: Record<number, string> = {
	1: "Lundi",
	2: "Mardi",
	3: "Mercredi",
	4: "Jeudi",
	5: "Vendredi",
	6: "Samedi",
	0: "Dimanche"
}

export function toDraft(schedule: WorkScheduleResponse | null): DayDraft[] {
	const dayDraft: DayDraft[] = WEEK_ORDER.map((dayOfWeek): DayDraft => {
		const day = schedule?.days.find((d) => d.dayOfWeek === dayOfWeek)
		if (day)
			return ({ dayOfWeek: dayOfWeek, worked: true, startTime: day.startTime.slice(0, 5), endTime: day.endTime.slice(0,5), breakMinutes: day.breakMinutes })
		else
			return ({ dayOfWeek: dayOfWeek, worked: false, startTime: "08:00", endTime: "17:00", breakMinutes: 30 })
	})
	return dayDraft
}

export function toDays(draft: DayDraft[]): WorkScheduleDay[] {
	return draft
		.filter((row) => row.worked)
		.map((row): WorkScheduleDay => ({
			dayOfWeek: row.dayOfWeek,
			startTime: `${row.startTime}:00`,
			endTime: `${row.endTime}:00`,
			breakMinutes: row.breakMinutes
		}))
}

export function rowError(row: DayDraft): string | null {
	if (!row.worked) return null

	if (!row.startTime || !row.endTime)
		return "Indique le début et la fin."

	const start = toMinutes(row.startTime)
	const end = toMinutes(row.endTime)

	if (end <= start)
		return "La fin doit être après le début."

	if (row.breakMinutes < 0)
		return "La pause ne peut pas être négative."

	if (row.breakMinutes >= end - start)
		return "La pause doit être plus courte que la journée."

	return null
}

export function weeklyMinutes(draft: DayDraft[]): number {
	let total = 0
	for (const row of draft) {
		if (!row.worked || rowError(row) !== null) continue
		total += toMinutes(row.endTime) - toMinutes(row.startTime) - row.breakMinutes
	}
	return total
}


export function formatDuration(minutes: number): string {
	const hrs = Math.floor(minutes / 60)
	const min = minutes % 60
	return `${hrs} h${min > 0 ? ` ${String(min).padStart(2, "0")}` : ""}`
}

export function currentSchedule(schedules: WorkScheduleResponse[], today: string) : WorkScheduleResponse | null {
	return schedules.find(s => s.effectiveFrom <= today) ?? null
}

function toMinutes(time: string): number {
	const [hours, minutes] = time.split(":").map(Number)
	return hours * 60 + minutes
}