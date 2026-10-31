import { useState, type SubmitEvent } from "react"
import { createSite, updateSite, type Site } from "../../api/sites"
import { inputClass, labelClass } from "../../ui/fields"
import { Modal } from "../dialogs/Modal"
import { ErrorBanner } from "../feedback/Banner"
import { primaryButton, secondaryButton } from "../../ui/buttons"

type Props = {
	open: boolean
	site: Site | null
	onClose: () => void
	onSaved: () => void
}

export function SiteDialog({ open, site, onClose, onSaved }: Props) {
	return (
		<Modal open={open} title={site === null ? "Nouveau site" : "Modifier un site"} onClose={onClose}>
			<SiteForm site={site} onClose={onClose} onSaved={onSaved} />
		</Modal>
	)
}

function SiteForm({ site, onSaved, onClose }: { site: Site | null, onClose: () => void, onSaved: () => void }) {
	const [name, setName] = useState<string>(site?.name ?? "")
	const [timeZoneId, setTimeZoneId] = useState<string>(site?.timeZoneId ?? "")
	const [busy, setBusy] = useState(false)
	const [error, setError] = useState<string | null>(null)

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setError(null)
		try {
			setBusy(true)
			if (site === null) await createSite(name, timeZoneId)
			else await updateSite(site.id, name, timeZoneId)
			onSaved()
		} catch (e) {
			setError(e instanceof Error ? e.message : "Impossible d'enregistrer le site.")
		} finally {
			setBusy(false)
		}
	}

	return (
		<>
			{error &&
				<div className="mt-3">
					<ErrorBanner message={error} />
				</div>
			}
			
			<form onSubmit={handleSubmit}>
				<section className="pt-2">
					<div className="grid gap-5 sm:grid-cols-2">
						<div>
							<label
								htmlFor="name"
								className={labelClass}
							>
								Nom
							</label>
							<input
								type="text"
								id="name"
								required
								value={name}
								maxLength={100}
								className={inputClass}
								onChange={(e) => setName(e.target.value)}
							/>
						</div>
						<div>
							<label
								htmlFor="timeZoneId"
								className={labelClass}
							>
								Fuseau horaire
							</label>
							<input
								id="timeZoneId"
								list="time-zones"
								required
								value={timeZoneId}
								className={inputClass}
								onChange={(e) => setTimeZoneId(e.target.value)}
							/>
							<datalist id="time-zones">
								{Intl.supportedValuesOf("timeZone").map((tz) => <option key={tz} value={tz} />)}
							</datalist>
						</div>
					</div>
				</section>
				<div className="mt-6 flex justify-end gap-3">
					<button
						type="button"
						onClick={onClose}
						disabled={busy}
						className={secondaryButton}
					>
						Annuler
					</button>
					<button
						type="submit"
						disabled={busy}
						className={primaryButton}
					>
						<span className="grid">
							<span className={`col-start-1 row-start-1 ${busy ? "invisible" : ""}`}>
								{site === null ? "Créer" : "Enregistrer"}
							</span>
							<span className={`col-start-1 row-start-1 ${busy ? "" : "invisible"}`}>
								{site === null ? "Création…" : "Enregistrement…"}
							</span>
						</span>
					</button>
				</div>
			</form>
		</>
	)
}