import { useState, type SubmitEvent } from "react"
import { useParams, Link } from "react-router-dom"
import { getEmployee } from "../api/employees"
import { useAsync } from "../hooks/useAsync"
import { ErrorBanner, SuccessBanner } from "../components/feedback/Banner"
import { PageHeader } from "../components/common/PageHeader"
import { createSchedule, deleteSchedule, listSchedules, updateSchedule, type WorkScheduleResponse } from "../api/schedules"
import { ScheduleEditor } from "../components/schedule/ScheduleEditor"
import { currentSchedule, rowError, toDays, toDraft } from "../utils/schedule"
import { addDays, formatDate, startOfWeek, todayIso } from "../utils/date"
import { inputClass, labelClass } from "../ui/fields"
import { dangerButton, primaryButton } from "../ui/buttons"
import { ConfirmDialog } from "../components/dialogs/ConfirmDialog"
import { ScheduleHistory } from "../components/schedule/ScheduleHistory"

export function EmployeeSchedulePage() {
	const [success, setSuccess] = useState<string | null>(null) 

	const { id } = useParams()

	const employeeId = Number(id)
	const idInvalide = !Number.isInteger(employeeId) || employeeId <= 0

	const { data, error, loading, retry } = useAsync(async (signal) => {
		if (idInvalide) throw new Error("Employé introuvable")
		const employee = await getEmployee(employeeId, signal)
		// Sans droit de gestion, l'API refuserait les horaires : on ne les demande même pas
		const schedules = employee.canManage ? await listSchedules(employeeId, signal) : []
		return { employee, schedules }
	}, [employeeId])
	const employee = data?.employee

	return (
		<div className="mx-auto max-w-2xl px-6 py-10">
			<Link to={`/employees/${employeeId}`} className="text-sm font-medium text-sky-700 hover:underline">
				Retour à la fiche
			</Link>

			{success && (
				<div className="mt-6">
					<SuccessBanner message={success} dismissible onDismiss={() => setSuccess(null)} />
				</div>
			)}

			{loading ? (
				<p className="mt-8 text-sm text-slate-500">Chargement...</p>
			) : error ? (
				<div className="mt-6">
					<ErrorBanner message={error} />
				</div>
			) : !employee?.canManage ? (
				<div className="mt-6">
					<ErrorBanner message="Tu n'as pas accès à cet horaire." />
				</div>
			) : (
				<div className="mt-6">
					<PageHeader eyebrow="Horaire" title={`${employee.firstName} ${employee.lastName}`} />
					<ScheduleForm employeeId={employee.id} schedules={data!.schedules} onSaved={(message) => { setSuccess(message); retry() }} />
					<div className="mt-10">
						<ScheduleHistory schedules={data!.schedules} excludeId={data!.schedules.find((s) => s.isEditable)?.id ?? null} />
					</div>
				</div>
			)}
		</div>
	)
}

type Props = {
	employeeId: number
	schedules: WorkScheduleResponse[]
	onSaved: (message: string) => void
}

function ScheduleForm({ employeeId, schedules, onSaved }: Props) {
	const upcoming = schedules.find((s) => s.isEditable) ?? null
	
	const [draft, setDraft] = useState(() => toDraft(upcoming ?? currentSchedule(schedules, todayIso())))
	const [effectiveFrom, setEffectiveFrom] = useState(upcoming?.effectiveFrom ?? addDays(startOfWeek(todayIso()), 7))
	const [error, setError] = useState<string | null>(null)
	const [busy, setBusy] = useState(false)
	const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)

	const saveBlocker =
		!effectiveFrom ? "Choisis une date."
		: !draft.some((row) => row.worked) ? "Coche au moins un jour."
		: draft.some((row) => rowError(row) !== null) ? "Corrige les erreurs de la grille."
		: null
	const canSave = saveBlocker === null

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setError(null)
		setBusy(true)
		const request = { effectiveFrom, days: toDays(draft) }
		try {
			if (upcoming) {
				await updateSchedule(employeeId, upcoming.id, request)
				onSaved("Horaire modifié.")
			}
			else {
				await createSchedule(employeeId, request)
				onSaved("Horaire créé.")
			}
		} catch (e) {
			setError(e instanceof Error ? e.message : "Impossible d'enregistrer l'horaire.")
		} finally {
			setBusy(false)
		}
	}

	async function handleDelete() {
		try {
			await deleteSchedule(employeeId, upcoming!.id)
			onSaved("Horaire supprimé.")

		} catch (e) {
			setConfirmDeleteOpen(false)
			setError(e instanceof Error ? e.message : "Impossible de supprimer l'horaire")
		}
	}
		
	return (
		<form onSubmit={handleSubmit}>
			{error && <ErrorBanner message={error} dismissible onDismiss={() => setError(null)} />}
			<div className="max-w-xs">
				<label htmlFor="effectiveFrom" className={labelClass}>
					En vigueur à partir du
				</label>
				<input
					id="effectiveFrom"
					type="date"
					value={effectiveFrom}
					min={todayIso()}
					onChange={(e) => setEffectiveFrom(e.target.value)}
					aria-describedby="effectiveFrom-help"
					className={inputClass}
					required
				/>
			</div>
			<p id="effectiveFrom-help" className="mt-1 text-xs text-slate-500">
				{!upcoming
					? "Remplacera l'horaire actuel à partir de cette date."
					: upcoming.effectiveFrom === todayIso()
						? "Cet horaire est en vigueur depuis aujourd'hui : tu peux encore le modifier jusqu'à ce soir."
						: "Cet horaire n'est pas encore en vigueur : tu peux encore le modifier."}
			</p>
			<div className="mt-6">
				<ScheduleEditor value={draft} onChange={setDraft} />
			</div>
			<div className="mt-6 flex flex-wrap items-center justify-end gap-3">
				{upcoming && (
					<button type="button" onClick={() => setConfirmDeleteOpen(true)} className={`${dangerButton} mr-auto`}>
						Supprimer cet horaire
					</button>
				)}
				{saveBlocker && <p className="text-sm text-slate-500">{saveBlocker}</p>}
				<button type="submit" disabled={busy || !canSave} className={primaryButton}>
					{upcoming ? "Enregistrer les modifications" : "Créer l'horaire"}
				</button>
			</div>

			{upcoming && <ConfirmDialog
				title="Supprimer cet horaire"
				message={`L'horaire prévu à partir du ${formatDate(upcoming.effectiveFrom)} sera supprimé. L'horaire actuel continuera de s'appliquer.`}
				confirmLabel="Supprimer"
				open={confirmDeleteOpen}
				onConfirm={handleDelete}
				onClose={() => setConfirmDeleteOpen(false)}
				destructive
			/>}
		</form>
	)
}
