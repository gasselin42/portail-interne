import { DaySource, type EffectiveDay } from "../../api/calendar"
import { absenceTitle, timeToPx } from "../../utils/timeGrid"

type Props = {
	day: EffectiveDay
	startHour: number
	pastPx: number
	showNow: boolean
}

const hatched = "bg-[repeating-linear-gradient(135deg,var(--color-slate-100)_0_6px,transparent_6px_12px)]"

function offDayLabel(day: EffectiveDay): string | null {
	if (day.source === DaySource.Conge) return null
	if (day.source === DaySource.Ferie) return day.holidayName
	if (day.source === DaySource.NonDefini) return "Horaire non défini"
	return "Non travaillé"
}

export function DayColumn({ day, startHour, pastPx, showNow }: Props) {
	const label = day.startTime === null ? offDayLabel(day) : null

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