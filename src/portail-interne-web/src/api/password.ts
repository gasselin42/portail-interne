import { apiFetch } from "./client"

export async function changePassword(
	actualPassword: string,
	newPassword: string,
	confirmNewPassword: string,
): Promise<void> {
	const res = await apiFetch("/api/auth/change-password", {
		method: "POST",
		body: JSON.stringify({ actualPassword, newPassword, confirmNewPassword }),
	})

	if (!res.ok) {
		if (res.status === 400) throw new Error("Vous devez choisir un nouveau mot de passe")
		else if (res.status === 401) throw new Error("Votre mot de passe actuel ne concorde pas")
		throw new Error("Une erreur est survenue")
	}
}
