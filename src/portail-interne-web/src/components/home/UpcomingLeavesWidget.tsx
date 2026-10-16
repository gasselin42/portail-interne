import { Link } from "react-router-dom"
import { LEAVE_TYPE_LABELS, LeaveStatus, listLeaves } from "../../api/leaves"
import { useAsync } from "../../hooks/useAsync"
import { formatRange, todayIso } from "../../utils/date"
import { ErrorBanner } from "../feedback/Banner"
import { Widget } from "./Widget"
import { textLink } from "../../ui/buttons"
import { StatusBadge } from "../leaves/StatusBadge"

export function UpcomingLeavesWidget() {
	const { data, error, loading, retry} = useAsync((signal) => listLeaves(signal), [])
	const today = todayIso()

	const upcoming = (data ?? [])
		.filter(l => l.status === LeaveStatus.Approuve || l.status === LeaveStatus.EnAttente)
		.filter(l => l.endDate.slice(0, 10) >= today)
		.sort((a, b) => a.startDate.localeCompare(b.startDate))
		.slice(0, 3)

	return (
		<Widget title="Mes prochaines absences" link={{ to: "/leaves", label: "Voir mes congés" }}>
			{error ? (
				<ErrorBanner message={error} onRetry={retry} />
			) : loading ? (
				<p role="status" className="text-center text-sm text-slate-500">
					Chargement…
				</p>
			) : upcoming.length === 0 ? (
				<div className="space-y-1 py-2 text-center">
					<p className="text-sm text-slate-500">Aucune absence prévue.</p>
					<Link to="/leaves/new" className={textLink}>Nouvelle demande</Link>
				</div>
			) : (
				<ul className="divide-y divide-slate-100">
					{upcoming.map((leave) => (
						<li key={leave.id} className="flex items-center justify-between py-2">
							<div>
								<p className="text-sm font-medium text-slate-900">
									{LEAVE_TYPE_LABELS[leave.type]}
									{today >= leave.startDate.slice(0, 10) && (
										<span className="ml-2 rounded bg-sky-50 px-1.5 py-0.5 text-xs font-medium text-sky-700">En cours</span>
									)}
								</p>
								<p className="text-xs text-slate-500">{formatRange(leave.startDate, leave.endDate)}</p>
							</div>
							<StatusBadge status={leave.status} />
						</li>
					))}
				</ul>
			)}
		</Widget>
	)
}