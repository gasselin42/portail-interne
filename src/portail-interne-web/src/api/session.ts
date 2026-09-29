export const TOKEN_KEY = 'portail_token'
export const MUST_CHANGE_KEY = 'portail_must_change'
export const ROLE_KEY = 'portail_role'
export const IS_MANAGER_KEY = 'portail_manager'

export function clearSession(): void {
	localStorage.removeItem(TOKEN_KEY)
	localStorage.removeItem(MUST_CHANGE_KEY)
	localStorage.removeItem(ROLE_KEY)
	localStorage.removeItem(IS_MANAGER_KEY)
}