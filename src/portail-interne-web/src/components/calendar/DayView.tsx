import type { EffectiveDay } from "../../api/calendar"
import type { CalendarHourRangeId } from "../../api/preferences"
import type { LeaveSegment } from "../../utils/calendar"
import { dayNote } from "../../utils/timeGrid"
import { LeaveBand } from "../leaves/LeaveBand"
import { TimeGrid } from "./TimeGrid"

type Props = {
	segments: LeaveSegment[]
	effectiveDay?: EffectiveDay
	range?: CalendarHourRangeId
}

export function DayView({ segments, effectiveDay, range }: Props) {
	const note = effectiveDay ? dayNote(effectiveDay) : null
	return (
		<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
			<ul aria-label="Absences du jour" className="min-h-12 space-y-1.5 py-2">
				{segments.map((segment) => (
					<LeaveBand key={segment.leave.id} segment={segment} />
				))}
			</ul>
			{note && <p className="text-[11px] font-medium text-violet-700 border-t border-slate-200/80 px-3 py-2">{note}</p>}
			{effectiveDay && range !== undefined && <TimeGrid days={[effectiveDay]} range={range} />}
		</div>
	)
}