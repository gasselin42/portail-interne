import { apiFetch, readApiError } from "./client"

export const LeaveType = {
	Conge: 0,
	Maladie: 1,
} as const

export type LeaveTypeId = (typeof LeaveType)[keyof typeof LeaveType]

export const LEAVE_TYPE_LABELS: Record<LeaveTypeId,  string> = {
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

export type LeaveRequestResponse = {
    id: number
    employeeId: number
	employeeFirstName: string
	employeeLastName: string
    startDate: string
    endDate: string
    type: LeaveTypeId
    status : LeaveStatusId
    reason: string
    createdAt: string
}

export type CreateLeaveRequest = {
	startDate: string
	endDate: string
	type: LeaveTypeId
	reason?: string
}

export type ReviewStatus = typeof LeaveStatus.Approuve | typeof LeaveStatus.Refuse

export async function listLeaves(): Promise<LeaveRequestResponse[]> {
	const res = await apiFetch("/api/leaves")
	if (!res.ok) {
		throw new Error(await readApiError(res, 'Impossible de charger les congés'))
	}
	return (await res.json()) as LeaveRequestResponse[]
}

export async function listLeavesPending(): Promise<LeaveRequestResponse[]> {
	const res = await apiFetch("/api/leaves/pending")
	if (!res.ok) {
		throw new Error(await readApiError(res, 'Impossible de charger les congés'))
	}
	return (await res.json()) as LeaveRequestResponse[]
}

export async function cancelLeave(id: number) {
	const res = await apiFetch(`/api/leaves/${id}/cancel`, { method: 'POST' })
	if (!res.ok) {
		throw new Error(await readApiError(res, 'Impossible d\'annuler le congé'))
	}
}

export async function createLeave(request: CreateLeaveRequest): Promise<LeaveRequestResponse> {
	const res = await apiFetch("/api/leaves", {
		method: "POST",
		body: JSON.stringify(request),
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, 'Impossible de créer le congé'))
	}
	return (await res.json()) as LeaveRequestResponse
}

export async function reviewLeave(id: number, status: ReviewStatus): Promise<void> {
	const res = await apiFetch(`/api/leaves/${id}/review`, {
		method: "POST",
		body: JSON.stringify({ status }),
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, 'Impossible de traiter la demande'))
	}
}