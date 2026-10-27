import { Fragment, useLayoutEffect, useRef } from "react"
import { HOUR_HEIGHT, timeToPx } from "../../utils/timeGrid"
import type { EffectiveDay } from "../../api/calendar"
import { DayColumn } from "./DayColumn"
import type { CalendarHourRangeId } from "../../api/preferences"
import { hourRange } from "../../utils/timeGrid"
import { useNow } from "../../hooks/useNow"
import { todayIso } from "../../utils/date"

export function TimeGrid({ days, range }: { days: EffectiveDay[], range: CalendarHourRangeId }) {
	const scrollRef = useRef<HTMLDivElement>(null)
	const scrolledFor = useRef<string | null>(null)

	const { start, end } = hourRange(days, range)
	const now = useNow()
	const today = todayIso()
	const total = (end - start) * HOUR_HEIGHT
	const nowPx = timeToPx(now, start)
	const nowVisible = nowPx >= 0 && nowPx <= total

	const followNow = days.some((d) => d.date === today) && nowVisible
	const starts = days.flatMap((d) => (d.startTime ? [timeToPx(d.startTime, start)] : []))
	const target =
		followNow
			? nowPx
			: starts.length > 0
				? Math.min(...starts) - HOUR_HEIGHT
				: timeToPx("07:00:00", start)

	const periodKey = `${days[0]?.date}|${days.length}|${range}`

	useLayoutEffect(() => {
		const el = scrollRef.current
		if (!el || scrolledFor.current === periodKey) return
		scrolledFor.current = periodKey
		el.scrollTop = followNow ? target - el.clientHeight / 3 : target
	}, [periodKey, target, followNow])

	return (
		<div ref={scrollRef} className="max-h-[30rem] overflow-y-auto border-t border-slate-200/80">
			<div className="relative pl-14" style={{ height: total }}>
				{Array.from({ length: end - start }, (_, i) => start + i).map((h) => (
					<Fragment key={h}>
						{h !== start && <span className="absolute left-0 w-14 -translate-y-1/2 pr-2 text-right text-xs text-slate-400" style={{ top: (h - start) * HOUR_HEIGHT }}>{h} h</span>}
						<div className="absolute left-14 right-0 border-t border-slate-200/60 pointer-events-none z-10" style={{ top: (h - start) * HOUR_HEIGHT }} />
					</Fragment>
				))}
				<div className="relative grid h-full" style={{ gridTemplateColumns: `repeat(${days.length}, 1fr)`}}>
					{days.map((day) => {
						const pastPx = day.date < today
							? total
							: day.date === today
								? Math.min(Math.max(nowPx, 0), total)
								: 0
	
						return (
							<DayColumn key={day.date} day={day} startHour={start} pastPx={pastPx} showNow={day.date === today && nowVisible} />
						)
					})}
				</div>
			</div>
		</div>
	)
}