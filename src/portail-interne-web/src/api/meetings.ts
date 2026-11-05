import { apiFetch, readApiError } from "./client"

export const MeetingKind = {
	Meeting: 0,
	PersonalBlock: 1
} as const

export type MeetingKindId = (typeof MeetingKind)[keyof typeof MeetingKind]

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
}

export type CreateMeetingRequest = {
	kind: MeetingKindId
	title: string
	description: string | null
	startAt: string
	endAt: string
	isFlexible: boolean
	participantIds: number[]
	color: MeetingColor | null
	confirmConflicts: boolean
}

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

export type CreateMeetingResult =
	| { status: "created"; meeting: MeetingResponse }
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
