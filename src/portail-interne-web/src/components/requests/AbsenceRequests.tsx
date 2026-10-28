import { useState } from "react"
import { useAsync } from "../../hooks/useAsync"
import { useSession } from "../../session/useSession"
import { ErrorBanner } from "../feedback/Banner"
import { DateText } from "../common/DateText"
import { formatLongDate, formatTime } from "../../utils/date"
import { StatusBadge } from "../leaves/StatusBadge"
import { rowIconDangerButton } from "../../ui/buttons"
import { DeleteForever } from "@mui/icons-material"
import { ConfirmDialog } from "../dialogs/ConfirmDialog"
import { cancelAbsence, getAbsences, type AbsenceResponse } from "../../api/absences"
import { AbsenceDialog } from "./AbsenceDialog"

type Props = {
	newOpen: boolean
	onNewClose: () => void
}

export function AbsenceRequests({ newOpen, onNewClose }: Props) {
	const { me } = useSession()
	const { data, error, loading, retry } = useAsync((signal) => getAbsences(me!.employeeId, signal), [])
	const [selected, setSelected] = useState<AbsenceResponse | null>(null)
	const [confirmOpen, setConfirmOpen] = useState(false)
	const [cancelError, setCancelError] = useState<string | null>(null)
	
	function openCancel(absence: AbsenceResponse) {
		setSelected(absence)
		setCancelError(null)
		setConfirmOpen(true)
	}

	async function handleCancel() {
		if (!selected) return

		try {
			await cancelAbsence(me!.employeeId, selected.id)
			setConfirmOpen(false)
			retry()
		} catch (e) {
			setCancelError(e instanceof Error ? e.message : "Erreur")
		}
	}

	return (
		<>
			<section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
				{error ? (
					<ErrorBanner message={error} onRetry={retry} />
				) : loading ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des absences…</p>
				) : data?.length === 0 ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Aucune absence à venir.</p>
				) : (
					<div className="overflow-x-auto">
						<table className="w-full min-w-200 table-fixed border-collapse text-left">
							<thead>
								<tr className="border-b border-slate-200 bg-slate-50/80">
									<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">Date</th>
									<th className="w-[18%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">Heures</th>
									<th className="px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">Motif</th>
									<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">Statut</th>
									<th className="w-16 px-3 py-3.5">
										<span className="sr-only">Actions</span>
									</th>
								</tr>
							</thead>
							<tbody>
								{data?.map((a) => (
									<tr key={a.id} className="border-b border-slate-100 transition last:border-b-0 hover:bg-slate-50/70">
										<td className="px-5 py-3.5 text-sm font-semibold text-slate-700">
											<DateText value={a.date} />
											{a.createdById !== null && a.createdById !== me!.employeeId && (
												<span className="block text-xs font-normal text-slate-500">
													Ajouté par {a.createdByFirstName} {a.createdByLastName}
												</span>
											)}
										</td>
										<td className="px-5 py-3.5 text-sm text-slate-700">
											{formatTime(a.start)} – {a.end ? formatTime(a.end) : "en cours"}
											{a.plannedStart === null && (
												<span className="block text-xs text-slate-500">Imprévue</span>
											)}
										</td>
										<td className="px-5 py-3.5 text-sm wrap-break-word whitespace-normal text-slate-700">
											{a.reason || "–"}
										</td>
										<td className="px-5 py-3.5 text-sm text-slate-600">
											<StatusBadge status={a.status} />
										</td>
										<td className="px-3 py-3.5 text-center">
											{a.canCancel && (
												<button
													type="button"
													onClick={() => openCancel(a)}
													aria-label="Annuler l'absence"
													title="Annuler l'absence"
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
				title="Annuler cette absence ?"
				message={selected ? `Ton absence du ${formatLongDate(selected.date)} sera annulée.` : ""}
				confirmLabel="Annuler l'absence"
				busyLabel="Annulation…"
				destructive
				onConfirm={handleCancel}
				onClose={() => setConfirmOpen(false)}
				onAfterClose={() => setSelected(null)}
			>
				{cancelError && <p role="alert" className="mt-2 text-sm text-red-600">{cancelError}</p>}
			</ConfirmDialog>
			<AbsenceDialog open={newOpen} onClose={onNewClose} onCreated={() => { onNewClose(); retry() }} />
		</>
	)
}