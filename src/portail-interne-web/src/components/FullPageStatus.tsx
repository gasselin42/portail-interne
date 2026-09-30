import { useState } from "react"

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

export function FullPageStatus({ title, message, loading = false, action}: Props) {
	const [busy, setBusy] = useState(false)

	async function handleAction() {
		if (!action) return
		setBusy(true)
		try {
			await Promise.all([
				action.onClick().catch(() => {}),
				new Promise((resolve) => setTimeout(resolve, 600))
			])
		} finally {
			setBusy(false)
		}
	}

	return (
		<div className="flex min-h-screen items-center justify-center bg-linear-to-b from-slate-50 via-slate-50 to-white px-6">
			<section
				role={loading ? "status" : "alert"}
				className="w-full max-w-sm rounded-2xl border border-slate-200/80 bg-white p-8 text-center shadow-sm shadow-slate-200/50"
			>
				{loading && (
					<div 
						aria-hidden="true"
						className="mx-auto mb-4 h-8 w-8 animate-spin rounded-full border-2 border-slate-200 border-t-sky-600"
					/>
				)}

				<h1 className="text-lg font-semibold text-slate-900">{title}</h1>

				{message && <p className="mt-2 text-sm text-slate-500">{message}</p>}

				{action && (
					<button
						type="button"
						onClick={handleAction}
						disabled={busy}
						className="mt-6 w-full rounded-lg bg-sky-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-sky-700 disabled:cursor-not-allowed disabled:opacity-50"
					>
						{busy ? (action.busyLabel ?? "Chargement...") : action.label}
					</button>
				)}
			</section>
		</div>
	)
}