import { getAbsences } from "../../api/absences";
import { LEAVE_TYPE_LABELS, listLeaves } from "../../api/leaves";
import { useAsync } from "../../hooks/useAsync";
import { useSession } from "../../session/useSession";
import { formatDayHeader, formatRange, formatTime, nowTime, todayIso } from "../../utils/date";
import { toUpcoming } from "../../utils/upcoming";
import { ErrorBanner } from "../feedback/Banner";
import { StatusBadge } from "../leaves/StatusBadge";

export function UpcomingList({ limit = 3 }: { limit?: number }) {
	const { me } = useSession()
	const today = todayIso()
	const now = nowTime()
	const { data, error, loading, retry } = useAsync(
		(signal) => Promise.all([listLeaves(signal), getAbsences(me!.employeeId, signal)]),
		[],
	)
	const items = data ? toUpcoming(data[0], data[1], today, now).slice(0, limit) : []

	return (
		<div>
			{error ? (
				<ErrorBanner message={error} onRetry={retry} />
			) : loading ? (
				<p role="status" className="text-center text-sm text-slate-500">
					Chargement…
				</p>
			) : items.length === 0 ? (
				<div className="space-y-1 py-2 text-center">
					<p className="text-sm text-slate-500">Rien de prévu.</p>
				</div>
			) : (
				<ul className="divide-y divide-slate-100">
					{items.map((item) => (
						<li
							key={`${item.kind}-${item.kind === "absence" ? item.absence.id : item.leave.id}`}
							className="flex items-center justify-between py-2"
						>
							{item.kind === "leave" && (
								<>
									<div>
										<p className="text-sm font-medium text-slate-900">
											{LEAVE_TYPE_LABELS[item.leave.type]}
											{today >= item.leave.startDate.slice(0, 10) && (
												<span className="ml-2 rounded bg-sky-50 px-1.5 py-0.5 text-xs font-medium text-sky-700">En cours</span>
											)}
										</p>
										<p className="text-xs text-slate-500">{formatRange(item.leave.startDate, item.leave.endDate)}</p>
									</div>
									<StatusBadge status={item.leave.status} />
								</>
							)}
							{item.kind === "absence" && (
								<>
									<div>
										<p className="text-sm font-medium text-slate-900">
											Absence
										</p>
										<p className="text-xs text-slate-500">{formatDayHeader(item.absence.date)} · {formatTime(item.absence.start)} – {formatTime(item.absence.end!)}{item.absence.reason !== null && item.absence.reason.trim() !== "" ? ` · ${item.absence.reason.trim()}` : ""}</p>
									</div>
									<StatusBadge status={item.absence.status} />
								</>
							)}
						</li>
					))}
				</ul>
			)
		}
		</div>
	)
}