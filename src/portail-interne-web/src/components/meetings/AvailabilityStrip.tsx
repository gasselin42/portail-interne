import { useState, type MouseEvent } from "react"
import { BusyKind, DayStatus, getAvailability, type BusyKindId, type EmployeeAvailability } from "../../api/availability"
import type { EmployeeOption } from "../../api/employees"
import { useAsync } from "../../hooks/useAsync"
import { useSession } from "../../session/useSession"
import { labelClass } from "../../ui/fields"
import { earliestStart, formatMinutes, minutesOnDate } from "../../utils/meetings"
import { ErrorBanner } from "../feedback/Banner"

type Props = {
	date: string
	people: EmployeeOption[]
	start: number
	end: number
	onPick: (minutes: number) => void
}

const BUSY_STYLES: Record<BusyKindId, { label: string; className: string }> = {
	[BusyKind.Busy]: { label: "Occupé", className: "bg-sky-600" },
	[BusyKind.FlexibleBusy]: { label: "Occupé (souple)", className: "border border-dashed border-sky-600 bg-sky-100" },
	[BusyKind.Absent]: { label: "Absence", className: "bg-amber-400" },
}

function visibleRange(data: EmployeeAvailability[] | null, date: string, start: number, end: number): { from: number, to: number } {
	const range = { from: start, to: end }
	let found = false

	if (data !== null) {
		for (const d of data) {
			for (const day of d.days.filter(f => f.status === DayStatus.Working)) {
				const startAt = minutesOnDate(day.startAt!, date)
				const endAt = minutesOnDate(day.endAt!, date)
				if (endAt <= 0 || startAt >= 1440) continue
				found = true
				range.from = Math.min(range.from, startAt)
				range.to = Math.max(range.to, endAt)
			}
		}
	}

	if (!found) {
		range.from = Math.min(480, range.from)
		range.to = Math.max(1080, range.to)
	}

	range.from = Math.floor(range.from / 60) * 60
	range.to = Math.ceil(range.to / 60) * 60

	return range
}

export function AvailabilityStrip({ date, people, start, end, onPick }: Props) {
	const [hover, setHover] = useState<number | null>(null)

	const { me } = useSession()

	const shown = people.slice(0, 20)
	const ids = shown.map(p => p.id)
	const { data, error, loading, retry } = useAsync((signal) => getAvailability(ids, date, date, signal), [ids, date])

	const { from, to } = visibleRange(data, date, start, end)
	const hours = Array.from({ length: (to - from) / 60 + 1}, (_, i) => from + i * 60)

	function percent(minutes: number) {
		let m = Math.max(from, minutes)
		m = Math.min(to, m)
		return (m - from) / (to - from) * 100
	}

	function minutesAt(e: MouseEvent<HTMLDivElement>): number {
		const rect = e.currentTarget.getBoundingClientRect() // Donne la position et la taille de la barre, entre autres
		const position = (e.clientX - rect.left) / rect.width // Donne entre 0 et 1
		const minutes = from + position * (to - from) // from en minutes + position en minutes
		const floor = Math.floor(minutes / 15) * 15
		const max = Math.max(floor, earliestStart(date))
		return Math.min(max, 1425)
	}

	return (
		<>
			<p className={labelClass}>Disponibilités</p>
			{error ? (
				<div className="mt-2">
					<ErrorBanner message={error} onRetry={retry} />
				</div>
			) : (
				<>
					<ul aria-label="Disponibilités" className="space-y-1.5">
						<li aria-hidden="true" className="grid grid-cols-[8rem_1fr] items-center gap-3">
							<span />
							<div className="relative h-4 text-xs text-slate-500">
								{hours.map((h) => (
									<span className="absolute whitespace-nowrap -translate-x-1/2" style={{ left: `${((h - from) / (to - from)) * 100}%` }}>
										{formatMinutes(h)}
									</span>
								))}
								{hover !== null &&
									<span className="z-10 absolute whitespace-nowrap -translate-x-1/2 rounded bg-slate-800 px-1 text-white" style={{ left: `${percent(hover)}%` }}>
										{formatMinutes(hover)}
									</span>
								}
							</div>
						</li>
						{shown.map((p) => {
							const availability = data?.find(a => a.employeeId === p.id)
							const workBlocks = availability?.days.filter(a => a.status === DayStatus.Working || a.status === DayStatus.Undefined).map(d => ({ from: d.status === DayStatus.Undefined ? from : minutesOnDate(d.startAt!, date), to: d.status === DayStatus.Undefined ? to : minutesOnDate(d.endAt!, date) })) ?? []
							const busyBlocks = availability?.busy.map(b => ({ from: minutesOnDate(b.startAt, date), to: minutesOnDate(b.endAt, date), kind: b.kind })) ?? []
							return (
								<li key={p.id} className="grid grid-cols-[8rem_1fr] items-center gap-3">
									<p className="truncate">{me?.employeeId === p.id ? "Toi" : p.firstName + " " + p.lastName}</p>
									<div
										onClick={(e) => onPick(minutesAt(e))}
										onMouseMove={(e) => setHover(minutesAt(e))}
										onMouseLeave={() => setHover(null)}
										className={`relative overflow-hidden h-7 rounded-md bg-slate-200 border border-slate-300 cursor-pointer ${loading ? "animate-pulse" : ""}`}
									>
										{workBlocks.map((w) => (
											<div key={w.from} className="absolute inset-y-0 bg-white" style={{ left: `${percent(w.from)}%`, width: `${percent(w.to) - percent(w.from)}%` }} />
										))}
										{busyBlocks.map((b, i) => (
											<div key={i} className={`absolute inset-y-0 rounded-sm ${BUSY_STYLES[b.kind].className}`} style={{ left: `${percent(b.from)}%`, width: `${percent(b.to) - percent(b.from)}%` }} title={`${BUSY_STYLES[b.kind].label}, ${formatMinutes(b.from)} – ${formatMinutes(b.to)}`}>
												<span className="sr-only">{BUSY_STYLES[b.kind].label}, {formatMinutes(b.from)} – {formatMinutes(b.to)}</span>
											</div>
										))}
										<div className="pointer-events-none absolute inset-y-0 rounded-sm border-2 border-sky-500 bg-sky-500/10" style={{ left: `${percent(start)}%`, width: `${percent(end) - percent(start)}%` }}></div>
										{hover !== null &&
											<div className="pointer-events-none absolute inset-y-0 rounded-sm border-2 border-dashed border-sky-400" style={{ left: `${percent(hover)}%`, width: `${percent(Math.min(hover + (end - start), 1440)) - percent(hover)}%` }}></div>
										}
									</div>
								</li>
							)
						})}
					</ul>
					<div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-600">
						{Object.values(BUSY_STYLES).map((s) => (
							<span key={s.label} className="flex items-center gap-1.5">
								<span className={`inline-block size-3 rounded-sm ${s.className}`} />
								{s.label}
							</span>
						))}
						<span className="flex items-center gap-1.5">
							<span className="inline-block size-3 rounded-sm bg-slate-200 border border-slate-300" />
							Hors horaire
						</span>
					</div>
					{people.length > 20 &&
						<p className="mt-1 text-xs text-slate-500">
							Disponibilités des 20 premières personnes seulement.
						</p>
					}
				</>
			)}
		</>
	)
}