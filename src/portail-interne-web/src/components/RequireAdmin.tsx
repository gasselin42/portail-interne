import { Navigate, Outlet } from "react-router-dom";
import { useSession } from "../session/useSession";
import { Role } from "../api/admin";

export function RequireAdmin() {
	const { me } = useSession()

	// Si le user n'est pas un admin
	if (!me || me.role !== Role.Admin) {
		return <Navigate to="/" replace />
	}

	// OK → affiche la route enfant
	return <Outlet />
}