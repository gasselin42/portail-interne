import { Link } from "react-router-dom"
import { useUnreadCount } from "../../notifications/useUnreadCount"
import { NotificationsOutlined } from "@mui/icons-material"
import { UnreadBadge } from "./UnreadBadge"
import { iconButton } from "../../ui/buttons"

export function MobileNotificationLink() {
	const unreadCount = useUnreadCount()
	return (
		<Link to="/notifications" aria-label={`Notifications, ${unreadCount.count ?? 0} non lue${unreadCount.count === null || unreadCount.count === 0 ? "" : "s"}`} className={`relative ml-auto ${iconButton}`}>
			<NotificationsOutlined fontSize="small" />
			<UnreadBadge />
		</Link>
	)
}