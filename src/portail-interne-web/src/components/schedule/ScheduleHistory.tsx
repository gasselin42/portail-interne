import { Fragment } from "react"
import type { WorkScheduleResponse } from "../../api/schedules"
import { sectionTitle } from "../../ui/fields"
import { addDays, formatDate, formatTime, todayIso } from "../../utils/date"
import { currentSchedule, DAY_LABELS, formatDuration, toDraft, weeklyMinutes } from "../../utils/schedule"
import { ExpandMoreOutlined } from "@mui/icons-material"

type Props = {
	schedules: WorkScheduleResponse[]
	excludeId: number | null
}

const BADGE_TEXT = ({ upcoming: "À venir", current: "En vigueur", past: "Passé" })
const BADGE_COLOR = ({ upcoming: "bg-amber-50 text-amber-700", current: "bg-sky-50 text-sky-700", past: "bg-slate-100 text-slate-600" })

export function ScheduleHistory({ schedules, excludeId }: Props) {
	const shown = schedules.filter((s) => s.id !== excludeId)
	if (shown.length === 0) return null

	const current = currentSchedule(schedules, todayIso())
	const today = todayIso()

	return (
		<div>
			<h2 className={sectionTitle}>Historique</h2>
			<ul className="mt-2 divide-y divide-slate-100">
				{schedules.map((schedule, i) => {
					if (schedule.id === excludeId) return null
					
					const status = schedule.effectiveFrom > today ? "upcoming" : schedule.id === current?.id ? "current" : "past"
					
					return (
						<li key={schedule.id}>
							<details className="group">
								<summary className="flex justify-between cursor-pointer py-3 text-sm items-center gap-2 list-none">
									<span>
										<span>
											{i === 0
												? `Depuis le ${formatDate(schedule.effectiveFrom)}`
												: `Du ${formatDate(schedule.effectiveFrom)} au ${formatDate(addDays(schedules[i - 1].effectiveFrom, -1))}`}
										</span>
										<span> · </span>
										<span>{formatDuration(weeklyMinutes(toDraft(schedule)))} / sem.</span>
									</span>
									<div>
										<span className={`ml-2 rounded px-1.5 py-0.5 text-xs font-medium ${BADGE_COLOR[status]}`}>{BADGE_TEXT[status]}</span>
										<ExpandMoreOutlined fontSize="small" className="text-slate-400 transition group-open:rotate-180" />
									</div>
								</summary>

								<dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 pb-3 text-sm">
									{schedule.days.map((day) => {
										const label = DAY_LABELS[day.dayOfWeek]
										return (
											<Fragment key={day.dayOfWeek}>
												<dt className="text-slate-500">{label}</dt>
												<dd className="text-slate-900">{formatTime(day.startTime)} – {formatTime(day.endTime)} · pause {day.breakMinutes} min</dd>
											</Fragment>
										)
									})}
								</dl>

								<p className="text-xs text-slate-500 pb-3">Créé par {schedule.createdByFirstName} {schedule.createdByLastName}</p>
							</details>
						</li>
					)
				})}
			</ul>
		</div>
	)
}