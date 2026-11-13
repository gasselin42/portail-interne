import { Widget } from "./Widget"
import { NotificationList } from "../notifications/NotificationList"

export function NotificationsWidget() {
	return (
		<Widget title="Notifications" link={{ to: "/notifications", label: "Tout voir" }} className="overflow-hidden">
			<div className="-mx-4 -mb-4">
				<NotificationList limit={3} />
			</div>
		</Widget>
	)
}