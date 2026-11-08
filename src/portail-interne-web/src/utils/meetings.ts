import type { MeetingResponse } from "../api/meetings"
import { addDays, formatTime, nowTime, todayIso, utcToZoned } from "./date"

export function minutesToTime(minutes: number): string {
	return Math.floor(minutes / 60).toString().padStart(2, "0") + ":" + (minutes % 60).toString().padStart(2, "0")
}

export function formatMinutes(minutes: number): string {
	if (minutes === 1440) return "minuit"
	return formatTime(minutesToTime(minutes))
}

export function formatOptionTime(minutes: number): string {
	if (minutes === 1440) return "minuit"
	return Math.floor(minutes / 60).toString() + " h " + (minutes % 60).toString().padStart(2, "0")
}

export function formatDuration(minutes: number): string {
	if (minutes < 60) return `${minutes} min`
	if (minutes % 60 === 0) return `${Math.floor(minutes / 60)} h`
	return `${Math.floor(minutes / 60)} h ${(minutes % 60).toString().padStart(2, "0")}`
}

export function startOptions(): number[] {
	return Array.from({ length: 96 }, (_, i) => i * 15)
}

export function endOptions(start: number): { value: number, label: string }[] {
	const length = Math.floor((1440 - start) / 15)
	return Array.from({ length }, (_, i) => {
		const value = start + (i + 1) * 15
		return { value, label: `${formatOptionTime(value)} (${formatDuration(value - start)})` }
	})
}

export function earliestStart(date: string): number {
	if (date === todayIso()) {
		const now = nowTime()
		const [hours, minutes] = now.split(":").map(Number)
		const nowMinutes = hours * 60 + minutes
		return Math.floor(nowMinutes / 15) * 15 + 15
	}
	return 0
}

export function firstBookableDate(): string {
	const today = todayIso()
	return earliestStart(today) < 1440 ? today : addDays(today, 1)
}

export function minutesOnDate(iso: string, date: string): number {
	const dayTime = utcToZoned(iso)
	if (dayTime.date < date) return 0
	if (dayTime.date > date) return 1440
	return dayTime.minutes
}

export function meetingSlice(meeting: MeetingResponse, date: string): { from: number, to: number} | null {
	const startAt = minutesOnDate(meeting.startAt, date)
	const endAt = minutesOnDate(meeting.endAt, date)
	return startAt === endAt ? null : { from: startAt, to: endAt }
}

export function meetingTimes(meeting: MeetingResponse): { date: string, start: number, end: number } {
	const start = utcToZoned(meeting.startAt)
	const end = utcToZoned(meeting.endAt)
	const endMinutes = end.date !== start.date ? 1440 : end.minutes
	return { date: start.date, start: start.minutes, end: endMinutes }
}