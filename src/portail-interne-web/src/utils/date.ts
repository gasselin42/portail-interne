import { APP_TIME_ZONE } from "../config"

let timeZone = APP_TIME_ZONE // valeur par défaut, en attendant /me
const relativeFormatter = new Intl.RelativeTimeFormat("fr-CA", { numeric: "auto" })

const dateFormatter = new Intl.DateTimeFormat("fr-CA", {
	day: "numeric",
	month: "short",
	year: "numeric",
	timeZone: "UTC",
})

const weekdayFormatter = new Intl.DateTimeFormat("fr-CA", {
	weekday: "long",
	timeZone: "UTC"
})

const headerFormatter = new Intl.DateTimeFormat("fr-CA", {
	weekday: "short",
	day: "numeric",
	timeZone: "UTC"
})

const longDateFormatter = new Intl.DateTimeFormat("fr-CA", {
	weekday: "long",
	day: "numeric",
	month: "long",
	year: "numeric",
	timeZone: "UTC"
})

const monthFormatter = new Intl.DateTimeFormat("fr-CA", {
	month: "long",
	year: "numeric",
	timeZone: "UTC"
})

const shortWeekdayFormatter = new Intl.DateTimeFormat("fr-CA", {
	weekday: "short",
	timeZone: "UTC"
})

const shortDayFormatter = new Intl.DateTimeFormat("fr-CA", {
	day: "numeric",
	month: "short",
	timeZone: "UTC"
})

/** Appelée par la session : « aujourd'hui » suit le fuseau du site de la personne connectée. */
export function setTimeZone(value: string): void {
	timeZone = value
}

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

export function formatRange(start: string, end: string): string {
	const startDate = toUtcDate(start)
	const endDate = toUtcDate(end)
	return dateFormatter.formatRange(startDate, endDate)
}

export function formatDayHeader(value: string): string {
	const date = toUtcDate(value)
	return headerFormatter.format(date)
}

export function formatLongDate(value: string): string {
	const date = toUtcDate(value)
	return longDateFormatter.format(date)
}

export function formatMonthYear(iso: string): string {
	const date = toUtcDate(iso)
	return monthFormatter.format(date)
}

export function formatShortWeekday(iso: string): string {
	const date = toUtcDate(iso)
	return shortWeekdayFormatter.format(date)
}

export function formatShortDay(iso: string): string {
	const date = toUtcDate(iso)
	return shortDayFormatter.format(date)
}

export function formatTime(time: string): string {
	const [hours, minutes] = time.split(":")
	const h = Number(hours)
	return minutes === "00" ? `${h} h` : `${h} h ${minutes}`
}

export function formatRelative(iso: string, now: number): string {
	const gap = now - Date.parse(iso)
	const minutes = Math.floor(gap / 60000)
	const hours = Math.floor(gap / (60000 * 60))
	
	// Jours du calendrier, dans le fuseau de la personne connectée.
	const day = utcToZoned(iso).date
	const today = utcToZoned(new Date(now).toISOString()).date
	const days = (Date.parse(today) - Date.parse(day)) / 86_400_000

	if (minutes < 1)
		return "à l'instant"

	if (minutes < 60)
		return relativeFormatter.format(-minutes, "minute")

	if (days === 0)
		return relativeFormatter.format(-hours, "hour")

	if (days < 7)
		return relativeFormatter.format(-days, "day")

	return formatShortDay(day)
}

export function daysFromToday(offset: number): string {
	return addDays(todayIso(), offset)
}

export function addDays(iso: string, offset: number): string {
	const date = toUtcDate(iso)
	date.setUTCDate(date.getUTCDate() + offset)
	return date.toISOString().slice(0, 10)
}

export function startOfWeek(iso: string): string {
	const weekday = toUtcDate(iso).getUTCDay()
	return addDays(iso, -((weekday + 6) % 7))
}

export function nowTime(): string {
	return new Date().toLocaleTimeString("en-CA", { timeZone, hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23" })
}

export function todayIso(): string {
	return new Date().toLocaleDateString("en-CA", { timeZone })
}

export function isIsoDate(value: string): boolean {
	const date = toUtcDate(value)
	if (Number.isNaN(date.getTime())) return false
	return date.toISOString().slice(0, 10) === value
}

export function startOfMonth(iso: string): string {
	return iso.slice(0, 8) + "01"
}

export function addMonths(iso: string, n: number): string {
	const monthStart = startOfMonth(iso)
	const [year, month, day] = monthStart.split("-").map(Number)
	return new Date(Date.UTC(year, month - 1 + n, day)).toISOString().slice(0, 10)
}

export function getWeekDay(iso: string): number {
	return toUtcDate(iso).getUTCDay()
}

export function iterateDays(value: string, length: number) {
	return Array.from({ length: length }, (_, i) => addDays(value, i))
}

/** Le décalage du fuseau du site à un instant donné, en minutes (ex. −240 à Montréal en octobre). */
function offsetMinutes(utcMs: number): number {
	const parts = new Intl.DateTimeFormat("en-CA", {
		timeZone,
		hourCycle: "h23",
		year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit",
	}).formatToParts(new Date(utcMs)) // Découpe l'instant donné en year, month, day, etc.

	const get = (type: string) => Number(parts.find((p) => p.type === type)!.value)
	// Prends la même date et temps que l'instant donné, mais en UTC
	const asUtc = Date.UTC(get("year"), get("month") - 1, get("day"), get("hour"), get("minute"))

	// La différence, en minutes (60 000 ms = 1 min)
	return Math.round((asUtc - utcMs) / 60000)
}

/** Une date du site et une heure en minutes depuis minuit (1440 = minuit le lendemain) → instant UTC ISO. */
export function zonedToUtc(date: string, minutes: number): string {
	const [year, month, day] = date.split("-").map(Number)
	const wall = Date.UTC(year, month - 1, day, 0, minutes)

	// Deux passes : le décalage peut changer entre l'estimation et la réponse (passage à l'heure d'été)
	let utc = wall - offsetMinutes(wall) * 60000
	utc = wall - offsetMinutes(utc) * 60000

	return new Date(utc).toISOString()
}

/** Un instant UTC → sa date et ses minutes depuis minuit, dans le fuseau du site. */
export function utcToZoned(iso: string): { date: string; minutes: number } {
	const utc = Date.parse(iso)
	const local = new Date(utc + offsetMinutes(utc) * 60000)

	return {
		date: local.toISOString().slice(0, 10),
		minutes: local.getUTCHours() * 60 + local.getUTCMinutes(),
	}
}
