import { apiFetch } from "./client";
import { clearSession, TOKEN_KEY, MUST_CHANGE_KEY, ROLE_KEY, IS_MANAGER_KEY } from "./session";

export type LoginResponse = {
	token: string
	email: string
	userAccountId: number
	employeeId: number
	role: number | string
	isManager: boolean
	mustChangePassword: boolean
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

export function getMustChangePassword(): boolean {
	return localStorage.getItem(MUST_CHANGE_KEY) === 'true'
}

export function getRole(): string | null {
	return localStorage.getItem(ROLE_KEY)
}

export function isAdmin(): boolean {
	return getRole() === 'Admin'
}

export function isManager(): boolean {
	return localStorage.getItem(IS_MANAGER_KEY) === 'true'
}

export function canApprove(): boolean {
	return (isAdmin() || isManager())
}

export function logout(): void {
	clearSession()
}

function saveSession(data: LoginResponse): void {
	localStorage.setItem(TOKEN_KEY, data.token)
	localStorage.setItem(MUST_CHANGE_KEY, String(data.mustChangePassword))
	const roleLabel = data.role === 0 || data.role === 'Admin' ? 'Admin' : 'Employee'
	localStorage.setItem(ROLE_KEY, roleLabel)
	localStorage.setItem(IS_MANAGER_KEY, String(data.isManager))
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