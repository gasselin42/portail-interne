import type { ReactNode } from "react"
import { primaryButton } from "../../ui/buttons"
import { inputClass } from "../../ui/fields"

type Props = {
	value: string
	onChange: (value: string) => void
	onSubmit: () => void
	label: string
	placeholder?: string
	/** Filtres affichés entre le champ et le bouton (ex. une liste de sites). */
	children?: ReactNode
}

export function SearchBar({ value, onChange, onSubmit, label, placeholder, children }: Props) {
	return (
		<form onSubmit={(e) => {e.preventDefault(); onSubmit()}} className="mb-6 flex flex-wrap gap-3">
			<input
				type="search"
				value={value}
				onChange={(e) => onChange(e.target.value)}
				aria-label={label}
				placeholder={placeholder}
				className={`${inputClass} min-w-[16rem] flex-1`}
			/>
			{children}
			<button type="submit" className={primaryButton}>
				Rechercher
			</button>
		</form>
	)
}