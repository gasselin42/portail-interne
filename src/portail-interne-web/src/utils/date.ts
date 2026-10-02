import { APP_TIME_ZONE } from "../config"

const dateFormatter = new Intl.DateTimeFormat("fr-CA", {
	day: "numeric",
	month: "short",
	year: "numeric",
	timeZone: "UTC",
})

const weekdayFormatter = Intl.DateTimeFormat("fr-CA", {
	weekday: "long",
	timeZone: "UTC"
})

function toUtcDate(value: string): Date {
	const isoDate = value.slice(0, 10)
	const [year, month, day] = isoDate.split("-").map(Number)
	return new Date(Date.UTC(year, month - 1, day))
}

export function formatDate(value: string): string {
	const date = toUtcDate(value)
	return dateFormatter.format(date)
}

export function formatWeekday(value: string): string {
	const date = toUtcDate(value)
	return weekdayFormatter.format(date);
}

export function daysFromToday(offset: number): string {
	// 1. La date du jour dans le fuseau de l'entreprise, ex. "2026-10-02"
	const today = new Date().toLocaleDateString("en-CA", { timeZone: APP_TIME_ZONE })
	const [year, month, day] = today.split("-").map(Number)

	// 2. Le calcul se fait en UTC : aucun changement d'heure ne peut le fausser
	const shifted = new Date(Date.UTC(year, month - 1, day + offset))
	return shifted.toISOString().slice(0, 10)
}
