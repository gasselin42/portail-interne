import { useState, type SubmitEvent } from "react"
import { useNavigate, Navigate } from "react-router-dom"
import { login } from "../api/auth"
import { ErrorBanner } from "../components/Banner"
import { useSession } from "../session/useSession"
import { FullPageStatus } from "../components/FullPageStatus"
import { primaryButton } from "../ui/buttons"
import { AuthCard } from "../components/AuthCard"
import { inputClass, labelClass } from "../ui/fields"
import { PasswordInput } from "../components/PasswordInput"

export function LoginPage() {
	const { me, loading, refresh } = useSession()

	const [email, setEmail] = useState<string>("")
	const [password, setPassword] = useState<string>("")

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
		<AuthCard title="Connexion" description="Accède à ton portail.">
			<form onSubmit={handleSubmit} className="space-y-5" noValidate>
				{erreur && <ErrorBanner message={erreur} dismissible onDismiss={() => setErreur(null)} />}
				<div>
					<label htmlFor="email" className={labelClass}>
						Email
					</label>
					<input
						id="email"
						type="email"
						value={email}
						onChange={(e) => setEmail(e.target.value)}
						className={inputClass}
						autoComplete="username"
						autoFocus
						required
					/>
				</div>
				<PasswordInput
					id="password"
					label="Mot de passe"
					value={password}
					onChange={setPassword}
					autoComplete="current-password"
				/>
				<button type="submit" disabled={enCours} className={`${primaryButton} w-full`}>
					{enCours ? "Connexion…" : "Se connecter"}
				</button>
			</form>
		</AuthCard>
	)
}
