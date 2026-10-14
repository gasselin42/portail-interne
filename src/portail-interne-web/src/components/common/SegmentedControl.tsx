import { useLayoutEffect, useRef } from "react"
import { focusRing } from "../../ui/buttons"

type Option<T extends string> = { value: T; label: string }

type Props<T extends string> = {
	/** Nom du groupe pour les lecteurs d'écran (ex. « Vue »). */
	label: string
	/** Doit être un tableau stable (constante hors du composant), sinon la pastille est remesurée à chaque rendu. */
	options: Option<T>[]
	value: T
	onChange: (value: T) => void
}

/**
 * Groupe de boutons dont un seul est actif (Jour / Semaine / Mois…).
 * Une pastille glisse sous le bouton actif et prend sa largeur.
 */
export function SegmentedControl<T extends string>({ label, options, value, onChange }: Props<T>) {
	const buttonsRef = useRef<(HTMLButtonElement | null)[]>([])
	const pillRef = useRef<HTMLSpanElement>(null)

	// Avant l'affichage : place la pastille sous le bouton actif, puis suit les changements de taille.
	useLayoutEffect(() => {
		const pill = pillRef.current
		const container = pill?.parentElement
		if (!pill || !container) return

		function measure() {
			const active = buttonsRef.current[options.findIndex((option) => option.value === value)]
			if (!active || !pill) return
			pill.style.width = `${active.offsetWidth}px`
			pill.style.transform = `translateX(${active.offsetLeft}px)`
			pill.dataset.ready = "true"
		}

		measure()

		// Polices chargées, zoom, passage mobile ↔ grand écran : on remesure.
		const observer = new ResizeObserver(measure)
		observer.observe(container)
		return () => observer.disconnect()
	}, [options, value])
	
	return (
		<div
			role="group"
			aria-label={label}
			className="relative inline-flex rounded-lg border border-slate-200 bg-white p-0.5"
		>
			<span
				ref={pillRef}
				aria-hidden="true"
				className="absolute inset-y-0.5 left-0 rounded-md bg-sky-600 opacity-0 shadow-sm duration-200 ease-out data-ready:opacity-100 data-ready:transition-[transform,width] motion-reduce:transition-none"
			/>
			{options.map((option, i) => (
				<button
					key={option.value}
					ref={(el) => {
						buttonsRef.current[i] = el
					}}
					type="button"
					aria-pressed={option.value === value}
					onClick={() => onChange(option.value)}
					className={`relative z-10 rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 transition-colors hover:text-slate-900 aria-pressed:text-white aria-pressed:hover:text-white ${focusRing}`}
				>
					{option.label}
				</button>
			))}
		</div>
	)
}