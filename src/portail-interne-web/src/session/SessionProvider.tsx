import { useState, useCallback, useEffect, useRef, type ReactNode } from "react"
import { getMe, getToken, type Me } from "../api/auth"
import { clearSession } from "../api/session"
import { SessionContext } from "./context"

export function SessionProvider({ children }: { children: ReactNode }) {
	const [me, setMe] = useState<Me | null>(null)
	const [loading, setLoading] = useState(() => getToken() !== null)
	const [error, setError] = useState<string | null>(null)

	const lastRequest = useRef(0)

	const refresh = useCallback(async () => {
		// Chaque appel prend un numéro. Quand la réponse arrive,
		// si ce n'est plus le dernier numéro, on l'ignore
		const requestId = ++lastRequest.current
		return getMe()
			.then((result) => {
				if (requestId !== lastRequest.current) return
				setMe(result)
				setError(null)
			})
			.catch((e) => {
				if (requestId !== lastRequest.current) return
				setError(e instanceof Error ? e.message : "Erreur")
			})
			.finally(() => {
				if (requestId === lastRequest.current) setLoading(false)
			})
	}, [])

	const logout = useCallback(() => {
		clearSession()
		setMe(null)
	}, [])

	useEffect(() => {
		refresh()

		function handleVisibility() {
			if (document.visibilityState === "visible")
				refresh()
		}

		document.addEventListener("visibilitychange", handleVisibility)
		return () => document.removeEventListener("visibilitychange", handleVisibility)
	}, [refresh])

	return (
		<SessionContext.Provider value={{ me, loading, error, refresh, logout }}>
			{children}
		</SessionContext.Provider>
	)
}