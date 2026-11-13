import { apiFetch, readApiError } from "./client"

export const CalendarHourRange = {
	WorkHours: 0,
	FullDay: 1,
} as const

export type CalendarHourRangeId = (typeof CalendarHourRange)[keyof typeof CalendarHourRange]

export const CalendarDensity = {
	Normal: 0,
	Compact: 1,
	Airy: 2
} as const

export type CalendarDensityId = (typeof CalendarDensity)[keyof typeof CalendarDensity]

export type Preferences = {
	calendarHourRange: CalendarHourRangeId
	calendarDensity: CalendarDensityId
}

export async function getPreferences(signal?: AbortSignal): Promise<Preferences> {
	const res = await apiFetch("/api/me/preferences", { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger tes préférences"))
	}
	return (await res.json()) as Preferences
}

export async function savePreferences(prefs: Preferences): Promise<Preferences> {
	const res = await apiFetch("/api/me/preferences", {
		method: "PUT",
		body: JSON.stringify(prefs)
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible d'enregistrer tes préférences"))
	}
	return (await res.json()) as Preferences
}
