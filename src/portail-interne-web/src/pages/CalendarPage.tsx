import { useSearchParams, Link, useOutletContext } from "react-router-dom"
import { PageHeader } from "../components/common/PageHeader"
import {
	addDays,
	addMonths,
	formatLongDate,
	formatMonthYear,
	formatRange,
	getWeekDay,
	isIsoDate,
	iterateDays,
	startOfMonth,
	startOfWeek,
	todayIso,
} from "../utils/date"
import { iconButton, primaryButton, secondaryButton } from "../ui/buttons"
import { ChevronLeft, ChevronRight } from "@mui/icons-material"
import { LeaveStatus } from "../api/leaves"
import { ErrorBanner } from "../components/feedback/Banner"
import { segmentsFor } from "../utils/calendar"
import { LEAVE_BAND_STYLES } from "../ui/leaveBands"
import { useMediaQuery } from "../hooks/useMediaQuery"
import { WeekView } from "../components/calendar/WeekView"
import { DayView } from "../components/calendar/DayView"
import { SegmentedControl } from "../components/common/SegmentedControl"
import { MonthView } from "../components/calendar/MonthView"
import { leaveSwatchBackground } from "../ui/leaveBandBackground"
import { getCalendar } from "../api/calendar"
import { getPreferences } from "../api/preferences"
import { useAsync } from "../hooks/useAsync"
import { MeetingDialog } from "../components/meetings/MeetingDialog"
import type { MeetingColor, MeetingResponse } from "../api/meetings"
import { MeetingDetailDialog } from "../components/meetings/MeetingDetailDialog"

type View = "jour" | "semaine" | "mois"

const DESKTOP_VIEWS: { value: View, label: string }[] = [
	{ value: "jour", label: "Jour" },
	{ value: "semaine", label: "Semaine" },
	{ value: "mois", label: "Mois" },
]

const MOBILE_VIEWS = DESKTOP_VIEWS.filter((option) => option.value !== "semaine")

const LEAVE_LEGEND = [
	{ status: LeaveStatus.Approuve, unpaid: false, label: "Approuvé" },
	{ status: LeaveStatus.EnAttente, unpaid: false, label: "En attente" },
	{ status: LeaveStatus.Approuve, unpaid: true, label: "Approuvé sans solde" },
	{ status: LeaveStatus.EnAttente, unpaid: true, label: "En attente sans solde" },
]

const MEETING_LEGEND_STYLE = "h-3 w-6 rounded-sm border-2 border-dashed border-blue-500 bg-blue-100"

type CalendarRange = {
	from: string
	to: string
	days: string[]
	shift: (direction: number) => string
}

function getRange(view: View, date: string): CalendarRange {
	switch (view) {
		case "jour":
			return {
				from: date,
				to: date,
				days: [date],
				shift: (direction) => addDays(date, direction),
			}

		case "semaine": {
			const monday = startOfWeek(date)
			return {
				from: monday,
				to: addDays(monday, 6),
				days: iterateDays(monday, 7),
				shift: (direction) => addDays(date, 7 * direction),
			}
		}

		case "mois": {
			const monthFirst = startOfMonth(date)
			const weekDay = getWeekDay(monthFirst)
			const weekStart = startOfWeek(monthFirst)
			const firstDay = weekDay >= 1 && weekDay <= 3 ? addDays(weekStart, -7) : weekStart
			return {
				from: firstDay,
				to: addDays(firstDay, 41),
				days: iterateDays(firstDay, 42),
				shift: (direction) => addMonths(date, direction),
			}
		}
	}
}

