import { apiFetch, readApiError } from "./client"

export type WorkScheduleDay = {
	dayOfWeek: number
	startTime: string
	endTime: string
	breakMinutes: number
}

export type SaveWorkScheduleRequest = {
	effectiveFrom: string
	days: WorkScheduleDay[]
}

export type WorkScheduleResponse = {
	id: number
	effectiveFrom: string
	createdAt: string
	createdByFirstName: string
	createdByLastName: string
	isEditable: boolean
	days: WorkScheduleDay[]
}

export async function listSchedules(employeeId: number, signal?: AbortSignal): Promise<WorkScheduleResponse[]> {
	const res = await apiFetch(`/api/employees/${employeeId}/schedules`, { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les horaires"))
	}
	return (await res.json()) as WorkScheduleResponse[]
}

export async function createSchedule(employeeId: number, request: SaveWorkScheduleRequest): Promise<WorkScheduleResponse> {
	const res = await apiFetch(`/api/employees/${employeeId}/schedules`, {
		method: "POST",
		body: JSON.stringify(request)
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de créer l'horaire"))
	}
	return (await res.json()) as WorkScheduleResponse
}

export async function updateSchedule(employeeId: number, id: number, request: SaveWorkScheduleRequest) {
	const res = await apiFetch(`/api/employees/${employeeId}/schedules/${id}`, {
		method: "PUT",
		body: JSON.stringify(request)
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de modifier l'horaire"))
	}
}

export async function deleteSchedule(employeeId: number, id: number) {
	const res = await apiFetch(`/api/employees/${employeeId}/schedules/${id}`, { method: "DELETE" })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de supprimer l'horaire"))
	}
}