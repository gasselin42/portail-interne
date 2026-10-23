import { Link } from "react-router-dom"
import { DEPARTEMENT_LABELS, ROLE_LABELS } from "../../api/admin"
import { getEmployee, type EmployeeDetail } from "../../api/employees"
import { getLeaveBalance, type LeaveBalance } from "../../api/leaves"
import { listSites, type Site } from "../../api/sites"
import { useAsync } from "../../hooks/useAsync"
import { useSession } from "../../session/useSession"
import { secondaryButton, textLink } from "../../ui/buttons"
import { todayIso } from "../../utils/date"
import { Modal } from "../dialogs/Modal"
import { ErrorBanner } from "../feedback/Banner"
import type { Me } from "../../api/auth"

type Props = {
	open: boolean
	onClose: () => void
}

export function MyInfoDialog({ open, onClose }: Props) {
	return (
		<Modal open={open} title="Mes infos" onClose={onClose}>
			<MyInfoContent onClose={onClose} />
			<div className="mt-6 flex justify-end gap-3">
				<button type="button" onClick={onClose} className={secondaryButton}>
					Fermer
				</button>
			</div>
		</Modal>
	)
}

function MyInfoContent({ onClose }: { onClose: () => void }) {
	const { me } = useSession()
	const year = Number(todayIso().slice(0, 4))
	const { data, error, loading, retry } = useAsync(
		(signal) => Promise.all([getEmployee(me!.employeeId, signal), listSites(signal), getLeaveBalance(year, signal)]),
		[],
	)

	return (
		<div>
			{error ? (
				<ErrorBanner message={error} onRetry={retry} />
			) : loading ? (
				<p role="status" className="text-center text-sm text-slate-500">
					Chargement…
				</p>
			) : data && me && 
				<MyInfoDetails me={me} year={year} employee={data[0]} sites={data[1]} balance={data[2]} onClose={onClose} />			
			}
		</div>
	)
}

type DetailsProps = {
	me: Me
	year: number
	employee: EmployeeDetail
	sites: Site[]
	balance: LeaveBalance
	onClose: () => void
}

function MyInfoDetails({ me, year, employee, sites, balance, onClose }: DetailsProps) {
	const site = sites.find(s => s.id === me.siteId)
	const manager = employee.managerId === null ? "Aucun" : `${employee.managerFirstName} ${employee.managerLastName}`
	const days = balance.remainingDays
	const remainingDays = `${days} jour${days > 1 ? "s" : ""} restant${days > 1 ? "s" : ""} sur ${balance.annualDays} en ${year}`

	return (
		<>
			<dl className="mt-4 grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
				<dt className="text-slate-500">Poste</dt>
					<dd className="text-slate-900">{me.jobTitle}</dd>
				<dt className="text-slate-500">Département</dt>
					<dd className="text-slate-900">{DEPARTEMENT_LABELS[me.departement]}</dd>
				<dt className="text-slate-500">Courriel</dt>
					<dd className="text-slate-900">{me.email}</dd>
				<dt className="text-slate-500">Téléphone</dt>
					<dd className="text-slate-900">{me.phoneNumber ?? "—"}</dd>
				<dt className="text-slate-500">Site</dt>
					<dd className="text-slate-900">{site?.name} ({site?.timeZoneId})</dd>
				<dt className="text-slate-500">Manager</dt>
					<dd className="text-slate-900">{manager}</dd>
				<dt className="text-slate-500">Rôle</dt>
					<dd className="text-slate-900">{ROLE_LABELS[me.role]}</dd>
				<dt className="text-slate-500">Solde {year}</dt>
					<dd className="text-slate-900">{remainingDays}</dd>
			</dl>
			<div className="mt-4">
				<Link to="/leaves" onClick={onClose} className={textLink}>
					Voir mes congés
				</Link>
			</div>
		</>
	)
}