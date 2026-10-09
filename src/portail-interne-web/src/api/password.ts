import { apiFetch, readApiError } from "./client"

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
		throw new Error(await readApiError(res, "Impossible de changer le mot de passe."))
	}
}
