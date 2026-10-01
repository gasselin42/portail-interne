import { createContext } from "react"
import type { Me } from "../api/auth"

export type SessionValue = {
	me: Me | null // null = pas connecté
	loading: boolean // true tant que le 1er /me n'a pas répondu
	error: string | null // serveur injoignable, etc.
	refresh: () => Promise<void>
	logout: () => void
}

export const SessionContext = createContext<SessionValue | null>(null)
