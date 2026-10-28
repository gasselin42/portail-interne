import { useEffect, useState, useCallback, Fragment } from "react"
import { DeleteForever } from "@mui/icons-material"

import {
	LeaveStatus,
	listLeaves,
	cancelLeave,
	type LeaveRequestResponse,
	LEAVE_TYPE_LABELS,
} from "../../api/leaves"
import { StatusBadge } from "../leaves/StatusBadge"
import { ErrorBanner, type PageError } from "../feedback/Banner"
import { DateText } from "../common/DateText"
import { ConfirmDialog } from "../dialogs/ConfirmDialog"
import { formatRange } from "../../utils/date"
import { rowIconDangerButton } from "../../ui/buttons"
import { LeaveBalanceCard } from "../leaves/LeaveBalanceCard"
import { LeaveDaysNote } from "../leaves/LeaveDaysNote"
import { LeaveDialog } from "./LeaveDialog"

type Props = {
	newOpen: boolean
	onNewClose: () => void
}

export function LeaveRequests({ newOpen, onNewClose }: Props) {
	const [leaves, setLeaves] = useState<LeaveRequestResponse[]>([])
	const [error, setError] = useState<PageError | null>(null)
	const [loading, setLoading] = useState(true)
	const [cancellingId, setCancellingId] = useState<number | null>(null)
	const [confirmOpen, setConfirmOpen] = useState(false)
	const [selectedLeave, setSelectedLeave] = useState<LeaveRequestResponse | null>(null)
	const [balanceKey, setBalanceKey] = useState(0)

	const cancelMessage = selectedLeave
		? `Ta demande (${LEAVE_TYPE_LABELS[selectedLeave.type]}) du ${formatRange(selectedLeave.startDate, selectedLeave.endDate)} sera annulée. Cette action est définitive.`
		: ""

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
		setError(null)
		setCancellingId(id)
		try {
			await cancelLeave(id)
			await loadLeaves()
			setBalanceKey((k) => k + 1)
		} catch (e) {
			setError({
				message: e instanceof Error ? e.message : "Erreur",
				dismissible: true,
			})
		} finally {
			setCancellingId(null)
		}
	}

	async function handleCancellation() {
		if (!selectedLeave) return

		await handleCancel(selectedLeave.id)
		setConfirmOpen(false)
	}

	function openCancelLeave(leave: LeaveRequestResponse) {
		setSelectedLeave(leave)
		setConfirmOpen(true)
	}

	return (
		<Fragment>
			{error && (
				<ErrorBanner
					message={error.message}
					dismissible={error.dismissible}
					onDismiss={() => setError(null)}
				/>
			)}

			<LeaveBalanceCard refreshKey={balanceKey} />

			<section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
				{loading ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des congés...</p>
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
										<td className="px-5 py-3.5 text-sm font-semibold text-slate-700">
											<DateText value={leave.startDate} />
										</td>
										<td className="px-5 py-3.5 text-sm font-semibold text-slate-700">
											<DateText value={leave.endDate} />
										</td>
										<td className="px-5 py-3.5 text-sm text-slate-600">
											{LEAVE_TYPE_LABELS[leave.type]}
											{leave.createdByFirstName && 
												<span className="block text-xs text-slate-500">
													Déclaré par {leave.createdByFirstName} {leave.createdByLastName}
												</span>
											}
											<LeaveDaysNote workingDays={leave.workingDays} unpaidDates={leave.unpaidDates} />
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
													onClick={() => openCancelLeave(leave)}
													disabled={cancellingId === leave.id}
													aria-label="Annuler la demande"
													title="Annuler la demande"
													className={rowIconDangerButton}
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
			<ConfirmDialog
				open={confirmOpen}
				title="Annuler cette demande ?"
				message={cancelMessage}
				confirmLabel="Annuler la demande"
				busyLabel="Annulation…"
				destructive
				onClose={() => setConfirmOpen(false)}
				onConfirm={handleCancellation}
			/>
			<LeaveDialog open={newOpen} onClose={onNewClose} onCreated={() => { onNewClose(); loadLeaves(); setBalanceKey((k) => k + 1)}} />
		</Fragment>
	)
}