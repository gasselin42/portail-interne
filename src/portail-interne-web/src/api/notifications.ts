import { apiFetch, readApiError } from "./client"

export const NotificationType = {
	MeetingInvitation: 0,
	MeetingUpdated: 1,
	MeetingCancelled: 2,
	MeetingConflict: 3,
	FlexibleBlockOverlapped: 4,
	AbsenceOverlapsMeeting: 5,
	AbsenceCancelled: 6,
	AbsenceUnplanned: 7,
	AbsenceReturnUnconfirmed: 8,
	LeaveOverlapsMeeting: 9,
	MeetingDeclined: 10,
	MeetingDeclinedWithdrawn: 11
} as const

export type NotificationTypeId = (typeof NotificationType)[keyof typeof NotificationType]

export type NotificationItem = {
	id: number
	type: NotificationTypeId
	message: string
	link: string | null
	createdAt: string
	readAt: string | null
}

export type NotificationList = {
	items: NotificationItem[]
	hasMore: boolean
}

export async function getNotifications(limit: number, beforeId?: number, signal?: AbortSignal): Promise<NotificationList> {
	const query = new URLSearchParams()

	query.set("limit", limit.toString())

	if (beforeId !== undefined)
		query.set("beforeId", beforeId.toString())

	const res = await apiFetch(`/api/notifications?${query}`, { signal })

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de charger les notifications"))
	}

	return (await res.json()) as NotificationList
}

export async function getUnreadCount(signal?: AbortSignal): Promise<number> {
	const res = await apiFetch("/api/notifications/unread-count", { signal })

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de compter le nombre de notifications non lues"))
	}

	return ((await res.json()) as { count: number }).count
}

export async function markNotificationRead(id: number): Promise<void> {
	const res = await apiFetch(`/api/notifications/${id}/read`, { method: "POST" })

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de marquer la notification comme lue"))
	}
}

export async function markAllNotificationsRead(): Promise<void> {
	const res = await apiFetch("/api/notifications/read-all", { method: "POST" })

	if (!res.ok) {
		throw new Error(await readApiError(res, "Impossible de marquer les notifications comme lues"))
	}
}