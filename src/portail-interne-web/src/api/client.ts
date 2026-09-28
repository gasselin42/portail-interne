import { getToken } from "./auth";
import { clearSession } from "./session";

export const API_URL = import.meta.env.VITE_API_URL as string;

export async function apiFetch(path: string, options: RequestInit = {}) {
	const url = `${API_URL}${path.startsWith('/') ? path : `/${path}`}`
	const isForm = options.body instanceof FormData
	const token = getToken()

	const res = await fetch(url, {
		...options,
		headers: {
			...(isForm ? {} : { 'Content-Type': 'application/json' }),
			...(token ? { Authorization: `Bearer ${token}`} : {}),
			...(options.headers ?? {}),
		},
	})

	if (res.status === 401 && token && !path.startsWith('/api/auth/login')) {
		clearSession()
		if (window.location.pathname !== '/login') {
			window.location.assign('/login')
		}
	}

	return res
}

export async function readApiError(res: Response, fallback: string): Promise<string> {
	const data = await res.json().catch(() => null)
	if (data && typeof data.message === 'string') {
	  return data.message
	}
	return fallback
}