import { useState, useRef } from "react"
import { LeaveStatus, type ReviewStatus } from "../api/leaves"
import { approvalOf, approvalWhen, reviewApproval, type ApprovalItem } from "../utils/approvals"

type Options = {
	/** Après une approbation ou un refus réussi (ex. retirer la demande de la liste). */
	onReviewed: (item: ApprovalItem) => void
	/** Après un échec (ex. recharger la liste, qui a peut-être changé). */
	onFailed?: () => void
	/** Après une approbation ou un refus réussi, une fois la ligne disparue : où placer le focus clavier. */
	focusAfterReview?: () => void
}

/** Approuver (direct) ou refuser (après confirmation) une demande : congé, ajustement ou absence. */
export function useApprovalReview({ onReviewed, onFailed, focusAfterReview }: Options) {
	const [reviewingKey, setReviewingKey] = useState<string | null>(null)
	const [error, setError] = useState<string | null>(null)
	const [declineOpen, setDeclineOpen] = useState(false)
	// Gardée après la fermeture : le message reste affiché pendant l'animation de sortie.
	const [declineTarget, setDeclineTarget] = useState<ApprovalItem | null>(null)
	const [announcement, setAnnouncement] = useState("")

	const declineSucceededRef = useRef(false)

	async function review(item: ApprovalItem, status: ReviewStatus): Promise<boolean> {
		setError(null)
		setAnnouncement("")
		setReviewingKey(item.key)
		try {
			await reviewApproval(item, status)
			onReviewed(item)
			setAnnouncement(
				`Demande ${approvalOf(item)} de ${item.employeeName} ${status === LeaveStatus.Approuve ? "approuvée" : "refusée"}.`,
			)
		} catch (e) {
			setError(e instanceof Error ? e.message : "Erreur")
			onFailed?.()
			return false
		} finally {
			setReviewingKey(null)
		}

		return true;
	}

	return {
		reviewingKey,
		announcement,
		error,
		dismissError: () => setError(null),
		approve: async (item: ApprovalItem) => {
			const success = await review(item, LeaveStatus.Approuve)
			if (success) focusAfterReview?.()
		},
		requestDecline: (item: ApprovalItem) => {
			setDeclineTarget(item)
			setDeclineOpen(true)
			declineSucceededRef.current = false
		},
		/** À passer tel quel : <ConfirmDialog {...declineDialog} /> */
		declineDialog: {
			open: declineOpen,
			title: "Refuser cette demande ?",
			message: declineTarget
				? `La demande ${approvalOf(declineTarget)} de ${declineTarget.employeeName} (${approvalWhen(declineTarget)}) sera refusée. ${declineTarget.employeeName} verra le statut Refusé.`
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