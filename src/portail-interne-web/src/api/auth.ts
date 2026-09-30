import type { DepartementId, RoleId } from "./admin";
import { apiFetch, readApiError } from "./client";
import { clearSession, TOKEN_KEY } from "./session";

export type LoginResponse = {
	token: string
	email: string
	userAccountId: number
	employeeId: number
}

export type Me = {
	employeeId: number
	firstName: string
	lastName: string
	email: string
	jobTitle: string
	phoneNumber: string | null
	departement: DepartementId | null
	role: RoleId
	isManager: boolean
	mustChangePassword: boolean
	isActive: boolean
}

function tokenIsExpired(token: string): boolean {
	try {
		const payload = token.split('.')[1]
		if (!payload) return true
		const json = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')))
		if (typeof json.exp !== 'number') return false
		return json.exp * 1000 <= Date.now()
	} catch {
		return true
	}
}

export function getToken(): string | null {
	const token = localStorage.getItem(TOKEN_KEY)
	if (!token) return null
	if (tokenIsExpired(token)) {
		clearSession()
		return null
	}
	return token
}

function saveSession(data: LoginResponse): void {
	localStorage.setItem(TOKEN_KEY, data.token)
}

export async function login(email: string, password: string): Promise<LoginResponse> {
	const res = await apiFetch('/api/auth/login', {
		method: 'POST',
		body: JSON.stringify({ email, password }),
	})

	if (!res.ok) {
		throw new Error('LOGIN_FAILED')
	}

	const data = (await res.json()) as LoginResponse
	saveSession(data)
	return data
}

export async function getMe(): Promise<Me | null> {
	if (!getToken()) return null

	const res = await apiFetch('/api/auth/me')

	if (res.status === 401)
		return null

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger ta session"))
	}

	return (await res.json()) as Me
}
