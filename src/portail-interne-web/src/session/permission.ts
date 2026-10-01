import type { Me } from "../api/auth"
import { Role } from "../api/admin";

export function canApprove(me: Me | null): boolean {
	return me !== null && (isAdmin(me) || me.isManager)
}

export function isAdmin(me: Me | null): boolean {
	return me !== null && me.role === Role.Admin
}