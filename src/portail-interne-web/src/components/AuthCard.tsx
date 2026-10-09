import type { ReactNode } from "react"
import { Brand } from "./Brand"

type Props = {
	title: string
	description?: string
	children?: ReactNode
	icon?: ReactNode
	centered?: boolean
	role?: "status" | "alert"
}

export function AuthCard({ title, description, children, icon, centered, role }: Props) {
	return (
		<div className="flex min-h-screen flex-col items-center justify-center bg-linear-to-b from-slate-50 via-slate-50 to-white px-4 py-12">
			<Brand />
			<section role={role} className={`mt-8 w-full max-w-sm p-8 rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50 ${centered ? "text-center" : ""}`}>
				{icon && <div className="mb-4 flex justify-center">{icon}</div>}
				<h1 className="text-xl font-semibold tracking-tight text-slate-900">{title}</h1>
				{description && <p className="mt-1 text-sm text-slate-500">{description}</p>}
				{children && <div className="mt-6">{children}</div>}
			</section>
		</div>
	)
}