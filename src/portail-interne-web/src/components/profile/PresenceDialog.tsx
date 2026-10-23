import { useState } from "react"
import { useSession } from "../../session/useSession"
import { Modal } from "../dialogs/Modal"
import { ConfirmDialog } from "../dialogs/ConfirmDialog"
import { ErrorBanner } from "../feedback/Banner"
import { primaryButton, secondaryButton } from "../../ui/buttons"
import { departAbsence } from "../../api/absences"
import { inputClass, labelClass, sectionTitle } from "../../ui/fields"
import { MeetingRoomOutlined } from "@mui/icons-material"
import { PresenceSummary } from "./PresenceSummary"
import { UpcomingList } from "../upcoming/UpcomingList"

type Props = {
	open: boolean
	onClose: () => void
	onDeparted: () => void
}

export function PresenceDialog({ open, onClose, onDeparted }: Props) {
	const [reason, setReason] = useState<string>("")
	const [confirmOpen, setConfirmOpen] = useState<boolean>(false)
	const [error, setError] = useState<string | null>(null)

	const { me } = useSession()

	async function handleDepart() {
		try {
			await departAbsence(me!.employeeId, reason.trim() || null)
			onDeparted()
			setConfirmOpen(false)
			setReason("")
			setError(null)
			onClose()
		} catch (e) {
			setConfirmOpen(false)
			setError(e instanceof Error ? e.message : "Impossible d'enregistrer ton départ")
		}
	}

	function handleClose() {
		setError(null)
		onClose()
	}

	return (
		<Modal open={open} title="Ma présence" onClose={handleClose}>
			{error && (
				<div className="mt-3">
					<ErrorBanner message={error} />
				</div>
			)}
			<div className="mt-4">
				<PresenceSummary />
				<section className="mt-5 border-t border-slate-200 pt-4">
					<h3 className={sectionTitle}>À venir</h3>
					<UpcomingList />
				</section>
			</div>
			<div className="mt-6 flex justify-end gap-3">
				<button type="button" onClick={handleClose} className={secondaryButton}>
					Fermer
				</button>
				<button type="button" onClick={() => setConfirmOpen(true)} className={`${primaryButton} inline-flex items-center gap-2`}>
					<MeetingRoomOutlined fontSize="small" />Je pars
				</button>
			</div>
			<ConfirmDialog
				open={confirmOpen}
				title="Confirmer ton départ"
				message="Ton manager verra que tu es parti maintenant. Tu indiqueras ton retour avec « Je suis de retour »."
				warning="Toute utilisation injustifiée sera réprimandée."
				confirmLabel="Je pars"
				busyLabel="Envoi…"
				onConfirm={handleDepart}
				onClose={() => {
					setConfirmOpen(false)
					setReason("")
				}}
			>
				<div className="mt-4">
					<label htmlFor="reason" className={labelClass}>
						Motif (optionnel)
					</label>
					<input
						id="reason"
						type="text"
						value={reason}
						onChange={(e) => setReason(e.target.value)}
						className={inputClass}
						data-autofocus
						maxLength={500}
					/>
				</div>
			</ConfirmDialog>
		</Modal>
	)
}
