import type { MeetingColor } from "../api/meetings"

/** Nom affiché et pastille de chaque couleur d'événement (classes écrites en entier pour Tailwind). */
export const MEETING_COLOR_STYLES: Record<MeetingColor, { label: string; swatch: string, block: string }> = {
	blue: { label: "Bleu", swatch: "bg-blue-500", block: "border-blue-500 bg-blue-100 text-blue-900" },
	indigo: { label: "Indigo", swatch: "bg-indigo-500", block: "border-indigo-500 bg-indigo-100 text-indigo-900" },
	cyan: { label: "Cyan", swatch: "bg-cyan-500", block: "border-cyan-500 bg-cyan-100 text-cyan-900" },
	teal: { label: "Sarcelle", swatch: "bg-teal-500", block: "border-teal-500 bg-teal-100 text-teal-900" },
	fuchsia: { label: "Fuchsia", swatch: "bg-fuchsia-500", block: "border-fuchsia-500 bg-fuchsia-100 text-fuchsia-900" },
	pink: { label: "Rose", swatch: "bg-pink-500", block: "border-pink-500 bg-pink-100 text-pink-900" },
	orange: { label: "Orange", swatch: "bg-orange-500", block: "border-orange-500 bg-orange-100 text-orange-900" },
	stone: { label: "Pierre", swatch: "bg-stone-500", block: "border-stone-500 bg-stone-100 text-stone-900" },
}
