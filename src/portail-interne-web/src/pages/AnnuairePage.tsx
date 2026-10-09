import { useEffect, useState, type SubmitEvent } from "react"
import { Link } from "react-router-dom"
import { DEPARTEMENT_LABELS } from "../api/admin"
import { listEmployees, type EmployeeListItem } from "../api/employees"
import { ErrorBanner } from "../components/Banner"
import { ContactPage } from "@mui/icons-material"
import { primaryButton, rowIconButton } from "../ui/buttons"

type PageError = {
	message: string
	dismissible: boolean
}

export function AnnuairePage() {
	const [search, setSearch] = useState("")
	const [query, setQuery] = useState("")
	const [employees, setEmployees] = useState<EmployeeListItem[]>([])
	const [loading, setLoading] = useState(true)
	const [error, setError] = useState<PageError | null>(null)

	useEffect(() => {
		listEmployees(query || undefined)
			.then(setEmployees)
			.catch((e) =>
				setError({
					message: e instanceof Error ? e.message : "Erreur",
					dismissible: false,
				}),
			)
			.finally(() => setLoading(false))
	}, [query])

	function handleSearch(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setError(null)
		setLoading(true)
		setQuery(search.trim())
	}

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<header className="mb-8">
				<p className="text-sm font-medium text-sky-700">Équipe</p>
				<h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">Annuaire</h1>
				<p className="mt-1 text-sm text-slate-500">
					Rechercher un employé par nom, email, poste ou département.
				</p>
			</header>

			<form onSubmit={handleSearch} className="mb-6 flex flex-wrap gap-3">
				<input
					type="search"
					value={search}
					onChange={(e) => setSearch(e.target.value)}
					placeholder="Nom, email, poste, département..."
					className="min-w-[16rem] flex-1 rounded-lg border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 shadow-sm transition outline-none focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
				/>
				<button type="submit" className={primaryButton}>
					Rechercher
				</button>
			</form>

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
					<p className="px-6 py-12 text-center text-sm text-slate-500">Aucun employé trouvé</p>
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
