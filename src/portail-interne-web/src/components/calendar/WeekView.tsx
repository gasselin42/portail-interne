import type { EffectiveDay } from "../../api/calendar"
import type { CalendarHourRangeId } from "../../api/preferences"
import type { LeaveSegment } from "../../utils/calendar"
import { formatDayHeader } from "../../utils/date"
import { dayNote } from "../../utils/timeGrid"
 import { LeaveRow } from "./LeaveRow"
import { TimeGrid } from "./TimeGrid"

type Props = {
	days: string[]
	segments: LeaveSegment[]
	today: string
	effectiveDays?: EffectiveDay[]
	range?: CalendarHourRangeId
}

export function WeekView({ days, segments, today, effectiveDays, range }: Props) {
	return (
		<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
			{/* 1. En-têtes : 7 colonnes, un par jour */}
			<header className={effectiveDays ? "pl-14" : ""}>
				<ol className="grid grid-cols-7 border-b border-slate-200/80">
					{days.map((day) => {
						const effective = effectiveDays?.find((d) => d.date === day)
						const note = effective ? dayNote(effective) : null
						return (
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
								{note && <p className="text-[11px] font-medium text-violet-700">{note}</p>}
							</li>
						)
					})}
				</ol>
			</header>

			{/* 2. Rangée « toute la journée » : un <li> par segment */}
			<div className={effectiveDays ? "pl-14" : ""}>	
				<LeaveRow segments={segments} />
			</div>

			{effectiveDays && range !== undefined && <TimeGrid days={effectiveDays} range={range} />}
		</div>
	)
}