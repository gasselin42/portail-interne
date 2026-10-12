import { useState, useEffect } from "react"
import { useSearchParams, Link } from "react-router-dom"
import { PageHeader } from "../components/common/PageHeader"
import { addDays, formatDayHeader, formatRange, isIsoDate, startOfWeek, todayIso } from "../utils/date"
import { iconButton, primaryButton, secondaryButton } from "../ui/buttons"
import { ChevronLeft, ChevronRight } from "@mui/icons-material"
import { LEAVE_STATUS_LABELS, LEAVE_TYPE_LABELS, LeaveStatus, listCalendar, type LeaveRequestResponse } from "../api/leaves"
import { ErrorBanner, type PageError } from "../components/feedback/Banner"
import { StatusBadge } from "../components/StatusBadge"
import { toSegment } from "../utils/calendar"
import { LEAVE_BAND_STYLES, leaveBandBase } from "../ui/leaveBands"

type WeekResult =
	| { monday: string; leaves: LeaveRequestResponse[] }
	| { monday: string; error: PageError }

export function CalendarPage() {
	const [searchParams, setSearchParams] = useSearchParams()
	const [result, setResult] = useState<WeekResult | null>(null)
	const [retry, setRetry] = useState(0)
	
	const semaine = searchParams.get("semaine")

	const today = todayIso()
	const monday = startOfWeek(semaine && isIsoDate(semaine) ? semaine : today)
	const days = Array.from({ length: 7 }, (_, i) => addDays(monday, i))

	const isCurrentWeek = monday === startOfWeek(today)
	const current = result?.monday === monday ? result : null
	const leaves = current && "leaves" in current ? current.leaves : null
	const segments = leaves?.map((leave) => toSegment(leave, days)) ?? []
	const error = current && "error" in current ? current.error : null

	useEffect(() => {
		const controller = new AbortController()

		listCalendar(monday, addDays(monday, 6), controller.signal)
			.then((leaves) => setResult({ monday, leaves }))
			.catch((e) => {
				if (controller.signal.aborted) return
				setResult({ monday, error: {
					message: e instanceof Error ? e.message : "Erreur",
					dismissible: false,
				}})
			})
		
		return () => controller.abort()
	}, [monday, retry])

	function handleRetry() {
		setResult(null)
		setRetry((n) => n + 1)
	}

	function leavesOn(day: string): LeaveRequestResponse[] {
		if (leaves === null) return []
		return leaves.filter((l) => l.startDate.slice(0, 10) <= day && l.endDate.slice(0, 10) >= day)
	}

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				eyebrow="Planning"
				title="Calendrier"
				description="Vois tes absences de la semaine, en attente ou approuvées."
				actions={
					<Link to="/leaves/new" className={primaryButton}>
						Nouvelle demande
					</Link>
				}
			/>

			<div className="flex flex-wrap items-center justify-between gap-3 mb-4">
				<h2 aria-live="polite" className="text-lg font-semibold text-slate-900">{formatRange(monday, days[6])}</h2>
				<div className="flex items-center gap-1">
					<button
						type="button"
						onClick={() => setSearchParams({ semaine: addDays(monday, -7) })}
						aria-label="Semaine précédente"
						className={iconButton}
					>
						<ChevronLeft fontSize="large" />
					</button>
					<button
						type="button"
						onClick={() => setSearchParams({ semaine: addDays(monday, 7) })}
						aria-label="Semaine suivante"
						className={iconButton}
					>
						<ChevronRight fontSize="large" />
					</button>
					<button
						type="button"
						disabled={isCurrentWeek}
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
					<p role="status" className="text-center text-sm text-slate-500">Chargement…</p>
				) : leaves?.length === 0 ? (
					<p role="status" className="text-center text-sm text-slate-500">Aucune absence cette semaine.</p>
				) : null}
			</div>

			{/* Grand écran : en-têtes + rangée « toute la journée » */}
			<div className="hidden overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50 md:block">
				{/* 1. En-têtes : 7 colonnes, un par jour */}
				<ol className="grid grid-cols-7 border-b border-slate-200/80">
					{days.map((day) => (
						<li key={day} aria-current={day === today ? "date" : undefined} className="group px-3 py-2">
							<time dateTime={day} className="text-sm font-medium text-slate-700 group-aria-[current=date]:font-semibold group-aria-[current=date]:text-sky-700">
								{formatDayHeader(day)}
							</time>
						</li>
					))}
				</ol>

				{/* 2. Rangée « toute la journée » : un <li> par segment */}
				<ul aria-label="Absences de la semaine" className="grid grid-cols-7 grid-flow-row-dense gap-y-1 py-1 min-h-12">
					{segments.map((segment) => {
						const { leave } = segment
						const label = `${LEAVE_TYPE_LABELS[leave.type]} · ${LEAVE_STATUS_LABELS[leave.status]}`
						const fullLabel = `${LEAVE_TYPE_LABELS[leave.type]} (${LEAVE_STATUS_LABELS[leave.status]}), ${formatRange(leave.startDate, leave.endDate)}`

						return (
							<li
								key={leave.id}
								title={fullLabel}
								style={{ gridColumn: `${segment.column} / span ${segment.span}` }}
								className={[
									leaveBandBase,
									LEAVE_BAND_STYLES[leave.status],
									!segment.continuesBefore && "ml-1 rounded-l-md",
									!segment.continuesAfter && "mr-1 rounded-r-md",
								].filter(Boolean).join(" ")}
							>
								<span aria-hidden="true">{label}</span>
								<span className="sr-only">{fullLabel}</span>
							</li>
						)
					})}
				</ul>
			</div>

			<div className="hidden md:flex">
				<ul aria-label="Légende" className="mt-3 gap-4 hidden md:flex text-xs text-slate-600">
					{[LeaveStatus.Approuve, LeaveStatus.EnAttente].map((status) => (
						<li key={status} className="flex items-center gap-1.5">
							<span aria-hidden="true" className={`inline-block h-3 w-6 rounded-sm ${LEAVE_BAND_STYLES[status]}`} />							{LEAVE_STATUS_LABELS[status]}
						</li>
					))}
				</ul>
			</div>

			{/* Mobile : la liste des jours (remplacée par la vue jour à 17b) */}
			<ol className="grid gap-3 md:hidden">
				{days.map((day) => {
					const dayLeaves = leavesOn(day)
					return (
						<li
							key={day}
							aria-current={day === today ? "date" : undefined}
							className="group rounded-xl border border-slate-200/80 bg-white p-3 shadow-sm shadow-slate-200/50 aria-[current=date]:border-sky-300"
						>
							<time dateTime={day} className="text-sm font-medium text-slate-700 group-aria-[current=date]:font-semibold group-aria-[current=date]:text-sky-700">
								{formatDayHeader(day)}
							</time>
							{dayLeaves.length > 0 && (
								<ul className="mt-2 space-y-1.5">
									{dayLeaves.map((leave) => (
										<li key={leave.id} className="flex items-center justify-between gap-2 text-sm flex-wrap">
											{LEAVE_TYPE_LABELS[leave.type]}
											<StatusBadge status={leave.status} />
										</li>
									))}
								</ul>
							)}
						</li>
					)
				})}
			</ol>
		</div>
	)
}