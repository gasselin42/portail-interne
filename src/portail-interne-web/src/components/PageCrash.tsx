import { Link } from "react-router-dom"
import { primaryButton, secondaryButton } from "../ui/buttons"

export function PageCrash({ onRetry }: { onRetry: () => void}) {
	return (
		<div className="mx-auto max-w-md px-6 py-20 text-center">
			<p className="text-sm font-semibold text-sky-700">Erreur</p>
			<h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-900">Une erreur est survenue</h1>
			<p className="mt-3 text-sm text-slate-500">Cette page n'a pas pu s'afficher. Réessaie, ou reviens à l'accueil.</p>
			<div className="mt-8 flex flex-wrap justify-center gap-3">
				<button type="button" className={secondaryButton} onClick={onRetry}>Réessayer</button>
				<Link to="/" className={primaryButton}>Retour à l'accueil</Link>
			</div>
		</div>
	)
}