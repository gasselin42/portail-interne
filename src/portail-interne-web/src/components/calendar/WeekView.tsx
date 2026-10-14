import type { LeaveSegment } from "../../utils/calendar"
import { formatDayHeader } from "../../utils/date"
import { LeaveBand } from "../leaves/LeaveBand"

type Props = {
	days: string[]
	segments: LeaveSegment[]
	today: string
}

export function WeekView({ days, segments, today }: Props) {
	return (
		<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
			{/* 1. En-têtes : 7 colonnes, un par jour */}
			<ol className="grid grid-cols-7 border-b border-slate-200/80">
				{days.map((day) => (
					<li
						key={day}
						aria-current={day === today ? "date" : undefined}
						className="group px-3 py-2"
					>
						<time
							dateTime={day}
							className="text-sm font-medium text-slate-700 group-aria-[current=date]:font-semibold group-aria-[current=date]:text-sky-700"
						>
							{formatDayHeader(day)}
						</time>
					</li>
				))}
			</ol>

			{/* 2. Rangée « toute la journée » : un <li> par segment */}
			<ul
				aria-label="Absences de la semaine"
				className="grid min-h-12 grid-flow-row-dense grid-cols-7 gap-y-1 py-1"
			>
				{segments.map((segment) => (
					<LeaveBand
						key={segment.leave.id}
						segment={segment}
						compact
						style={{ gridColumn: `${segment.column} / span ${segment.span}` }}
					/>
				))}
			</ul>
		</div>
	)
}