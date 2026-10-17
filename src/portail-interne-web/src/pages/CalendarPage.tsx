import { useState, useEffect } from "react"
import { useSearchParams, Link } from "react-router-dom"
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
import {
	LeaveStatus,
	listCalendar,
	type LeaveRequestResponse,
} from "../api/leaves"
import { ErrorBanner, type PageError } from "../components/feedback/Banner"
import { segmentsFor } from "../utils/calendar"
import { LEAVE_BAND_STYLES } from "../ui/leaveBands"
import { useMediaQuery } from "../hooks/useMediaQuery"
import { WeekView } from "../components/calendar/WeekView"
import { DayView } from "../components/calendar/DayView"
import { SegmentedControl } from "../components/common/SegmentedControl"
import { MonthView } from "../components/calendar/MonthView"
import { leaveSwatchBackground } from "../ui/leaveBandBackground"

type CalendarResult =
	| { from: string; to: string; leaves: LeaveRequestResponse[] }
	| { from: string; to: string; error: PageError }

type View = "jour" | "semaine" | "mois"

const DESKTOP_VIEWS: { value: View, label: string }[] = [
	{ value: "jour", label: "Jour" },
	{ value: "semaine", label: "Semaine" },
	{ value: "mois", label: "Mois" },
]

const MOBILE_VIEWS = DESKTOP_VIEWS.filter((option) => option.value !== "semaine")

const LEGEND = [
	{ status: LeaveStatus.Approuve, unpaid: false, label: "Approuvé" },
	{ status: LeaveStatus.EnAttente, unpaid: false, label: "En attente" },
	{ status: LeaveStatus.Approuve, unpaid: true, label: "Approuvé sans solde" },
	{ status: LeaveStatus.EnAttente, unpaid: true, label: "En attente sans solde" },
]


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
	const [result, setResult] = useState<CalendarResult | null>(null)
	const [retry, setRetry] = useState(0)

	const isDesktop = useMediaQuery("(min-width: 48rem)")

	const today = todayIso()
	const dateParam = searchParams.get("date")
	const vueParam = searchParams.get("vue")
	const date = dateParam && isIsoDate(dateParam) ? dateParam : today

	const view: View =
		vueParam === "mois" || vueParam === "jour"
			? vueParam
			: vueParam === "semaine" && isDesktop
				? "semaine"
				: isDesktop
					? "semaine"
					: "jour"

	const { from, to, days, shift } = getRange(view, date)

	const containsToday =
		view === "mois" ? startOfMonth(date) === startOfMonth(today) : from <= today && today <= to

	const current = result?.from === from && result.to === to ? result : null
	const leaves = current && "leaves" in current ? current.leaves : null
	const segments = view === "mois" ? segmentsFor(leaves ?? [], [date]) : segmentsFor(leaves ?? [], days)
	const error = current && "error" in current ? current.error : null

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

	useEffect(() => {
		const controller = new AbortController()

		listCalendar(from, to, controller.signal)
			.then((leaves) => setResult({ from, to, leaves }))
			.catch((e) => {
				if (controller.signal.aborted) return
				setResult({
					from,
					to,
					error: {
						message: e instanceof Error ? e.message : "Erreur",
						dismissible: false,
					},
				})
			})

		return () => controller.abort()
	}, [from, to, retry])

	function handleRetry() {
		setResult(null)
		setRetry((n) => n + 1)
	}

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				eyebrow="Planning"
				title="Calendrier"
				description="Vois tes absences, en attente ou approuvées."
				actions={
					<Link to="/leaves/new" className={primaryButton}>
						Nouvelle demande
					</Link>
				}
			/>

			<div className="mb-4 flex flex-wrap items-center justify-between gap-3">
				<h2 aria-live="polite" className="text-lg font-semibold text-slate-900">
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
					<ErrorBanner message={error.message} onRetry={handleRetry} />
				) : current === null ? (
					<p role="status" className="text-center text-sm text-slate-500">
						Chargement…
					</p>
				) : leaves?.length === 0 ? (
					<p role="status" className="text-center text-sm text-slate-500">
						{noLeaves}
					</p>
				) : null}
			</div>

			{view === "jour" && <DayView segments={segments} />}
			{view === "semaine" && <WeekView days={days} segments={segments} today={today} />}
			{view === "mois" && (
				<MonthView
					days={days}
					leaves={leaves ?? []}
					today={today}
					date={date}
					compact={!isDesktop}
					onSelectDay={(day) =>
						setSearchParams(isDesktop ? { vue: "jour", date: day } : { vue: "mois", date: day })
					}
				/>
			)}
			{view === "mois" && !isDesktop && <DayView segments={segments} />}

			<ul aria-label="Légende" className="mt-3 flex gap-4 gap-y-1 text-xs text-slate-600">
				{LEGEND.map(({ status, unpaid, label }) => (
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
	)
}
