import { LeaveStatus, type LeaveStatusId } from "../api/leaves";

/** Commun à tous les bandeaux de congé du calendrier. */
export const leaveBandBase = "truncate px-2 font-medium"

/**
 * Style d'un bandeau selon le statut (R5).
 * Approuvé = plein, en attente = hachuré : distincts même sans les couleurs.
 */
export const LEAVE_BAND_STYLES: Record<LeaveStatusId, string> = {
	[LeaveStatus.Approuve]: "text-white",
	[LeaveStatus.EnAttente]: "border border-dashed border-slate-400 text-amber-900",
	// Jamais renvoyés par l'endpoint calendrier ; style neutre par sécurité.
	[LeaveStatus.Refuse]: "bg-slate-100 text-slate-500 line-through",
	[LeaveStatus.Annule]: "bg-slate-100 text-slate-500 line-through",
}

/** Pastille d'un congé dans la vue mois mobile : pleine si approuvé, en anneau si en attente. */
export const LEAVE_DOT_STYLES: Record<LeaveStatusId, string> = {
	[LeaveStatus.Approuve]: "bg-emerald-600",
	[LeaveStatus.EnAttente]: "ring-1 ring-inset ring-amber-500 bg-white",
	[LeaveStatus.Refuse]: "bg-slate-300",
	[LeaveStatus.Annule]: "bg-slate-300",
}

/** Même chose pour un jour sans solde (R19) : même motif, teinte violette. */
export const LEAVE_UNPAID_DOT_STYLES: Record<LeaveStatusId, string> = {
	[LeaveStatus.Approuve]: "bg-violet-600",
	[LeaveStatus.EnAttente]: "ring-1 ring-inset ring-violet-500 bg-white",
	[LeaveStatus.Refuse]: "bg-slate-300",
	[LeaveStatus.Annule]: "bg-slate-300",
}
