import { useState, useRef } from "react"
import { LEAVE_TYPE_LABELS, LeaveStatus, reviewLeave, type LeaveRequestResponse } from "../api/leaves"
import { formatRange } from "../utils/date"

type Options = {
	/** Après une approbation ou un refus réussi (ex. retirer la demande de la liste). */
	onReviewed: (leave: LeaveRequestResponse) => void
	/** Après un échec (ex. recharger la liste, qui a peut-être changé). */
	onFailed?: () => void
	/** Après une approbation ou un refus réussi, une fois la ligne disparue : où placer le focus clavier. */
	focusAfterReview?: () => void
}

/** Approuver (direct) ou refuser (après confirmation) une demande de congé. */
export function useLeaveReview({ onReviewed, onFailed, focusAfterReview }: Options) {
	const [reviewingId, setReviewingId] = useState<number | null>(null)
	const [error, setError] = useState<string | null>(null)
	const [declineOpen, setDeclineOpen] = useState(false)
	// Gardée après la fermeture : le message reste affiché pendant l'animation de sortie.
	const [declineTarget, setDeclineTarget] = useState<LeaveRequestResponse | null>(null)
	const [announcement, setAnnouncement] = useState("")

	const declineSucceededRef = useRef(false)

	async function review(leave: LeaveRequestResponse, status: typeof LeaveStatus.Approuve | typeof LeaveStatus.Refuse): Promise<boolean> {
		setError(null)
		setAnnouncement("")
		setReviewingId(leave.id)
		try {
			await reviewLeave(leave.id, status)
			onReviewed(leave)
			setAnnouncement(
				`Demande de ${leave.employeeFirstName} ${leave.employeeLastName} ${status === LeaveStatus.Approuve ? "approuvée" : "refusée"}.`,
			)
		} catch (e) {
			setError(e instanceof Error ? e.message : "Erreur")
			onFailed?.()
			return false
		} finally {
			setReviewingId(null)
		}

		return true;
	}

	return {
		reviewingId,
		announcement,
		error,
		dismissError: () => setError(null),
		approve: async (leave: LeaveRequestResponse) => {
			const success = await review(leave, LeaveStatus.Approuve)
			if (success) focusAfterReview?.()
		},
		requestDecline: (leave: LeaveRequestResponse) => {
			setDeclineTarget(leave)
			setDeclineOpen(true)
			declineSucceededRef.current = false
		},
		/** À passer tel quel : <ConfirmDialog {...declineDialog} /> */
		declineDialog: {
			open: declineOpen,
			title: "Refuser cette demande ?",
			message: declineTarget
				? `Cette demande (${LEAVE_TYPE_LABELS[declineTarget.type]}) de ${declineTarget.employeeFirstName} ${declineTarget.employeeLastName} du ${formatRange(declineTarget.startDate, declineTarget.endDate)} sera refusée. ${declineTarget.employeeFirstName} verra le statut Refusé.`
				: "",
			confirmLabel: "Refuser",
			busyLabel: "Refus…",
			destructive: true,
			onClose: () => setDeclineOpen(false),
			onConfirm: async () => {
				if (!declineTarget) return
				declineSucceededRef.current = await review(declineTarget, LeaveStatus.Refuse)
				setDeclineOpen(false)
			},
			onAfterClose: () => {if (declineSucceededRef.current) focusAfterReview?.()}
		},
	}
}