import type { LeaveSegment } from "../../utils/calendar"
import { LeaveBand } from "../leaves/LeaveBand"

type Props = {
	segments: LeaveSegment[]
}

export function LeaveRow({ segments }: Props) {
	return (
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
	)
}