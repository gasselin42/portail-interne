import type { EffectiveDay } from "./calendar"
import { apiFetch, readApiError } from "./client"

export type TeamMember = {
	employeeId: number
	firstName: string
	lastName: string
	jobTitle: string
	details: boolean
	days: EffectiveDay[]
}

export async function getTeamWeek(from: string, to: string, siteId?: number, signal?: AbortSignal): Promise<TeamMember[]> {
	const query = new URLSearchParams()
	
	query.set("from", from)
	query.set("to", to)
	if (siteId !== undefined)
		query.set("siteId", String(siteId))
	
	const res = await apiFetch(`/api/team/week?${query}`, { signal })
	
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger l'équipe"))
	}
	
	return (await res.json()) as TeamMember[]
}