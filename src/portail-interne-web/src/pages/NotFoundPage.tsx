import { Link, useLocation, useNavigate } from "react-router-dom"
import { primaryButton, secondaryButton } from "../ui/buttons"

export function NotFoundPage() {
	const navigate = useNavigate()
	const { key } = useLocation()

	return (
		<div className="mx-auto max-w-md px-6 py-20 text-center">
			<p className="text-sm font-semibold text-sky-700">404</p>
			<h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-900">Page introuvable</h1>
			<p className="mt-3 text-sm text-slate-500">Cette page n'existe pas ou a été déplacée.</p>
			<div className="mt-8 flex flex-wrap justify-center gap-3">
				{key !== "default" && <button type="button" className={secondaryButton} onClick={() => navigate(-1)}>Page précédente</button>}
				<Link to="/" className={primaryButton}>Retour à l'accueil</Link>
			</div>
		</div>
	)
}