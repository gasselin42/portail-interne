import { useState, useEffect } from "react"
import { useSearchParams, Link } from "react-router-dom"
import { PageHeader } from "../components/common/PageHeader"
import { addDays, formatDayHeader, formatRange, isIsoDate, startOfWeek, todayIso } from "../utils/date"
import { iconButton, primaryButton, secondaryButton } from "../ui/buttons"
import { ChevronLeft, ChevronRight } from "@mui/icons-material"
import { LEAVE_TYPE_LABELS, listCalendar, type LeaveRequestResponse } from "../api/leaves"
import { ErrorBanner, type PageError } from "../components/feedback/Banner"
import { StatusBadge } from "../components/StatusBadge"

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

			<ol className="grid gap-3 md:grid-cols-7">
				{days.map((day) => {
					const dayLeaves = leavesOn(day)
					return (
						<li
							key={day}
							aria-current={day === today ? "date" : undefined}
							className="group rounded-xl border border-slate-200/80 bg-white p-3 shadow-sm shadow-slate-200/50 md:min-h-40 aria-[current=date]:border-sky-300"
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