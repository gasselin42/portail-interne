import { Navigate, Outlet } from "react-router-dom"
import { useSession } from "../session/useSession"
import { canApprove } from "../session/permission"

export function RequireApprover() {
	const { me } = useSession()

	// Si le user n'est ni un admin, ni un manager
	if (!canApprove(me)) {
		return <Navigate to="/" replace />
	}

	// OK → affiche la route enfant
	return <Outlet />
}
