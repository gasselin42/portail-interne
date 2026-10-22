import { useState, type SubmitEvent } from "react"
import { Link, useNavigate } from "react-router-dom"
import { createLeave, LEAVE_TYPE_LABELS, LeaveType, previewLeave, type LeaveTypeId } from "../api/leaves"
import { ErrorBanner } from "../components/feedback/Banner"
import { daysFromToday, formatDate } from "../utils/date"
import { primaryButton, secondaryButton } from "../ui/buttons"
import { inputClass, labelClass } from "../ui/fields"
import { PageHeader } from "../components/common/PageHeader"
import { useAsync } from "../hooks/useAsync"

const MAX_SICK_BACKDATE_DAYS = 14

export function NewLeavePage() {
	const [startDate, setStartDate] = useState<string>("")
	const [endDate, setEndDate] = useState<string>("")
	const [reason, setReason] = useState<string>("")
	const [leaveType, setLeaveType] = useState<LeaveTypeId | null>(null)
	const [enCours, setEnCours] = useState<boolean>(false)
	const [erreur, setErreur] = useState<string | null>(null)

	const navigate = useNavigate()

	const minStartDate =
		leaveType === LeaveType.Maladie ? daysFromToday(-MAX_SICK_BACKDATE_DAYS) : daysFromToday(0)

	const canPreview = leaveType === LeaveType.Conge && startDate !== "" && endDate !== "" && endDate >= startDate

	const preview = useAsync(
		(signal) => (canPreview ? previewLeave(startDate, endDate, signal) : Promise.resolve(null)),
		[canPreview, startDate, endDate]
	)

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
					setErreur(
						`Un congé maladie peut être rétroactif de ${MAX_SICK_BACKDATE_DAYS} jours au maximum.`,
					)
				return
			}

			setEnCours(true)

			await createLeave({
				startDate,
				endDate,
				type: leaveType,
				reason,
			})

			navigate("/leaves")
		} catch (e) {
			setErreur(e instanceof Error ? e.message : "Erreur à la création du congé")
		} finally {
			setEnCours(false)
		}
	}

	return (
		<div className="mx-auto max-w-2xl px-6 py-10">
			<PageHeader
				eyebrow="Congés"
				title="Nouvelle demande de congé"
				description="Crée une nouvelle demande de congé."
				actions={
					<Link to="/leaves" className={secondaryButton}>
						Retour à mes congés
					</Link>
				}
			/>

			{erreur && <ErrorBanner message={erreur} dismissible onDismiss={() => setErreur(null)} />}

			<section className="overflow-hidden rounded-2xl border border-slate-200 bg-white p-6 shadow-sm shadow-slate-200/50">
				<form onSubmit={handleSubmit} className="space-y-5">
					<div className="grid gap-5 sm:grid-cols-3">
						<div>
							<label
								htmlFor="leaveType"
								className={labelClass}
							>
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
								className={inputClass}
							>
								<option value="">Choisir...</option>
								{Object.entries(LeaveType).map(([, id]) => (
									<option key={id} value={id}>
										{LEAVE_TYPE_LABELS[id]}
									</option>
								))}
							</select>
						</div>
						<div>
							<label
								htmlFor="startDate"
								className={labelClass}
							>
								Début
							</label>
							<input
								id="startDate"
								type="date"
								required
								min={minStartDate}
								value={startDate}
								onChange={(e) => setStartDate(e.target.value)}
								className={inputClass}
							/>
						</div>
						<div>
							<label htmlFor="endDate" className={labelClass}>
								Fin
							</label>
							<input
								id="endDate"
								type="date"
								required
								min={startDate || minStartDate}
								value={endDate}
								onChange={(e) => setEndDate(e.target.value)}
								className={inputClass}
							/>
						</div>
					</div>
					{canPreview && (
						<div className="min-h-5 text-sm">
							{preview.error ? (
								<p className="text-slate-500">Impossible de calculer le solde pour l'instant.</p>
							) : preview.data ? (
								preview.data.workingDays === 0 ? (
									<p className="text-slate-500">Tu ne travailles aucun jour durant cette période.</p>
								) : (
									<>
										<p className="text-slate-500">
											{preview.data.workingDays} jour{preview.data.workingDays > 1 ? "s" : ""} travaillé{preview.data.workingDays > 1 ? "s" : ""}
										</p>
										{preview.data.unpaidDates.length > 0 && (
											<p role="status" className="mt-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-amber-800">
												Cette demande dépasse ton solde :{" "}
												<strong>
													{preview.data.unpaidDates.length} jour{preview.data.unpaidDates.length > 1 ? "s" : ""}
												</strong>{" "}
												{preview.data.unpaidDates.length > 1 ? "seront" : "sera"} sans solde ({formatDateList(preview.data.unpaidDates)}).
											</p>
										)}
									</>
								)
							) : null}
						</div>
					)}
					<div>
						<label htmlFor="reason" className={labelClass}>
							Motif
						</label>
						<input
							id="reason"
							type="text"
							value={reason}
							onChange={(e) => setReason(e.target.value)}
							className={inputClass}
						/>
					</div>

					<button type="submit" disabled={enCours} className={`${primaryButton} w-full`}>
						{enCours ? "Enregistrement..." : "Créer la demande de congé"}
					</button>
				</form>
			</section>
		</div>
	)
}

const listFormatter = new Intl.ListFormat("fr-CA", { type: "conjunction" })

/** « 18 nov. 2026, 19 nov. 2026 et 20 nov. 2026 » */
function formatDateList(dates: string[]): string {
	return listFormatter.format(dates.map(formatDate))
}
