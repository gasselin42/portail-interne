import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import WarningAmber from "@mui/icons-material/WarningAmber"
import { LockReset, Edit } from "@mui/icons-material"
import {
	listAccounts,
	setAccountActive,
	resetAccountPassword,
	Departement,
	type AccountListItem,
} from "../api/admin"
import { ErrorBanner } from "../components/ErrorBanner"

type PageError = {
	message: string
	dismissible: boolean
}

function AccountActiveToggle({
	checked,
	disabled,
	onChange,
}: {
	checked: boolean
	disabled?: boolean
	onChange: (next: boolean) => void
}) {
	return (
		<label className={`relative inline-block h-[34px] w-[60px] ${disabled ? "opacity-50" : ""}`}>
			<input
				type="checkbox"
				className="peer sr-only"
				checked={checked}
				disabled={disabled}
				onChange={(e) => onChange(e.target.checked)}
			/>
			<span className="peer-focus-visible::ring-2 peer-focus-visible::ring-sky-300 absolute inset-0 cursor-pointer rounded-full bg-zinc-300 transition peer-checked:bg-sky-500 peer-disabled:cursor-not-allowed before:absolute before:bottom-1 before:left-1 before:h-[26px] before:w-[26px] before:rounded-full before:bg-white before:shadow-sm before:transition before:content-[''] peer-checked:before:translate-x-[26px]" />
		</label>
	)
}

function RoleBadge({ role }: { role: number }) {
	const isAdmin = role === 0
	return (
		<span
			className={
				isAdmin
					? "inline-flex rounded-full bg-sky-100 px-2.5 py-0.5 text-xs font-medium text-sky-800"
					: "inline-flex rounded-full bg-zinc-100 px-2.5 py-0.5 text-xs font-medium text-zinc-700"
			}
		>
			{isAdmin ? "Admin" : "Employé"}
		</span>
	)
}

function departementLabel(id: number | null): string {
	if (id === null) return "—"
	const entry = Object.entries(Departement).find(([, value]) => value === id)
	return entry?.[0] ?? "—"
}

