import { AuthCard } from "./AuthCard"
import { primaryButton } from "../ui/buttons"

export function AppCrash() {
	return (
		<AuthCard
			title="Une erreur est survenue"
			description="Le portail a rencontré un problème. Recharge la page pour continuer."
			centered
			role="alert"
		>
			<button
				type="button"
				onClick={() => window.location.reload()}
				className={`${primaryButton} w-full`}
			>
				Recharger la page
			</button>
		</AuthCard>
	)
}