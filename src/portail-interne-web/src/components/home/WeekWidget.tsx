import { getCalendar } from "../../api/calendar"
import { useAsync } from "../../hooks/useAsync"
import { useMediaQuery } from "../../hooks/useMediaQuery"
import { segmentsFor } from "../../utils/calendar"
import { addDays, startOfWeek, todayIso } from "../../utils/date"
import { DayView } from "../calendar/DayView"
import { WeekView } from "../calendar/WeekView"
import { ErrorBanner } from "../feedback/Banner"
import { Widget } from "./Widget"

export function WeekWidget() {
	const today = todayIso()
	const from = startOfWeek(today)
	const days = Array.from({ length: 7 }, (_, i) => addDays(from, i))
	const to = days[6]

	const { data, error, loading, retry } = useAsync((signal) => getCalendar(from, to, signal), [from, to])

	const isDesktop = useMediaQuery("(min-width: 48rem)")

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
					segments={segmentsFor(data?.leaves ?? [], days)}
					today={today}
				/>
			) : (
				<DayView segments={segmentsFor(data?.leaves ?? [], [today])} />
			)}
		</Widget>
	)
}