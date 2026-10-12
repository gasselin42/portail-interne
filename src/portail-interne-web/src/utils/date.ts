import { APP_TIME_ZONE } from "../config"

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

export function todayIso(): string {
	return new Date().toLocaleDateString("en-CA", { timeZone: APP_TIME_ZONE })
}

export function isIsoDate(value: string): boolean {
	const date = toUtcDate(value)
	if (Number.isNaN(date.getTime())) return false
	return date.toISOString().slice(0, 10) === value
}
