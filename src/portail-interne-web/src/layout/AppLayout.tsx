import { useState, useEffect } from "react"
import { Dialog, DialogBackdrop, DialogPanel } from "@headlessui/react"
import { Outlet, useLocation } from "react-router-dom"
import { SidebarContent } from "./SidebarContent"
import { useSession } from "../session/useSession"
import { MenuOutlined, MenuOpenOutlined, CloseOutlined } from "@mui/icons-material"
import { iconButton } from "../ui/buttons"
import { Brand } from "../components/Brand"
import { ErrorBoundary } from "../components/ErrorBoundary"
import { PageCrash } from "../components/PageCrash"

const COLLAPSED_KEY = "portail_sidebar_collapsed"

function readCollapsed(): boolean {
	try {
		return localStorage.getItem(COLLAPSED_KEY) === "true"
	} catch {
		return false
	}
}

export function AppLayout() {
	const { me } = useSession()
	const [collapsed, setCollapsed] = useState(readCollapsed)
	const [mobileOpen, setMobileOpen] = useState(false)

	const { pathname } = useLocation()

	function toggleCollapsed() {
		const next = !collapsed
		setCollapsed(next)
		try {
			localStorage.setItem(COLLAPSED_KEY, String(next))
		} catch {
			// Stockage indisponible : le choix ne sera simplement pas mémorisé
		}
	}

	useEffect(() => {
		const desktop = window.matchMedia("(min-width: 64rem)")

		function handleChange() {
			if (desktop.matches) setMobileOpen(false)
		}

		desktop.addEventListener("change", handleChange)
		return () => desktop.removeEventListener("change", handleChange)
	}, [])

	if (me?.mustChangePassword) return <Outlet />

	return (
		<div className="flex min-h-screen bg-linear-to-b from-slate-50 via-slate-50 to-white">
			<aside
				className={`sticky top-0 hidden h-screen shrink-0 flex-col lg:flex ${collapsed ? "w-20" : "w-64"} overflow-hidden border-r border-slate-200 bg-white transition-[width] duration-200`}
			>
				<div className="min-h-0 flex-1">
					<SidebarContent collapsed={collapsed} />
				</div>
				<button
					type="button"
					onClick={toggleCollapsed}
					aria-expanded={!collapsed}
					title={collapsed ? "Agrandir le menu" : "Réduire le menu"}
					aria-label={collapsed ? "Agrandir le menu" : "Réduire le menu"}
					className={`${iconButton} m-3`}
				>
					{collapsed ? <MenuOutlined fontSize="small" /> : <MenuOpenOutlined fontSize="small" />}
				</button>
			</aside>
			<div className="flex min-w-0 flex-1 flex-col">
				<header className="sticky top-0 z-30 flex items-center gap-3 border-b border-slate-200 bg-white px-4 py-3 lg:hidden">
					<button
						type="button"
						onClick={() => setMobileOpen(true)}
						aria-label="Ouvrir le menu"
						className={iconButton}
					>
						<MenuOutlined fontSize="small" />
					</button>
					<Brand />
				</header>
				<main className="flex-1">
					<ErrorBoundary key={pathname} fallback={(reset) => <PageCrash onRetry={reset} />}>
						<Outlet />
					</ErrorBoundary>
				</main>
			</div>
			<Dialog open={mobileOpen} onClose={setMobileOpen} className="relative z-50 lg:hidden">
				<DialogBackdrop
					transition
					className="fixed inset-0 bg-slate-900/40 transition-opacity duration-200 data-closed:opacity-0"
				/>
				<div className="fixed inset-0 flex">
					<DialogPanel
						transition
						className="relative flex w-72 max-w-[85%] flex-col bg-white shadow-xl transition duration-200 ease-out data-closed:-translate-x-full"
					>
						<button
							type="button"
							onClick={() => setMobileOpen(false)}
							aria-label="Fermer le menu"
							className={`${iconButton} absolute top-4 right-3`}
						>
							<CloseOutlined fontSize="small" />
						</button>
						<div className="min-h-0 flex-1">
							<SidebarContent collapsed={false} onNavigate={() => setMobileOpen(false)} />
						</div>
					</DialogPanel>
				</div>
			</Dialog>
		</div>
	)
}
