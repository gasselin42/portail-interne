import { LeaveStatus, type LeaveStatusId } from "../api/leaves"

const STATUS_STYLES: Record<LeaveStatusId, { label: string; className: string }> = {
	[LeaveStatus.EnAttente]: {
		label: "En attente",
		className: "bg-amber-50 text-amber-700 ring-amber-600/20",
	},
	[LeaveStatus.Approuve]: {
		label: "Approuvé",
		className: "bg-emerald-50 text-emerald-700 ring-emerald-600/20",
	},
	[LeaveStatus.Refuse]: { label: "Refusé", className: "bg-red-50 text-red-700 ring-red-600/20" },
	[LeaveStatus.Annule]: {
		label: "Annulé",
		className: "bg-slate-100 text-slate-600 ring-slate-500/20",
	},
}

export function StatusBadge({ status }: { status: LeaveStatusId }) {
	const style = STATUS_STYLES[status]
	return (
		<span
			className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${style.className}`}
		>
			{style.label}
		</span>
	)
}