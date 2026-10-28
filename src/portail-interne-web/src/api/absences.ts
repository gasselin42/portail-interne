import { apiFetch, readApiError } from "./client"
import type { LeaveStatusId } from "./leaves"

export type AbsenceResponse = {
	id: number
	date: string
	plannedStart: string | null
	plannedEnd: string | null
	reason: string | null
	status: LeaveStatusId
	start: string
	end: string | null
	inProgress: boolean
	returnConfirmed: boolean
	createdAt: string
	createdById: number | null
	createdByFirstName: string | null
	createdByLastName: string | null
	canCancel: boolean
}

export type SaveAbsenceRequest = {
	date: string
	plannedStart: string
	plannedEnd: string
	reason: string | null
}

export async function getAbsences(employeeId: number, signal?: AbortSignal): Promise<AbsenceResponse[]> {
	const res = await apiFetch(`/api/employees/${employeeId}/absences`, { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger tes absences"))
	}
	return (await res.json()) as AbsenceResponse[]
}

export async function departAbsence(employeeId: number, reason: string | null): Promise<AbsenceResponse> {
	const res = await apiFetch(`/api/employees/${employeeId}/absences/depart`, {
		method: "POST",
		body: JSON.stringify({ reason })
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible d'enregistrer ton départ"))
	}
	return (await res.json()) as AbsenceResponse
}

export async function returnFromAbsence(employeeId: number): Promise<AbsenceResponse> {
	const res = await apiFetch(`/api/employees/${employeeId}/absences/return`, { method: "POST" })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible d'enregistrer ton retour"))
	}
	return (await res.json()) as AbsenceResponse
}

export async function createAbsence(employeeId: number, request: SaveAbsenceRequest): Promise<AbsenceResponse> {
	const res = await apiFetch(`/api/employees/${employeeId}/absences`, {
		method: "POST",
		body: JSON.stringify(request)
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de créer l'absence"))
	}
	return (await res.json()) as AbsenceResponse
}

export async function cancelAbsence(employeeId: number, id: number) {
	const res = await apiFetch(`/api/employees/${employeeId}/absences/${id}/cancel`, { method: "POST" })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible d'annuler cette absence"))
	}
}
