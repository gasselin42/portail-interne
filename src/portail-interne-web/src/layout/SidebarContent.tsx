import { useLocation, Link, useNavigate } from "react-router-dom"
import { useSession } from "../session/useSession"
import { isNavItemActive, NAV_ITEMS } from "./navItems"
import { ROLE_LABELS } from "../api/admin"
import { LogoutOutlined, SettingsOutlined, ScheduleOutlined, BadgeOutlined } from "@mui/icons-material"
import { Avatar } from "../components/employees/Avatar"
import { Brand } from "../components/common/Brand"
import { linkClasses } from "../ui/buttons"

export type UserDialog = "presence" | "info" | "preferences"

type Props = {
	collapsed: boolean
	onNavigate?: () => void
	onOpenDialog: (dialog: UserDialog) => void
}

export function SidebarContent({ collapsed, onNavigate, onOpenDialog }: Props) {
	const { me, logout } = useSession()
	const navigate = useNavigate()
	const { pathname } = useLocation()

	const navItems = NAV_ITEMS.filter((i) => !i.visible || i.visible(me))
	const labelClass = collapsed ? "sr-only" : "whitespace-nowrap"

	function handleLogout() {
		onNavigate?.()
		logout()
		navigate("/login", { replace: true })
	}


	return (
		<div className="flex h-full flex-col">
			{/* 1. En-tête */}
			<div className={`flex px-4 py-5 ${collapsed ? "justify-center" : ""}`}>
				<Brand hideName={collapsed} />
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
									className={linkClasses(active, collapsed)}
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
					<div className={`flex items-center gap-3 px-3 ${collapsed ? "justify-center" : ""}`}>
						<Avatar firstName={me.firstName} lastName={me.lastName} size="md" />
						<div className={`min-w-0 ${labelClass}`}>
							<p className="truncate text-sm font-medium text-slate-900">
								{me.firstName} {me.lastName}
							</p>
							<p className="text-xs text-slate-500 truncate">{ROLE_LABELS[me.role]}</p>
						</div>
					</div>
				)}
				<ul className="mt-3 space-y-1">
					<li>
						<button
							type="button"
							onClick={() => onOpenDialog("presence")}
							title={collapsed ? "Ma présence" : undefined}
							className={linkClasses(false, collapsed)}
						>
							<ScheduleOutlined fontSize="small" />
							<span className={labelClass}>Ma présence</span>
						</button>
					</li>
					<li>
						<button
							type="button"
							onClick={() => onOpenDialog("info")}
							title={collapsed ? "Mes infos" : undefined}
							className={linkClasses(false, collapsed)}
						>
							<BadgeOutlined fontSize="small" />
							<span className={labelClass}>Mes infos</span>
						</button>
					</li>
					<li>
						<button
							type="button"
							onClick={() => onOpenDialog("preferences")}
							title={collapsed ? "Préférences" : undefined}
							className={linkClasses(false, collapsed)}
						>
							<SettingsOutlined fontSize="small" />
							<span className={labelClass}>Préférences</span>
						</button>
					</li>
					<li>
						<button
							type="button"
							onClick={handleLogout}
							title={collapsed ? "Déconnexion" : undefined}
							className={linkClasses(false, collapsed)}
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
