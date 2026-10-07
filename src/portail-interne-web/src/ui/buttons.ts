/**
 * Classes Tailwind partagées par tous les boutons (et liens qui ressemblent à des boutons).
 * Pour ajouter un placement (marge, largeur), on compose : `${primaryButton} w-full`.
 */

export const focusRing =
	"focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"

const base = `inline-flex items-center justify-center gap-2 rounded-lg shadow-sm transition disabled:cursor-not-allowed disabled:opacity-50 ${focusRing}`

const regular = "px-4 py-2.5 text-sm"

/** Action principale d'un écran ou d'une boîte (Enregistrer, Confirmer, Copier…). */
export const primaryButton = `${base} ${regular} bg-sky-600 font-semibold text-white hover:bg-sky-700`

/** Action principale destructive (Annuler la demande, Refuser…). */
export const dangerButton = `${base} ${regular} bg-red-600 font-semibold text-white hover:bg-red-700`

/** Action secondaire (Retour, Terminé, liens « Retour à… »). */
export const secondaryButton = `${base} ${regular} border border-slate-200 bg-white font-medium text-slate-700 hover:border-slate-300 hover:bg-slate-100 hover:text-slate-900`

/** Action secondaire compacte, dans une ligne de tableau. */
export const smallSecondaryButton = `${base} border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:border-slate-300 hover:bg-slate-100 hover:text-slate-900`

/** Bouton icône de l'interface (menu, fermeture de tiroir). */
export const iconButton = `inline-flex items-center justify-center rounded-lg p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 disabled:cursor-not-allowed disabled:opacity-50 ${focusRing}`

const rowIconBase = `inline-flex items-center justify-center rounded-lg p-1.5 text-slate-400 transition disabled:cursor-not-allowed disabled:opacity-50 ${focusRing}`

/** Icône d'action dans une ligne de tableau : neutre (voir, modifier). */
export const rowIconButton = `${rowIconBase} hover:bg-sky-50 hover:text-sky-600`

/** Icône d'action dans une ligne de tableau : positive (approuver). */
export const rowIconSuccessButton = `${rowIconBase} hover:bg-emerald-50 hover:text-emerald-600`

/** Icône d'action dans une ligne de tableau : destructive (annuler, refuser). */
export const rowIconDangerButton = `${rowIconBase} hover:bg-red-50 hover:text-red-600`
