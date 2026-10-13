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