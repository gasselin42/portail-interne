type Props = {
	firstName: string,
	lastName: string,
	size?: "sm" | "md"
}

export function Avatar({ firstName, lastName, size = "md" }: Props) {
	return (
		<span className={`${size === "md" ? "h-9 w-9 text-sm" : "h-7 w-7 text-xs"} shrink-0 rounded-full flex items-center justify-center bg-sky-100 text-sky-700 font-semibold`} aria-hidden="true">
			{firstName.charAt(0).toLocaleUpperCase()}{lastName.charAt(0).toLocaleUpperCase()}
		</span>
	)
}