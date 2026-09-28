import { apiFetch, readApiError } from "./client"

export const LeaveType = {
	Conge: 0,
	Maladie: 1,
} as const

export const LeaveStatus = {
	EnAttente: 0,
	Approuve: 1,
	Refuse: 2,
	Annule: 3,
} as const

export type LeaveRequestResponse = {
    id: number
    employeeId: number
	employeeFirstName: string
	employeeLastName: string
    startDate: string
    endDate: string
    type: number
    status : number
    reason: string
    createdAt: string
}

export async function listLeaves(): Promise<LeaveRequestResponse[]> {
	const res = await apiFetch("/api/leaves")
	if (!res.ok) {
		throw new Error(await readApiError(res, 'Impossible de charger tes congés'))
	}
	return (await res.json()) as LeaveRequestResponse[]
}

export async function cancelLeave(id: number) {
	const res = await apiFetch(`/api/leaves/${id}/cancel`)
	if (!res.ok) {
		throw new Error(await readApiError(res, 'Impossible d\'annuler le congé'))
	}
}