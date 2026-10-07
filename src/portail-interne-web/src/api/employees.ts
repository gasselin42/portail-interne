import type { DepartementId } from "./admin"
import { apiFetch, readApiError } from "./client"

export type EmployeeListItem = {
	id: number
	firstName: string
	lastName: string
	email: string
	jobTitle: string
	departement: DepartementId
	phoneNumber: string
	photoUrl: string | null
}

export type EmployeeDetail = EmployeeListItem & {
	managerId: number | null
	managerFirstName: string | null
	managerLastName: string | null
}

export type EmployeeEdition = EmployeeDetail & {
	role: number | null
}

export async function listEmployees(search?: string): Promise<EmployeeListItem[]> {
	const query = search?.trim() ? `?search=${encodeURIComponent(search.trim())}` : ""

	const res = await apiFetch(`/api/employees${query}`)

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger l'annuaire"))
	}

	return (await res.json()) as EmployeeListItem[]
}

export async function getEmployee(id: number): Promise<EmployeeDetail> {
	const res = await apiFetch(`/api/employees/${id}`)

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger l'employé"))
	}

	return (await res.json()) as EmployeeDetail
}
