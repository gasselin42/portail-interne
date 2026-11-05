import type { MeetingColor } from "../api/meetings"

/** Nom affiché et pastille de chaque couleur d'événement (classes écrites en entier pour Tailwind). */
export const MEETING_COLOR_STYLES: Record<MeetingColor, { label: string; swatch: string }> = {
	blue: { label: "Bleu", swatch: "bg-blue-500" },
	indigo: { label: "Indigo", swatch: "bg-indigo-500" },
	cyan: { label: "Cyan", swatch: "bg-cyan-500" },
	teal: { label: "Sarcelle", swatch: "bg-teal-500" },
	fuchsia: { label: "Fuchsia", swatch: "bg-fuchsia-500" },
	pink: { label: "Rose", swatch: "bg-pink-500" },
	orange: { label: "Orange", swatch: "bg-orange-500" },
	stone: { label: "Pierre", swatch: "bg-stone-500" },
}
