import { LEAVE_STATUS_LABELS, LEAVE_TYPE_LABELS, type LeaveRequestResponse } from "../../api/leaves"
import { focusRing } from "../../ui/buttons"
import { LEAVE_DOT_STYLES, LEAVE_UNPAID_DOT_STYLES } from "../../ui/leaveBands"
import { isUnpaidDay } from "../../ui/leaveBandBackground"
import { segmentsFor } from "../../utils/calendar"
import { formatLongDate, formatShortWeekday } from "../../utils/date"
import { LeaveRow } from "./LeaveRow"
import { ParticipantResponse, type MeetingResponse } from "../../api/meetings"
import { formatMinutes, meetingSlice } from "../../utils/meetings"
import { MEETING_COLOR_STYLES } from "../../ui/meetingColors"
import { HourglassEmptyOutlined } from "@mui/icons-material"

type Props = {
	days: string[]
	leaves: LeaveRequestResponse[]
	meetings: MeetingResponse[]
	today: string
	date: string // le jour sélectionné
	compact: boolean // true sur mobile
	onSelectDay: (day: string) => void
	onOpenMeeting?: (id: number) => void
}

export function MonthView({ days, leaves, meetings, today, date, compact, onSelectDay, onOpenMeeting }: Props) {
	const month = date.slice(0, 7) // "2026-10" : un jour est « du mois » si day.startsWith(month)
	const weeks = Array.from({ length: 6 }, (_, w) => days.slice(w * 7, w * 7 + 7))

	function meetingsOn(day: string): { meeting: MeetingResponse, from: number, to: number }[] {
		return meetings.flatMap((m) => {
			const slice = meetingSlice(m, day)
			return slice ? [{ meeting: m, ...slice }] : []
		}).sort((a, b) => a.from - b.from)
	}

	const weekdayHeader = (align: string) => (
		<ol aria-hidden="true" className="grid grid-cols-7 border-b border-slate-200/80">
			{weeks[0].map((day) => (
				<li key={day} className={`py-2 text-sm ${align}`}>
					{formatShortWeekday(day)}
				</li>
			))}
		</ol>
	)

	if (compact) {
		function leaveOn(day: string) {
			return leaves.find(
				(leave) => leave.startDate.slice(0, 10) <= day && leave.endDate.slice(0, 10) >= day,
			)
		}

		return (
			<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
				{weekdayHeader("text-center")}
				<div className="grid grid-cols-7">
					{days.map((day) => {
						const leave = leaveOn(day)
						const unpaid = leave !== undefined && isUnpaidDay(leave, day)
						const dayMeetings = meetingsOn(day)

						return (
							<button
								key={day}
								type="button"
								aria-pressed={day === date}
								aria-current={day === today ? "date" : undefined}
								aria-label={(leave
									? `${formatLongDate(day)}, ${LEAVE_TYPE_LABELS[leave.type]} (${LEAVE_STATUS_LABELS[leave.status]})${unpaid ? ", sans solde" : ""}`
									: formatLongDate(day))
									+ (dayMeetings.length > 0
										? `, ${dayMeetings.length} événement${dayMeetings.length > 1 ? "s" : ""}`
										: ""
									)} 
								onClick={() => onSelectDay(day)}
								className={`group flex flex-col items-center gap-1 py-1.5 ${focusRing}`}
							>
								<span className={`grid size-7 place-items-center rounded-full group-aria-pressed:bg-sky-600 group-aria-pressed:text-white group-aria-[current=date]:ring-2 group-aria-[current=date]:ring-sky-600 font-semibold ${!day.startsWith(month) ? "text-slate-400" : "text-slate-900"}`}>
									{Number(day.slice(8))}
								</span>
								<span className="flex gap-0.5 h-1.5">
									{leave &&
										<span
											aria-hidden="true"
											className={`size-1.5 rounded-full ${(unpaid ? LEAVE_UNPAID_DOT_STYLES : LEAVE_DOT_STYLES)[leave.status]}`}
										/>
									}
									{dayMeetings.length > 0 &&
										<span
											aria-hidden="true"
											className="size-1.5 rounded-full bg-slate-500"
										/>
									}
								</span>
							</button>
						)
					})}
				</div>
			</div>
		)
	}

	return (
		<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
			{/* 1. En-têtes : 7 colonnes, un par jour */}
			{weekdayHeader("px-3")}

			{/* 2. Semaines */}
			<ol>
				{weeks.map((week) => {
					const segments = segmentsFor(leaves, week)

					return (
						<li key={week[0]} className="min-h-24 border-b border-b-slate-200 last:border-b-0">
							<div className="grid grid-cols-7">
								{week.map((day) => (
									<button
										key={day}
										type="button"
										aria-label={formatLongDate(day)}
										aria-current={day === today ? "date" : undefined}
										onClick={() => onSelectDay(day)}
										className={`m-1 grid size-7 place-items-center justify-self-start rounded-full text-sm hover:bg-slate-100 ${focusRing} aria-[current=date]:bg-sky-600 aria-[current=date]:text-white ${!day.startsWith(month) ? "text-slate-400" : "text-slate-900"}`}
									>
										{Number(day.slice(8))}
									</button>
								))}
							</div>

							{segments.length > 0 && <LeaveRow segments={segments} />}

							<div className="grid grid-cols-7 gap-x-1 px-1 pb-1">
								{week.map((day) => {
									const dayMeetings = meetingsOn(day)
									return (
										<ul key={day} className="min-w-0 space-y-0.5">
											{dayMeetings.slice(0, 3).map((m) => (
												<li key={m.meeting.id} title={`${m.meeting.title}, ${formatMinutes(m.from)} – ${formatMinutes(m.to)}${m.meeting.myResponse === ParticipantResponse.EnAttente ? ", en attente de ta réponse" : m.meeting.myResponse === ParticipantResponse.Decline ? ", décliné" : ""}`}>
													<button type="button" className={`flex items-center gap-1 text-xs text-slate-700 w-full text-left ${focusRing} ${m.meeting.myResponse === ParticipantResponse.Decline ? "opacity-50" : ""}`} onClick={() => onOpenMeeting?.(m.meeting.id)}>
														<span className={`size-2 shrink-0 rounded-full ${MEETING_COLOR_STYLES[m.meeting.color].swatch}`} aria-hidden="true" />
														{m.meeting.myResponse == ParticipantResponse.EnAttente &&
															<span className="flex text-slate-600">
																<HourglassEmptyOutlined fontSize="inherit" aria-hidden="true" />
																<p className="sr-only">En attente de ta réponse</p>
															</span>
														}
														<span className="font-medium shrink-0">{formatMinutes(m.from)}</span>
														<span className={`font-light truncate ${m.meeting.myResponse === ParticipantResponse.Decline ? "line-through" : ""}`}> – {m.meeting.title}</span>
													</button>
												</li>
											))}
											{dayMeetings.length > 3 &&
												<li>
													<button type="button" onClick={() => onSelectDay(day)} className={`text-xs text-slate-500 hover:text-slate-900 ${focusRing}`}>
														+ {dayMeetings.length - 3} autre{dayMeetings.length >= 5 ? "s" : ""}
													</button>
												</li>
											}
										</ul>
									)
								})}
							</div>
						</li>
					)
				})}
			</ol>
		</div>
	)
}
