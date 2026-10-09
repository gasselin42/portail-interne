type Props = {
	hideName?: boolean
}

export function Brand({ hideName = false }: Props) {
	const labelClass = hideName ? "sr-only" : "whitespace-nowrap"

	return (
		<div className="flex items-center gap-3">
			<span
				aria-hidden="true"
				className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-sky-600 text-sm font-semibold text-white"
			>
				P
			</span>
			<span className={`text-lg font-semibold text-slate-900 ${labelClass}`}>Portail</span>
		</div>
	)
}