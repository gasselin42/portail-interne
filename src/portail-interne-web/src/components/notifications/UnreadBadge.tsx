import { useUnreadCount } from "../../notifications/useUnreadCount"

export function UnreadBadge() {
	const unreadCount = useUnreadCount()

	return unreadCount.count !== null && unreadCount.count > 0 ? (
		<span className="absolute rounded-full min-w-4 h-4 px-1 flex items-center justify-center leading-none bg-red-700 text-white font-semibold text-[10px] -top-0.5 -right-0.5 ring-1 ring-white">
			{unreadCount.count <= 9 ? unreadCount.count : "9+"}
		</span>
	) : null
}