import type { CSSProperties } from "react"
import { LEAVE_STATUS_LABELS, LEAVE_TYPE_LABELS } from "../../api/leaves"
import { LEAVE_BAND_STYLES, leaveBandBase } from "../../ui/leaveBands"
import type { LeaveSegment } from "../../utils/calendar"
import { formatRange } from "../../utils/date"

type Props = {
	segment: LeaveSegment
	/** true : texte court (colonnes étroites), texte complet pour les lecteurs d'écran. */
	compact?: boolean
	/** Placement dans une grille (ex. gridColumn en vue semaine). */
	style?: CSSProperties
}

/** Bandeau d'un congé dans le calendrier : plein si approuvé, hachuré si en attente (R5). */
export function LeaveBand({ segment, compact = false, style}: Props) {
	const { leave } = segment
	const type = LEAVE_TYPE_LABELS[leave.type]
	const status = LEAVE_STATUS_LABELS[leave.status]
	const label = `${type} · ${status}`
	const fullLabel = `${type} (${status}), ${formatRange(leave.startDate, leave.endDate)}`

	return (
		<li
			title={fullLabel}
			style={style}
			className={[
				leaveBandBase,
				LEAVE_BAND_STYLES[leave.status],
				compact && "py-0.5 text-xs",
				!compact && "py-2 text-sm",
				!segment.continuesBefore && "ml-1 rounded-l-md",
				!segment.continuesAfter && "mr-1 rounded-r-md",
			].filter(Boolean).join(" ")}
		>
			{compact ? (
				<>
					<span aria-hidden="true">{label}</span>
					<span className="sr-only">{fullLabel}</span>
				</>
			) : (
				fullLabel
			)}
		</li>
	)
}