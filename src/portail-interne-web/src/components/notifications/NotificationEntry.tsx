import {
	type SvgIconComponent,
	BeachAccessOutlined,
	DirectionsRunOutlined,
	EventAvailableOutlined,
	EventBusyOutlined,
	FreeCancellationOutlined,
	HowToRegOutlined,
	InsertInvitationOutlined,
	LayersOutlined,
	PersonOffOutlined,
	PersonSearchOutlined,
	UpdateOutlined,
	WarningAmberOutlined
} from "@mui/icons-material"
import { NotificationType, type NotificationItem, type NotificationTypeId } from "../../api/notifications"
import { formatLongDate, formatRelative, utcToZoned } from "../../utils/date"
import { formatMinutes } from "../../utils/meetings"
import { Link } from "react-router-dom"
import { focusRing } from "../../ui/buttons"

const NOTIFICATIONS_ICONS: Record<NotificationTypeId, SvgIconComponent> = {
	[NotificationType.MeetingInvitation]: InsertInvitationOutlined,
	[NotificationType.MeetingUpdated]: UpdateOutlined,
	[NotificationType.MeetingCancelled]: FreeCancellationOutlined,
	[NotificationType.MeetingConflict]: WarningAmberOutlined,
	[NotificationType.FlexibleBlockOverlapped]: LayersOutlined,
	[NotificationType.AbsenceOverlapsMeeting]: PersonOffOutlined,
	[NotificationType.AbsenceCancelled]: HowToRegOutlined,
	[NotificationType.AbsenceUnplanned]: DirectionsRunOutlined,
	[NotificationType.AbsenceReturnUnconfirmed]: PersonSearchOutlined,
	[NotificationType.LeaveOverlapsMeeting]: BeachAccessOutlined,
	[NotificationType.MeetingDeclined]: EventBusyOutlined,
	[NotificationType.MeetingDeclinedWithdrawn]: EventAvailableOutlined
}

type Props = {
	notification: NotificationItem
	now: number
	onOpen: (notification: NotificationItem) => void
}

export function NotificationEntry({ notification, now, onOpen }: Props) {
	const Icon = NOTIFICATIONS_ICONS[notification.type]
	const unread = notification.readAt === null
	const created = utcToZoned(notification.createdAt)
	
	const content = (
		<>
			<Icon aria-hidden="true" className={`shrink-0 self-center ${unread ? "text-slate-900" : "text-slate-400"}`} fontSize="small" />
			<div className="flex-1 min-w-0">
				<p className={`${unread ? "font-semibold text-slate-900" : "text-slate-600"} text-sm`}>
					{notification.message}
				</p>
				<p className="text-xs text-slate-500 mt-0.5" title={`${formatLongDate(created.date)} – ${formatMinutes(created.minutes)}`}>
					{formatRelative(notification.createdAt, now)}
				</p>
			</div>
			{unread && (
				<span className="self-center shrink-0 rounded-full size-2.5 bg-sky-700">
					<span className="sr-only">Non lue</span>
				</span>
			)}
		</>
	)

	const css = `flex gap-x-2 px-4 py-3 ${unread ? "bg-sky-50" : ""}`

	if (notification.link !== null) {
		return (
			<Link to={notification.link} onClick={() => onOpen(notification)} className={`${css} ${focusRing} hover:bg-sky-100`}>
				{content}
			</Link>
		)
	}

	if (unread) {
		return (
			<button onClick={() => onOpen(notification)} type="button" className={`${css} cursor-pointer w-full text-left ${focusRing} hover:bg-sky-100`}>
				{content}
			</button>
		)
	}

	return (
		<div className={css}>
			{content}
		</div>
	)
}