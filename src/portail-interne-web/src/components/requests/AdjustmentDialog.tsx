import { useState, type SubmitEvent } from "react"
import { Modal } from "../dialogs/Modal"
import { primaryButton, secondaryButton } from "../../ui/buttons"
import { inputClass, labelClass } from "../../ui/fields"
import { hoursError } from "../../utils/schedule"
import { createAdjustment } from "../../api/adjustments"
import { useSession } from "../../session/useSession"
import { ErrorBanner } from "../feedback/Banner"
import { formatTime, todayIso } from "../../utils/date"
import { useAsync } from "../../hooks/useAsync"
import { getCalendar } from "../../api/calendar"

type Props = {
	open: boolean
	onClose: () => void
	onCreated: () => void
}

export function AdjustmentDialog({ open, onClose, onCreated }: Props) {
	return (
		<Modal open={open} title="Demander un ajustement d'horaire" onClose={onClose}>
			<AdjustmentForm onClose={onClose} onCreated={onCreated} />
		</Modal>
	)
}

function AdjustmentForm({ onClose, onCreated }: { onClose: () => void, onCreated: () => void }) {
	const [date, setDate] = useState<string>("")
	const [startTime, setStartTime] = useState<string>("")
	const [endTime, setEndTime] = useState<string>("")
	const [breakMinutes, setBreakMinutes] = useState<number>(0)
	const [reason, setReason] = useState<string>("")
	const [error, setError] = useState<string | null>(null)
	const [busy, setBusy] = useState(false)

	const { me } = useSession()
	const today = todayIso()

	const planned = useAsync(
		(signal) => (date ? getCalendar(date, date, signal) : Promise.resolve(null)),
		[date],
	)
	const plannedDay = planned.data?.days[0]


	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setError(null)
		try {
			const message = hoursError(startTime, endTime, breakMinutes)

			if (message) {
				setError(message)
				return
			}

			setBusy(true)

			await createAdjustment(me!.employeeId, {
					date,
					startTime: `${startTime}:00`,
					endTime: `${endTime}:00`,
					breakMinutes,
					reason: reason.trim() || null
			})
			onCreated()
		} catch (e) {
			setError(e instanceof Error ? e.message : "Impossible d'envoyer la demande.")
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
					<div className="grid gap-5 sm:grid-cols-3">
						<div>
							<label
								htmlFor="date"
								className={labelClass}
							>
								Date
							</label>
							<input
								id="date"
								type="date"
								required
								data-autofocus
								min={today}
								value={date}
								onChange={(e) => setDate(e.target.value)}
								className={inputClass}
							/>
							{plannedDay && <p className="mt-1.5 text-xs text-slate-500">
								{plannedDay.startTime && plannedDay.endTime
									? `Horaire prévu : ${formatTime(plannedDay.startTime)} – ${formatTime(plannedDay.endTime)}, pause de ${plannedDay.breakMinutes} min`
									: "Jour non travaillé"
								}
							</p>}
						</div>
						<div>
							<label htmlFor="startTime" className={labelClass}>
								Début
							</label>
							<input
								id="startTime"
								type="time"
								value={startTime}
								onChange={(e) => setStartTime(e.target.value)}
								className={inputClass}
							/>
						</div>
						<div>
							<label htmlFor="endTime" className={labelClass}>
								Fin
							</label>
							<input
								id="endTime"
								type="time"
								value={endTime}
								onChange={(e) => setEndTime(e.target.value)}
								className={inputClass}
							/>
						</div>
					</div>
					<div className="mt-5 grid gap-5 sm:grid-cols-3">
						<div>
							<label htmlFor="breakMinutes" className={labelClass}>
								Minutes de pause
							</label>
							<input
								id="breakMinutes"
								type="number"
								required
								min={0}
								step={5}
								value={breakMinutes}
								onChange={(e) => setBreakMinutes(Number(e.target.value))}
								className={inputClass}
							/>
						</div>
						<div className="col-span-2">
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
								Envoyer la demande
							</span>
							<span className={`col-start-1 row-start-1 ${busy ? "" : "invisible"}`}>
								Envoi…
							</span>
						</span>
					</button>
				</div>
			</form>
		</>
	)
}