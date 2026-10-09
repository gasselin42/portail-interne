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

/** Le minimum pour afficher une personne sélectionnée. */
export type EmployeeOption = {
	id: number
	firstName: string
	lastName: string
}

/** Un résultat de recherche : une option, avec de quoi la reconnaître. */
export type EmployeeLookupItem = EmployeeOption & {
	jobTitle: string
	departement: DepartementId
}

export type EmployeeLookupResult = {
	items: EmployeeLookupItem[]
	hasMore: boolean
}

export async function listEmployees(search?: string, signal?: AbortSignal): Promise<EmployeeListItem[]> {
	const query = search?.trim() ? `?search=${encodeURIComponent(search.trim())}` : ""

	const res = await apiFetch(`/api/employees${query}`, { signal })

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

export async function lookupEmployees(
	params: { search: string; limit?: number; excludeTeamOf?: number },
	signal?: AbortSignal,
): Promise<EmployeeLookupResult> {
	const query = new URLSearchParams()

	if (params.search.trim().length > 0) query.set("search", params.search.trim())

	if (params.limit !== undefined) query.set("limit", params.limit.toString())

	if (params.excludeTeamOf !== undefined)
		query.set("excludeTeamOf", params.excludeTeamOf.toString())

	const res = await apiFetch(`/api/employees/lookup?${query}`, { signal })

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les employés"))
	}

	return (await res.json()) as EmployeeLookupResult
}
