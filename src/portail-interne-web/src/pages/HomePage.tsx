import { Link } from "react-router-dom"
import { useSession } from "../session/useSession"

export function HomePage() {
	const { me } = useSession()

	return (
		<div className="mx-auto max-w-5xl px-6 py-10">
			<header className="mb-8">
				<p className="text-sm font-medium text-sky-700">Accueil</p>
				<h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">Bonjour, {me?.firstName}</h1>
				<p className="mt-1 text-sm text-slate-500">Voici ton portail</p>
			</header>
			<Link to="/leaves/new">Nouvelle demande de congé</Link>
		</div>
	)
}
