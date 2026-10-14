import type { LeaveRequestResponse } from "../api/leaves";

export type LeaveSegment = {
	leave: LeaveRequestResponse
	column: number
	span: number
	continuesBefore: boolean
	continuesAfter: boolean
}

export function toSegment(leave: LeaveRequestResponse, days: string[]): LeaveSegment {
	const firstDay = days[0]
	const lastDay = days[days.length - 1]
	const leaveStart = leave.startDate.slice(0, 10)
	const leaveEnd = leave.endDate.slice(0, 10)

	const start = leaveStart > firstDay ? leaveStart : firstDay
	const end = leaveEnd < lastDay ? leaveEnd : lastDay
	const column = days.indexOf(start) + 1
	const span = days.indexOf(end) - days.indexOf(start) + 1
	const continuesBefore = start > leaveStart
	const continuesAfter = end < leaveEnd

	return {
		leave,
		column,
		span,
		continuesAfter,
		continuesBefore
	}
}

/** 
 * Segments des congés qui touchent au moins un des jours donnés. 
 * Le map du Calendar imprime une semaine à la fois en vue mois par exemple,
 * donc renvoie seulement les congés de cette semaine-là.
 * */
export function segmentsFor(leaves: LeaveRequestResponse[], days: string[]): LeaveSegment[] {
	const first = days[0]
	const last = days[days.length - 1]
	return leaves
		.filter((leave) => leave.startDate.slice(0, 10) <= last && leave.endDate.slice(0, 10) >= first)
		.map((leave) => toSegment(leave, days))
}