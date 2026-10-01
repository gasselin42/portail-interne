import type { SvgIconComponent } from "@mui/icons-material"
import {
	HomeOutlined,
	EventAvailableOutlined,
	CalendarMonthOutlined,
	PeopleOutlined,
	FactCheckOutlined,
	ManageAccountsOutlined,
} from "@mui/icons-material"
import type { Me } from "../api/auth"
import { canApprove, isAdmin } from "../session/permission"

export type NavItem = {
	to: string
	label: string
	icon: SvgIconComponent
	/** Autres débuts d'URL pour lesquels ce lien est actif */
	alsoActiveOn?: string[]
	/** Si absent : visible par tout le monde */
	visible?: (me: Me | null) => boolean
}

export const NAV_ITEMS: NavItem[] = [
	{ to: "/", label: "Accueil", icon: HomeOutlined },
	{ to: "/leaves", label: "Mes congés", icon: EventAvailableOutlined },
	{ to: "/calendar", label: "Calendrier", icon: CalendarMonthOutlined },
	{ to: "/employees", label: "Annuaire", icon: PeopleOutlined },
	{ to: "/approvals", label: "À approuver", icon: FactCheckOutlined, visible: canApprove },
	{
		to: "/admin/accounts",
		label: "Comptes",
		icon: ManageAccountsOutlined,
		visible: isAdmin,
		alsoActiveOn: ["/admin/employees"],
	},
]

export function isNavItemActive(item: NavItem, pathname: string): boolean {
	if (item.to === "/") return pathname === "/"

	const paths = [item.to, ...(item.alsoActiveOn ?? [])]
	return paths.some((p) => pathname === p || pathname.startsWith(`${p}/`))
}
