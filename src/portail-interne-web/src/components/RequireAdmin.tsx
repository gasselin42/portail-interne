import { Navigate, Outlet } from "react-router-dom";
import { getToken, getMustChangePassword, isAdmin } from "../api/auth";

export function RequireAdmin() {
	const token = getToken()

	// Pas connecté → login
	if (!token) {
		return <Navigate to="/login" replace />
	}

	// Doit changer son mdp
	if (getMustChangePassword()) {
		return <Navigate to="/change-password" replace />
	}

	// Si le user n'est pas un admin
	if (!isAdmin()) {
		return <Navigate to="/" replace />
	}

	// OK → affiche la route enfant
	return <Outlet />
}