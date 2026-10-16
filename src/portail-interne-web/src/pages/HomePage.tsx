import { Link } from "react-router-dom"
import { useSession } from "../session/useSession"
import { PageHeader } from "../components/common/PageHeader"
import { primaryButton } from "../ui/buttons"
import { canApprove } from "../session/permission"
import { UpcomingLeavesWidget } from "../components/home/UpcomingLeavesWidget"
import { PendingApprovalsWidget } from "../components/home/PendingApprovalsWidget"
import { WeekWidget } from "../components/home/WeekWidget"

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

			<div className="grid gap-4 md:grid-cols-2">
				<WeekWidget />
				<UpcomingLeavesWidget />
				{canApprove(me) && (
					<PendingApprovalsWidget />
				)}
			</div>
		</div>
	)
}
