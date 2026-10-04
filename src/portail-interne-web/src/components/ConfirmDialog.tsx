import { useState } from "react"
import {
	Dialog,
	DialogBackdrop,
	DialogPanel,
	DialogTitle,
	Description
} from "@headlessui/react"

type Props = {
	open: boolean
	title: string
	message: string
	confirmLabel: string
	busyLabel?: string
	destructive?: boolean
	onConfirm: () => Promise<void>
	onClose: () => void
}

export function ConfirmDialog({
	open,
	title,
	message,
	confirmLabel,
	busyLabel,
	destructive = false,
	onConfirm,
	onClose,
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
		<Dialog open={open} onClose={handleClose} className="relative z-50">
			<DialogBackdrop transition className="fixed inset-0 bg-slate-900/40 transition-opacity duration-200 data-closed:opacity-0" />
			<div className="fixed inset-0 flex items-center justify-center p-4">
				<DialogPanel transition className="w-full max-w-md p-6 rounded-2xl bg-white shadow-xl transition duration-200 ease-out data-closed:scale-95 data-closed:opacity-0">
					<DialogTitle className="text-lg font-semibold text-slate-900">{title}</DialogTitle>
					<Description className="mt-2 text-sm text-slate-600">{message}</Description>
					<div className="mt-6 flex justify-end gap-3">
						<button
							type="button"
							onClick={handleClose}
							disabled={busy}
							autoFocus
							className="rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-300 disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
						>
							Retour
						</button>
						<button
							type="button"
							onClick={handleConfirm}
							disabled={busy}
							className={`rounded-lg px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600 ${destructive ? "bg-red-600 hover:bg-red-700" : "bg-sky-600 hover:bg-sky-700"}`}
						>
							<span className="grid">
								<span className={`col-start-1 row-start-1 ${busy ? "invisible" : ""}`}>{confirmLabel}</span>
								<span className={`col-start-1 row-start-1 ${busy ? "" : "invisible"}`}>{busyLabel ?? confirmLabel}</span>
							</span>
						</button>
					</div>
				</DialogPanel>
			</div>
		</Dialog>
	)
}