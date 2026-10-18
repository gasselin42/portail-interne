import { apiFetch, readApiError } from "./client"

export type Site = {
	id: number
	name: string
	timeZoneId: string
}

export async function listSites(signal?: AbortSignal): Promise<Site[]> {
	const res = await apiFetch("/api/sites", { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les sites"))
	}
	return (await res.json()) as Site[]
}