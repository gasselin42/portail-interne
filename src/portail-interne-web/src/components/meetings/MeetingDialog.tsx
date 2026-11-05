import { useState, type SubmitEvent } from "react"
import { type MeetingColor, MeetingKind, type MeetingKindId, type MeetingResponse, MEETING_COLORS, createMeeting, type CreateMeetingRequest, type MeetingConflictResponse } from "../../api/meetings"
import { Modal } from "../dialogs/Modal"
import { ErrorBanner } from "../feedback/Banner"
import { focusRing, primaryButton, secondaryButton } from "../../ui/buttons"
import { inputClass, labelClass } from "../../ui/fields"
import { SegmentedControl } from "../common/SegmentedControl"
import { zonedToUtc } from "../../utils/date"
import { earliestStart, endOptions, firstBookableDate, formatOptionTime, startOptions } from "../../utils/meetings"
import { useSession } from "../../session/useSession"
import type { EmployeeOption } from "../../api/employees"
import { EmployeeCombobox } from "../employees/EmployeeCombobox"
import { Avatar } from "../employees/Avatar"
import { CloseOutlined } from "@mui/icons-material"
import { MEETING_COLOR_STYLES } from "../../ui/meetingColors"
import { MeetingConflictPanel } from "./MeetingConflictPanel"
import { AvailabilityStrip } from "./AvailabilityStrip"

type Props = FormProps & {
	open: boolean
}

type FormProps = {
	initialDate: string
	onClose: () => void
	onCreated: (meeting: MeetingResponse) => void
}

const KIND_OPTIONS: { value: MeetingKindId; label: string }[] = [
	{ value: MeetingKind.Meeting, label: "Meeting" },
	{ value: MeetingKind.PersonalBlock, label: "Bloc personnel" },
]

export function MeetingDialog({ open, initialDate, onClose, onCreated }: Props) {
	return (
		<Modal open={open} title="Nouvel événement" onClose={onClose} size="xl">
			<MeetingForm initialDate={initialDate} onClose={onClose} onCreated={onCreated} />
		</Modal>
	)
}

