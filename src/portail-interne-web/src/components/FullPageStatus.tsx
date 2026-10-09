import { useState } from "react"
import { primaryButton } from "../ui/buttons"
import { AuthCard } from "./AuthCard"

type Props = {
	title: string
	message?: string
	loading?: boolean
	action?: {
		label: string
		busyLabel?: string
		onClick: () => Promise<void>
	}
}

export function FullPageStatus({ title, message, loading = false, action }: Props) {
	const [busy, setBusy] = useState(false)

	async function handleAction() {
		if (!action) return
		setBusy(true)
		try {
			await Promise.all([
				action.onClick().catch(() => {}),
				new Promise((resolve) => setTimeout(resolve, 600)),
			])
		} finally {
			setBusy(false)
		}
	}

	return (
		<AuthCard
			title={title}
			description={message}
			centered
			role={loading ? "status" : "alert"}
			icon={loading ? (
					<div
						aria-hidden="true"
						className="h-8 w-8 animate-spin rounded-full border-2 border-slate-200 border-t-sky-600"
					/>
				) : undefined}
		>
			{action && (
				<button
					type="button"
					onClick={handleAction}
					disabled={busy}
					className={`${primaryButton} w-full`}
				>
					{busy ? (action.busyLabel ?? "Chargement…") : action.label}
				</button>
			)}
		</AuthCard>
	)
}
