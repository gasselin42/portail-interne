import { useState, type ReactNode } from "react"
import { Modal } from "./Modal"
import { dangerButton, primaryButton, secondaryButton } from "../../ui/buttons"

type Props = {
	open: boolean
	title: string
	message: string
	warning?: string
	children?: ReactNode
	confirmLabel: string
	busyLabel?: string
	destructive?: boolean
	onConfirm: () => Promise<void>
	onClose: () => void
	onAfterClose?: () => void
}

export function ConfirmDialog({
	open,
	title,
	message,
	warning,
	children,
	confirmLabel,
	busyLabel,
	destructive = false,
	onConfirm,
	onClose,
	onAfterClose
}: Props) {
	const [busy, setBusy] = useState(false)

	async function handleConfirm() {
		setBusy(true)
		try {
			await onConfirm()
		} finally {
			setBusy(false)
		}
	}

	function handleClose() {
		if (busy) return
		onClose()
	}

	return (
		<Modal open={open} onClose={handleClose} onAfterClose={onAfterClose} title={title} description={message}>
			{children}
			{warning && (
				<p role="note" className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 mt-4 text-sm text-amber-800">
					{warning}
				</p>
			)}
			<div className="mt-6 flex justify-end gap-3">
				<button
					type="button"
					onClick={handleClose}
					disabled={busy}
					data-autofocus
					className={secondaryButton}
				>
					Retour
				</button>
				<button
					type="button"
					onClick={handleConfirm}
					disabled={busy}
					className={destructive ? dangerButton : primaryButton}
				>
					<span className="grid">
						<span className={`col-start-1 row-start-1 ${busy ? "invisible" : ""}`}>
							{confirmLabel}
						</span>
						<span className={`col-start-1 row-start-1 ${busy ? "" : "invisible"}`}>
							{busyLabel ?? confirmLabel}
						</span>
					</span>
				</button>
			</div>
		</Modal>
	)
}
