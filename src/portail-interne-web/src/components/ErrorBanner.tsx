type Props = {
	message: string
	/** Si true : bouton × pour fermer. Si false : erreur bloquante, non fermable. */
	dismissible?: boolean
	onDismiss?: () => void
}

export function ErrorBanner({ message, dismissible = false, onDismiss }: Props) {
	return (
		<div className="mb-4 flex items-start gap-3 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
			<p className="flex-1 text-center">{message}</p>
			{dismissible && onDismiss && (
				<button
					type="button"
					aria-label="Fermer l'erreur"
					className="shrink-0 rounded px-1.5 text-base leading-none text-red-500 transition hover:bg-red-100 hover:text-red-800"
					onClick={onDismiss}
				>
					×
				</button>
			)}
		</div>
	)
}
