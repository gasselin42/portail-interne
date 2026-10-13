import { useState, useEffect } from "react"
import { useSearchParams, Link } from "react-router-dom"
import { PageHeader } from "../components/common/PageHeader"
import {
	addDays,
	formatDayHeader,
	formatLongDate,
	formatRange,
	isIsoDate,
	startOfWeek,
	todayIso,
} from "../utils/date"
import { iconButton, primaryButton, secondaryButton } from "../ui/buttons"
import { ChevronLeft, ChevronRight } from "@mui/icons-material"
import {
	LEAVE_STATUS_LABELS,
	LeaveStatus,
	listCalendar,
	type LeaveRequestResponse,
} from "../api/leaves"
import { ErrorBanner, type PageError } from "../components/feedback/Banner"
import { toSegment } from "../utils/calendar"
import { LEAVE_BAND_STYLES } from "../ui/leaveBands"
import { useMediaQuery } from "../hooks/useMediaQuery"
import { LeaveBand } from "../components/leaves/LeaveBand"

type CalendarResult =
	| { from: string; to: string; leaves: LeaveRequestResponse[] }
	| { from: string; to: string; error: PageError }

export function CalendarPage() {
	const [searchParams, setSearchParams] = useSearchParams()
	const [result, setResult] = useState<CalendarResult | null>(null)
	const [retry, setRetry] = useState(0)

	const isDesktop = useMediaQuery("(min-width: 48rem)")

	const today = todayIso()
	const param = searchParams.get("date")
	const date = param && isIsoDate(param) ? param : today

	const view = isDesktop ? "semaine" : "jour"
	const from = view === "semaine" ? startOfWeek(date) : date
	const to = view === "semaine" ? addDays(from, 6) : date
	const days = Array.from({ length: view === "semaine" ? 7 : 1 }, (_, i) => addDays(from, i))
	const step = view === "jour" ? 1 : 7

	const containsToday = from <= today && today <= to

	const current = result?.from === from && result.to === to ? result : null
	const leaves = current && "leaves" in current ? current.leaves : null
	const segments = leaves?.map((leave) => toSegment(leave, days)) ?? []
	const error = current && "error" in current ? current.error : null

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
					{view === "jour" ? formatLongDate(date) : formatRange(from, to)}
				</h2>
				<div className="flex items-center gap-1">
					<button
						type="button"
						onClick={() => setSearchParams({ date: addDays(date, -step) })}
						aria-label={view === "jour" ? "Jour précédent" : "Semaine précédente"}
						className={iconButton}
					>
						<ChevronLeft fontSize="large" />
					</button>
					<button
						type="button"
						onClick={() => setSearchParams({ date: addDays(date, step) })}
						aria-label={view === "jour" ? "Jour suivant" : "Semaine suivante"}
						className={iconButton}
					>
						<ChevronRight fontSize="large" />
					</button>
					<button
						type="button"
						disabled={containsToday}
						onClick={() => setSearchParams({})}
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
						{view === "jour" ? "Aucune absence ce jour-là." : "Aucune absence cette semaine."}
					</p>
				) : null}
			</div>

			{/* Mobile : vue jour */}
			<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50 md:hidden">
				<ul aria-label="Absences du jour" className="min-h-12 space-y-1.5 py-2">
					{segments.map((segment) => (
						<LeaveBand key={segment.leave.id} segment={segment} />
					))}
				</ul>
			</div>

			{/* Grand écran : en-têtes + rangée « toute la journée » */}
			<div className="hidden overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50 md:block">
				{/* 1. En-têtes : 7 colonnes, un par jour */}
				<ol className="grid grid-cols-7 border-b border-slate-200/80">
					{days.map((day) => (
						<li
							key={day}
							aria-current={day === today ? "date" : undefined}
							className="group px-3 py-2"
						>
							<time
								dateTime={day}
								className="text-sm font-medium text-slate-700 group-aria-[current=date]:font-semibold group-aria-[current=date]:text-sky-700"
							>
								{formatDayHeader(day)}
							</time>
						</li>
					))}
				</ol>

				{/* 2. Rangée « toute la journée » : un <li> par segment */}
				<ul
					aria-label="Absences de la semaine"
					className="grid min-h-12 grid-flow-row-dense grid-cols-7 gap-y-1 py-1"
				>
					{segments.map((segment) => (
						<LeaveBand
							key={segment.leave.id}
							segment={segment}
							compact
							style={{ gridColumn: `${segment.column} / span ${segment.span}` }}
						/>
					))}
				</ul>
			</div>

			<ul aria-label="Légende" className="mt-3 flex gap-4 text-xs text-slate-600">
				{[LeaveStatus.Approuve, LeaveStatus.EnAttente].map((status) => (
					<li key={status} className="flex items-center gap-1.5">
						<span
							aria-hidden="true"
							className={`inline-block h-3 w-6 rounded-sm ${LEAVE_BAND_STYLES[status]}`}
						/>
						{LEAVE_STATUS_LABELS[status]}
					</li>
				))}
			</ul>
		</div>
	)
}
