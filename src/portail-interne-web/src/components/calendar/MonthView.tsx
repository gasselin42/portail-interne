import { LEAVE_STATUS_LABELS, LEAVE_TYPE_LABELS, type LeaveRequestResponse } from "../../api/leaves"
import { focusRing } from "../../ui/buttons"
import { LEAVE_DOT_STYLES } from "../../ui/leaveBands"
import { segmentsFor } from "../../utils/calendar"
import { formatLongDate, formatShortWeekday } from "../../utils/date"
import { LeaveRow } from "./LeaveRow"

type Props = {
	days: string[]
	leaves: LeaveRequestResponse[]
	today: string
	date: string // le jour sélectionné
	compact: boolean // true sur mobile
	onSelectDay: (day: string) => void
}

export function MonthView({ days, leaves, today, date, compact, onSelectDay }: Props) {
	const month = date.slice(0, 7) // "2026-10" : un jour est « du mois » si day.startsWith(month)
	const weeks = Array.from({ length: 6 }, (_, w) => days.slice(w * 7, w * 7 + 7))

	const weekdayHeader = (align: string) => (
		<ol aria-hidden="true" className="grid grid-cols-7 border-b border-slate-200/80">
			{weeks[0].map((day) => (
				<li key={day} className={`py-2 text-sm ${align}`}>
					{formatShortWeekday(day)}
				</li>
			))}
		</ol>
	)

	if (compact) {
		function leaveOn(day: string) {
			return leaves.find(
				(leave) => leave.startDate.slice(0, 10) <= day && leave.endDate.slice(0, 10) >= day,
			)
		}

		return (
			<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
				{weekdayHeader("text-center")}
				<div className="grid grid-cols-7">
					{days.map((day) => {
						const leave = leaveOn(day)

						return (
							<button
								key={day}
								type="button"
								aria-pressed={day === date}
								aria-current={day === today ? "date" : undefined}
								aria-label={leave
									? `${formatLongDate(day)}, ${LEAVE_TYPE_LABELS[leave.type]} (${LEAVE_STATUS_LABELS[leave.status]})`
									: formatLongDate(day)}
								onClick={() => onSelectDay(day)}
								className={`group flex flex-col items-center gap-1 py-1.5 ${focusRing}`}
							>
								<span className={`grid size-8 place-items-center rounded-full group-aria-pressed:bg-sky-600 group-aria-[current=date]:ring-2 group-aria-[current=date]:ring-sky-600 font-semibold ${!day.startsWith(month) ? "text-slate-400" : "text-slate-900"}`}>
									{Number(day.slice(8))}
								</span>
								<span
									aria-hidden="true"
									className={`size-1.5 rounded-full ${leave ? LEAVE_DOT_STYLES[leave.status] : ""}`}
								/>
							</button>
						)
					})}
				</div>
			</div>
		)
	}

	return (
		<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
			{/* 1. En-têtes : 7 colonnes, un par jour */}
			{weekdayHeader("px-3")}

			{/* 2. Semaines */}
			<ol>
				{weeks.map((week) => (
					<li key={week[0]} className="min-h-24 border-b border-b-slate-200 last:border-b-0">
						<div className="grid grid-cols-7">
							{week.map((day) => (
								<button
									key={day}
									type="button"
									aria-label={formatLongDate(day)}
									aria-current={day === today ? "date" : undefined}
									onClick={() => onSelectDay(day)}
									className={`m-1 grid size-7 place-items-center justify-self-start rounded-full text-sm hover:bg-slate-100 ${focusRing} aria-[current=date]:bg-sky-600 aria-[current=date]:text-white ${!day.startsWith(month) ? "text-slate-400" : "text-slate-900"}`}
								>
									{Number(day.slice(8))}
								</button>
							))}
						</div>

						<LeaveRow segments={segmentsFor(leaves, week)} />
					</li>
				))}
			</ol>
		</div>
	)
}
