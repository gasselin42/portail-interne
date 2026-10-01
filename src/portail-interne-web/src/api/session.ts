export const TOKEN_KEY = "portail_token"

export function clearSession(): void {
	localStorage.removeItem(TOKEN_KEY)
}
