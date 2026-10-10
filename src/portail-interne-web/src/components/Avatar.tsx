import { PersonOutlined } from "@mui/icons-material"
import { AVATAR_SIZES, type AvatarSize } from "../ui/avatar"

type Props = {
	firstName: string,
	lastName: string,
	size?: AvatarSize
}

export function Avatar({ firstName, lastName, size = "md" }: Props) {
	const initials = `${firstName.charAt(0)}${lastName.charAt(0)}`.toLocaleUpperCase()
 
	return (
		<span className={`${AVATAR_SIZES[size].box} ${AVATAR_SIZES[size].text} shrink-0 rounded-full flex items-center justify-center bg-sky-100 text-sky-700 font-semibold`} aria-hidden="true">
			{initials || <PersonOutlined fontSize="inherit" />}
		</span>
	)
}