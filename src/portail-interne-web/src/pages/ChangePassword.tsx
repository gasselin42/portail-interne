import { useState, type SubmitEvent } from "react"
import { useNavigate } from "react-router-dom"
import { changePassword } from "../api/password"
import { ErrorBanner, SuccessBanner } from "../components/Banner"
import { useSession } from "../session/useSession"
import { focusRing, primaryButton } from "../ui/buttons"
import { PasswordInput } from "../components/PasswordInput"
import { AuthCard } from "../components/AuthCard"
import { PageHeader } from "../components/PageHeader"

export function ChangePassword() {
	const { me, logout, refresh } = useSession()

	const [actualPassword, setActualPassword] = useState<string>("")
	const [newPassword, setNewPassword] = useState<string>("")
	const [confirmNewPassword, setConfirmNewPassword] = useState<string>("")

	const [enCours, setEnCours] = useState<boolean>(false)
	const [success, setSuccess] = useState<boolean>(false)
	const [erreur, setErreur] = useState<string | null>(null)

	const navigate = useNavigate()

	const forced = me?.mustChangePassword === true

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setSuccess(false)
		setErreur(null)

		if (actualPassword === "" || newPassword === "" || confirmNewPassword === "") {
			setErreur("Remplis les 3 champs.")
			return
		} else if (newPassword.length < 8) {
			setErreur("Le nouveau mot de passe doit faire au moins 8 caractères.")
			return
		} else if (newPassword !== confirmNewPassword) {
			setErreur("Les deux nouveaux mots de passe ne concordent pas.")
			return
		} else if (newPassword === actualPassword) {
			setErreur("Le nouveau mot de passe doit être différent de l'actuel.")
			return
		}

		setEnCours(true)

		try {
			await changePassword(actualPassword, newPassword, confirmNewPassword)
			if (forced) {
				await refresh()
				navigate("/")
			} else {
				setSuccess(true)
				setActualPassword("")
				setNewPassword("")
				setConfirmNewPassword("")
			}
		} catch (err) {
			if (err instanceof Error) {
				setErreur(err.message) // le texte du throw dans password.ts
			} else {
				setErreur("Une erreur est survenue.")
			}
		} finally {
			setEnCours(false)
		}
	}

	const form = (
		<form noValidate onSubmit={handleSubmit} className="space-y-5">
			{erreur && <ErrorBanner message={erreur} dismissible onDismiss={() => setErreur(null)} />}
			<PasswordInput
				id="currentPassword"
				label={forced ? "Mot de passe temporaire" : "Mot de passe actuel"}
				value={actualPassword}
				onChange={setActualPassword}
				autoComplete="current-password"
			/>
			<PasswordInput
				id="newPassword"
				label="Nouveau mot de passe"
				value={newPassword}
				onChange={setNewPassword}
				autoComplete="new-password"
				hint="Au moins 8 caractères."
			/>
			<PasswordInput
				id="confirmPassword"
				label="Confirme le nouveau mot de passe"
				value={confirmNewPassword}
				onChange={setConfirmNewPassword}
				autoComplete="new-password"
			/>
			<button type="submit" disabled={enCours} className={`${primaryButton} w-full`}>
				{enCours ? "Enregistrement…" : "Enregistrer"}
			</button>
		</form>
	)

	if (forced) {
		return (
			<AuthCard
				title="Choisis ton mot de passe"
				description="Remplace le mot de passe temporaire reçu de ton administrateur."
			>
				{form}
				<button
					type="button"
					onClick={() => {
						logout()
						navigate("/login", { replace: true })
					}}
					className={`mt-4 w-full text-center text-sm text-slate-500 hover:text-slate-700 hover:underline ${focusRing}`}
				>
					Pas maintenant ? Se déconnecter
				</button>
			</AuthCard>
		)
	}

	return (
		<div className="mx-auto max-w-2xl px-6 py-10">
			<PageHeader
				eyebrow="Compte"
				title="Changer mon mot de passe"
				description="Choisis un nouveau mot de passe pour ton compte."
			/>

			{success && (
				<SuccessBanner
					message="Ton mot de passe a été modifié avec succès."
					dismissible
					onDismiss={() => setSuccess(false)}
				/>
			)}

			<section className="overflow-hidden rounded-2xl border border-slate-200 bg-white p-6 shadow-sm shadow-slate-200/50">
				{form}
			</section>
		</div>
	)
}
