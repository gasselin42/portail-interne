import { useState, type SubmitEvent } from "react"
import { inputClass, labelClass } from "../../ui/fields"
import { daysFromToday } from "../../utils/date"
import { Modal } from "../dialogs/Modal"
import { ErrorBanner } from "../feedback/Banner"
import { primaryButton, secondaryButton } from "../../ui/buttons"
import { declareSickLeave, type LeaveRequestResponse } from "../../api/leaves"
import type { EmployeeDetail } from "../../api/employees"

type Props = {
	open: boolean
	employee: EmployeeDetail
	onClose: () => void
	onDeclared: (leave: LeaveRequestResponse) => void
}

const MAX_SICK_BACKDATE_DAYS = 14

export function DeclareSickLeaveDialog({ open, employee, onClose, onDeclared }: Props) {
	const [startDate, setStartDate] = useState<string>("")
	const [endDate, setEndDate] = useState<string>("")
	const [reason, setReason] = useState<string>("")
	const [erreur, setErreur] = useState<string | null>(null)
	const [busy, setBusy] = useState(false)

	const minStartDate = daysFromToday(-MAX_SICK_BACKDATE_DAYS);

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setErreur(null)
		try {
			if (endDate < startDate) {
				setErreur("La date de fin ne peut pas être avant la date de début.")
				return
			}

			setBusy(true)

			const sickLeave = await declareSickLeave({
					employeeId: employee.id,
					startDate,
					endDate,
					reason,
				})

			onDeclared(sickLeave)
		} catch (e) {
			setErreur(e instanceof Error ? e.message : "Impossible de déclarer le congé.")
		} finally {
			setBusy(false)
		}
	}

	return (
		<Modal open={open} title={`Déclarer un congé maladie pour ${employee.firstName} ${employee.lastName}`} onClose={onClose}>
			{erreur &&
				<div className="mt-3">
					<ErrorBanner message={erreur} />
				</div>
			}
			
			<form onSubmit={handleSubmit}>
				<section className="pt-2">
					<div className="grid gap-5 sm:grid-cols-2">
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
								data-autofocus
								min={minStartDate}
								value={startDate}
								onChange={(e) => {
									setStartDate(e.target.value)
									if (!endDate || endDate < e.target.value)
										setEndDate(e.target.value)
								}}
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
					<div className="mt-3">
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
				</section>
				<div className="mt-6 flex justify-end gap-3">
					<button
						type="button"
						onClick={onClose}
						disabled={busy}
						className={secondaryButton}
					>
						Annuler
					</button>
					<button
						type="submit"
						disabled={busy}
						className={primaryButton}
					>
						<span className="grid">
							<span className={`col-start-1 row-start-1 ${busy ? "invisible" : ""}`}>
								Déclarer
							</span>
							<span className={`col-start-1 row-start-1 ${busy ? "" : "invisible"}`}>
								Déclaration…
							</span>
						</span>
					</button>
				</div>
			</form>
		</Modal>
	)
}