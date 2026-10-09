import { Link } from "react-router-dom"
import { useSession } from "../session/useSession"
import { PageHeader } from "../components/PageHeader"
import { primaryButton } from "../ui/buttons"

export function HomePage() {
	const { me } = useSession()

	return (
		<div className="mx-auto max-w-5xl px-6 py-10">
			<PageHeader
				eyebrow="Accueil"
				title={`Bonjour, ${me?.firstName}`}
				description="Voici ton portail."
				actions={
					<Link to="/leaves/new" className={primaryButton}>
						Nouvelle demande de congé
					</Link>
				}
			/>
			
		</div>
	)
}
