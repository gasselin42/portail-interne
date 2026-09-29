import { useState, type SubmitEvent } from "react"
import { Link, useNavigate } from "react-router-dom"
import { createLeave, LEAVE_TYPE_LABELS, LeaveType, type LeaveTypeId } from "../api/leaves"
import { ErrorBanner } from "../components/ErrorBanner"
import { daysFromToday } from "../utils/date"

const MAX_SICK_BACKDATE_DAYS = 14

export function NewLeavePage() {
	const [startDate, setStartDate] = useState<string>("")
	const [endDate, setEndDate] = useState<string>("")
	const [reason, setReason] = useState<string>("")
	const [leaveType, setLeaveType] = useState<LeaveTypeId | null>(null)
	const [enCours, setEnCours] = useState<boolean>(false)
	const [erreur, setErreur] = useState<string | null>(null)

	const navigate = useNavigate();

	const minStartDate = leaveType === LeaveType.Maladie ? daysFromToday(-MAX_SICK_BACKDATE_DAYS) : daysFromToday(0)

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
			e.preventDefault()
			setErreur(null)
			try {
				if (leaveType === null) {
					setErreur("Veuillez choisir un type de congé.")
					return
				}
				
				if (endDate < startDate) {
					setErreur("La date de fin ne peut pas être avant la date de début.")
					return
				}

				if (startDate < minStartDate) {
					if (leaveType === LeaveType.Conge)
						setErreur("Un congé ne peut pas commencer dans le passé.")
					else
						setErreur(`Un congé maladie peut être rétroactif de ${MAX_SICK_BACKDATE_DAYS} jours au maximum.`)
					return
				}

				setEnCours(true)

				await createLeave({
					startDate,
					endDate,
					type: leaveType,
					reason
				})

				navigate("/leaves")
			} catch (e) {
				setErreur(e instanceof Error ? e.message : 'Erreur à la création du congé')
			} finally {
				setEnCours(false)
			}
	}

	return (
		<div className="min-h-screen bg-linear-to-b from-slate-50 via-slate-50 to-white">
			<div className="mx-auto max-w-2xl px-6 py-10">
				<header className="mb-8 flex flex-wrap items-end justify-between gap-4">
					<div>
						<p className="text-sm font-medium text-sky-700">Portail</p>
						<h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">
							Nouveau congé
						</h1>
						<p className="mt-1 text-sm text-slate-500">
							Créer une nouvelle demande de congé
						</p>
					</div>
					<Link
						to="/leaves"
						className="rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-50"
					>
						Retour à mes congés
					</Link>
				</header>

				{erreur && (
					<ErrorBanner
						message={erreur}
						dismissible
						onDismiss={() => setErreur(null)}
					/>
				)}

				<section className="overflow-hidden rounded-2xl border border-slate-200 bg-white p-6 shadow-sm shadow-slate-200/50">
					<form onSubmit={handleSubmit} className="space-y-5">
						<div className="grid gap-5 sm:grid-cols-3">
							<div>
								<label htmlFor="leaveType" className="mb-1.5 block text-sm font-medium text-slate-700">
									Type
								</label>
								<select
									id="leaveType"
									required
									value={leaveType ?? ""}
									onChange={(e) =>
										setLeaveType(
											e.target.value === "" ? null : (Number(e.target.value) as LeaveTypeId),
										)
									}
									className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
								>
									<option value="">Choisir...</option>
									{Object.entries(LeaveType).map(([, id]) =>(
										<option key={id} value={id}>{LEAVE_TYPE_LABELS[id]}</option>
									))}
								</select>
							</div>
							<div>
								<label htmlFor="startDate" className="mb-1.5 block text-sm font-medium text-slate-700">
									Début
								</label>
								<input
									id="startDate"
									type="date"
									required
									min={minStartDate}
									value={startDate}
									onChange={(e) => setStartDate(e.target.value)}
									className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
								/>
							</div>
							<div>
								<label htmlFor="endDate" className="mb-1.5 block text-sm font-medium text-slate-700">
									Fin
								</label>
								<input
									id="endDate"
									type="date"
									required
									min={startDate || minStartDate}
									value={endDate}
									onChange={(e) => setEndDate(e.target.value)}
									className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
								/>
							</div>
						</div>
						<div>
							<label htmlFor="reason" className="mb-1.5 block text-sm font-medium text-slate-700">
								Motif
							</label>
							<input
								id="reason"
								type="text"
								value={reason}
								onChange={(e) => setReason(e.target.value)}
								className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
							/>
						</div>

						<button
							type='submit'
							disabled={enCours}
							className="w-full rounded-lg bg-sky-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-sky-700 disabled:cursor-not-allowed disabled:opacity-50"
						>
							{enCours ? "Enregistrement..." : "Créer la demande de congé"}
						</button>
					</form>
				</section>
			</div>
		</div>
	)
}