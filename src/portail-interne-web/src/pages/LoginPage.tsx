import { useState, type SubmitEvent } from 'react';
import { useNavigate, Navigate } from 'react-router-dom';
import { Visibility, VisibilityOff } from '@mui/icons-material';
import { login, getToken, getMustChangePassword } from '../api/auth';
import { ErrorBanner } from '../components/ErrorBanner';

export function LoginPage() {
	const [email, setEmail] = useState<string>("")
	const [password, setPassword] = useState<string>("")

	const [motDePasseVisible, setMotDePasseVisible] = useState<boolean>(false)
	const [enCours, setEnCours] = useState<boolean>(false)
	const [erreur, setErreur] = useState<string | null>(null)

	const navigate = useNavigate()

	const token = getToken()

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setErreur(null)
		setEnCours(true)
		try {
			const data = await login(email, password)
			navigate(data.mustChangePassword ? '/change-password' : '/')
		} catch {
			setErreur('Email ou mot de passe invalide')
		} finally {
			setEnCours(false)
		}
	}

	if (token) {
		return (
			<Navigate
				to={getMustChangePassword() ? '/change-password' : '/'}
				replace
			/>
		)
	}

	return (
		<form onSubmit={handleSubmit} className="mx-auto mt-16 max-w-sm space-y-4 rounded p-6">
			<div className="mb-3">
				<label htmlFor="email">Email</label>
				<input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="w-full rounded border border-gray-300 px-3 py-2" />
			</div>
			<div>
				<label htmlFor="password">Mot de passe</label>
				<div className="relative mb-3">
					<input id="password" type={motDePasseVisible ? 'text' : 'password'} value={password} onChange={(p) => setPassword(p.target.value)} className="w-full rounded border border-gray-300 px-3 py-2"/>
					<button className="border-2 border-b-gray-800 absolute right-2 top-0.5 translate-y-1" type='button' onClick={() => setMotDePasseVisible(v => !v)}>
						{motDePasseVisible ? <VisibilityOff /> : <Visibility />}
					</button>
				</div>
			</div>
			<button type='submit' disabled={enCours} className="mt-4 w-full rounded bg-sky-600 px-4 py-2 font-medium text-white hover:bg-sky-700 hover:cursor-pointer disabled:opacity-50">
				<span>Se connecter</span>
			</button>
			{erreur && (
				<ErrorBanner
					message={erreur}
					dismissible
					onDismiss={() => setErreur(null)}
				/>
			)}
		</form>
	)
}