export function CalendarPage() {
	const [searchParams, setSearchParams] = useSearchParams()

	const isDesktop = useMediaQuery("(min-width: 48rem)")

	const today = todayIso()
	const dateParam = searchParams.get("date")
	const vueParam = searchParams.get("vue")
	const newEventOpen = searchParams.get("nouvel-evenement") === "1"
	const meetingParam = searchParams.get("evenement")

	// Le test ignore une URL trafiquée comme ?evenement=abc
	const openedMeetingId = meetingParam && /^\d+$/.test(meetingParam) ? Number(meetingParam) : null

	const date = dateParam && isIsoDate(dateParam) ? dateParam : today
	const { preferencesVersion } = useOutletContext<{ preferencesVersion: number }>()

	const view: View =
		vueParam === "mois" || vueParam === "jour"
			? vueParam
			: vueParam === "semaine" && isDesktop
				? "semaine"
				: isDesktop
					? "semaine"
					: "jour"

	const { from, to, days, shift } = getRange(view, date)

	const { data, error, loading, retry, setData } = useAsync((signal) => Promise.all([getCalendar(from, to, signal), getPreferences(signal)]), [from, to, preferencesVersion])
	const [calendar, preferences] = data ?? [null, null]

	const leaves = calendar?.leaves ?? null
	const segments = view === "mois" ? segmentsFor(leaves ?? [], [date]) : segmentsFor(leaves ?? [], days)
	const effectiveDays = calendar?.days ?? null
	const meetings = calendar?.meetings ?? []
	const range = preferences?.calendarHourRange

	const containsToday =
		view === "mois" ? startOfMonth(date) === startOfMonth(today) : from <= today && today <= to

	const title =
		view === "mois"
			? formatMonthYear(date)
			: view === "semaine"
				? formatRange(from, to)
				: formatLongDate(date)

	const noLeaves =
		view === "mois"
			? "Aucune absence ce mois-là"
			: view === "semaine"
				? "Aucune absence cette semaine."
				: "Aucune absence ce jour-là."

	const openNew = () => setSearchParams((prev) => { prev.set("nouvel-evenement", "1"); return prev })
	const closeNew = () => setSearchParams((prev) => { prev.delete("nouvel-evenement"); return prev })

	const openMeeting = (id: number) => setSearchParams((prev) => { prev.set("evenement", String(id)); return prev })
	const closeMeeting = () => setSearchParams((prev) => { prev.delete("evenement"); return prev })

	function handleCreated(meeting: MeetingResponse) {
		closeNew()
		setData(([c, p]) => [{ ...c, meetings: [...c.meetings, meeting] }, p])
	}

	function handleColorChanged(id: number, color: MeetingColor) {
		setData(([c, p]) => [{ ...c, meetings: c.meetings.map((m) => m.id === id ? { ...m, color } : m) }, p])
	}

	function handleCancelled(id: number) {
		closeMeeting()
		setData(([c, p]) => [{ ...c, meetings: c.meetings.filter((m) => m.id !== id) }, p])
	}

	function handleUpdated(meeting: MeetingResponse) {
		closeMeeting()
		setData(([c, p]) => [{ ...c, meetings: c.meetings.map((m) => m.id === meeting.id ? meeting : m) }, p])
	}

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				eyebrow="Planning"
				title="Calendrier"
				description="Vois tes absences, en attente ou approuvées."
				actions={
					<>
						<button
							type="button"
							onClick={openNew}
							className={primaryButton}
						>
							Nouvel événement
						</button>
						<Link to="/leaves?nouvelle=1" className={secondaryButton}>
							Nouvelle demande
						</Link>
					</>
				}
			/>

			<div className="mb-4 flex flex-wrap items-center justify-between gap-3">
				<h2 aria-live="polite" className="text-lg font-semibold text-slate-900 first-letter:uppercase">
					{title}
				</h2>
				<div className="flex items-center gap-1">
					<SegmentedControl
						label="Vue"
						options={isDesktop ? DESKTOP_VIEWS : MOBILE_VIEWS}
						value={view}
						onChange={(v) => setSearchParams(dateParam ? { vue: v, date } : { vue: v })}
					/>
					<button
						type="button"
						onClick={() => setSearchParams({ vue: view, date: shift(-1) })}
						aria-label={view === "jour" ? "Jour précédent" : "Semaine précédente"}
						className={iconButton}
					>
						<ChevronLeft fontSize="large" />
					</button>
					<button
						type="button"
						onClick={() => setSearchParams({ vue: view, date: shift(1) })}
						aria-label={view === "jour" ? "Jour suivant" : "Semaine suivante"}
						className={iconButton}
					>
						<ChevronRight fontSize="large" />
					</button>
					<button
						type="button"
						disabled={containsToday}
						onClick={() => setSearchParams({ vue: view })}
						className={secondaryButton}
					>
						Aujourd'hui
					</button>
				</div>
			</div>

			<div className="mb-3 min-h-6">
				{error ? (
					<ErrorBanner message={error} onRetry={retry} />
				) : loading ? (
					<p role="status" className="text-center text-sm text-slate-500">
						Chargement…
					</p>
				) : leaves?.length === 0 ? (
					<p role="status" className="text-center text-sm text-slate-500">
						{noLeaves}
					</p>
				) : null}
			</div>

			{view === "jour" && <DayView segments={segments} effectiveDay={effectiveDays?.[0]} meetings={meetings} range={range} onOpenMeeting={openMeeting} />}
			{view === "semaine" && <WeekView days={days} segments={segments} today={today} effectiveDays={effectiveDays ?? undefined} meetings={meetings} range={range} onOpenMeeting={openMeeting} />}
			{view === "mois" && (
				<MonthView
					days={days}
					leaves={leaves ?? []}
					meetings={meetings}
					today={today}
					date={date}
					compact={!isDesktop}
					onSelectDay={(day) =>
						setSearchParams(isDesktop ? { vue: "jour", date: day } : { vue: "mois", date: day })
					}
					onOpenMeeting={openMeeting}
				/>
			)}
			{view === "mois" && !isDesktop && (
				<DayView
					segments={segments}
					effectiveDay={effectiveDays?.find((d) => d.date === date)}
					meetings={meetings}
					range={range}
					onOpenMeeting={openMeeting}
				/> 
			)}

			<div className="mt-3 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs">
				<span className="font-medium text-slate-700">Congés :</span>
				<ul aria-label="Légende des congés" className="flex flex-wrap gap-4 gap-y-1 text-xs text-slate-600">
					{LEAVE_LEGEND.map(({ status, unpaid, label }) => (
						<li key={label} className="flex items-center gap-1.5">
							<span
								aria-hidden="true"
								className={`inline-block h-3 w-6 rounded-sm ${LEAVE_BAND_STYLES[status]}`}
								style={{ background: leaveSwatchBackground(status === LeaveStatus.Approuve, unpaid) }}
							/>
							{label}
						</li>
					))}
				</ul>
			</div>
			<div className="mt-3 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs">
				<span className="font-medium text-slate-700">Événements :</span>
				<ul aria-label="Légende des événements" className="flex flex-wrap gap-4 gap-y-1 text-xs text-slate-600">
					{["Bloc souple", "Bloc souple chevauché"].map((label) => (
						<li key={label} className="flex items-center gap-1.5">
							<span
								aria-hidden="true"
								className={`${MEETING_LEGEND_STYLE} ${label === "Bloc souple chevauché" ? "opacity-50" : ""}`}
							/>
							{label}
						</li>
					))}
				</ul>
			</div>
			<MeetingDialog open={newEventOpen} initialDate={date} onClose={closeNew} onSaved={handleCreated} />
			<MeetingDetailDialog meetingId={openedMeetingId} onClose={closeMeeting} onColorChanged={handleColorChanged} onCancelled={handleCancelled} onUpdated={handleUpdated} />
		</div>
	)
}
