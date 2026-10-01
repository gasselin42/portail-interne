import { useState, type SubmitEvent } from "react"
import { useNavigate, Link } from "react-router-dom"
import { Visibility, VisibilityOff } from "@mui/icons-material"
import { changePassword } from "../api/password"
import { ErrorBanner } from "../components/ErrorBanner"
import { useSession } from "../session/useSession"

export function ChangePassword() {
	const { me, refresh } = useSession()

	const [actualPassword, setActualPassword] = useState<string>("")
	const [newPassword, setNewPassword] = useState<string>("")
	const [confirmNewPassword, setConfirmNewPassword] = useState<string>("")

	const [actualPasswordVisible, setActualPasswordVisible] = useState<boolean>(false)
	const [newPasswordVisible, setNewPasswordVisible] = useState<boolean>(false)
	const [confirmPasswordVisible, setConfirmPasswordVisible] = useState<boolean>(false)

	const [enCours, setEnCours] = useState<boolean>(false)
	const [erreur, setErreur] = useState<string | null>(null)

	const doitChanger = me?.mustChangePassword

	const navigate = useNavigate()

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setErreur(null)
		setEnCours(true)
		try {
			if (newPassword !== confirmNewPassword) {
				setErreur("Vos nouveaux mots de passe ne concordent pas")
				return
			}
			if (newPassword.length < 8) {
				setErreur("Votre mot de passe doit faire au moins 8 caractères")
				return
			}
			await changePassword(actualPassword, newPassword, confirmNewPassword)
			await refresh()
			navigate("/")
		} catch (err) {
			if (err instanceof Error) {
				setErreur(err.message) // le texte du throw dans password.ts
			} else {
				setErreur("Une erreur est survenue")
			}
		} finally {
			setEnCours(false)
		}
	}

	return (
		<form onSubmit={handleSubmit} className="mx-auto mt-16 max-w-sm space-y-4 rounded p-6">
			{!doitChanger && (
				<Link to="/" className="inline-block text-sm font-medium text-sky-700 hover:underline">
					Retour à l'accueil
				</Link>
			)}
			<div>
				<label htmlFor="newPassword">Mot de passe actuel</label>
				<div className="relative mb-3">
					<input
						id="confirmPassword"
						type={actualPasswordVisible ? "text" : "password"}
						value={actualPassword}
						onChange={(p) => setActualPassword(p.target.value)}
						className="w-full rounded border border-gray-300 px-3 py-2"
					/>
					<button
						className="absolute top-0.5 right-2 translate-y-1 border-2 border-b-gray-800"
						type="button"
						onClick={() => setActualPasswordVisible((v) => !v)}
					>
						{actualPasswordVisible ? <VisibilityOff /> : <Visibility />}
					</button>
				</div>
			</div>
			<div>
				<label htmlFor="newPassword">Nouveau mot de passe</label>
				<div className="relative mb-3">
					<input
						id="confirmPassword"
						type={newPasswordVisible ? "text" : "password"}
						value={newPassword}
						onChange={(p) => setNewPassword(p.target.value)}
						className="w-full rounded border border-gray-300 px-3 py-2"
					/>
					<button
						className="absolute top-0.5 right-2 translate-y-1 border-2 border-b-gray-800"
						type="button"
						onClick={() => setNewPasswordVisible((v) => !v)}
					>
						{newPasswordVisible ? <VisibilityOff /> : <Visibility />}
					</button>
				</div>
			</div>
			<div>
				<label htmlFor="confirmPassword">Confirme ton nouveau mot de passe</label>
				<div className="relative mb-3">
					<input
						id="confirmPassword"
						type={confirmPasswordVisible ? "text" : "password"}
						value={confirmNewPassword}
						onChange={(p) => setConfirmNewPassword(p.target.value)}
						className="w-full rounded border border-gray-300 px-3 py-2"
					/>
					<button
						className="absolute top-0.5 right-2 translate-y-1 border-2 border-b-gray-800"
						type="button"
						onClick={() => setConfirmPasswordVisible((v) => !v)}
					>
						{confirmPasswordVisible ? <VisibilityOff /> : <Visibility />}
					</button>
				</div>
			</div>
			<button
				type="submit"
				disabled={enCours}
				className="mt-4 w-full rounded bg-sky-600 px-4 py-2 font-medium text-white hover:cursor-pointer hover:bg-sky-700 disabled:opacity-50"
			>
				<span>Enregistrer</span>
			</button>
			{erreur && <ErrorBanner message={erreur} dismissible onDismiss={() => setErreur(null)} />}
		</form>
	)
}
