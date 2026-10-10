import { apiFetch } from "./client"

/** Photos déjà téléchargées (ou en cours), par adresse d'API. */
const cache = new Map<string, Promise<string>>()

/**
 * Télécharge une photo protégée (avec le token) et renvoie une adresse locale (blob:)
 * utilisable dans une <img>. Chaque photo n'est téléchargée qu'une fois.
 */
export function loadPhoto(path: string): Promise<string> {
	const cached = cache.get(path)
	if (cached) return cached

	const promise = apiFetch(path).then(async (res) =>{
		if (!res.ok) throw new Error("Photo introuvable")
		return URL.createObjectURL(await res.blob())
	})

	// En cas d'échec, on l'oublie : un prochain affichage pourra réessayer.
	promise.catch(() => cache.delete(path))

	cache.set(path, promise)
	return promise
}

/** Libère toutes les photos en mémoire. À appeler à la déconnexion. */
export function clearPhotoCache(): void {
	for (const promise of cache.values()) {
		promise.then(URL.revokeObjectURL, () => {})
	}
	cache.clear()
}