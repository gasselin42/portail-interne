import { useEffect, useRef, useState } from "react"
import { Combobox, ComboboxInput, ComboboxOption, ComboboxOptions } from "@headlessui/react"
import { lookupEmployees, type EmployeeLookupItem, type EmployeeOption } from "../api/employees"
import { DEPARTEMENT_LABELS } from "../api/admin"
import { CloseOutlined } from "@mui/icons-material"
import { focusRing } from "../ui/buttons"
import { Avatar } from "./Avatar"
import { fieldBase } from "../ui/fields"

const SEARCH_DELAY_MS = 250

type Props = {
	id: string
	value: EmployeeOption | null
	onChange: (value: EmployeeOption | null) => void
	excludeTeamOf?: number
	placeholder?: string
	clearLabel?: string
}

function fullName(e: EmployeeOption): string {
	return `${e.firstName} ${e.lastName}`
}

export function EmployeeCombobox({
	id,
	value,
	onChange,
	excludeTeamOf,
	placeholder,
	clearLabel = "Retirer la sélection",
}: Props) {
	const [query, setQuery] = useState("")
	const [results, setResults] = useState<EmployeeLookupItem[]>([])
	const [loading, setLoading] = useState(true)
	const [error, setError] = useState(false)
	const [hasMore, setHasMore] = useState(false)
	const inputRef = useRef<HTMLInputElement>(null)

	useEffect(() => {
		const controller = new AbortController()

		const timer = setTimeout(() => {
			setLoading(true)
			setError(false)
			lookupEmployees({ search: query, excludeTeamOf }, controller.signal)
				.then((r) => {
					setResults(r.items)
					setHasMore(r.hasMore)
					setLoading(false)
				})
				.catch(() => {
					if (controller.signal.aborted) return
					setError(true)
					setLoading(false)
				})
		}, SEARCH_DELAY_MS)

		return () => {
			clearTimeout(timer)
			controller.abort()
		}
	}, [query, excludeTeamOf])

	function handleClear() {
		onChange(null)
		setQuery("")
		inputRef.current?.focus()
	}

	return (
		<Combobox value={value} onChange={onChange} by="id" onClose={() => setQuery("")}>
			{({ open }) => (
				<>
					<div className="relative">
						<ComboboxInput
							id={id}
							placeholder={placeholder}
							displayValue={(e: EmployeeOption | null) => (e ? fullName(e) : "")}
							onKeyDown={(e) => {
								if (e.key === "Enter" && !open) e.preventDefault()
							}}
							onChange={(event) => setQuery(event.target.value)}
							autoComplete="off"
							spellCheck={false}
							className={`${fieldBase} pl-3 pr-9`}
							ref={inputRef}
						/>
						{value && (
							<button
								type="button"
								onMouseDown={(e) => e.preventDefault()}
								onClick={handleClear}
								aria-label={clearLabel}
								className={`${focusRing} absolute inset-y-0 right-0 flex items-center px-2.5 text-slate-400 transition hover:text-slate-600`}
							>
								<CloseOutlined fontSize="small" />
							</button>
						)}
					</div>
					<ComboboxOptions anchor="bottom start" className="w-(--input-width) [--anchor-gap:4px] [--anchor-max-height:18rem] rounded-lg border border-slate-200 bg-white p-1 shadow-lg overflow-y-auto z-50 empty:invisible">
						{loading && results.length === 0 && (
							<div role="status" className="px-2 py-2 text-sm text-slate-500">
								Recherche…
							</div>
						)}

						{error && (
							<div role="alert" className="px-2 py-2 text-sm text-red-600">
								Impossible de charger les employés.
							</div>
						)}

						{!loading && !error && results.length === 0 && (
							<div role="status" className="px-2 py-2 text-sm text-slate-500">
								Aucun employé trouvé.
							</div>
						)}

						{!error &&
							results.map((employee) => (
								<ComboboxOption
									key={employee.id}
									value={employee}
									className={`group flex items-center gap-3 rounded-md px-2 py-2 cursor-default select-none data-focus:bg-sky-600 data-focus:text-white data-selected:not-data-focus:bg-sky-50 ${loading ? "opacity-60" : ""}`}
								>
									<Avatar firstName={employee.firstName} lastName={employee.lastName} size="sm" />
									<div className="min-w-0 flex-1">
										<p className="truncate text-sm text-slate-900 group-data-focus:text-white">{fullName(employee)}</p>
										<p className="truncate text-xs text-slate-500 group-data-focus:text-sky-100">{[employee.jobTitle, DEPARTEMENT_LABELS[employee.departement]].filter(Boolean).join(" · ")}</p>
									</div>
								</ComboboxOption>
							))}

						{hasMore && !error && (
							<div role="status" className="px-2 py-2 mt-1 border-t border-slate-100 text-xs text-slate-500">
								Affine ta recherche pour voir plus de résultats.
							</div>
						)}
					</ComboboxOptions>
				</>
			)}
		</Combobox>
	)
}
