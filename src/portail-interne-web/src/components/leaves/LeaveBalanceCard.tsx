import { useState } from "react"
import { todayIso } from "../../utils/date"
import { useAsync } from "../../hooks/useAsync"
import { getLeaveBalance } from "../../api/leaves"
import { iconButton } from "../../ui/buttons"
import { ChevronLeft, ChevronRight } from "@mui/icons-material"
import { ErrorBanner } from "../feedback/Banner"
import { leaveSwatchBackground } from "../../ui/leaveBandBackground"

type Props = {
	/** Change après une annulation : le solde est rechargé. */
	refreshKey: number
}

export function LeaveBalanceCard({ refreshKey }: Props) {
	const [year, setYear] = useState(() => Number(todayIso().slice(0, 4)))
	const { data, error, loading, retry } = useAsync(
		(signal) => getLeaveBalance(year, signal),
		[year, refreshKey]
	)

	return (
		<section aria-labelledby="balance-title" className="mb-6 rounded-xl border border-slate-200/80 bg-white px-5 py-4 shadow-sm shadow-slate-200/50">
			{error ? (
				<ErrorBanner message={error} onRetry={retry} />
			) : (
				<div className="flex flex-wrap items-center gap-x-6 gap-y-3">
					{/* 1. L'année, et le chiffre principal dessous */}
					<div className="shrink-0">
						<div className="flex items-center gap-1">
							<button type="button" aria-label="Année précédente" onClick={() => setYear((y) => y - 1)} className={iconButton}><ChevronLeft fontSize="small" /></button>
							<h2 id="balance-title" className="text-base font-semibold text-slate-900">Solde {year}</h2>
							<button type="button" aria-label="Année suivante" onClick={() => setYear((y) => y + 1)} className={iconButton}><ChevronRight fontSize="small" /></button>
						</div>
						{loading || !data ? (
							<p role="status" className="text-sm text-slate-500 text-center">Chargement…</p>
						) : (
							<p className="text-slate-900 text-center">
								<span className="text-2xl font-semibold">{data.remainingDays}</span>{" "}
								<span className="text-sm text-slate-500">restants sur {data.annualDays}</span>
							</p>
						)}
					</div>

					{data && !loading && (
						<>
							{/* 2. La barre, qui prend toute la place restante */}
							<div aria-hidden="true" className="flex h-4 min-w-40 flex-1 overflow-hidden rounded-full bg-slate-100">
								<div style={{ width: `${percent(data.approvedDays, data.annualDays)}%`, background: leaveSwatchBackground(true) }} />
								<div style={{ width: `${percent(data.pendingDays, data.annualDays)}%`, background: leaveSwatchBackground(false) }} />
							</div>

							{/* 3. Le détail empilé, qui sert aussi de légende */}
							<ul className="shrink-0 space-y-0.5 text-sm text-slate-600">
								<li className="flex items-center gap-2">
									<span aria-hidden="true" className="inline-block h-2.5 w-4 rounded-sm" style={{ background: leaveSwatchBackground(true) }} />
									{data.approvedDays} approuvé{data.approvedDays > 1 ? "s" : ""}
								</li>
								<li className="flex items-center gap-2">
									<span aria-hidden="true" className="inline-block h-2.5 w-4 rounded-sm" style={{ background: leaveSwatchBackground(false) }} />
									{data.pendingDays} en attente
								</li>
								{data.unpaidDays > 0 && (
									<li className="flex items-center gap-2">
										<span aria-hidden="true" className="inline-block h-2.5 w-4 rounded-sm" />
										{data.unpaidDays} sans solde
									</li>
								)}
							</ul>
						</>
					)}
				</div>
			)}
		</section>
	)
}

/** Part en %, bornée à 100 (un solde de 0 jour ne divise pas par zéro). */
function percent(days: number, total: number): number {
	return total === 0 ? 0 : Math.min(100, (days / total) * 100)
}