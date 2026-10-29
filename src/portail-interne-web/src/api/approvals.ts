import { apiFetch, readApiError } from "./client"
import type { ReviewStatus } from "./leaves"

export type PendingAdjustmentItem = {
	id: number
	employeeId: number
	employeeFirstName: string
	employeeLastName: string
	date: string
	startTime: string | null
	endTime: string | null
	breakMinutes: number
	reason: string | null
	createdAt: string
	canApprove: boolean
}

export type PendingAbsenceItem = {
	id: number
	employeeId: number
	employeeFirstName: string
	employeeLastName: string
	date: string
	plannedStart: string | null
	plannedEnd: string | null
	reason: string | null
	createdAt: string
	canApprove: boolean
}

export type ApprovalQueue = {
	adjustments: PendingAdjustmentItem[]
	absences: PendingAbsenceItem[]
}

export async function getApprovalQueue(signal?: AbortSignal): Promise<ApprovalQueue> {
	const res = await apiFetch("/api/approvals", { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les demandes à approuver"))
	}
	return (await res.json()) as ApprovalQueue
}

export async function reviewAdjustment(employeeId: number, id: number, status: ReviewStatus): Promise<void> {
	const res = await apiFetch(`/api/employees/${employeeId}/adjustments/${id}/review`, {
		method: "POST",
		body: JSON.stringify({ status }),
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de traiter la demande"))
	}
}

export async function reviewAbsence(employeeId: number, id: number, status: ReviewStatus): Promise<void> {
	const res = await apiFetch(`/api/employees/${employeeId}/absences/${id}/review`, {
		method: "POST",
		body: JSON.stringify({ status }),
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de traiter la demande"))
	}
}