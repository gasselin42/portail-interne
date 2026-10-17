import { LeaveStatus, type LeaveRequestResponse } from "../api/leaves"
import type { LeaveSegment } from "../utils/calendar"
import { addDays, getWeekDay } from "../utils/date"

/** Hachures blanches semi-transparentes, posées par-dessus la couleur (congé en attente). */
const HATCH = "repeating-linear-gradient(135deg, rgb(255 255 255 / 0.55) 0 6px, transparent 6px 12px)"

/**
 * Le jour est-il sans solde pour ce congé ?
 * Une fin de semaine prend l'état du dernier jour ouvrable du congé qui la précède
 * (payé si le congé commence un samedi ou un dimanche).
 */
export function isUnpaidDay(leave: LeaveRequestResponse, day: string): boolean {
	const unpaidDates = leave.unpaidDates ?? []
	if (unpaidDates.length === 0) return false

	const leaveStart = leave.startDate.slice(0, 10)
	let workday = day
	while (getWeekDay(workday) === 0 || getWeekDay(workday) === 6) {
		workday = addDays(workday, -1)
		if (workday < leaveStart) return false
	}
	return unpaidDates.includes(workday)
}

/** Fond d'un LeaveBand : une coupure franche à chaque passage payé ↔ sans solde. */
export function leaveBandBackground(segment: LeaveSegment): string {
	const { leave, start, span } = segment
	const approved = leave.status === LeaveStatus.Approuve
	const paid = approved ? "var(--leave-approved)" : "var(--leave-pending)"
	const unpaid = approved ? "var(--leave-approved-unpaid)" : "var(--leave-pending-unpaid)"

	const stops: string[] = []

	for (let i = 0; i < span; i++) {
		const color = isUnpaidDay(leave, addDays(start, i)) ? unpaid : paid
		stops.push(`${color} ${(i / span) * 100}% ${((i + 1) / span) * 100}%`)
	}

	const colors = `linear-gradient(to right, ${stops.join(", ")})`
	return approved ? colors : `${HATCH}, ${colors}`
}

/** Fond d'un échantillon uni (légende, barre du solde) : la même apparence qu'un LeaveBand. */
export function leaveSwatchBackground(approved: boolean, unpaid = false): string {
	const color = approved
		? unpaid ? "var(--leave-approved-unpaid)" : "var(--leave-approved)"
		: unpaid ? "var(--leave-pending-unpaid)" : "var(--leave-pending)"
	return approved ? color : `${HATCH}, ${color}`
}
