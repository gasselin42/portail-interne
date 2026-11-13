import { useNavigate, useOutletContext } from "react-router-dom"
import { getCalendar } from "../../api/calendar"
import { CalendarHourRange, getPreferences } from "../../api/preferences"
import { useAsync } from "../../hooks/useAsync"
import { useMediaQuery } from "../../hooks/useMediaQuery"
import { segmentsFor } from "../../utils/calendar"
import { addDays, startOfWeek, todayIso, utcToZoned } from "../../utils/date"
import { DayView } from "../calendar/DayView"
import { WeekView } from "../calendar/WeekView"
import { ErrorBanner } from "../feedback/Banner"
import { Widget } from "./Widget"

export function WeekWidget() {
	const today = todayIso()
	const from = startOfWeek(today)
	const days = Array.from({ length: 7 }, (_, i) => addDays(from, i))
	const to = days[6]

	const { preferencesVersion } = useOutletContext<{ preferencesVersion: number }>()
	const { data, error, loading, retry } = useAsync((signal) => Promise.all([getCalendar(from, to, signal), getPreferences(signal)]), [from, to, preferencesVersion])
	const [calendar, preferences] = data ?? [null, null]

	const isDesktop = useMediaQuery("(min-width: 48rem)")
	const navigate = useNavigate()

	function handleOpenMeeting(id: number) {
		const meeting = calendar?.meetings.find((m) => m.id === id)
		if (meeting === undefined) return
		const date = utcToZoned(meeting.startAt).date
		navigate(`/calendar?vue=jour&date=${date}&evenement=${id}`)
	}

	return (
		<Widget title={isDesktop ? "Ma semaine" : "Aujourd'hui"} link={{ to: "/calendar", label: "Ouvrir le calendrier" }} className="md:col-span-2">
			{error ? (
				<ErrorBanner message={error} onRetry={retry} />
			) : loading ? (
				<p role="status" className="text-center text-sm text-slate-500">
					Chargement…
				</p>
			) : isDesktop ? (
				<WeekView
					days={days}
					effectiveDays={calendar?.days}
					meetings={calendar?.meetings}
					segments={segmentsFor(calendar?.leaves ?? [], days)}
					range={preferences?.calendarHourRange ?? CalendarHourRange.WorkHours}
					today={today}
					density={preferences?.calendarDensity}
					onOpenMeeting={handleOpenMeeting}
				/>
			) : (
				<DayView
					effectiveDay={calendar?.days.find((d) => d.date === today)}
					meetings={calendar?.meetings}
					segments={segmentsFor(calendar?.leaves ?? [], [today])}
					range={preferences?.calendarHourRange ?? CalendarHourRange.WorkHours}
					density={preferences?.calendarDensity}
					onOpenMeeting={handleOpenMeeting}
				/>
			)}
		</Widget>
	)
}