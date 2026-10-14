import type { LeaveSegment } from "../../utils/calendar"
import { LeaveBand } from "../leaves/LeaveBand"

type Props = {
	segments: LeaveSegment[]
}

export function DayView({ segments }: Props) {
	return (
		<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
			<ul aria-label="Absences du jour" className="min-h-12 space-y-1.5 py-2">
				{segments.map((segment) => (
					<LeaveBand key={segment.leave.id} segment={segment} />
				))}
			</ul>
		</div>
	)
}