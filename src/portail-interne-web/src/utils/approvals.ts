import { getApprovalQueue, reviewAbsence, reviewAdjustment, type ApprovalQueue, type PendingAbsenceItem, type PendingAdjustmentItem } from "../api/approvals"
import { LEAVE_TYPE_LABELS, LeaveType, listLeavesPending, reviewLeave, type LeaveRequestResponse, type ReviewStatus } from "../api/leaves"
import { formatLongDate, formatRange, formatTime } from "./date"

type ApprovalBase = {
	key: string          // "leave-12", "adjustment-3" : unique dans la liste fusionnée
	sortKey: string
	employeeName: string
	reason: string | null
	canApprove: boolean
}

export type ApprovalItem = ApprovalBase & (
	| { kind: "leave"; leave: LeaveRequestResponse }
	| { kind: "adjustment"; adjustment: PendingAdjustmentItem }
	| { kind: "absence"; absence: PendingAbsenceItem }
)

export function toApprovals(leaves: LeaveRequestResponse[], queue: ApprovalQueue): ApprovalItem[] {
	const leaveItems = leaves.map((l): ApprovalItem => ({
		kind: "leave",
		leave: l,
		key: `leave-${l.id}`,
		sortKey: l.startDate.slice(0, 10),
		employeeName: `${l.employeeFirstName} ${l.employeeLastName}`,
		reason: l.reason || null,
		canApprove: true
	}))
	
	const absenceItems = queue.absences.map((a): ApprovalItem => ({
		kind: "absence",
		absence: a,
		key: `absence-${a.id}`,
		sortKey: `${a.date}T${a.plannedStart ?? "00:00:00"}`,
		employeeName: `${a.employeeFirstName} ${a.employeeLastName}`,
		reason: a.reason,
		canApprove: a.canApprove
	}))
	
	const adjustmentItems = queue.adjustments.map((a): ApprovalItem => ({
		kind: "adjustment",
		adjustment: a,
		key: `adjustment-${a.id}`,
		sortKey: `${a.date}T${a.startTime ?? "00:00:00"}`,
		employeeName: `${a.employeeFirstName} ${a.employeeLastName}`,
		reason: a.reason,
		canApprove: a.canApprove
	}))

	return [...leaveItems, ...absenceItems, ...adjustmentItems].sort((a, b) => a.sortKey.localeCompare(b.sortKey))
}

export async function loadApprovals(signal: AbortSignal): Promise<ApprovalItem[]> {
	const [leaves, queue] = await Promise.all([listLeavesPending(signal), getApprovalQueue(signal)])
	return toApprovals(leaves, queue)
}

export function reviewApproval(item: ApprovalItem, status: ReviewStatus): Promise<void> {
	switch (item.kind) {
		case "absence" :
			return reviewAbsence(item.absence.employeeId, item.absence.id, status)
		case "adjustment" :
			return reviewAdjustment(item.adjustment.employeeId, item.adjustment.id, status)
		case "leave" :
			return reviewLeave(item.leave.id, status)
	}
}

export function approvalLabel(item: ApprovalItem): string {
	switch (item.kind) {
		case "absence" :
			return "Absence"
		case "adjustment" :
			return "Ajustement"
		case "leave" :
			return LEAVE_TYPE_LABELS[item.leave.type]
	}
}

export function approvalWhen(item: ApprovalItem): string {
	switch (item.kind) {
		case "absence" :
			return `${formatLongDate(item.absence.date)} · ${formatTime(item.absence.plannedStart!)} – ${formatTime(item.absence.plannedEnd!)}`
		case "adjustment" :
			return `${formatLongDate(item.adjustment.date)} · ${item.adjustment.startTime && item.adjustment.endTime 
				? `${formatTime(item.adjustment.startTime)} – ${formatTime(item.adjustment.endTime)}`
				: "journée retirée"
			}`
		case "leave" :
			return formatRange(item.leave.startDate, item.leave.endDate)
	}
}

export function approvalOf(item: ApprovalItem): string {
	switch (item.kind) {
		case "absence" :
			return "d'absence"
		case "adjustment" :
			return "d'ajustement"
		case "leave" :
			return item.leave.type === LeaveType.Maladie ? "de congé maladie" : "de congé"
	}
}
