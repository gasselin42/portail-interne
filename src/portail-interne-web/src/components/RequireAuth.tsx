import { Navigate, Outlet } from "react-router-dom";
import { getToken, getMustChangePassword } from "../api/auth";

type Props = {
	/** Si true : autorise /change-password même avec le flag mdp */
	allowPasswordChange?: boolean
}

export function RequireAuth({ allowPasswordChange = false }: Props) {
	const token = getToken()

	// Pas connecté → login
	if (!token) {
		return <Navigate to="/login" replace />
	}

	// Doit changer son mdp, et ce n'est PAS la page change-password
	if (getMustChangePassword() && !allowPasswordChange) {
		return <Navigate to="/change-password" replace />
	}

	// OK → affiche la route enfant
	return <Outlet />
}