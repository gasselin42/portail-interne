import { useState } from "react"
import { getNotifications, markNotificationRead, type NotificationItem } from "../../api/notifications"
import { useAsync } from "../../hooks/useAsync"
import { ErrorBanner } from "../feedback/Banner"
import { NotificationEntry } from "./NotificationEntry"
import { useUnreadCount } from "../../notifications/useUnreadCount"

type Props = {
	limit: number
	onNavigate?: () => void,
}

export function NotificationList({limit, onNavigate}: Props) {
	const unreadCount = useUnreadCount()
	const { data, error, loading, retry, setData }  = useAsync((signal) => getNotifications(limit, undefined, signal), [])
	const [now] = useState(() => Date.now())

	function handleOpen(notification: NotificationItem) {
		onNavigate?.()
		if (notification.readAt !== null) return
		markNotificationRead(notification.id).catch(() => {})
		setData((n) => ({ ...n, items: n.items.map((i) => i.id === notification.id ? { ...i, readAt: new Date().toISOString() } : i) }))
		unreadCount.markOneRead()
	}

	return (
		<>
			{error ? (
				<div className="p-4">
					<ErrorBanner message={error} onRetry={retry} />
				</div>
			) : loading ? (
				<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des notifications…</p>
			) : data && data.items.length === 0 ? (
				<p className="px-6 py-12 text-center text-sm text-slate-500">Aucune notification.</p>
			) : data && (
				<>
					<ul className="divide-y divide-slate-200">
						{data.items.map((n) => (
							<li key={n.id}>
								<NotificationEntry notification={n} now={now} onOpen={() => handleOpen(n)} />
							</li>
						))}
					</ul>
				</>
			)}
		</>
	)
}