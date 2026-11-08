import { DaySource, type EffectiveDay } from "../../api/calendar"
import type { MeetingResponse } from "../../api/meetings"
import { focusRing } from "../../ui/buttons"
import { MEETING_COLOR_STYLES } from "../../ui/meetingColors"
import { formatMinutes } from "../../utils/meetings"
import { absenceTitle, minutesToPx, timeToPx } from "../../utils/timeGrid"

type Props = {
	day: EffectiveDay
	startHour: number
	pastPx: number
	showNow: boolean
	meetings: { meeting: MeetingResponse; from: number; to: number, column: number, columns: number }[]
	onOpenMeeting?: (id: number) => void
}

const hatched = "bg-[repeating-linear-gradient(135deg,var(--color-slate-100)_0_6px,transparent_6px_12px)]"

function offDayLabel(day: EffectiveDay): string | null {
	if (day.source === DaySource.Conge) return null
	if (day.source === DaySource.Ferie) return day.holidayName
	if (day.source === DaySource.NonDefini) return "Horaire non défini"
	return "Non travaillé"
}

export function DayColumn({ day, startHour, pastPx, showNow, meetings, onOpenMeeting }: Props) {
	const label = day.startTime === null ? offDayLabel(day) : null
	const Block = onOpenMeeting ? "button" : "div"

	return (
		<div className={`relative border-l border-slate-100 ${day.source === DaySource.NonDefini ? "bg-slate-100" : "bg-slate-50/60"} ${day.startTime === null ? hatched : ""}`}>
			{label && <p className="sticky top-2 px-2 text-center text-xs font-medium text-slate-500">{label}</p>}
			{day.startTime && day.endTime &&
				<div 
					className={`absolute inset-x-1 rounded-md border-l-4 bg-white shadow-sm ${day.source === DaySource.Ajustement ? "border-violet-400" : "border-sky-400"}`}
					style={{
						top: timeToPx(day.startTime, startHour),
						height: timeToPx(day.endTime, startHour) - timeToPx(day.startTime, startHour)
					}}
				/>
			}
			{day.pendingAdjustment && day.pendingAdjustment.startTime && day.pendingAdjustment.endTime &&
				<div
					className="absolute inset-x-1 rounded-md border-2 border-dashed border-violet-300"
					style={{
						top: timeToPx(day.pendingAdjustment.startTime, startHour),
						height: timeToPx(day.pendingAdjustment.endTime, startHour) - timeToPx(day.pendingAdjustment.startTime, startHour),
					}}
				/>
			}
			{day.absences.map((a) => (
				<div
					key={a.id}
					title={absenceTitle(a)}
					className="absolute inset-x-1 rounded-md border-l-4 border-amber-400 bg-amber-100"
					style={{ top: timeToPx(a.start, startHour), height: (a.end ? timeToPx(a.end, startHour) : pastPx) - timeToPx(a.start, startHour) }}
				/>
			))}
			{day.pendingAbsences.map((a) => (
				<div
					key={a.id}
					title={absenceTitle(a)}
					className="absolute inset-x-1 rounded-md border-2 border-dashed border-amber-400"
					style={{ top: timeToPx(a.start, startHour), height: (a.end ? timeToPx(a.end, startHour) : pastPx) - timeToPx(a.start, startHour) }}
				/>
			))}
			<div className="absolute inset-x-1 inset-y-0 pointer-events-none">
				{meetings.map((m) => {
					const duration = m.to - m.from
					const overlapped = m.meeting.isFlexible && meetings.some((o) => o.meeting.isTeamMeeting && o.from < m.to && m.from < o.to)
					return (
						<Block
							key={m.meeting.id}
							title={`${m.meeting.title}, ${formatMinutes(m.from)} – ${formatMinutes(m.to)}`}
							type={onOpenMeeting ? "button" : undefined}
							onClick={() => onOpenMeeting?.(m.meeting.id)}
							className={`
								absolute ring-1 ring-white overflow-hidden rounded-md text-xs px-1.5 pointer-events-auto 
								${(duration < 30) ? "py-0 leading-tight" : "py-0.5"}
								${m.meeting.isFlexible ? "border-2 border-dashed" : "border-l-4"}
								${MEETING_COLOR_STYLES[m.meeting.color].block}
								${overlapped ? "opacity-50" : ""}
								${onOpenMeeting ? `text-left cursor-pointer ${focusRing}` : ""}
							`}
							style={{ top: minutesToPx(m.from, startHour), left: `${(m.column / m.columns) * 100}%`, width: `calc(${100 / m.columns}% - 2px)`, height: (minutesToPx(m.to, startHour) - minutesToPx(m.from, startHour))}}
						>
							<p className="truncate font-medium">{m.meeting.title}</p>
							{(duration >= 45) && <p className="truncate">{formatMinutes(m.from)} – {formatMinutes(m.to)}</p>}
						</Block>
					)
				})}
			</div>
			{pastPx > 0 &&
				<div
					className="absolute inset-x-0 top-0 pointer-events-none bg-slate-400/15"
					style={{ height: pastPx }}
				/>
			}
			{showNow &&
				<div
					className="absolute inset-x-0 z-20 pointer-events-none border-t border-red-800"
					style={{ top: pastPx }}
				/>
			}
		</div>
	)
}