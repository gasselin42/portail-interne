import { apiFetch, readApiError } from "./client"
import type { LeaveStatusId } from "./leaves"

export type AdjustmentResponse = {
	id: number
	date: string
	startTime: string | null
	endTime: string | null
	breakMinutes: number
	reason: string | null
	status: LeaveStatusId
	createdById: number
	createdByFirstName: string
	createdByLastName: string
	canCancel: boolean
}

export type SaveAdjustmentRequest = {
	date: string
	startTime: string
	endTime: string
	breakMinutes: number
	reason: string | null
}

export async function listAdjustments(employeeId: number, signal?: AbortSignal): Promise<AdjustmentResponse[]> {
	const res = await apiFetch(`/api/employees/${employeeId}/adjustments`, { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger tes ajustements"))
	}
	return (await res.json()) as AdjustmentResponse[]
}

export async function createAdjustment(employeeId: number, request: SaveAdjustmentRequest): Promise<AdjustmentResponse> {
	const res = await apiFetch(`/api/employees/${employeeId}/adjustments`, {
		method: "POST",
		body: JSON.stringify(request)
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de créer l'ajustement"))
	}
	return (await res.json()) as AdjustmentResponse
}

export async function cancelAdjustment(employeeId: number, id: number) {
	const res = await apiFetch(`/api/employees/${employeeId}/adjustments/${id}/cancel`, { method: "POST" })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible d'annuler cet ajustement"))
	}
}
