import { useState, type SubmitEvent } from "react"
import { Modal } from "../dialogs/Modal"
import { ErrorBanner } from "../feedback/Banner"
import { focusRing, primaryButton, secondaryButton } from "../../ui/buttons"
import { inputClass, labelClass } from "../../ui/fields"
import { SegmentedControl } from "../common/SegmentedControl"
import { zonedToUtc } from "../../utils/date"
import { earliestStart, endOptions, firstBookableDate, formatOptionTime, meetingTimes, startOptions } from "../../utils/meetings"
import { useSession } from "../../session/useSession"
import type { EmployeeOption } from "../../api/employees"
import { EmployeeCombobox } from "../employees/EmployeeCombobox"
import { Avatar } from "../employees/Avatar"
import { CloseOutlined } from "@mui/icons-material"
import { MeetingConflictPanel } from "./MeetingConflictPanel"
import { AvailabilityStrip } from "./AvailabilityStrip"
import { ColorPicker } from "./ColorPicker"
import { 
	type MeetingColor,
	MeetingKind,
	type MeetingKindId,
	type MeetingResponse,
	createMeeting,
	type CreateMeetingRequest,
	type MeetingConflictResponse,
	type MeetingDetailResponse,
	updateMeeting,
	type MeetingFormRequest
} from "../../api/meetings"

type Props = FormProps & {
	open: boolean
}

type FormProps = {
	initialDate: string
	onClose: () => void
	onSaved: (meeting: MeetingResponse) => void
	meeting?: MeetingDetailResponse
}

const KIND_OPTIONS: { value: MeetingKindId; label: string }[] = [
	{ value: MeetingKind.Meeting, label: "Meeting" },
	{ value: MeetingKind.PersonalBlock, label: "Bloc personnel" },
]

export function MeetingDialog({ open, initialDate, onClose, onSaved, meeting }: Props) {
	return (
		<Modal open={open} title={meeting !== undefined ? "Modifier l'événement" : "Nouvel événement"} onClose={onClose} size="xl">
			<MeetingForm initialDate={initialDate} onClose={onClose} onSaved={onSaved} meeting={meeting} />
		</Modal>
	)
}

function MeetingForm({ initialDate, onClose, onSaved, meeting }: FormProps) {
	const startDate = initialDate < firstBookableDate() ? firstBookableDate() : initialDate
	const startTime = Math.max(540, earliestStart(startDate))

	const times = meeting ? meetingTimes(meeting.meeting) : null

	const [kind, setKind] = useState<MeetingKindId>(meeting?.meeting.kind ?? MeetingKind.Meeting)
	const [title, setTitle] = useState(meeting?.meeting.title ?? "")
	const [description, setDescription] = useState(meeting?.meeting.description ?? "")
	const [date, setDate] = useState<string>(times?.date ?? startDate)
	const [start, setStart] = useState(times?.start ?? startTime)
	const [end, setEnd] = useState(times?.end ?? Math.min(startTime + 60, 1440))
	const [participants, setParticipants] = useState<EmployeeOption[]>(meeting?.participants.map((p) => ({ id: p.employeeId, firstName: p.firstName, lastName: p.lastName })) ?? [])
	const [isFlexible, setIsFlexible] = useState(meeting?.meeting.isFlexible ?? false)
	const [color, setColor] = useState<MeetingColor>(meeting?.meeting.color ?? "blue")
	const [error, setError] = useState<string | null>(null)
	const [busy, setBusy] = useState(false)

	const { me } = useSession()

	const [conflict, setConflict] = useState<{ response: MeetingConflictResponse; requestKey: string } | null>(null)

	// Les champs communs à la création et à la modification (sans la confirmation).
	const form: Omit<MeetingFormRequest, "confirmConflicts"> = {
		title: title.trim(),
		description: description.trim() || null,
		startAt: zonedToUtc(date, start),
		endAt: zonedToUtc(date, end),
		isFlexible: kind === MeetingKind.PersonalBlock && isFlexible,
		participantIds: kind === MeetingKind.PersonalBlock ? [] : participants.map(p => p.id),
	}

	// La requête de création, telle que le formulaire la décrit maintenant.
	const draft: Omit<CreateMeetingRequest, "confirmConflicts"> = { ...form, kind, color }

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

			const result = meeting
				? await updateMeeting(meeting.meeting.id, { ...form, confirmConflicts: pendingConflict !== null })
				: await createMeeting({
					...draft,
					confirmConflicts: pendingConflict !== null
				})

			if (result.status === "conflict")
				setConflict({response: result.conflict, requestKey: draftKey})
			else
				onSaved(result.meeting)
		} catch (e) {
			setError(e instanceof Error ? e.message : "Impossible d'enregistrer l'événement.")
		} finally {
			setBusy(false)
		}
	}

	// L'organisateur : celui de l'événement en modification, toi en création.
	const organizer: EmployeeOption | null = meeting
		? { id: meeting.organizer.employeeId, firstName: meeting.organizer.firstName, lastName: meeting.organizer.lastName }
		: me
			? { id: me.employeeId, firstName: me.firstName, lastName: me.lastName }
			: null

	function addParticipant(employee: EmployeeOption | null) {
		if (employee === null) return
		if (organizer?.id === employee.id || participants.some((p) => p.id === employee.id)) return
		setParticipants([...participants, employee])
	}

	function moveStart(newStart: number) {
		const duration = end - start
		setStart(newStart)
		setEnd(Math.min(newStart + duration, 1440))
	}

	const people: EmployeeOption[] = [...(organizer ? [organizer] : []), ...participants]

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
									disabled={meeting !== undefined}
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
					<div className={`mt-5 grid gap-5 items-start ${meeting === undefined ? "sm:grid-cols-2" : ""}`}>
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
						{meeting === undefined &&
							<div>
								<ColorPicker legend="Couleur" value={color} onChange={setColor} />
							</div>
						}
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
				{pendingConflict && <MeetingConflictPanel conflict={pendingConflict} myId={me?.employeeId} isEditing={meeting !== undefined} />}
				<div className="mt-6 flex justify-end gap-3">
					<button type="button" onClick={onClose} disabled={busy} className={secondaryButton}>
						Annuler
					</button>
					<button type="submit" disabled={busy} className={primaryButton}>
						<span className="grid">
							<span className={`col-start-1 row-start-1 ${busy ? "invisible" : ""}`}>
								{meeting
									? pendingConflict ? "Enregistrer quand même" : "Enregistrer"
									: pendingConflict ? "Créer quand même" : "Créer"
								}
							</span>
							<span className={`col-start-1 row-start-1 ${busy ? "" : "invisible"}`}>
								{meeting ? "Enregistrement…" : "Création…"}
							</span>
						</span>
					</button>
				</div>
			</form>
		</>
	)
}
