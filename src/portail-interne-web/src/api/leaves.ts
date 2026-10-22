import { apiFetch, readApiError } from "./client"

export const LeaveType = {
	Conge: 0,
	Maladie: 1,
} as const

export type LeaveTypeId = (typeof LeaveType)[keyof typeof LeaveType]

export const LEAVE_TYPE_LABELS: Record<LeaveTypeId, string> = {
	[LeaveType.Conge]: "Congé",
	[LeaveType.Maladie]: "Maladie",
}

export const LeaveStatus = {
	EnAttente: 0,
	Approuve: 1,
	Refuse: 2,
	Annule: 3,
} as const

export type LeaveStatusId = (typeof LeaveStatus)[keyof typeof LeaveStatus]

export const LEAVE_STATUS_LABELS: Record<LeaveStatusId, string> = {
	[LeaveStatus.EnAttente]: "En attente",
	[LeaveStatus.Approuve]: "Approuvé",
	[LeaveStatus.Refuse]: "Refusé",
	[LeaveStatus.Annule]: "Annulé",
}

export type LeaveRequestResponse = {
	id: number
	employeeId: number
	employeeFirstName: string
	employeeLastName: string
	startDate: string
	endDate: string
	type: LeaveTypeId
	status: LeaveStatusId
	reason: string
	workingDays: number | null
	unpaidDates: string[] | null
	workedDates: string[] | null
	createdAt: string
	createdByFirstName: string | null
	createdByLastName: string | null
}

export type CreateLeaveRequest = {
	startDate: string
	endDate: string
	type: LeaveTypeId
	reason?: string
}

export type DeclareSickLeaveRequest = {
	employeeId: number
	startDate: string
	endDate: string
	reason?: string
}

export type LeaveBalance = {
	year: number
	annualDays: number
	approvedDays: number
	pendingDays: number
	remainingDays: number
	unpaidDays: number	
}

export type LeavePreview = {
	workingDays: number
	unpaidDates: string[]
}

export type ReviewStatus = typeof LeaveStatus.Approuve | typeof LeaveStatus.Refuse

export async function listLeaves(signal?: AbortSignal): Promise<LeaveRequestResponse[]> {
	const res = await apiFetch("/api/leaves", { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les congés"))
	}
	return (await res.json()) as LeaveRequestResponse[]
}

export async function listLeavesPending(signal?: AbortSignal): Promise<LeaveRequestResponse[]> {
	const res = await apiFetch("/api/leaves/pending", { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les congés"))
	}
	return (await res.json()) as LeaveRequestResponse[]
}

export async function cancelLeave(id: number) {
	const res = await apiFetch(`/api/leaves/${id}/cancel`, { method: "POST" })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible d'annuler le congé"))
	}
}

export async function createLeave(request: CreateLeaveRequest): Promise<LeaveRequestResponse> {
	const res = await apiFetch("/api/leaves", {
		method: "POST",
		body: JSON.stringify(request),
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de créer le congé"))
	}
	return (await res.json()) as LeaveRequestResponse
}

export async function declareSickLeave(request: DeclareSickLeaveRequest): Promise<LeaveRequestResponse> {
	const res = await apiFetch("/api/leaves/declare", {
		method: "POST",
		body: JSON.stringify(request),
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de déclarer le congé"))
	}
	return (await res.json()) as LeaveRequestResponse
}

export async function reviewLeave(id: number, status: ReviewStatus): Promise<void> {
	const res = await apiFetch(`/api/leaves/${id}/review`, {
		method: "POST",
		body: JSON.stringify({ status }),
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de traiter la demande"))
	}
}

export async function getLeaveBalance(year: number, signal?: AbortSignal): Promise<LeaveBalance> {
	const query = new URLSearchParams()

	query.set("year", year.toString())
	
	const res = await apiFetch(`/api/leaves/balance?${query}`, { signal })

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de traiter la demande"))
	}

	return (await res.json()) as LeaveBalance
}

export async function previewLeave(startDate: string, endDate: string, signal?: AbortSignal): Promise<LeavePreview> {
	const query = new URLSearchParams()

	query.set("startDate", startDate)
	query.set("endDate", endDate)
	
	const res = await apiFetch(`/api/leaves/balance/preview?${query}`, { signal })

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de traiter la demande"))
	}

	return (await res.json()) as LeavePreview
}
