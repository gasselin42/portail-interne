import type { Me } from "../api/auth"
import { Role } from "../api/admin";

export function canApprove(me: Me | null): boolean {
	return me !== null && (me.role === Role.Admin || me.isManager)
}