function MeetingForm({ initialDate, onClose, onCreated }: FormProps) {
	const startDate = initialDate < firstBookableDate() ? firstBookableDate() : initialDate
	const startTime = Math.max(540, earliestStart(startDate))

	const [kind, setKind] = useState<MeetingKindId>(MeetingKind.Meeting)
	const [title, setTitle] = useState("")
	const [description, setDescription] = useState("")
	const [date, setDate] = useState<string>(startDate)
	const [start, setStart] = useState(startTime)
	const [end, setEnd] = useState(Math.min(startTime + 60, 1440))
	const [participants, setParticipants] = useState<EmployeeOption[]>([])
	const [isFlexible, setIsFlexible] = useState(false)
	const [color, setColor] = useState<MeetingColor>("blue")
	const [error, setError] = useState<string | null>(null)
	const [busy, setBusy] = useState(false)

	const { me } = useSession()

	const [conflict, setConflict] = useState<{ response: MeetingConflictResponse; requestKey: string } | null>(null)

	// La requête telle que le formulaire la décrit maintenant (sans la confirmation).
	const draft: Omit<CreateMeetingRequest, "confirmConflicts"> = {
		kind,
		title: title.trim(),
		description: description.trim() || null,
		startAt: zonedToUtc(date, start),
		endAt: zonedToUtc(date, end),
		isFlexible: kind === MeetingKind.PersonalBlock && isFlexible,
		participantIds: kind === MeetingKind.PersonalBlock ? [] : participants.map(p => p.id),
		color
	}
	const draftKey = JSON.stringify(draft)

	// L'avertissement ne vaut que pour la requête exacte qui l'a reçu.
	const pendingConflict = conflict?.requestKey === draftKey ? conflict.response : null

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setError(null)

		try {
			if (draft.title === "")
			{
				setError("Le titre est obligatoire.")
				return
			}
			
			setBusy(true)

			const result = await createMeeting({
				...draft,
				confirmConflicts: pendingConflict !== null
			})

			if (result.status === "created")
				onCreated(result.meeting)
			else
				setConflict({response: result.conflict, requestKey: draftKey})
		} catch (e) {
			setError(e instanceof Error ? e.message : "Impossible de créer l'événement.")
		} finally {
			setBusy(false)
		}
	}

	function addParticipant(employee: EmployeeOption | null) {
		if (employee === null) return
		if (me?.employeeId === employee.id || participants.some((p) => p.id === employee.id)) return
		setParticipants([...participants, employee])
	}

	function moveStart(newStart: number) {
		const duration = end - start
		setStart(newStart)
		setEnd(Math.min(newStart + duration, 1440))
	}

	const meTab: EmployeeOption[] = me !== null ? [{ id: me?.employeeId, firstName: me?.firstName, lastName: me?.lastName}] : []
	const people: EmployeeOption[] = [...meTab, ...participants]

	return (
		<>
			{error && (
				<div className="mt-3">
					<ErrorBanner message={error} />
				</div>
			)}

			<form onSubmit={handleSubmit}>
				<section className="pt-2">
					<div className="grid gap-5 sm:grid-cols-3">
						<div className="flex flex-col">
							<span className={labelClass} aria-hidden="true">
								Type
							</span>
							<div className="flex flex-1 items-center">
								<SegmentedControl
									label="Type"
									options={KIND_OPTIONS}
									value={kind}
									onChange={(k) => setKind(k)}
								/>
							</div>
						</div>
						<div>
							<label htmlFor="title" className={labelClass}>
								Titre
							</label>
							<input
								id="title"
								type="text"
								required
								data-autofocus
								value={title}
								onChange={(e) => setTitle(e.target.value)}
								className={inputClass}
							/>
						</div>
						<div>
							<label htmlFor="description" className={labelClass}>
								Description (facultative)
							</label>
							<input
								id="description"
								type="text"
								value={description}
								onChange={(e) => setDescription(e.target.value)}
								className={inputClass}
							/>
						</div>
					</div>
					<div className="mt-5 grid gap-5 sm:grid-cols-3">
						<div>
							<label htmlFor="date" className={labelClass}>
								Date
							</label>
							<input
								id="date"
								type="date"
								required
								min={firstBookableDate()}
								value={date}
								onChange={(e) => {
									const newDate = e.target.value
									if (newDate === "") return
									setDate(newDate)
									if (start < earliestStart(newDate))
										moveStart(earliestStart(newDate))
								}}
								className={inputClass}
							/>
						</div>
						<div>
							<label htmlFor="start" className={labelClass}>
								Début
							</label>
							<select
								id="start"
								required
								value={start}
								onChange={(e) => moveStart(Number(e.target.value))}
								className={inputClass}
							>
								{startOptions().filter(o => o >= earliestStart(date)).map((m) => (
									<option key={m} value={m}>
										{formatOptionTime(m)}
									</option>
								))}
							</select>
						</div>
						<div>
							<label htmlFor="end" className={labelClass}>
								Fin
							</label>
							<select
								id="end"
								required
								value={end}
								onChange={(e) => setEnd(Number(e.target.value))}
								className={inputClass}
							>
								{endOptions(start).map((m) => (
									<option key={m.label} value={m.value}>
										{m.label}
									</option>
								))}
							</select>
						</div>
					</div>
					<div className="mt-5 grid gap-5 sm:grid-cols-2 items-start">
						<div>
							{kind === MeetingKind.Meeting && (
								<>
									<label htmlFor="participants" className={labelClass}>
										Participants
									</label>
									<EmployeeCombobox
										id="participants"
										value={null}
										onChange={addParticipant}
										placeholder="Ajouter un participant…"
									/>
									{participants.length > 0 && (
										<ul className="mt-2 flex flex-wrap gap-2" aria-label="Participants">
											{participants.map((p) => (
												<li
													key={p.id}
													className="flex items-center gap-1.5 rounded-full bg-slate-100 py-0.5 pr-1 pl-0.5 text-sm"
												>
													<Avatar firstName={p.firstName} lastName={p.lastName} size="sm" />
													<span>
														{p.firstName} {p.lastName}
													</span>
													<button
														type="button"
														aria-label={`Retirer ${p.firstName} ${p.lastName}`}
														onClick={() =>
															setParticipants(participants.filter((f) => p.id !== f.id))
														}
														className={`rounded-full p-0.5 text-slate-500 hover:bg-slate-200 hover:text-slate-700 ${focusRing}`}
													>
														<CloseOutlined fontSize="small" />
													</button>
												</li>
											))}
										</ul>
									)}
								</>
							)}
							{kind === MeetingKind.PersonalBlock && (
								<>
									<span className={labelClass}>Souplesse</span>
									<label className="flex items-center gap-2 font-normal text-slate-900">
										<input
											type="checkbox"
											checked={isFlexible}
											onChange={(e) => setIsFlexible(e.target.checked)}
											className="size-4 accent-sky-600"
											aria-describedby="flexible-help"
										/>
										Bloc souple
									</label>
									<p id="flexible-help" className="mt-1 text-xs text-slate-500">
										Un meeting d'équipe pourra chevaucher ce bloc sans conflit ; tu recevras simplement une notification.
									</p>
								</>
							)}
						</div>
						<div>
							<fieldset className="flex flex-1">
								<legend className={labelClass}>Couleur</legend>
								<div className="flex flex-1 flex-wrap gap-2 justify-center mt-1">
									{MEETING_COLORS.map((c) => (
										<label key={c} title={MEETING_COLOR_STYLES[c].label}>
										<input
											type="radio"
											name="color"
											value={c}
											checked={color === c}
											onChange={() => setColor(c)}
											aria-label={MEETING_COLOR_STYLES[c].label}
											className="peer sr-only"
										/>
										<span className={`block size-8 rounded-full ${MEETING_COLOR_STYLES[c].swatch} ring-offset-2 peer-checked:ring-2 peer-checked:ring-slate-900 peer-focus-visible:ring-2 peer-focus-visible:ring-sky-400`} />
									</label>
									))}
								</div>
							</fieldset>
						</div>
					</div>
				</section>
				<div className="mt-5">
					<AvailabilityStrip
						date={date}
						people={people}
						start={start}
						end={end}
						onPick={moveStart}
					/>
				</div>
				{pendingConflict && <MeetingConflictPanel conflict={pendingConflict} myId={me?.employeeId} />}
				<div className="mt-6 flex justify-end gap-3">
					<button type="button" onClick={onClose} disabled={busy} className={secondaryButton}>
						Annuler
					</button>
					<button type="submit" disabled={busy} className={primaryButton}>
						<span className="grid">
							<span className={`col-start-1 row-start-1 ${busy ? "invisible" : ""}`}>
								{pendingConflict ? "Créer quand même" : "Créer"}
							</span>
							<span className={`col-start-1 row-start-1 ${busy ? "" : "invisible"}`}>
								Création…
							</span>
						</span>
					</button>
				</div>
			</form>
		</>
	)
}
