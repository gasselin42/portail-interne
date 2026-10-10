export type AvatarSize = "sm" | "md" | "lg" | "xl"

/** Dimensions communes à l'avatar et à la photo. */
export const AVATAR_SIZES: Record<AvatarSize, { box: string; text: string }> = {
	sm: { box: "h-7 w-7", text: "text-xs" },
	md: { box: "h-9 w-9", text: "text-sm" },
	lg: { box: "h-16 w-16", text: "text-xl" },
	xl: { box: "h-24 w-24", text: "text-3xl" },
}