import { useState } from "react"
import { Modal } from "../dialogs/Modal"
import { useAsync } from "../../hooks/useAsync"
import { ErrorBanner } from "../feedback/Banner"
import { dangerButton, secondaryButton, focusRing } from "../../ui/buttons"
import { formatLongDate } from "../../utils/date"
import { formatMinutes, meetingTimes } from "../../utils/meetings"
import { Avatar } from "../employees/Avatar"
import { useSession } from "../../session/useSession"
import { labelClass } from "../../ui/fields"
import { ColorPicker } from "./ColorPicker"
import { ConfirmDialog } from "../dialogs/ConfirmDialog"
import { MeetingDialog } from "./MeetingDialog"
import { CheckCircleOutlineOutlined, HighlightOff, HourglassEmptyOutlined, type SvgIconComponent } from "@mui/icons-material"
import {
	changeMeetingColor,
	deleteMeeting,
	getMeeting,
	MeetingKind,
	respondToMeeting,
	ParticipantResponse,
	type ParticipantResponseId,
	type MeetingColor,
	type MeetingPerson,
	type MeetingResponse
} from "../../api/meetings"

type Props = DetailProps & {
	meetingId: number | null
}

type DetailProps = {
	onClose: () => void
	onColorChanged: (id: number, color: MeetingColor) => void
	onResponded: (id: number, response: ParticipantResponseId) => void
	onCancelled: (id: number) => void
	onUpdated: (meeting: MeetingResponse) => void
}

const TAG = "rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700"

const RESPONSE_BUTTON = `inline-flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-sm font-medium transition disabled:cursor-not-allowed disabled:opacity-50 ${focusRing}`
const RESPONSE_IDLE = "border-slate-200 bg-white text-slate-700 hover:bg-slate-50"
const RESPONSE_ACCEPTED = "border-emerald-600 bg-emerald-50 text-emerald-800"
const RESPONSE_DECLINED = "border-red-600 bg-red-50 text-red-800"

const PARTICIPANT_RESPONSES: Record<ParticipantResponseId, { icon: SvgIconComponent, label: string; className: string }> = {
	[ParticipantResponse.EnAttente]: { icon: HourglassEmptyOutlined, label: "En attente", className: "text-slate-600" },
	[ParticipantResponse.Accepte]: { icon: CheckCircleOutlineOutlined, label: "Accepté", className: "text-emerald-600" },
	[ParticipantResponse.Decline]: { icon: HighlightOff, label: "Décliné", className: "text-red-600" },
}

export function MeetingDetailDialog({ meetingId, onClose, onColorChanged, onResponded, onCancelled, onUpdated }: Props) {
	// Garde le dernier événement affiché pendant le fondu de fermeture.
	const [shownId, setShownId] = useState(meetingId)
	if (meetingId !== null && meetingId !== shownId) setShownId(meetingId)
	
	return (
		<Modal open={meetingId !== null} title="Détails de l'événement" onClose={onClose}>
			{shownId !== null && <MeetingDetail key={shownId} id={shownId} onClose={onClose} onColorChanged={onColorChanged} onResponded={onResponded} onCancelled={onCancelled} onUpdated={onUpdated} />}
		</Modal>
	)
}

function PersonChip({ person, myId }: { person: MeetingPerson, myId: number | undefined }) {
	const Icon = PARTICIPANT_RESPONSES[person.response].icon
	const label = PARTICIPANT_RESPONSES[person.response].label
	const css = PARTICIPANT_RESPONSES[person.response].className
	return (
		<span className="flex items-center gap-1.5 rounded-full bg-slate-100 py-0.5 pr-2 pl-0.5 text-sm">
			<Avatar firstName={person.firstName} lastName={person.lastName} size="sm" />
			{person.employeeId === myId ? "Toi" : `${person.firstName} ${person.lastName}`}
			<span className={`${css} flex`}>
				<Icon fontSize="inherit" aria-hidden="true" />
				<p className="sr-only">{label}</p>
			</span>
		</span>
	)
}

