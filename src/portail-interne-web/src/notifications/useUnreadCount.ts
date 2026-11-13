import { useContext } from "react"
import { UnreadCountContext } from "./context"

export function useUnreadCount() {
	const value = useContext(UnreadCountContext)
	if (!value) throw new Error("useUnreadCount doit être utilisé dans <UnreadCountProvider>")
	return value
}