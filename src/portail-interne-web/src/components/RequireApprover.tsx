import { Navigate, Outlet } from "react-router-dom";
import { getToken, getMustChangePassword, canApprove } from "../api/auth";

export function RequireApprover() {
	const token = getToken()

	// Pas connecté → login
	if (!token) {
		return <Navigate to="/login" replace />
	}

	// Doit changer son mdp
	if (getMustChangePassword()) {
		return <Navigate to="/change-password" replace />
	}

	// Si le user n'est ni un admin, ni un manager
	if (!canApprove()) {
		return <Navigate to="/" replace />
	}

	// OK → affiche la route enfant
	return <Outlet />
}