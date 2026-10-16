import { useEffect, useState, type DependencyList } from "react"

type Result<T> = { key: string; data: T } | { key: string; error: string }

/**
 * Charge des données avec annulation des requêtes périmées et « Réessayer ».
 * Quand deps change, les anciennes données ne sont jamais affichées pendant le rechargement.
 */

export function useAsync<T>(load: (signal: AbortSignal) => Promise<T>, deps: DependencyList) {
	const key = JSON.stringify(deps)
	const [result, setResult] = useState<Result<T> | null>(null)
	const [attempt, setAttempt] = useState(0)

	useEffect(() => {
		const controller = new AbortController()

		load(controller.signal)
			.then((data) => setResult({ key, data }))
			.catch((e) => {
				if (controller.signal.aborted) return
				setResult({ key, error: e instanceof Error ? e.message : "Erreur" })
			})
		
		return () => controller.abort()
		// load change à chaque rendu : seules les valeurs de deps (via key) relancent le chargement.
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [key, attempt])

	const current = result?.key === key ? result : null

	return {
		data: current && "data" in current ? current.data : null,
		error: current && "error" in current ? current.error : null,
		loading: current === null,
		retry: () => {
			setResult(null)
			setAttempt((n) => n + 1)
		},
	}
}
