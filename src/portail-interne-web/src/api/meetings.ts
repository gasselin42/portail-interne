import { apiFetch, readApiError } from "./client"

export const MeetingKind = {
	Meeting: 0,
	PersonalBlock: 1
} as const

export type MeetingKindId = (typeof MeetingKind)[keyof typeof MeetingKind]

export const ParticipantResponse = {
	EnAttente: 0,
	Accepte: 1,
	Decline: 2
} as const

export type ParticipantResponseId = (typeof ParticipantResponse)[keyof typeof ParticipantResponse]

export const MEETING_COLORS = ["blue", "indigo", "cyan", "teal", "fuchsia", "pink", "orange", "stone"] as const

export type MeetingColor = (typeof MEETING_COLORS)[number]

export const OutsideHoursReason = {
	NotWorking: 0,
	Holiday: 1,
	OutsideHours: 2,
	Absence: 3
} as const

export type OutsideHoursReasonId = (typeof OutsideHoursReason)[keyof typeof OutsideHoursReason]

export type MeetingResponse = {
	id: number
	kind: MeetingKindId
	title: string
	description: string | null
	startAt: string
	endAt: string
	organizerId: number
	isTeamMeeting: boolean
	isFlexible: boolean
	color: MeetingColor
	myResponse: ParticipantResponseId | null
}

export type MeetingDetailResponse = {
	meeting: MeetingResponse
	organizer: MeetingPerson
	participants: MeetingPerson[]
	canEdit: boolean
}

export type MeetingFormRequest = {
	title: string
	description: string | null
	startAt: string
	endAt: string
	isFlexible: boolean
	participantIds: number[]
	confirmConflicts: boolean
}

export type CreateMeetingRequest = MeetingFormRequest & {
	kind: MeetingKindId
	color: MeetingColor | null
}

export type UpdateMeetingRequest = MeetingFormRequest

export type MeetingConflict = {
	employeeId: number
	firstName: string
	lastName: string
	withTeamMeeting: boolean
}

export type MeetingOutsideHours = {
	employeeId: number
	firstName: string
	lastName: string
	reason: OutsideHoursReasonId
}

export type MeetingConflictResponse = {
	message: string
	conflicts: MeetingConflict[]
	outsideHours: MeetingOutsideHours[]
}

export type MeetingPerson = {
	employeeId: number
	firstName: string
	lastName: string
	response: ParticipantResponseId
}

export type CreateMeetingResult =
	| { status: "created"; meeting: MeetingResponse }
	| { status: "conflict"; conflict: MeetingConflictResponse }

export type UpdateMeetingResult =
	| { status: "updated"; meeting: MeetingResponse }
	| { status: "conflict"; conflict: MeetingConflictResponse }

export async function createMeeting(request: CreateMeetingRequest): Promise<CreateMeetingResult> {
	const res = await apiFetch("/api/meetings", {
		method: "POST",
		body: JSON.stringify(request)
	})
	
	if (res.status === 409) {
		return { status: "conflict", conflict: (await res.json()) as MeetingConflictResponse }
	}
	else if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de créer l'événement"))
	}

	return { status: "created", meeting: (await res.json()) as MeetingResponse }
}

export async function getMeeting(id: number, signal?: AbortSignal): Promise<MeetingDetailResponse> {
	const res = await apiFetch(`/api/meetings/${id}`, { signal })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger l'événement"))
	}
	return (await res.json()) as MeetingDetailResponse
}

export async function changeMeetingColor(id: number, color: MeetingColor): Promise<void> {
	const res = await apiFetch(`/api/meetings/${id}/color`, {
		method: "PUT",
		body: JSON.stringify({ color })
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de changer la couleur"))
	}
}

export async function deleteMeeting(id: number): Promise<void> {
	const res = await apiFetch(`/api/meetings/${id}`, { method: "DELETE" })
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de supprimer l'événement"))
	}
}

export async function updateMeeting(id: number, request: UpdateMeetingRequest): Promise<UpdateMeetingResult> {
	const res = await apiFetch(`/api/meetings/${id}`, {
		method: "PUT",
		body: JSON.stringify(request)
	})
	
	if (res.status === 409) {
		return { status: "conflict", conflict: (await res.json()) as MeetingConflictResponse }
	}
	else if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de modifier l'événement"))
	}

	return { status: "updated", meeting: (await res.json()) as MeetingResponse }
}

export async function respondToMeeting(id: number, response: ParticipantResponseId): Promise<void> {
	const res = await apiFetch(`/api/meetings/${id}/response`, {
		method: "PUT",
		body: JSON.stringify({ response })
	})
	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible d'enregistrer ta réponse"))
	}
}
