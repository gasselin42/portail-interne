import { Popover, PopoverButton, PopoverPanel } from "@headlessui/react"
import { NotificationsOutlined } from "@mui/icons-material"
import { iconButton, textLink } from "../../ui/buttons"
import { useUnreadCount } from "../../notifications/useUnreadCount"
import { UnreadBadge } from "./UnreadBadge"
import { NotificationList } from "./NotificationList"
import { Link } from "react-router-dom"

export function NotificationBell() {
	const unreadCount = useUnreadCount()

	return (
		<Popover className="hidden lg:block">
			<PopoverButton className={`relative ${iconButton}`} aria-label={`Notifications, ${unreadCount.count ?? 0} non lue${unreadCount.count === null || unreadCount.count === 0 ? "" : "s"}`}>
				<NotificationsOutlined fontSize="small" />
				<UnreadBadge />
			</PopoverButton>
			<PopoverPanel anchor={{ to: "bottom start", gap: 8 }} className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-lg shadow-slate-200/50 w-80 z-50">
				{({ close }) => (
					<>
						<NotificationList limit={5} onNavigate={close} />
						<Link to="/notifications" onClick={() => close()} className={`${textLink} block text-center py-2 border-t-2 border-slate-200`}>
							Voir toutes les notifications
						</Link>
					</>
				)}
			</PopoverPanel>
		</Popover>
	)
}