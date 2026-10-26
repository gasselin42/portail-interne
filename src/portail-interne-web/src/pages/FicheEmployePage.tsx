import { useEffect, useState } from "react"
import { Link, useParams } from "react-router-dom"
import { DEPARTEMENT_LABELS } from "../api/admin"
import { getEmployee, type EmployeeDetail } from "../api/employees"
import { ErrorBanner, SuccessBanner } from "../components/feedback/Banner"
import { EmployeePhoto } from "../components/employees/EmployeePhoto"
import { useSession } from "../session/useSession"
import { secondaryButton } from "../ui/buttons"
import { DeclareSickLeaveDialog } from "../components/leaves/DeclareSickLeaveDialog"
import { formatRange } from "../utils/date"
import { ScheduleOutlined } from "@mui/icons-material"

export function FicheEmployePage() {
	const { id } = useParams()
	const { me } = useSession()
	const [employee, setEmployee] = useState<EmployeeDetail | null>(null)
	const [loading, setLoading] = useState(true)
	const [error, setError] = useState<string | null>(null)
	const [success, setSuccess] = useState<string | null>(null)
	const [declareLeaveOpen, setDeclareLeaveOpen] = useState(false)
	const [formRun, setFormRun] = useState(0)

	const employeeId = Number(id)
	const idInvalide = !Number.isInteger(employeeId) || employeeId <= 0

	const canDeclare =
			employee !== null &&
			me !== null &&
			employee.id !== me.employeeId &&
			employee.canManage

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

			{success && (
				<div className="mt-6">
					<SuccessBanner message={success} dismissible onDismiss={() => setSuccess(null)} />
				</div>
			)}

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
						{/* Rangée du haut : photo, nom, action */}
						<div className="flex flex-wrap items-center gap-5">
							<EmployeePhoto
								photoUrl={employee.photoUrl}
								firstName={employee.firstName}
								lastName={employee.lastName}
								size="xl"
							/>
							<div className="min-w-0">
								<h1 className="text-3xl font-semibold tracking-tight text-slate-900">
									{employee.firstName} {employee.lastName}
								</h1>
								<p className="mt-1 text-sm text-slate-500">{employee.jobTitle || "-"}</p>
							</div>
							<div className="ml-auto flex gap-3">
								{employee.canManage &&
									<Link
										to={`/employees/${employee.id}/schedule`}
										className={`${secondaryButton} inline-flex items-center gap-2`}
									>
										<ScheduleOutlined fontSize="small" />Horaire
									</Link>
								}
								{canDeclare &&
									<button
										type="button"
										className={secondaryButton}
										onClick={() => {
											setFormRun((n) => n + 1)
											setDeclareLeaveOpen(true)
										}}
									>
										Déclarer un congé maladie
									</button>
								}
							</div>
						</div>

						{/* Informations, sous la rangée */}
						<dl className="mt-8 grid gap-4 text-sm sm:grid-cols-2">
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
					</section>
				)
			)}
			{employee &&
				<DeclareSickLeaveDialog
					key={formRun}
					open={declareLeaveOpen}
					employee={employee}
					onClose={() => setDeclareLeaveOpen(false)}
					onDeclared={(leave) => {
						setDeclareLeaveOpen(false)
						setSuccess(`Congé maladie déclaré pour ${employee.firstName} ${employee.lastName}, ${formatRange(leave.startDate, leave.endDate)}.`)
					}}
				/>
			}
		</div>
	)
}
