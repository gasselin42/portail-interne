import { Navigate, Outlet } from "react-router-dom"
import { useSession } from "../session/useSession"
import { isAdmin } from "../session/permission"

export function RequireAdmin() {
	const { me } = useSession()

	// Si le user n'est pas un admin
	if (!isAdmin(me)) {
		return <Navigate to="/" replace />
	}

	// OK → affiche la route enfant
	return <Outlet />
}
