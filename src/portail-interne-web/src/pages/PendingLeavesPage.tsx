import { useState, useEffect, useCallback } from "react"
import { ErrorBanner } from "../components/Banner"
import {
	listLeavesPending,
	type LeaveRequestResponse,
	LEAVE_TYPE_LABELS,
	reviewLeave,
	LeaveStatus,
} from "../api/leaves"
import { CheckCircleOutlineOutlined, HighlightOff } from "@mui/icons-material"
import { DateText } from "../components/DateText"
import { formatRange } from "../utils/date"
import { ConfirmDialog } from "../components/ConfirmDialog"
import { rowIconDangerButton, rowIconSuccessButton } from "../ui/buttons"

type PageError = {
	message: string
	dismissible: boolean
}

export function PendingLeavesPage() {
	const [leaves, setLeaves] = useState<LeaveRequestResponse[]>([])
	const [error, setError] = useState<PageError | null>(null)
	const [loading, setLoading] = useState(true)
	const [reviewingId, setReviewingId] = useState<number | null>(null)
	const [confirmOpen, setConfirmOpen] = useState(false)
	const [selectedLeave, setSelectedLeave] = useState<LeaveRequestResponse | null>(null)

	const declineMessage = selectedLeave
		? `Cette demande (${LEAVE_TYPE_LABELS[selectedLeave.type]}) de ${selectedLeave.employeeFirstName} ${selectedLeave.employeeLastName} du ${formatRange(selectedLeave.startDate, selectedLeave.endDate)} sera refusée. ${selectedLeave.employeeFirstName} verra le statut Refusé.`
		: ""

	const loadLeaves = useCallback(() => {
		return listLeavesPending()
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

	async function handleValidation(id: number) {
		setError(null)
		setReviewingId(id)
		try {
			await reviewLeave(id, LeaveStatus.Approuve)
			setLeaves((prev) => prev.filter((l) => l.id !== id))
		} catch (e) {
			setError({
				message: e instanceof Error ? e.message : "Erreur",
				dismissible: true,
			})
			loadLeaves()
		} finally {
			setReviewingId(null)
		}
	}

	async function handleDecline(leave: LeaveRequestResponse) {
		setError(null)
		setReviewingId(leave.id)
		try {
			await reviewLeave(leave.id, LeaveStatus.Refuse)
			setLeaves((prev) => prev.filter((l) => l.id !== leave.id))
		} catch (e) {
			setError({
				message: e instanceof Error ? e.message : "Erreur",
				dismissible: true,
			})
			loadLeaves()
		} finally {
			setReviewingId(null)
		}
	}

	async function handleConfirmDecline() {
		if (!selectedLeave) return

		await handleDecline(selectedLeave)
		setConfirmOpen(false)
	}

	function openDeclineConfirm(leave: LeaveRequestResponse) {
		setSelectedLeave(leave)
		setConfirmOpen(true)
	}

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<header className="mb-8 flex flex-wrap items-end justify-between gap-4">
				<div>
					<p className="text-sm font-medium text-sky-700">Congés</p>
					<h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">
						Congés en attente
					</h1>
					<p className="mt-1 text-sm text-slate-500">
						Voir et accepter ou refuser les demandes de congés
					</p>
				</div>
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
					<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des congés...</p>
				) : leaves.length === 0 ? (
					!(error && !error.dismissible) && (
						<p className="px-6 py-12 text-center text-sm text-slate-500">
							Aucune demande en attente
						</p>
					)
				) : (
					<div className="overflow-x-auto">
						<table className="w-full min-w-200 table-fixed border-collapse text-left">
							<thead>
								<tr className="border-b border-slate-200 bg-slate-50/80">
									<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
										Employé
									</th>
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
									<th className="w-24 px-3 py-3.5">
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
											{leave.employeeFirstName} {leave.employeeLastName}
										</td>
										<td className="px-5 py-3.5 text-sm font-semibold text-slate-700">
											<DateText value={leave.startDate} />
										</td>
										<td className="px-5 py-3.5 text-sm font-semibold text-slate-700">
											<DateText value={leave.endDate} />
										</td>
										<td className="px-5 py-3.5 text-sm text-slate-600">
											{LEAVE_TYPE_LABELS[leave.type]}
										</td>
										<td className="px-5 py-3.5 text-sm wrap-break-word whitespace-normal text-slate-700">
											{leave.reason || "-"}
										</td>
										<td className="px-3 py-3.5 text-center">
											<div className="inline-flex gap-1">
												<button
													type="button"
													onClick={() => handleValidation(leave.id)}
													disabled={reviewingId === leave.id}
													title="Accepter"
													aria-label={`Accepter la demande de ${leave.employeeFirstName} ${leave.employeeLastName}`}
													className={rowIconSuccessButton}
												>
													<CheckCircleOutlineOutlined fontSize="small" />
												</button>
												<button
													type="button"
													onClick={() => openDeclineConfirm(leave)}
													disabled={reviewingId === leave.id}
													title="Refuser"
													aria-label={`Refuser la demande de ${leave.employeeFirstName} ${leave.employeeLastName}`}
													className={rowIconDangerButton}
												>
													<HighlightOff fontSize="small" />
												</button>
											</div>
										</td>
									</tr>
								))}
							</tbody>
						</table>
					</div>
				)}
			</section>
			<ConfirmDialog
				open={confirmOpen}
				title="Refuser cette demande ?"
				message={declineMessage}
				confirmLabel="Refuser"
				busyLabel="Refus…"
				destructive
				onClose={() => setConfirmOpen(false)}
				onConfirm={handleConfirmDecline}
			/>
		</div>
	)
}
