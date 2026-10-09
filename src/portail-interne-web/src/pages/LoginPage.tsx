import { useState, type SubmitEvent } from "react"
import { useNavigate, Navigate } from "react-router-dom"
import { Visibility, VisibilityOff } from "@mui/icons-material"
import { login } from "../api/auth"
import { ErrorBanner } from "../components/Banner"
import { useSession } from "../session/useSession"
import { FullPageStatus } from "../components/FullPageStatus"
import { primaryButton } from "../ui/buttons"

export function LoginPage() {
	const { me, loading, refresh } = useSession()

	const [email, setEmail] = useState<string>("")
	const [password, setPassword] = useState<string>("")

	const [motDePasseVisible, setMotDePasseVisible] = useState<boolean>(false)
	const [enCours, setEnCours] = useState<boolean>(false)
	const [erreur, setErreur] = useState<string | null>(null)

	const navigate = useNavigate()

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setErreur(null)

		if (email.trim() === "" || password === "") {
			setErreur("Entre ton email et ton mot de passe.")
			return
		}

		setEnCours(true)
		try {
			await login(email, password)
			await refresh()
			navigate("/")
		} catch (e) {
			if (e instanceof TypeError)
				setErreur("Impossible de joindre le serveur. Vérifie ta connexion.")
			else setErreur(e instanceof Error ? e.message : "Email ou mot de passe invalide")
		} finally {
			setEnCours(false)
		}
	}

	if (loading) {
		return <FullPageStatus loading title="Chargement..." />
	}

	if (me) {
		return <Navigate to="/" replace />
	}

	return (
		<form onSubmit={handleSubmit} className="mx-auto mt-16 max-w-sm space-y-4 rounded p-6">
			<div className="mb-3">
				<label htmlFor="email">Email</label>
				<input
					id="email"
					type="email"
					value={email}
					onChange={(e) => setEmail(e.target.value)}
					className="w-full rounded border border-gray-300 px-3 py-2"
				/>
			</div>
			<div>
				<label htmlFor="password">Mot de passe</label>
				<div className="relative mb-3">
					<input
						id="password"
						type={motDePasseVisible ? "text" : "password"}
						value={password}
						onChange={(p) => setPassword(p.target.value)}
						className="w-full rounded border border-gray-300 px-3 py-2"
					/>
					<button
						className="absolute top-0.5 right-2 translate-y-1 border-2 border-b-gray-800"
						type="button"
						onClick={() => setMotDePasseVisible((v) => !v)}
					>
						{motDePasseVisible ? <VisibilityOff /> : <Visibility />}
					</button>
				</div>
			</div>
			<button type="submit" disabled={enCours} className={`${primaryButton} mt-4 w-full`}>
				<span>Se connecter</span>
			</button>
			{erreur && <ErrorBanner message={erreur} dismissible onDismiss={() => setErreur(null)} />}
		</form>
	)
}
