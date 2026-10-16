import { useId, type ReactNode } from "react"
import { Link } from "react-router-dom"
import { textLink } from "../../ui/buttons"

type Props = {
	title: string
	/** le lien en haut à droite, optionnel */
	link?: { to: string; label: string }
	children: ReactNode
	className?: string
}

export function Widget({ title, link, children, className }: Props) {
	const id = useId()
	return (
		<section aria-labelledby={id} className={`rounded-xl border border-slate-200/80 bg-white p-4 shadow-sm shadow-slate-200/50 ${className ?? ""}`}>
			<header className="flex items-center justify-between mb-3">
				<h2 id={id} className="text-base font-semibold text-slate-900">{title}</h2>
				{link && (
					<Link to={link.to} className={textLink}>
						{link.label}
					</Link>
				)}
			</header>
			{children}
		</section>
	)
}