export function AdminAccountsPage() {
	const [accounts, setAccounts] = useState<AccountListItem[]>([])
	const [error, setError] = useState<PageError | null>(null)
	const [loading, setLoading] = useState(true)
	const [togglingId, setTogglingId] = useState<number | null>(null)
	const [resettingId, setResettingId] = useState<number | null>(null)
	const [temporaryPassword, setTemporaryPassword] = useState<string | null>(null)
	const [resetForEmail, setResetForEmail] = useState<string | null>(null)
	const [passwordCopied, setPasswordCopied] = useState(false)

	useEffect(() => {
		listAccounts()
			.then(setAccounts)
			.catch((e) =>
				setError({
					message: e instanceof Error ? e.message : "Erreur",
					dismissible: false,
				}),
			)
			.finally(() => setLoading(false))
	}, [])

	async function handleToggle(account: AccountListItem, next: boolean) {
		setError(null)
		setTogglingId(account.userAccountId)
		try {
			await setAccountActive(account.userAccountId, next)
			setAccounts((prev) =>
				prev.map((a) =>
					a.userAccountId === account.userAccountId
						? { ...a, accountIsActive: next, employeeIsActive: next }
						: a,
				),
			)
		} catch (e) {
			setError({
				message: e instanceof Error ? e.message : "Impossible de modifier le compte",
				dismissible: true,
			})
		} finally {
			setTogglingId(null)
		}
	}

	async function handleResetPassword(account: AccountListItem) {
		const ok = window.confirm(
			`Réinitialiser le mot de passe de ${account.firstName} ${account.lastName} ?`,
		)
		if (!ok) return

		setError(null)
		setTemporaryPassword(null)
		setPasswordCopied(false)
		setResettingId(account.userAccountId)
		try {
			const data = await resetAccountPassword(account.userAccountId)
			setTemporaryPassword(data.temporaryPassword)
			setResetForEmail(account.email)
			setPasswordCopied(false)
			setAccounts((prev) =>
				prev.map((a) =>
					a.userAccountId === account.userAccountId ? { ...a, mustChangePassword: true } : a,
				),
			)
		} catch (e) {
			setError({
				message: e instanceof Error ? e.message : "Impossible de réinitialiser le mot de passe",
				dismissible: true,
			})
		} finally {
			setResettingId(null)
		}
	}

	return (
		<div className="min-h-screen bg-gradient-to-b from-slate-50 via-slate-50 to-white">
			<div className="mx-auto max-w-6xl px-6 py-10">
				<header className="mb-8 flex flex-wrap items-end justify-between gap-4">
					<div>
						<p className="text-sm font-medium text-sky-700">Administration</p>
						<h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">Comptes</h1>
						<p className="mt-1 text-sm text-slate-500">
							Gérer les accès et l'activation des employés
						</p>
					</div>
					<Link
						to="/admin/employees/new"
						className="rounded-lg bg-sky-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition hover:bg-sky-700"
					>
						Créer un employé
					</Link>
				</header>

				{error && (
					<ErrorBanner
						message={error.message}
						dismissible={error.dismissible}
						onDismiss={() => setError(null)}
					/>
				)}

				{temporaryPassword && (
					<div className="mb-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
						<p className="font-medium">
							Nouveau mot de passe temporaire
							{resetForEmail ? ` pour ${resetForEmail}` : ""} (affiché une seule fois)
						</p>
						<div className="mt-2 flex flex-wrap items-center gap-3">
							<p className="font-mono text-base tracking-wide">{temporaryPassword}</p>
							<button
								type="button"
								disabled={passwordCopied}
								className="rounded-lg border border-amber-300 bg-white px-3 py-1.5 text-xs font-medium text-amber-900 transition hover:bg-amber-100 disabled:cursor-default disabled:opacity-70"
								onClick={async () => {
									await navigator.clipboard.writeText(temporaryPassword)
									setPasswordCopied(true)
								}}
							>
								{passwordCopied ? "Copié" : "Copier"}
							</button>
							<button
								type="button"
								className="rounded-lg px-3 py-1.5 text-xs font-medium text-amber-800 hover:underline"
								onClick={() => {
									setTemporaryPassword(null)
									setResetForEmail(null)
									setPasswordCopied(false)
								}}
							>
								Fermer
							</button>
						</div>
					</div>
				)}

				<section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
					{loading ? (
						<p className="px-6 py-12 text-center text-sm text-slate-500">
							Chargement des comptes...
						</p>
					) : accounts.length === 0 ? (
						!(error && !error.dismissible) && (
							<p className="px-6 py-12 text-center text-sm text-slate-500">
								Aucun compte pour le moment
							</p>
						)
					) : (
						<div className="overflow-x-auto">
							<table className="w-full min-w-[720px] border-collapse text-left">
								<thead>
									<tr className="border-b border-slate-200 bg-slate-50/80">
										<th className="px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Nom
										</th>
										<th className="px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Prénom
										</th>
										<th className="px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Email
										</th>
										<th className="px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Rôle
										</th>
										<th className="px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Département
										</th>
										<th className="px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
											Actif
										</th>
										<th className="w-0 px-5 py-3.5 text-xs font-semibold tracking-wide whitespace-nowrap text-slate-500 uppercase">
											Actions
										</th>
									</tr>
								</thead>
								<tbody>
									{accounts.map((account) => (
										<tr
											key={account.userAccountId}
											className="border-b border-slate-100 transition last:border-b-0 hover:bg-slate-50/70"
										>
											<td className="px-5 py-3.5 text-sm font-medium text-slate-900">
												{account.lastName}
											</td>
											<td className="px-5 py-3.5 text-sm text-slate-700">{account.firstName}</td>
											<td className="px-5 py-3.5 text-sm text-slate-600">{account.email}</td>
											<td className="px-5 py-3.5">
												<RoleBadge role={account.role} />
											</td>
											<td className="px-5 py-3.5 text-sm text-slate-600">
												{departementLabel(account.departement)}
											</td>
											<td className="px-5 py-3.5">
												<AccountActiveToggle
													checked={account.accountIsActive}
													disabled={togglingId === account.userAccountId}
													onChange={(next) => handleToggle(account, next)}
												/>
											</td>
											<td className="w-0 px-5 py-3.5 whitespace-nowrap">
												<div className="flex items-center gap-2">
													<Link to={`/admin/employees/${account.employeeId}/edit`} title="Modifier">
														<Edit fontSize="small" />
													</Link>
													<button
														type="button"
														disabled={resettingId === account.userAccountId}
														onClick={() => handleResetPassword(account)}
														title="Réinitialiser le mot de passe"
														className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 shadow-sm transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
													>
														<LockReset fontSize="small" />
														{resettingId === account.userAccountId ? "…" : "Réinit. mdp"}
													</button>
													{account.mustChangePassword && (
														<span
															title="Mot de passe temporaire à changer"
															className="inline-flex text-amber-500"
														>
															<WarningAmber fontSize="small" />
														</span>
													)}
												</div>
											</td>
										</tr>
									))}
								</tbody>
							</table>
						</div>
					)}
				</section>
			</div>
		</div>
	)
}
