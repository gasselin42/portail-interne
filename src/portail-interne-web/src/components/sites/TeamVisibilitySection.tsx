import { useState } from "react"
import { setTeamVisibility, TEAM_VISIBILITY_LABELS, TeamVisibility, type Site, type TeamVisibilityId } from "../../api/sites"
import { SegmentedControl } from "../common/SegmentedControl"
import { ErrorBanner } from "../feedback/Banner"
import { sectionTitle } from "../../ui/fields"

const SITE_VISIBILITY_VIEWS: { value: TeamVisibilityId, label: string, description: string }[] = [
	{ value: TeamVisibility.Site, label: TEAM_VISIBILITY_LABELS[TeamVisibility.Site], description: "Chaque employé voit tout le site, sans détails." },
	{ value: TeamVisibility.Team, label: TEAM_VISIBILITY_LABELS[TeamVisibility.Team], description: "Chaque employé voit son manager et ses collègues, sans détails." },
	{ value: TeamVisibility.Nobody, label: TEAM_VISIBILITY_LABELS[TeamVisibility.Nobody], description: "Les employés n'ont pas de vue Équipe." },
]

export function TeamVisibilitySection({ site }: { site: Site}) {
	const [value, setValue] = useState<TeamVisibilityId>(site.teamVisibility)
	const [error, setError] = useState<string | null>(null)
	const [saving, setSaving] = useState(false)
	const [saved, setSaved] = useState(false)

	async function handleChange(next: TeamVisibilityId) {
		if (saving || next === value) return
		const previous = value
		setValue(next)
		setError(null)
		setSaved(false)
		setSaving(true)

		try {
			await setTeamVisibility(site.id, next)
			setSaved(true)
		} catch (e) {
			setValue(previous)
			setError(e instanceof Error ? e.message : "Impossible d'enregistrer la visibilité")
		} finally {
			setSaving(false)
		}
	}
	
	return (
		<section className="mt-8 overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
			<div className="px-4 py-3">
				<div className="flex flex-wrap items-center justify-between gap-3">
					<h2 className={sectionTitle}>Visibilité de l'équipe</h2>
					<SegmentedControl
						label="Visibilité de l'équipe"
						options={SITE_VISIBILITY_VIEWS}
						value={value}
						onChange={handleChange}
					/>
				</div>

				<p role="status" className="mt-1 h-4 text-right text-xs font-medium text-emerald-700">
					{saved ? "Enregistré" : ""}
				</p>

				{error && (
					<div className="mt-3">
						<ErrorBanner message={error} dismissible onDismiss={() => setError(null)} />
					</div>
				)}

				<div className="mt-3 space-y-1">
					<p className="text-sm text-slate-600">
						{SITE_VISIBILITY_VIEWS.find((o) => o.value === value)?.description}
					</p>
					<p className="text-xs text-slate-500">
						Les managers et les admins voient toujours leurs équipes, avec les détails.
					</p>
				</div>
			</div>
		</section>
	)
}