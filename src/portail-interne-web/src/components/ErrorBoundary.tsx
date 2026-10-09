import { Component, type ErrorInfo, type ReactNode } from "react"

type Props = {
	children: ReactNode
	/** Ce qu'on affifche en cas de plantage. Reçoit une fonction pour réessayer */
	fallback: (reset: () => void) => ReactNode
}

type State = {
	error: Error | null
}

							// function X({}: Props)
export class ErrorBoundary extends Component<Props, State> {
	state: State = { error: null } // useState(null)

	// Appelé automatiquement quand un children plante
	static getDerivedStateFromError(error: Error): State {
		return { error }
	}

	// Appelé juste après getDerivedStateFromError pour journaliser
	componentDidCatch(error: Error, info: ErrorInfo) {
		console.error("Erreur d'affichage :", error, info.componentStack)
	}

	// setError(null), efface l'erreur, et React réessaie d'afficher les enfants
	reset = () => {
		this.setState({ error: null })
	}

	// le return de la function, si erreur, on affiche fallback, sinon les enfants normalement
	render() {
		if (this.state.error) return this.props.fallback(this.reset)
		return this.props.children
	}
}

// Ce qu'un Error Boundary n'attrape pas
// C'est important à comprendre, pour ne pas lui demander l'impossible :

// les erreurs dans les gestionnaires d'événements (un onClick) ;
// les erreurs asynchrones (un fetch qui échoue, un setTimeout).

// Il n'attrape que les plantages pendant l'affichage.
// Les erreurs d'API, elles, sont déjà gérées par
// tes try/catch et tes ErrorBanner : chacun son rôle.

