import { useState } from "react"
import { Link, useSearchParams } from "react-router-dom"
import { DEPARTEMENT_LABELS } from "../api/admin"
import { listEmployees } from "../api/employees"
import { ErrorBanner } from "../components/feedback/Banner"
import { ContactPage } from "@mui/icons-material"
import { rowIconButton } from "../ui/buttons"
import { PageHeader } from "../components/common/PageHeader"
import { SearchBar } from "../components/common/SearchBar"
import { useAsync } from "../hooks/useAsync"
import { listSites } from "../api/sites"
import { inputClass } from "../ui/fields"

export function AnnuairePage() {
	const [searchParams, setSearchParams] = useSearchParams()
	const [search, setSearch] = useState("")
	const [query, setQuery] = useState("")
	const [searchRun, setSearchRun] = useState(0)

	let siteId: number | undefined = Number(searchParams.get("site"))
	if (!(Number.isInteger(siteId) && siteId > 0)) siteId = undefined
	
	const {
		data: employeesData,
		loading: employeesLoading,
		error: employeesError,
		retry: employeesRetry
	} = useAsync((signal) => listEmployees(query || undefined, siteId, signal), [query, siteId, searchRun])
	const employees = employeesData ?? []

	const { data: sites } = useAsync((signal) => listSites(signal), [])
	
	function handleSearch() {
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
			>
				{sites && sites.length > 1 && (
					<div className="w-48">
						<select
							aria-label="Site"
							value={siteId ?? ""}
							onChange={(e) => setSearchParams(e.target.value === "" ? {} : { site: e.target.value })}
							className={inputClass}
						>
							<option value="">Tous les sites</option>
							{sites.map((site) => (
								<option key={site.id} value={site.id}>
									{site.name}
								</option>
							))}
						</select>
					</div>
				)}
			</SearchBar>


			<section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
				{employeesError ? (
					<div className="p-4">
						<ErrorBanner message={employeesError} onRetry={employeesRetry} />
					</div>
				) : employeesLoading ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">
						Chargement de l'annuaire...
					</p>
				) : employees.length === 0 ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">
						{query !== ""
							? `Aucun employé ne correspond à « ${query} ».`
							: "Aucun employé sur ce site."
						}
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
									<th className="px-5 py-3.5 text-sm font-semibold tracking-wide text-slate-500 uppercase">
										Site
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
										<td className="px-5 py-3.5 text-sm text-slate-600">
											{employee.siteName}
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
