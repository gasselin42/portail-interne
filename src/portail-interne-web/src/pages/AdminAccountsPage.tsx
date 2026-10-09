import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import WarningAmber from "@mui/icons-material/WarningAmber"
import { LockReset, Edit } from "@mui/icons-material"
import {
	listAccounts,
	setAccountActive,
	resetAccountPassword,
	DEPARTEMENT_LABELS,
	type AccountListItem,
} from "../api/admin"
import { ErrorBanner, type PageError } from "../components/Banner"
import { ConfirmDialog } from "../components/ConfirmDialog"
import { TemporaryPasswordDialog } from "../components/TemporaryPasswordDialog"
import { primaryButton, rowIconButton, smallSecondaryButton } from "../ui/buttons"
import { PageHeader } from "../components/PageHeader"

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

export function AdminAccountsPage() {
	const [accounts, setAccounts] = useState<AccountListItem[]>([])
	const [error, setError] = useState<PageError | null>(null)
	const [loading, setLoading] = useState(true)
	const [togglingId, setTogglingId] = useState<number | null>(null)
	const [resettingId, setResettingId] = useState<number | null>(null)
	const [temporaryPassword, setTemporaryPassword] = useState<string | null>(null)
	const [resetForName, setResetForName] = useState<string | null>(null)
	const [passwordDialogOpen, setPasswordDialogOpen] = useState(false)
	const [confirmOpen, setConfirmOpen] = useState(false)
	const [selectedAccount, setSelectedAccount] = useState<AccountListItem | null>(null)

	const resetMessage = selectedAccount
		? `Un mot de passe temporaire sera généré pour ${selectedAccount.firstName} ${selectedAccount.lastName}. Son mot de passe actuel ne fonctionnera plus, et ${selectedAccount.firstName} devra en choisir un nouveau à sa prochaine connexion.`
		: ""

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
		setError(null)
		setResetForName(null)
		setTemporaryPassword(null)
		setPasswordDialogOpen(false)
		setResettingId(account.userAccountId)
		try {
			const data = await resetAccountPassword(account.userAccountId)
			setTemporaryPassword(data.temporaryPassword)
			setResetForName(`${account.firstName} ${account.lastName}`)
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

	async function handleResetting() {
		if (!selectedAccount) return

		await handleResetPassword(selectedAccount)
		setConfirmOpen(false)
	}

	function openResetConfirm(account: AccountListItem) {
		setSelectedAccount(account)
		setConfirmOpen(true)
	}

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				eyebrow="Administration"
				title="Comptes"
				description="Gère les accès et l'activation des employés."
				actions={
					<Link to="/admin/employees/new" className={primaryButton}>
						Crée un employé
					</Link>
				}
			/>

			{error && (
				<ErrorBanner
					message={error.message}
					dismissible={error.dismissible}
					onDismiss={() => setError(null)}
				/>
			)}

			<section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
				{loading ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des comptes...</p>
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
											{DEPARTEMENT_LABELS[account.departement]}
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
												<Link
													to={`/admin/employees/${account.employeeId}/edit`}
													title="Modifier"
													aria-label={`Modifier ${account.firstName} ${account.lastName}`}
													className={rowIconButton}
												>
													<Edit fontSize="small" />
												</Link>
												<button
													type="button"
													disabled={resettingId === account.userAccountId}
													onClick={() => openResetConfirm(account)}
													title="Réinitialiser le mot de passe"
													className={smallSecondaryButton}
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
			<ConfirmDialog
				open={confirmOpen}
				title="Réinitialiser le mot de passe ?"
				message={resetMessage}
				confirmLabel="Réinitialiser"
				busyLabel="Réinitialisation…"
				onConfirm={handleResetting}
				onClose={() => setConfirmOpen(false)}
				onAfterClose={() => {
					if (temporaryPassword) setPasswordDialogOpen(true)
				}}
			/>
			<TemporaryPasswordDialog
				key={temporaryPassword ?? "aucun"}
				open={passwordDialogOpen}
				password={temporaryPassword ?? ""}
				employeeName={resetForName ?? ""}
				onClose={() => {
					setPasswordDialogOpen(false)
				}}
				onAfterClose={() => {
					setTemporaryPassword(null)
					setResetForName(null)
				}}
			/>
		</div>
	)
}
