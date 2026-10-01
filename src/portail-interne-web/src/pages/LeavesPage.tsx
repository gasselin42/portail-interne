import { useEffect, useState, useCallback } from "react"
import { Link } from "react-router-dom"
import { DeleteForever } from "@mui/icons-material"

import {
	LeaveStatus,
	listLeaves,
	cancelLeave,
	type LeaveRequestResponse,
	LEAVE_TYPE_LABELS,
	type LeaveStatusId,
} from "../api/leaves"
import { ErrorBanner } from "../components/ErrorBanner"

type PageError = {
	message: string
	dismissible: boolean
}

const STATUS_STYLES: Record<LeaveStatusId, { label: string; className: string }> = {
	[LeaveStatus.EnAttente]: {
		label: "En attente",
		className: "bg-amber-50 text-amber-700 ring-amber-600/20",
	},
	[LeaveStatus.Approuve]: {
		label: "Approuvé",
		className: "bg-emerald-50 text-emerald-700 ring-emerald-600/20",
	},
	[LeaveStatus.Refuse]: { label: "Refusé", className: "bg-red-50 text-red-700 ring-red-600/20" },
	[LeaveStatus.Annule]: {
		label: "Annulé",
		className: "bg-slate-100 text-slate-600 ring-slate-500/20",
	},
}

function StatusBadge({ status }: { status: LeaveStatusId }) {
	const style = STATUS_STYLES[status]
	return (
		<span
			className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${style.className}`}
		>
			{style.label}
		</span>
	)
}

export function LeavesPage() {
	const [leaves, setLeaves] = useState<LeaveRequestResponse[]>([])
	const [error, setError] = useState<PageError | null>(null)
	const [loading, setLoading] = useState(true)
	const [cancellingId, setCancellingId] = useState<number | null>(null)

	const loadLeaves = useCallback(() => {
		return listLeaves()
			.then(setLeaves)
			.catch((e) =>
				setError({
					message: e instanceof Error ? e.message : "Erreur",
					dismissible: false,
				}),
			)
			.finally(() => setLoading(false))
	}, [])

	useEffect(() => {
		loadLeaves()
	}, [loadLeaves])

	async function handleCancel(id: number) {
		if (!window.confirm("Annuler cette demande de congé ?")) return
		setError(null)
		setCancellingId(id)
		try {
			await cancelLeave(id)
			await loadLeaves()
		} catch (e) {
			setError({
				message: e instanceof Error ? e.message : "Erreur",
				dismissible: true,
			})
		} finally {
			setCancellingId(null)
		}
	}

	return (
		<div className="min-h-screen bg-linear-to-b from-slate-50 via-slate-50 to-white">
			<div className="mx-auto max-w-6xl px-6 py-10">
				<header className="mb-8 flex flex-wrap items-end justify-between gap-4">
					<div>
						<p className="text-sm font-medium text-sky-700">Portail</p>
						<h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">
							Mes congés
						</h1>
						<p className="mt-1 text-sm text-slate-500">Voir et créer tes demandes de congés</p>
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
								Aucune demande pour le moment
							</p>
						)
					) : (
						<div className="overflow-x-auto">
							<table className="w-full min-w-200 table-fixed border-collapse text-left">
								<thead>
									<tr className="border-b border-slate-200 bg-slate-50/80">
										<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Début
										</th>
										<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Fin
										</th>
										<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Type
										</th>
										<th className="px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Motif
										</th>
										<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Statut
										</th>
										<th className="w-16 px-3 py-3.5">
											<span className="sr-only">Actions</span>
										</th>
									</tr>
								</thead>
								<tbody>
									{leaves.map((leave) => (
										<tr
											key={leave.id}
											className="border-b border-slate-100 transition last:border-b-0 hover:bg-slate-50/70"
										>
											<td className="px-5 py-3.5 text-sm font-medium text-slate-900">
												{leave.startDate.slice(0, 10)}
											</td>
											<td className="px-5 py-3.5 text-sm text-slate-700">
												{leave.endDate.slice(0, 10)}
											</td>
											<td className="px-5 py-3.5 text-sm text-slate-600">
												{LEAVE_TYPE_LABELS[leave.type]}
											</td>
											<td className="px-5 py-3.5 text-sm wrap-break-word whitespace-normal text-slate-700">
												{leave.reason || "-"}
											</td>
											<td className="px-5 py-3.5 text-sm text-slate-600">
												<StatusBadge status={leave.status} />
											</td>
											<td className="px-3 py-3.5 text-center">
												{leave.status === LeaveStatus.EnAttente && (
													<button
														type="button"
														onClick={() => handleCancel(leave.id)}
														disabled={cancellingId === leave.id}
														aria-label="Annuler la demande"
														title="Annuler la demande"
														className="inline-flex items-center justify-center rounded-lg p-1.5 text-slate-400 transition hover:bg-red-50 hover:text-red-600 disabled:opacity-50"
													>
														<DeleteForever fontSize="small" />
													</button>
												)}
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
