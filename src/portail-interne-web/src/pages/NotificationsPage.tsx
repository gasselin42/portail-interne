import { useState } from "react"
import { getNotifications, markAllNotificationsRead, markNotificationRead, type NotificationItem } from "../api/notifications"
import { PageHeader } from "../components/common/PageHeader"
import { ErrorBanner } from "../components/feedback/Banner"
import { useAsync } from "../hooks/useAsync"
import { NotificationEntry } from "../components/notifications/NotificationEntry"
import { secondaryButton } from "../ui/buttons"
import { useUnreadCount } from "../notifications/useUnreadCount"

export function NotificationsPage() {
	const { data, error, loading, retry, setData } = useAsync((signal) => getNotifications(20, undefined, signal), [])
	const unreadCount = useUnreadCount()
	const [now] = useState(() => Date.now())
	const [markingAll, setMarkingAll] = useState(false)
	const [errorMarkAllRead, setErrorMarkAllRead] = useState<string | null>(null)
	const [retrievingHasMore, setRetrievingHasMore] = useState(false)
	const [errorHasMore, setErrorHasMore] = useState<string | null>(null)

	function handleOpen(notification: NotificationItem) {
		if (notification.readAt !== null) return
		markNotificationRead(notification.id).catch(() => {})
		setData((n) => ({ ...n, items: n.items.map((i) => i.id === notification.id ? { ...i, readAt: new Date().toISOString() } : i) }))
		unreadCount.markOneRead()
	}

	async function handleMarkAllRead() {
		setErrorMarkAllRead(null)
		setMarkingAll(true)

		try {
			await markAllNotificationsRead()
			setData((n) => ({ ...n, items: n.items.map((i) => ({ ...i, readAt: new Date().toISOString() }) )}))
			unreadCount.markAllRead()
		} catch (e) {
			setErrorMarkAllRead(e instanceof Error ? e.message : "Impossible de marquer toutes les notifications comme lues.")
		} finally {
			setMarkingAll(false)
		}
	}

	async function handleHasMore() {
		setErrorHasMore(null)
		setRetrievingHasMore(true)

		try {
			const hasMoreData = await getNotifications(20, data?.items[data.items.length - 1].id)
			setData((n) => ({ hasMore: hasMoreData.hasMore, items: [...n.items, ...hasMoreData.items]}))
		} catch (e) {
			setErrorHasMore(e instanceof Error ? e.message : "Impossible de charger les notifications suivantes.")
		} finally {
			setRetrievingHasMore(false)
		}
	}
	
	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				eyebrow="Compte"
				title="Notifications"
				description="Voir tes notifications et les marquer comme lues"
				actions={
					<button type="button" className={secondaryButton} onClick={handleMarkAllRead} disabled={markingAll || !unreadCount.count}>
						Tout marquer comme lu
					</button>
				}
			/>

			{errorMarkAllRead && (
				<div className="my-3">
					<ErrorBanner message={errorMarkAllRead} />
				</div>
			)}

			<section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
				{error ? (
					<div className="p-4">
						<ErrorBanner message={error} onRetry={retry} />
					</div>
				) : loading ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des notifications…</p>
				) : data && data.items.length === 0 ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Aucune notification.</p>
				) : data && (
					<ul className="divide-y divide-slate-200">
						{data.items.map((n) => (
							<li key={n.id}>
								<NotificationEntry notification={n} now={now} onOpen={handleOpen} />
							</li>
						))}
					</ul>
				)}
			</section>

			{errorHasMore && (
				<div className="my-3">
					<ErrorBanner message={errorHasMore} />
				</div>
			)}

			{data?.hasMore && (
				<div className="pt-4 justify-center flex">
					<button
						type="button"
						onClick={handleHasMore}
						disabled={retrievingHasMore}
						className={secondaryButton}
					>
						<span className="grid">
							<span className={`col-start-1 row-start-1 ${retrievingHasMore ? "invisible" : ""}`}>
								Voir plus
							</span>
							<span className={`col-start-1 row-start-1 ${retrievingHasMore ? "" : "invisible"}`}>
								Chargement…
							</span>
						</span>
					</button>
				</div>
			)}
		</div>
	)
}