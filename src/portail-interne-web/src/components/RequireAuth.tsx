import { Navigate, Outlet } from "react-router-dom";
import { useSession } from "../session/useSession";
import { FullPageStatus } from "./FullPageStatus";

type Props = {
	/** Si true : autorise /change-password même avec le flag mdp */
	allowPasswordChange?: boolean
}

export function RequireAuth({ allowPasswordChange = false }: Props) {
	const { me, loading, error, refresh} = useSession()

	if (loading)
		return <FullPageStatus loading title="Chargement..." />

	if (error && !me) {
		return <FullPageStatus 
					title="Impossible de joindre le serveur"
					message={error}
					action={{ label: "Réessayer", onClick: refresh, busyLabel: "Connexion..." }}
				/>
	}
	
	// Pas connecté → login
	if (!me) {
		return <Navigate to="/login" replace />
	}

	// Doit changer son mdp, et ce n'est PAS la page change-password
	if (me.mustChangePassword && !allowPasswordChange) {
		return <Navigate to="/change-password" replace />
	}

	// OK → affiche la route enfant
	return <Outlet />
}