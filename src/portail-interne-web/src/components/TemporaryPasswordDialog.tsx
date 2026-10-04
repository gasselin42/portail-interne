import { useState, useEffect } from "react"
import { Modal } from "./Modal"
import { primaryButton, secondaryButton } from "../ui/buttons"

type Props = {
	open: boolean
	password: string
	employeeName: string
	title?: string
	onClose: () => void
	onAfterClose?: () => void
}

export function TemporaryPasswordDialog({
	open,
	password,
	employeeName,
	title = "Mot de passe réinitialisé",
	onClose,
	onAfterClose
}: Props) {
	const [copied, setCopied] = useState(false)
	const [copyFailed, setCopyFailed] = useState(false)

	useEffect(() => {
		if (!copied) return
		const timer = setTimeout(() => setCopied(false), 2000)
		return () => clearTimeout(timer)
	}, [copied])

	async function handleCopy() {
		try {
			await navigator.clipboard.writeText(password)
			setCopied(true)
			setCopyFailed(false)
		} catch {
			setCopied(false)
			setCopyFailed(true)
		}
	}

	return (
		<Modal
			open={open}
			onClose={() => {}}
			onAfterClose={onAfterClose}
			title={title}
			description={`Transmets ce mot de passe temporaire à ${employeeName}. Copie-le maintenant : il ne sera plus affiché.`}
		>
			<p className="mt-4 rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 text-center font-mono text-lg tracking-wide text-slate-900 select-all">
				{password}
			</p>
			{copyFailed && (
				<p role="alert" className="mt-2 text-sm text-red-600">
					Copie impossible : sélectionne le mot de passe et copie-le manuellement.
				</p>
			)}
			<div className="mt-6 flex justify-end gap-3">
				<button type="button" onClick={onClose} className={secondaryButton}>
					Terminé
				</button>
				<button type="button" onClick={handleCopy} data-autofocus className={primaryButton}>
					<span className="grid">
						<span className={`col-start-1 row-start-1 ${copied ? "invisible" : ""}`}>Copier</span>
						<span className={`col-start-1 row-start-1 ${copied ? "" : "invisible"}`}>Copié ✓</span>
					</span>
				</button>
			</div>
		</Modal>
	)
}
