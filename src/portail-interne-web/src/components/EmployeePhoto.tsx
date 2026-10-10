import { useEffect, useState } from "react"
import { AVATAR_SIZES, type AvatarSize } from "../ui/avatar"
import { loadPhoto } from "../api/photos"
import { Avatar } from "./Avatar"

type Props = {
	photoUrl: string | null
	firstName: string
	lastName: string
	size?: AvatarSize
}

export function EmployeePhoto({ photoUrl, firstName, lastName, size = "md" }: Props) {
	const [loaded, setLoaded] = useState<{ url: string; src: string} | null>(null)

	useEffect(() => {
		if (!photoUrl) return
		let active = true

		loadPhoto(photoUrl)
			.then((src) => {
				if (active) setLoaded({ url: photoUrl, src })
			})
			.catch(() => {
				// La photo n'a pas pu être chargée : l'avatar reste affiché.
			})

		return () => {
			active = false
		}
	}, [photoUrl])

	// Valeur dérivée : la photo n'est affichée que si elle correspond à l'adresse actuelle.
	const src = photoUrl && loaded?.url === photoUrl ? loaded.src : null

	return (src ? 
		<img
			src={src}
			alt=""
			className={`${AVATAR_SIZES[size].box} shrink-0 rounded-full object-cover`}
		/>
		:
		<Avatar
			firstName={firstName}
			lastName={lastName}
			size={size}
		/>
	)
}