export function MeetingDetail({ id, onClose, onColorChanged, onResponded, onCancelled, onUpdated }: { id: number } & DetailProps) {
	const [colorBusy, setColorBusy] = useState(false)
	const [colorError, setColorError] = useState<string | null>(null)
	const [confirmOpen, setConfirmOpen] = useState(false)
	const [cancelError, setCancelError] = useState<string | null>(null)
	const [responseBusy, setResponseBusy] = useState(false)
	const [responseError, setResponseError] = useState<string | null>(null)
	const [editOpen, setEditOpen] = useState(false)
	const [time] = useState(() => Date.now())

	const { me } = useSession()
	const { data, error, loading, retry, setData } = useAsync((signal) => getMeeting(id, signal), [id])

	if (loading) return <div className="mt-3"><p className="text-sm text-slate-500">Chargement…</p></div>
	if (error) return <div className="mt-3"><ErrorBanner message={error} onRetry={retry} /></div>
	if (!data) return null

	const { meeting, organizer, participants } = data
	const { date, start, end } = meetingTimes(meeting)
	const isParticipant = me?.employeeId === organizer.employeeId || participants.some(p => p.employeeId === me?.employeeId)
	const canModify = data.canEdit && Date.parse(meeting.startAt) > time

	// Un invité (pas l'organisateur) répond tant que le meeting n'a pas commencé.
	const canRespond = meeting.myResponse !== null && me?.employeeId !== organizer.employeeId && Date.parse(meeting.startAt) > time

	async function handleRespond(response: ParticipantResponseId) {
		if (response === meeting.myResponse) return
		setResponseBusy(true)
		setResponseError(null)
		try {
			await respondToMeeting(meeting.id, response)
			setData((d) => ({
				...d,
				meeting: { ...d.meeting, myResponse: response },
				participants: d.participants.map((p) => p.employeeId === me?.employeeId ? { ...p, response } : p),
			}))
			onResponded(meeting.id, response)
		} catch (e) {
			setResponseError(e instanceof Error ? e.message : "Impossible d'enregistrer ta réponse.")
		} finally {
			setResponseBusy(false)
		}
	}

	async function handleColor(color: MeetingColor) {
		setColorBusy(true)
		setColorError(null)
		try {
			await changeMeetingColor(meeting.id, color)
			setData((d) => ({ ...d, meeting: { ...d.meeting, color } }))
			onColorChanged(meeting.id, color)
		} catch (e) {
			setColorError(e instanceof Error ? e.message : "Impossible de changer la couleur.")
		} finally {
			setColorBusy(false)
		}
	}

	async function handleCancel()
	{
		setCancelError(null)
		try {
			await deleteMeeting(meeting.id)
			setConfirmOpen(false)
			onCancelled(meeting.id)
		} catch (e) {
			setCancelError(e instanceof Error ? e.message : "Impossible de supprimer l'événement.")
		}
	}

	return (
		<div className="mt-3 space-y-4">
			<div>
				<p className="text-lg font-semibold text-slate-900">{data.meeting.title}</p>
				<p className="text-sm text-slate-600 first-letter:uppercase">
					{formatLongDate(date)}, {formatMinutes(start)} – {formatMinutes(end)}
				</p>
			</div>

			{canRespond && (
				<div>
					<p className={labelClass}>Ta réponse</p>
					<div className="mt-1 flex gap-2">
						<button
							type="button"
							onClick={() => handleRespond(ParticipantResponse.Accepte)}
							disabled={responseBusy}
							aria-pressed={meeting.myResponse === ParticipantResponse.Accepte}
							className={`${RESPONSE_BUTTON} ${meeting.myResponse === ParticipantResponse.Accepte ? RESPONSE_ACCEPTED : RESPONSE_IDLE}`}
						>
							<CheckCircleOutlineOutlined fontSize="small" />
							Accepter
						</button>
						<button
							type="button"
							onClick={() => handleRespond(ParticipantResponse.Decline)}
							disabled={responseBusy}
							aria-pressed={meeting.myResponse === ParticipantResponse.Decline}
							className={`${RESPONSE_BUTTON} ${meeting.myResponse === ParticipantResponse.Decline ? RESPONSE_DECLINED : RESPONSE_IDLE}`}
						>
							<HighlightOff fontSize="small" />
							Décliner
						</button>
					</div>
					{responseError && <div className="mt-2"><ErrorBanner message={responseError} /></div>}
				</div>
			)}

			{(meeting.kind === MeetingKind.PersonalBlock || meeting.isTeamMeeting) && (
				<ul className="flex flex-wrap gap-1.5">
					{meeting.kind === MeetingKind.PersonalBlock && <li className={TAG}>Bloc personnel</li>}
					{meeting.isFlexible && <li className={TAG}>Souple</li>}
					{meeting.isTeamMeeting && <li className={TAG}>Meeting d'équipe</li>}
				</ul>
			)}

			{meeting.description && (
				<p className="whitespace-pre-line text-sm text-slate-700">{meeting.description}</p>
			)}

			<div>
				<p className={labelClass}>Organisé par</p>
				<div className="mt-1 flex">
					<PersonChip person={organizer} myId={me?.employeeId} />
				</div>
			</div>

			{participants.length > 0 && (
				<div>
					<p className={labelClass}>Participants</p>
					<ul className="mt-1 flex flex-wrap gap-2" aria-label="Participants">
						{participants.map((p) => (
							<li key={p.employeeId}>
								<PersonChip person={p} myId={me?.employeeId} />
							</li>
						))}
					</ul>
				</div>
			)}

			{isParticipant && (
				<div>
					<ColorPicker legend="Ta couleur" value={meeting.color} onChange={handleColor} disabled={colorBusy} />
					{colorError && <ErrorBanner message={colorError} />}
				</div>
			)}
			
			<div className="mt-6 flex justify-between">
				{canModify && (
					<div className="flex gap-1">
						<button type="button" className={secondaryButton} onClick={() => { setEditOpen(true) }}>
							Modifier
						</button>
						<button type="button" className={dangerButton} onClick={() => { setCancelError(null); setConfirmOpen(true) }}>
							Annuler l'événement
						</button>
					</div>
				)}
				{!canModify && <span/>}
				<button type="button" onClick={onClose} className={secondaryButton}>
					Fermer
				</button>
			</div>

			<ConfirmDialog
				open={confirmOpen}
				title="Annuler cet événement ?"
				message={meeting.kind === MeetingKind.PersonalBlock
					? "Ce bloc sera retiré de ton calendrier."
					: "Les participants recevront une notification."}
				confirmLabel="Annuler l'événement"
				busyLabel="Annulation…"
				destructive
				onConfirm={handleCancel}
				onClose={() => setConfirmOpen(false)}
			>
				{cancelError && (
					<div className="mt-4">
						<ErrorBanner message={cancelError} />
					</div>
				)}
			</ConfirmDialog>
			<MeetingDialog 
				open={editOpen} 
				initialDate={date} 
				onClose={() => setEditOpen(false)}
				onSaved={(result) => { onUpdated(result); setEditOpen(false)}}
				meeting={data}
			/>
		</div>
	)
}