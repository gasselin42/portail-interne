import { apiFetch, readApiError } from "./client"

export const TeamVisibility = {
	Team: 0,
	Site: 1,
	Nobody: 2,
} as const

export type TeamVisibilityId = (typeof TeamVisibility)[keyof typeof TeamVisibility]

export type Site = {
	id: number
	name: string
	timeZoneId: string
	teamVisibility: TeamVisibilityId
}

export const TEAM_VISIBILITY_LABELS: Record<TeamVisibilityId, string> = {
	[TeamVisibility.Team]: "Mon équipe",
	[TeamVisibility.Site]: "Tout le site",
	[TeamVisibility.Nobody]: "Personne",
}

export async function listSites(signal?: AbortSignal): Promise<Site[]> {
	const res = await apiFetch("/api/sites", { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les sites"))
	}
	return (await res.json()) as Site[]
}

export async function createSite(name: string, timeZoneId: string): Promise<Site> {
	const res = await apiFetch("/api/sites", {
		method: "POST",
		body: JSON.stringify({ name, timeZoneId })
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de créer le site"))
	}
	return (await res.json()) as Site
}

export async function updateSite(id: number, name: string, timeZoneId: string): Promise<void> {
	const res = await apiFetch(`/api/sites/${id}`, {
		method: "PUT",
		body: JSON.stringify({ name, timeZoneId })
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de mettre à jour le site"))
	}
}

export async function setTeamVisibility(id: number, teamVisibility: TeamVisibilityId): Promise<void> {
	const res = await apiFetch(`/api/sites/${id}/team-visibility`, {
		method: "PUT",
		body: JSON.stringify({ teamVisibility })
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de mettre à jour la visibilité du site"))
	}
}