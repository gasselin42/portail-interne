import { useRef, useState, useEffect, type ReactNode, useCallback } from "react"
import { UnreadCountContext } from "./context"
import { getUnreadCount } from "../api/notifications"

export function UnreadCountProvider({ children }: { children: ReactNode }) {
	const [count, setCount] = useState<number | null>(null)
	const lastRequest = useRef(0)

	const markOneRead = useCallback(() => {
		setCount((c) => c === null ? null : Math.max(0, c - 1))
		lastRequest.current++
	}, [])

	const markAllRead = useCallback(() => {
		setCount((c) => c === null ? null : 0)
		lastRequest.current++
	}, [])

	useEffect(() => {
		const controller = new AbortController()

		// Charge le compte ; en cas d'échec, on garde la dernière valeur (le prochain essai corrigera).
		function load() {
			const requestId = ++lastRequest.current
			getUnreadCount(controller.signal)
				.then((c) => {
					if (requestId !== lastRequest.current) return
					setCount(c)
				})
				.catch(() => {})
		}

		function handleVisibility() {
			if (document.visibilityState === "visible")
				load()
		}

		load()
		const timer = setInterval(load, 60_000)
		document.addEventListener("visibilitychange", handleVisibility)

		return () => {
			controller.abort()
			clearInterval(timer)
			document.removeEventListener("visibilitychange", handleVisibility)
		}
	}, [])

	return (
		<UnreadCountContext.Provider value={{ count, markOneRead, markAllRead }}>
			{children}
		</UnreadCountContext.Provider>
	)
}