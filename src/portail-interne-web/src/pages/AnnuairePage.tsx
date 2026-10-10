import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { DEPARTEMENT_LABELS } from "../api/admin"
import { listEmployees, type EmployeeListItem } from "../api/employees"
import { ErrorBanner, type PageError } from "../components/Banner"
import { ContactPage } from "@mui/icons-material"
import { rowIconButton } from "../ui/buttons"
import { PageHeader } from "../components/PageHeader"
import { SearchBar } from "../components/SearchBar"

export function AnnuairePage() {
	const [search, setSearch] = useState("")
	const [query, setQuery] = useState("")
	const [searchRun, setSearchRun] = useState(0)
	const [employees, setEmployees] = useState<EmployeeListItem[]>([])
	const [loading, setLoading] = useState(true)
	const [error, setError] = useState<PageError | null>(null)

	useEffect(() => {
		const controller = new AbortController()

		listEmployees(query || undefined, controller.signal)
			.then(setEmployees)
			.catch((e) => {
				if (controller.signal.aborted) return
				setError({
					message: e instanceof Error ? e.message : "Erreur",
					dismissible: false,
				})
			})
			.finally(() => {
				if (controller.signal.aborted) return
				setLoading(false)
			})
		
		return () => controller.abort()
	}, [query, searchRun])

	function handleSearch() {
		setError(null)
		setLoading(true)
		setQuery(search.trim())
		setSearchRun((n) => n + 1)
	}

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				eyebrow="Équipe"
				title="Annuaire"
				description="Recherche un employé par nom, email, poste ou département."
			/>

			<SearchBar
				value={search}
				onChange={setSearch}
				onSubmit={handleSearch}
				label="Rechercher un employé"
				placeholder="Nom, email, poste, département…"
			/>

			{error && (
				<ErrorBanner
					message={error.message}
					dismissible={error.dismissible}
					onDismiss={() => setError(null)}
				/>
			)}

			<section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
				{loading ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">
						Chargement de l'annuaire...
					</p>
				) : error && !error.dismissible ? null : employees.length === 0 ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">
						{`Aucun employé ne correspond à « ${query} »`}
					</p>
				) : (
					<div className="overflow-x-auto">
						<table className="w-full min-w-180 border-collapse text-left">
							<thead>
								<tr className="border-b border-slate-200 bg-slate-50/80">
									<th className="px-5 py-3.5 text-sm font-semibold tracking-wide text-slate-500 uppercase">
										Nom
									</th>
									<th className="px-5 py-3.5 text-sm font-semibold tracking-wide text-slate-500 uppercase">
										Prénom
									</th>
									<th className="px-5 py-3.5 text-sm font-semibold tracking-wide text-slate-500 uppercase">
										Email
									</th>
									<th className="px-5 py-3.5 text-sm font-semibold tracking-wide text-slate-500 uppercase">
										Poste
									</th>
									<th className="px-5 py-3.5 text-sm font-semibold tracking-wide text-slate-500 uppercase">
										Département
									</th>
									<th className="w-0 px-5 py-3.5" />
								</tr>
							</thead>
							<tbody>
								{employees.map((employee) => (
									<tr
										key={employee.id}
										className="border-b border-slate-100 transition last:border-b-0 hover:bg-slate-50/70"
									>
										<td className="px-5 py-3.5 text-sm font-medium text-slate-900">
											{employee.lastName}
										</td>
										<td className="px-5 py-3.5 text-sm text-slate-700">{employee.firstName}</td>
										<td className="px-5 py-3.5 text-sm text-slate-600">{employee.email}</td>
										<td className="px-5 py-3.5 text-sm text-slate-600">{employee.jobTitle}</td>
										<td className="px-5 py-3.5 text-sm text-slate-600">
											{DEPARTEMENT_LABELS[employee.departement]}
										</td>
										<td className="w-0 px-5 py-3.5">
											<Link
												to={`/employees/${employee.id}`}
												title="Voir la fiche"
												className={rowIconButton}
											>
												<ContactPage fontSize="small" />
											</Link>
										</td>
									</tr>
								))}
							</tbody>
						</table>
					</div>
				)}
			</section>
		</div>
	)
}
