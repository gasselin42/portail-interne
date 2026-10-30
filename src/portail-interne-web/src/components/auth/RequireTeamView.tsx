import { Navigate, Outlet } from "react-router-dom"
import { useSession } from "../../session/useSession"
import { canSeeTeam } from "../../session/permission"

export function RequireTeamView() {
	const { me } = useSession()

	// Si le user n'a pas accès à la vue équipe
	if (!canSeeTeam(me)) {
		return <Navigate to="/" replace />
	}

	// OK → affiche la route enfant
	return <Outlet />
}
