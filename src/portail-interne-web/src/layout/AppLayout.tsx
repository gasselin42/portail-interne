import { useState, useEffect } from "react"
import { Dialog, DialogBackdrop, DialogPanel } from "@headlessui/react"
import { Outlet } from "react-router-dom"
import { SidebarContent } from "./SidebarContent"
import { useSession } from "../session/useSession"
import { MenuOutlined, MenuOpenOutlined, CloseOutlined } from "@mui/icons-material"

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
			<aside className={`sticky top-0 h-screen shrink-0 hidden lg:flex flex-col ${collapsed ? "w-20" : "w-64"} bg-white border-r border-slate-200 transition-[width] duration-200 overflow-hidden`}>
				<div className="min-h-0 flex-1">
					<SidebarContent collapsed={collapsed} />
				</div>
				<button
					type="button"
					onClick={toggleCollapsed}
					aria-expanded={!collapsed}
					title={collapsed ? "Agrandir le menu" : "Réduire le menu"}
					aria-label={collapsed ? "Agrandir le menu" : "Réduire le menu"}
					className="m-3 flex items-center justify-center rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
				>
					{collapsed ? <MenuOutlined fontSize="small" /> : <MenuOpenOutlined fontSize="small" />}
				</button>
			</aside>
			<div className="flex min-w-0 flex-1 flex-col">
				<header className="flex items-center gap-3 sticky top-0 z-30 px-4 py-3 border-b border-slate-200 bg-white lg:hidden">
					<button
						type="button"
						onClick={() => setMobileOpen(true)}
						aria-label="Ouvrir le menu"
						className="flex items-center justify-center rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
					>
						<MenuOutlined fontSize="small" />
					</button>
					<span
						aria-hidden="true"
						className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-sky-600 text-sm font-semibold text-white"
					>
						P
					</span>
					<span className="text-lg font-semibold text-slate-900">Portail</span>
				</header>
				<main className="flex-1">
					<Outlet />
				</main>
			</div>
			<Dialog open={mobileOpen} onClose={setMobileOpen} className="relative z-50 lg:hidden">
				<DialogBackdrop transition className="fixed inset-0 bg-slate-900/40 transition-opacity duration-200 data-closed:opacity-0" />
				<div className="fixed inset-0 flex">
					<DialogPanel transition className="relative flex w-72 max-w-[85%] flex-col bg-white shadow-xl transition duration-200 ease-out data-closed:-translate-x-full">
						<button
							type="button"
							onClick={() => setMobileOpen(false)}
							aria-label="Fermer le menu"
							className="absolute top-4 right-3 flex items-center justify-center rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
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