import { LeaveStatus, type LeaveStatusId } from "../api/leaves";

/** Commun à tous les bandeaux de congé du calendrier. */
export const leaveBandBase = "truncate px-2 font-medium"

/**
 * Style d'un bandeau selon le statut (R5).
 * Approuvé = plein, en attente = hachuré : distincts même sans les couleurs.
 */
export const LEAVE_BAND_STYLES: Record<LeaveStatusId, string> = {
	[LeaveStatus.Approuve]: "bg-emerald-600 text-white",
	[LeaveStatus.EnAttente]: "border border-dashed border-amber-400 text-amber-900 bg-[repeating-linear-gradient(135deg,var(--color-amber-100)_0_6px,var(--color-amber-50)_6px_12px)]",
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
