import { createContext } from "react"

export type UnreadCountValue = {
	count: number | null
	markOneRead: () => void
	markAllRead: () => void
}

export const UnreadCountContext = createContext<UnreadCountValue | null>(null)