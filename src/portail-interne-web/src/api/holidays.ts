import { apiFetch, readApiError } from "./client"

export type Holiday = {
	id: number
	date: string
	name: string
	isEditable: boolean
}

export async function listHolidays(siteId: number, year: number, signal?: AbortSignal): Promise<Holiday[]> {
	const query = new URLSearchParams()
	
	query.set("year", String(year))
	
	const res = await apiFetch(`/api/sites/${siteId}/holidays?${query}`, { signal })
	
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les jours fériés"))
	}
	
	return (await res.json()) as Holiday[]
}

export async function createHoliday(siteId: number, date: string, name: string): Promise<Holiday> {
	const res = await apiFetch(`/api/sites/${siteId}/holidays`, {
		method: "POST",
		body: JSON.stringify({ date, name })
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de créer le jour férié"))
	}
	return (await res.json()) as Holiday
}

export async function updateHoliday(siteId: number, id: number, date: string, name: string): Promise<void> {
	const res = await apiFetch(`/api/sites/${siteId}/holidays/${id}`, {
		method: "PUT",
		body: JSON.stringify({ date, name })
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de mettre à jour le jour férié"))
	}
}

export async function deleteHoliday(siteId: number, id: number): Promise<void> {
	const res = await apiFetch(`/api/sites/${siteId}/holidays/${id}`, { method: "DELETE" })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de supprimer le jour férié"))
	}
}