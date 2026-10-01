import { useNavigate, Link } from "react-router-dom"
import { useSession } from "../session/useSession"
import { Role } from "../api/admin"
import { canApprove } from "../session/permission"
import { SidebarContent } from "../layout/SidebarContent"

export function HomePage() {
	const { me, logout } = useSession()
	const navigate = useNavigate()
	return (
		<div className="min-h-screen bg-linear-to-b from-slate-50 via-slate-50 to-white">
			<div className="h-96 w-64 border bg-white">
				<SidebarContent collapsed={true} />
			</div>

			<div className="mx-auto max-w-5xl px-6 py-10">
				<header className="mb-8">
					<p className="text-sm font-medium text-sky-700">Portail</p>
					<h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">Accueil</h1>
					<p className="mt-1 text-sm text-slate-500">Annuaire, congés et calendrier</p>
				</header>
				<div className="mt-8 grid gap-4 sm:grid-cols-2">
					<Link
						to="/leaves"
						className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-sky-200 hover:bg-slate-50"
					>
						<h2 className="text-base font-semibold text-slate-900">Mes congés</h2>
						<p className="mt-1 text-sm text-slate-500">Voir et créer tes demandes</p>
					</Link>
					<Link
						to="/calendar"
						className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-sky-200 hover:bg-slate-50"
					>
						<h2 className="text-base font-semibold text-slate-900">Calendrier</h2>
						<p className="mt-1 text-sm text-slate-500">Tes absences, jour par jour</p>
					</Link>
					<Link
						to="/employees"
						className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-sky-200 hover:bg-slate-50"
					>
						<h2 className="text-base font-semibold text-slate-900">Annuaire</h2>
						<p className="mt-1 text-sm text-slate-500">Retrouver un collègue</p>
					</Link>
					{me && me.role === Role.Admin && (
						<Link
							to="/admin/accounts"
							className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-sky-200 hover:bg-slate-50"
						>
							<h2 className="text-base font-semibold text-slate-900">Comptes</h2>
							<p className="mt-1 text-sm text-slate-500">Gérer les employés et les accès</p>
						</Link>
					)}
					{canApprove(me) && (
						<Link
							to="/approvals"
							className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-sky-200 hover:bg-slate-50"
						>
							<h2 className="text-base font-semibold text-slate-900">Congés en attente</h2>
							<p className="mt-1 text-sm text-slate-500">Gérer les demandes de congés</p>
						</Link>
					)}
				</div>

				<div className="mt-8 flex flex-wrap gap-3">
					<Link
						to="/change-password"
						className="rounded-lg bg-slate-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-slate-700"
					>
						Changer mon mot de passe
					</Link>
					<button
						type="button"
						className="rounded-lg bg-slate-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-slate-700"
						onClick={() => {
							logout()
							navigate("/login", { replace: true })
						}}
					>
						Déconnexion
					</button>
				</div>
			</div>
		</div>
	)
}
