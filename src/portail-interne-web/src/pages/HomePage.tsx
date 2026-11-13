import { Link } from "react-router-dom"
import { useSession } from "../session/useSession"
import { PageHeader } from "../components/common/PageHeader"
import { primaryButton, secondaryButton } from "../ui/buttons"
import { canApprove } from "../session/permission"
import { UpcomingWidget } from "../components/home/UpcomingWidget"
import { PendingApprovalsWidget } from "../components/home/PendingApprovalsWidget"
import { WeekWidget } from "../components/home/WeekWidget"
import { NotificationsWidget } from "../components/home/NotificationsWidget"

export function HomePage() {
	const { me } = useSession()

	return (
		<div className="mx-auto max-w-5xl px-6 py-10">
			<PageHeader
				eyebrow="Accueil"
				title={`Bonjour, ${me?.firstName}`}
				description="Voici ton portail."
				actions={
					<>
						<Link to="/calendar?nouvel-evenement=1" className={secondaryButton}>
							Nouvel événement
						</Link>
						<Link to="/leaves?nouvelle=1" className={primaryButton}>
							Nouvelle demande de congé
						</Link>
					</>
				}
			/>

			<div className="grid gap-4 md:grid-cols-2">
				<WeekWidget />
				<UpcomingWidget />
				<NotificationsWidget />
				{canApprove(me) && (
					<PendingApprovalsWidget />
				)}
			</div>
		</div>
	)
}
