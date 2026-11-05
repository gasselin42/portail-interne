import type { ReactNode } from "react"
import {
	Dialog,
	DialogBackdrop,
	DialogPanel,
	DialogTitle,
	Description,
	Transition
} from "@headlessui/react"

type Props = {
	open: boolean
	title: string
	description?: string
	onClose: () => void
	onAfterClose?: () => void
	children: ReactNode
	size?: "md" | "xl"
}

export function Modal({
	open,
	title,
	description,
	onClose,
	onAfterClose,
	children,
	size = "md"
}: Props) {
	const panelSize = { md: "max-w-md", xl: "max-w-4xl"}
	return (
		<Transition show={open} afterLeave={onAfterClose}>
			<Dialog onClose={onClose} className="relative z-50">
				<DialogBackdrop transition className="fixed inset-0 bg-slate-900/40 transition-opacity duration-200 data-closed:opacity-0" />
				<div className="fixed inset-0 flex items-center justify-center p-4">
					<DialogPanel transition className={`w-full ${panelSize[size]} max-h-[90vh] p-6 rounded-2xl bg-white shadow-xl overflow-y-auto transition duration-200 ease-out data-closed:scale-95 data-closed:opacity-0`}>
						<DialogTitle className="text-lg font-semibold text-slate-900">{title}</DialogTitle>
						{description && <Description className="mt-2 text-sm text-slate-600">{description}</Description>}
						{children}
					</DialogPanel>
				</div>
			</Dialog>
		</Transition>
	)
}