import { APP_TIME_ZONE } from "../config"

let timeZone = APP_TIME_ZONE // valeur par défaut, en attendant /me

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

export function formatTime(time: string): string {
	const [hours, minutes] = time.split(":")
	const h = Number(hours)
	return minutes === "00" ? `${h} h` : `${h} h ${minutes}`
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
