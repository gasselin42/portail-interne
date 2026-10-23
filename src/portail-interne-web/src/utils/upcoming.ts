import type { AbsenceResponse } from "../api/absences";
import { LeaveStatus, type LeaveRequestResponse } from "../api/leaves";

export type UpcomingItem =
	| { kind: "leave"; sortKey: string; leave: LeaveRequestResponse }
	| { kind: "absence"; sortKey: string; absence: AbsenceResponse }

export function toUpcoming(leaves: LeaveRequestResponse[], absences: AbsenceResponse[], today: string, now: string): UpcomingItem[] {
	const upcomingLeaves = leaves
		.filter(l => l.status === LeaveStatus.Approuve || l.status === LeaveStatus.EnAttente)
		.filter(l => l.endDate.slice(0, 10) >= today)

	const upcomingAbsences = absences.filter(a => isUpcoming(a, today, now))

	const leaveItems = upcomingLeaves.map((l): UpcomingItem => ({ kind: "leave", sortKey: l.startDate.slice(0, 10), leave: l}))
	const absenceItems = upcomingAbsences.map((a): UpcomingItem => ({ kind: "absence", sortKey: `${a.date}T${a.start}`, absence: a}))

	const concatItems = [...leaveItems, ...absenceItems]

	const sortedItems = concatItems.sort((a, b) => a.sortKey.localeCompare(b.sortKey))

	return sortedItems
}

export function isUpcoming(absence: AbsenceResponse, today: string, now: string): boolean {
	return ((absence.status === LeaveStatus.Approuve 
				|| absence.status === LeaveStatus.EnAttente)
			&& absence.plannedStart !== null
			&& !absence.inProgress
			&& (absence.date > today
				|| (absence.date === today && absence.end! > now)))
}
