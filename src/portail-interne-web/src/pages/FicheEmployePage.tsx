import { useEffect, useState } from "react"
import { Link, useParams } from "react-router-dom"
import { DEPARTEMENT_LABELS } from "../api/admin"
import { getEmployee, type EmployeeDetail } from "../api/employees"
import { ErrorBanner } from "../components/Banner"
import { EmployeePhoto } from "../components/EmployeePhoto"

export function FicheEmployePage() {
	const { id } = useParams()
	const [employee, setEmployee] = useState<EmployeeDetail | null>(null)
	const [loading, setLoading] = useState(true)
	const [error, setError] = useState<string | null>(null)

	const employeeId = Number(id)
	const idInvalide = !Number.isInteger(employeeId) || employeeId <= 0

	useEffect(() => {
		if (idInvalide) return

		getEmployee(employeeId)
			.then(setEmployee)
			.catch((e) => setError(e instanceof Error ? e.message : "Erreur"))
			.finally(() => setLoading(false))
	}, [employeeId, idInvalide])

	return (
		<div className="mx-auto max-w-2xl px-6 py-10">
			<Link to="/employees" className="text-sm font-medium text-sky-700 hover:underline">
				Retour à l'annuaire
			</Link>

			{idInvalide ? (
				<div className="mt-6">
					<ErrorBanner message="Employé introuvable" />
				</div>
			) : loading ? (
				<p className="mt-8 text-sm text-slate-500">Chargement...</p>
			) : error ? (
				<div className="mt-6">
					<ErrorBanner message={error} />
				</div>
			) : (
				employee && (
					<section className="mt-6 overflow-hidden rounded-2xl border border-slate-200/80 bg-white p-6 shadow-sm shadow-slate-200/50">
						<div className="flex items-center gap-5">
							<EmployeePhoto
								photoUrl={employee.photoUrl}
								firstName={employee.firstName}
								lastName={employee.lastName}
								size="xl"
							/>
							<div>
								<h1 className="text-3xl font-semibold tracking-tight text-slate-900">
									{employee.firstName} {employee.lastName}
								</h1>
								<p className="mt-1 text-sm text-slate-500">{employee.jobTitle || "-"}</p>
							</div>

							<dl className="mt-8 space-y-4 text-sm">
								<div>
									<dt className="font-medium text-slate-500">Email</dt>
									<dd className="text-slate-900">{employee.email}</dd>
								</div>
								<div>
									<dt className="font-medium text-slate-500">Département</dt>
									<dd className="text-slate-900">{DEPARTEMENT_LABELS[employee.departement]}</dd>
								</div>
								<div>
									<dt className="font-medium text-slate-500">Téléphone</dt>
									<dd className="text-slate-900">{employee.phoneNumber || "-"}</dd>
								</div>
								<div>
									<dt className="font-medium text-slate-500">Manager</dt>
									<dd className="text-slate-900">
										{employee.managerFirstName && employee.managerLastName
											? `${employee.managerFirstName} ${employee.managerLastName}`
											: "-"}
									</dd>
								</div>
							</dl>
						</div>
					</section>
				)
			)}
		</div>
	)
}
