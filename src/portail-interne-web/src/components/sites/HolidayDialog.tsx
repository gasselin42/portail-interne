import { useState, type SubmitEvent } from "react"
import { inputClass, labelClass } from "../../ui/fields"
import { Modal } from "../dialogs/Modal"
import { ErrorBanner } from "../feedback/Banner"
import { primaryButton, secondaryButton } from "../../ui/buttons"
import { createHoliday, updateHoliday, type Holiday } from "../../api/holidays"
import { todayIso } from "../../utils/date"

type Props = {
	open: boolean
	siteId: number
	holiday: Holiday | null
	onClose: () => void
	onSaved: (date: string) => void
}

type FormProps = {
	siteId: number
	holiday: Holiday | null
	onClose: () => void
	onSaved: (date: string) => void
}

export function HolidayDialog({ open, siteId, holiday, onClose, onSaved }: Props) {
	return (
		<Modal open={open} title={holiday === null ? "Nouveau férié" : "Modifier un férié"} onClose={onClose}>
			<HolidayForm siteId={siteId} holiday={holiday} onClose={onClose} onSaved={onSaved} />
		</Modal>
	)
}

function HolidayForm({ siteId, holiday, onSaved, onClose }: FormProps) {
	const [name, setName] = useState<string>(holiday?.name ?? "")
	const [date, setDate] = useState<string>(holiday?.date ?? "")
	const [busy, setBusy] = useState(false)
	const [error, setError] = useState<string | null>(null)

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setError(null)
		try {
			setBusy(true)
			if (holiday === null) await createHoliday(siteId, date, name)
			else await updateHoliday(siteId, holiday.id, date, name)
			onSaved(date)
		} catch (e) {
			setError(e instanceof Error ? e.message : "Impossible d'enregistrer le férié.")
		} finally {
			setBusy(false)
		}
	}

	return (
		<>
			{error &&
				<div className="mt-3">
					<ErrorBanner message={error} />
				</div>
			}
			
			<form onSubmit={handleSubmit}>
				<section className="pt-2">
					<div className="grid gap-5 sm:grid-cols-2">
						<div>
							<label
								htmlFor="date"
								className={labelClass}
							>
								Date
							</label>
							<input
								type="date"
								id="date"
								required
								value={date}
								min={todayIso()}
								className={inputClass}
								onChange={(e) => setDate(e.target.value)}
							/>
						</div>
						<div>
							<label
								htmlFor="name"
								className={labelClass}
							>
								Nom
							</label>
							<input
								type="text"
								id="name"
								required
								value={name}
								className={inputClass}
								onChange={(e) => setName(e.target.value)}
							/>
						</div>
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
								{holiday === null ? "Créer" : "Enregistrer"}
							</span>
							<span className={`col-start-1 row-start-1 ${busy ? "" : "invisible"}`}>
								{holiday === null ? "Création…" : "Enregistrement…"}
							</span>
						</span>
					</button>
				</div>
			</form>
		</>
	)
}