import { useContext } from "react"
import { SessionContext } from "./context"

export function useSession() {
	const value = useContext(SessionContext)
	if (!value) throw new Error("useSession doit être utilisé dans <SessionProvider>")
	return value
}
