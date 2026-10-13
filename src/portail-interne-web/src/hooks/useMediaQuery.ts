import { useCallback, useSyncExternalStore } from "react"

/** true tant que la media query correspond (ex. "(min-width: 48rem)"). */
export function useMediaQuery(query: string): boolean {
	const subscribe = useCallback((onChange: () => void) => {
		const media = window.matchMedia(query)
		media.addEventListener("change", onChange)
		return () => media.removeEventListener("change", onChange)
	}, [query])

	return useSyncExternalStore(subscribe, () => window.matchMedia(query).matches)
}