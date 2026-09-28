import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { LeaveType, listLeaves, type LeaveRequestResponse } from "../api/leaves"
import { ErrorBanner } from "../components/ErrorBanner"

type PageError = {
	message: string
	dismissible: boolean
}

function leaveTypeLabel(id: number): string {
	const entry = Object.entries(LeaveType).find(([, value]) => value === id)
	return entry?.[0] ?? "—"
}

function StatusBadge({ status }: { status: number }) {
	return (
		<span
			className="font-medium"
		>
			{status === 0 ? ("En attente")
			: status === 1 ? ("Approuvé")
			: status === 2 ? ("Refusé")
			: ("Annulé")}
		</span>
	)
}

export function LeavesPage() {
	const [leaves, setLeaves] = useState<LeaveRequestResponse[]>([])
	const [error, setError] = useState<PageError | null>(null)
	const [loading, setLoading] = useState(true)

	useEffect(() => {
		listLeaves()
			.then(setLeaves)
			.catch((e) =>
				setError({
					message: e instanceof Error ? e.message : "Erreur",
					dismissible: false,
				}),
			)
			.finally(() => setLoading(false))
	}, [])
	
	return (
		<div className="min-h-screen bg-gradient-to-b from-slate-50 via-slate-50 to-white">
			<div className="mx-auto max-w-6xl px-6 py-10">
				<header className="mb-8 flex flex-wrap items-end justify-between gap-4">
					<div>
						<p className="text-sm font-medium text-sky-700">Portail</p>
						<h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">
							Mes congés
						</h1>
						<p className="mt-1 text-sm text-slate-500">
							Voir et créer tes demandes de congés
						</p>
					</div>
					<Link
						to="/leaves/new"
						className="rounded-lg bg-sky-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition hover:bg-sky-700"
					>
						Nouvelle demande
					</Link>
				</header>

				{error && (
					<ErrorBanner
						message={error.message}
						dismissible={error.dismissible}
						onDismiss={() => setError(null)}
					/>
				)}

				<section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
					{loading ? (
						<p className="px-6 py-12 text-center text-sm text-slate-500">
							Chargement des congés...
						</p>
					) : leaves.length === 0 ? (
						!(error && !error.dismissible) && (
							<p className="px-6 py-12 text-center text-sm text-slate-500">
								Aucun demande pour le moment
							</p>
						)
					) : (
						<div className="overflow-x-auto">
							<table className="w-full min-w-[720px] table-fixed border-collapse text-left">
								<thead>
									<tr className="border-b border-slate-200 bg-slate-50/80">
										<th className="px-5 py-3.5 text-xs font-semibold uppercase tracking-wide text-slate-500">
											Début
										</th>
										<th className="px-5 py-3.5 text-xs font-semibold uppercase tracking-wide text-slate-500">
											Fin
										</th>
										<th className="px-5 py-3.5 text-xs font-semibold uppercase tracking-wide text-slate-500">
											Type
										</th>
										<th className="px-5 py-3.5 text-xs font-semibold uppercase tracking-wide text-slate-500">
											Motif
										</th>
										<th className="px-5 py-3.5 text-xs font-semibold uppercase tracking-wide text-slate-500">
											Statut
										</th>
									</tr>
								</thead>
								<tbody>
									{leaves.map((leave) => (
										<tr
											key={leave.id}
											className="border-b border-slate-100 transition hover:bg-slate-50/70 last:border-b-0"
										>
											<td className="px-5 py-3.5 text-sm font-medium text-slate-900">
												{leave.startDate.slice(0, 10)}
											</td>
											<td className="px-5 py-3.5 text-sm text-slate-700">
												{leave.endDate.slice(0, 10)}
											</td>
											<td className="px-5 py-3.5 text-sm text-slate-600">
												{leaveTypeLabel(leave.type)}
											</td>
											<td className="px-5 py-3.5 text-sm break-words whitespace-normal text-slate-700">
												{leave.reason || "-"}
											</td>
											<td className="px-5 py-3.5 text-sm text-slate-600">
												<StatusBadge status={leave.status} />
											</td>
										</tr>
									))}
								</tbody>
							</table>
						</div>
					)}
				</section>
			</div>
		</div>
	)
}