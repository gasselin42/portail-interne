import { useLocation, Link, useNavigate } from "react-router-dom"
import { useSession } from "../session/useSession"
import { isNavItemActive, NAV_ITEMS } from "./navItems"
import { ROLE_LABELS } from "../api/admin"
import { KeyOutlined, LogoutOutlined } from "@mui/icons-material"

type Props = {
	collapsed: boolean
	onNavigate?: () => void
}

export function SidebarContent({ collapsed, onNavigate }: Props) {
	const { me, logout } = useSession()
	const navigate = useNavigate()
	const { pathname } = useLocation()

	const navItems = NAV_ITEMS.filter((i) => !i.visible || i.visible(me))
	const labelClass = collapsed ? "sr-only" : ""

	function linkClasses(active: boolean): string {
		const base = "flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm transition"
		const state = active
			? "bg-sky-50 font-medium text-sky-700"
			: "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
		const layout = collapsed ? "justify-center" : ""
		return `${base} ${state} ${layout}`
	}

	function handleLogout() {
		onNavigate?.()
		logout()
		navigate("/login", { replace: true })
	}

	const passwordActive = pathname === "/change-password"

	return (
		<div className="flex h-full flex-col">
			{/* 1. En-tête */}
			<div className={`flex items-center gap-3 px-4 py-5 ${collapsed ? "justify-center" : ""}`}>
				<span
					aria-hidden="true"
					className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-sky-600 text-sm font-semibold text-white"
				>
					Portail
				</span>
				<span className={`text-lg font-semibold text-slate-900 ${labelClass}`}>Portail</span>
			</div>

			{/* 2. Liens : flex-1 prend toute la hauteur restante */}
			<nav aria-label="Navigation principale" className="flex-1 overflow-y-auto px-3">
				<ul className="space-y-1">
					{navItems.map((item) => {
						const active = isNavItemActive(item, pathname)
						return (
							<li key={item.to}>
								<Link
									to={item.to}
									onClick={onNavigate}
									title={collapsed ? item.label : undefined}
									aria-current={active ? "page" : undefined}
									className={linkClasses(active)}
								>
									<item.icon fontSize="small" />
									<span className={labelClass}>{item.label}</span>
								</Link>
							</li>
						)
					})}
				</ul>
			</nav>

			{/* 3. Bloc utilisateur */}
			<div className="border-t border-slate-200 px-3 py-4">
				{me && (
					<div className={`px-3 ${labelClass}`}>
						<p className="truncate text-sm font-medium text-slate-900">
							{me.firstName} {me.lastName}
						</p>
						<p className="text-xs text-slate-500">{ROLE_LABELS[me.role]}</p>
					</div>
				)}
				<ul className="mt-3 space-y-1">
					<li>
						<Link
							to="/change-password"
							onClick={onNavigate}
							title={collapsed ? "Changer mon mot de passe" : undefined}
							aria-current={passwordActive ? "page" : undefined}
							className={linkClasses(passwordActive)}
						>
							<KeyOutlined fontSize="small" />
							<span className={labelClass}>Changer mon mot de passe</span>
						</Link>
					</li>
					<li>
						<button
							type="button"
							onClick={handleLogout}
							title={collapsed ? "Déconnexion" : undefined}
							className={linkClasses(false)}
						>
							<LogoutOutlined fontSize="small" />
							<span className={labelClass}>Déconnexion</span>
						</button>
					</li>
				</ul>
			</div>
		</div>
	)
}
