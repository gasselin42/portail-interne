import type { ReactNode } from "react"

type Props = {
	eyebrow?: string
	title: string
	description?: string
	actions?: ReactNode
}

export function PageHeader({ eyebrow, title, description, actions }: Props) {
	return (
		<header className="mb-8 flex flex-wrap items-end justify-between gap-4">
			<div className="space-y-1">
				{eyebrow && <p className="text-sm font-medium text-sky-700">{eyebrow}</p>}
				<h1 className="text-3xl font-semibold tracking-tight text-slate-900">
					{title}
				</h1>
				{description && <p className="text-sm text-slate-500">{description}</p>}
			</div>
			{actions && <div className="flex flex-wrap gap-3">{actions}</div>}
		</header>
	)
}