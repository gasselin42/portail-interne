import { apiFetch, readApiError } from "./client"

export const DayStatus = {
	Working: 0,
	NotWorking: 1,
	Undefined: 2
} as const

export type DayStatusId = (typeof DayStatus)[keyof typeof DayStatus]

export const BusyKind = {
	Busy: 0,
	FlexibleBusy: 1,
	Absent: 2
} as const

export type BusyKindId = (typeof BusyKind)[keyof typeof BusyKind]

export type AvailabilityDay = {
	day: string
	status: DayStatusId
	startAt: string | null
	endAt: string | null
}

export type BusySlot = {
	startAt: string
	endAt: string
	kind: BusyKindId
}

export type EmployeeAvailability = {
	employeeId: number
	firstName: string
	lastName: string
	days: AvailabilityDay[]
	busy: BusySlot[]
}

export async function getAvailability(employeeIds: number[], from: string, to: string, signal?: AbortSignal): Promise<EmployeeAvailability[]> {
	const query = new URLSearchParams()

	query.set("from", from)
	query.set("to", to)

	for (const employee of employeeIds)
		query.append("employeeIds", employee.toString())
	
	const res = await apiFetch(`/api/availability?${query}`, { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les disponibilités"))
	}
	return (await res.json()) as EmployeeAvailability[]
}
