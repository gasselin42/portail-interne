import { apiFetch, readApiError } from "./client"
import type { LeaveRequestResponse, LeaveStatusId } from "./leaves"

export const DaySource = {
	Horaire: 0,
	Ferie: 1,
	Ajustement: 2,
	Conge: 3,
	NonDefini: 4,
} as const

export type DaySourceId = (typeof DaySource)[keyof typeof DaySource]

export type PendingAdjustment = {
	id: number
	startTime: string | null
	endTime: string | null
	breakMinutes: number
}

export type EffectiveDay = {
	date: string
	source: DaySourceId
	startTime: string | null
	endTime: string | null
	breakMinutes: number
	holidayName: string | null
	pendingAdjustment: PendingAdjustment | null
	absences: CalendarAbsence[]
	pendingAbsences: CalendarAbsence[]
}

export type CalendarData = {
	days: EffectiveDay[]
	leaves: LeaveRequestResponse[]
}

export type CalendarAbsence = {
	id: number
	status: LeaveStatusId
	reason: string | null
	plannedStart: string | null
	plannedEnd: string | null
	start: string
	end: string | null
	inProgress: boolean
	returnConfirmed: boolean
}

export async function getCalendar(from: string, to: string, signal?: AbortSignal): Promise<CalendarData> {
	const query = new URLSearchParams()

	query.set("from", from)
	query.set("to", to)
	
	const res = await apiFetch(`/api/calendar?${query}`, { signal })
	
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger le calendrier"))
	}
	
	return (await res.json()) as CalendarData
}