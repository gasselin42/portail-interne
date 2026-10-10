import { apiFetch, readApiError } from "./client"
import type { EmployeeEdition } from "./employees"

export type CreateEmployeeRequest = {
	firstName: string
	lastName: string
	email: string
	departement: DepartementId
	role: RoleId
	jobTitle?: string
	phoneNumber?: string
	managerId?: number
}

export type UpdateEmployeeRequest = {
	firstName: string
	lastName: string
	departement: DepartementId
	role: RoleId
	jobTitle?: string
	phoneNumber?: string
	managerId?: number
}

export type CreateEmployeeResponse = {
	employeeId: number
	userAccountId: number
	email: string
	firstName: string
	lastName: string
	role: RoleId
	mustChangePassword: boolean
	temporaryPassword: string
}

export const Departement = {
	Direction: 0,
	Ventes: 1,
	RH: 2,
	TI: 3,
} as const

export type DepartementId = (typeof Departement)[keyof typeof Departement]

// Renommer dans Employee.cs en cas de changement
export const DEPARTEMENT_LABELS: Record<DepartementId, string> = {
	[Departement.Direction]: "Direction",
	[Departement.Ventes]: "Ventes",
	[Departement.RH]: "Ressources humaines",
	[Departement.TI]: "Technologies de l'information",
}

export const Role = {
	Admin: 0,
	Employé: 1,
} as const

export type RoleId = (typeof Role)[keyof typeof Role]

// Renommer dans UserAccounts.cs en cas de changement
export const ROLE_LABELS: Record<RoleId, string> = {
	[Role.Admin]: "Admin",
	[Role.Employé]: "Employé",
}

export async function createEmployee(
	body: CreateEmployeeRequest,
	photo?: File | null,
): Promise<CreateEmployeeResponse> {
	const form = new FormData()
	form.append("firstName", body.firstName)
	form.append("lastName", body.lastName)
	form.append("email", body.email)
	form.append("departement", String(body.departement))
	form.append("role", String(body.role))
	if (body.jobTitle) form.append("jobTitle", body.jobTitle)
	if (body.phoneNumber) form.append("phoneNumber", body.phoneNumber)
	if (body.managerId) form.append("managerId", String(body.managerId))
	if (photo) form.append("photo", photo)

	const res = await apiFetch("/api/admin/employees", {
		method: "POST",
		body: form,
	})

	if (!res.ok) {
		throw new Error(await readApiError(res, "Création impossible"))
	}

	return (await res.json()) as CreateEmployeeResponse
}

export async function updateEmployee(
	id: number,
	body: UpdateEmployeeRequest,
	photo?: File | null,
): Promise<void> {
	const form = new FormData()
	form.append("firstName", body.firstName)
	form.append("lastName", body.lastName)
	form.append("departement", String(body.departement))
	form.append("role", String(body.role))
	if (body.jobTitle) form.append("jobTitle", body.jobTitle)
	if (body.phoneNumber) form.append("phoneNumber", body.phoneNumber)
	if (body.managerId) form.append("managerId", String(body.managerId))
	if (photo) form.append("photo", photo)

	const res = await apiFetch(`/api/admin/employees/${id}`, {
		method: "PATCH",
		body: form,
	})

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de modifier l'employé"))
	}
}

export type AccountListItem = {
	userAccountId: number
	employeeId: number
	email: string
	firstName: string
	lastName: string
	role: RoleId
	departement: DepartementId
	accountIsActive: boolean
	employeeIsActive: boolean
	mustChangePassword: boolean
	createdAt: string
}

export async function listAccounts(search?: string, signal?: AbortSignal): Promise<AccountListItem[]> {
	const query = new URLSearchParams()

	if (search && search.trim().length > 0) query.set("search", search!.trim())
	
	const res = await apiFetch(`/api/admin/accounts?${query}`, { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les comptes"))
	}
	return (await res.json()) as AccountListItem[]
}

export async function setAccountActive(id: number, isActive: boolean) {
	const res = await apiFetch(`/api/admin/accounts/${id}`, {
		method: "PATCH",
		body: JSON.stringify({ isActive }),
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de changer les paramètres du compte"))
	}
}

export async function resetAccountPassword(id: number): Promise<{ temporaryPassword: string }> {
	const res = await apiFetch(`/api/admin/accounts/${id}/reset-password`, {
		method: "POST",
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de réinitialiser le mot de passe"))
	}
	return (await res.json()) as { temporaryPassword: string }
}

export async function getAdminEmployee(id: number): Promise<EmployeeEdition> {
	const res = await apiFetch(`/api/admin/employees/${id}`)
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger l'employé"))
	}
	return (await res.json()) as EmployeeEdition
}
