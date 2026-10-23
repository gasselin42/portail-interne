import { getCalendar } from "../../api/calendar"
import { useAsync } from "../../hooks/useAsync"
import { sectionTitle } from "../../ui/fields"
import { addDays, formatDayHeader, nowTime, startOfWeek, todayIso } from "../../utils/date"
import { daySchedule, dayStatus } from "../../utils/presence"
import { ErrorBanner } from "../feedback/Banner"

export function PresenceSummary() {
	const today = todayIso()
	const from = startOfWeek(today)
	const days = Array.from({ length: 7 }, (_, i) => addDays(from, i))
	const to = days[6]

	const { data, error, loading, retry } = useAsync((signal) => getCalendar(from, to, signal), [from, to])

	const day = data?.days.find((d) => d.date === today)

	return (
		<div>
			{error ? (
				<ErrorBanner message={error} onRetry={retry} />
			) : loading ? (
				<p role="status" className="text-center text-sm text-slate-500">
					Chargement…
				</p>
			) : (
				<div>
					<p className="text-base font-semibold text-slate-900 rounded-lg bg-slate-50 px-4 py-3">{day && dayStatus(day, nowTime())}</p>
					<h3 className={`${sectionTitle} mt-4 mb-1`}>Cette semaine</h3>
					<ul>
						{data?.days.map((d) => (
							<li
								key={d.date}
								aria-current={d.date === today ? "date" : undefined}
								className="flex justify-between text-sm aria-[current=date]:font-semibold aria-[current=date]:text-sky-700"
							>
								<span>{formatDayHeader(d.date)}</span>
								<span>{daySchedule(d)}</span>
							</li>
						))}
					</ul>
				</div>
			)}
		</div>
	)
}