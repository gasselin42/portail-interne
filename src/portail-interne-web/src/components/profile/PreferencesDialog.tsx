import { useState } from "react"
import { Modal } from "../dialogs/Modal"
import { linkClasses, secondaryButton } from "../../ui/buttons"
import { Link } from "react-router-dom"
import { KeyOutlined } from "@mui/icons-material"
import { labelClass, sectionTitle } from "../../ui/fields"
import { ErrorBanner } from "../feedback/Banner"
import { useAsync } from "../../hooks/useAsync"
import { CalendarDensity, CalendarHourRange, getPreferences, savePreferences, type CalendarDensityId, type CalendarHourRangeId } from "../../api/preferences"
import { SegmentedControl } from "../common/SegmentedControl"

type Props = {
	open: boolean
	onClose: () => void
	onSaved: () => void
}

export function PreferencesDialog({ open, onClose, onSaved }: Props) {
	return (
		<Modal open={open} title="Préférences" onClose={onClose}>
			<h3 className={`${sectionTitle} mb-2`}>Calendrier</h3>
			<PreferencesContent onSaved={onSaved} />
			<Link to="/change-password" onClick={onClose} className={`${linkClasses(false, false)} mt-6`}>
				<KeyOutlined fontSize="small" />Changer mon mot de passe
			</Link>
			<div className="mt-6 flex justify-end gap-3">
				<button type="button" onClick={onClose} className={secondaryButton}>
					Fermer
				</button>
			</div>
		</Modal>
	)
}

function PreferencesContent({ onSaved }: { onSaved: () => void }) {
	const { data, error, loading, retry } = useAsync((signal) => getPreferences(signal), [])

	return (
		<div>
			{error ? (
				<ErrorBanner message={error} onRetry={retry} />
			) : loading ? (
				<p role="status" className="text-center text-sm text-slate-500">
					Chargement…
				</p>
			) : data && 
				<CalendarChoices initialRange={data.calendarHourRange} initialDensity={data.calendarDensity} onSaved={onSaved} />
			}
		</div>
	)
}

const HOUR_RANGE_VIEWS: { value: CalendarHourRangeId, label: string }[] = [
	{ value: CalendarHourRange.WorkHours, label: "Mes heures de travail" },
	{ value: CalendarHourRange.FullDay, label: "Journée complète" }
]

const DENSITY_VIEWS: { value: CalendarDensityId, label: string }[] = [
	{ value: CalendarDensity.Normal, label: "Normale" },
	{ value: CalendarDensity.Compact, label: "Compacte" },
	{ value: CalendarDensity.Airy, label: "Aérée" },
]

function CalendarChoices({ initialRange, initialDensity, onSaved }: { initialRange: CalendarHourRangeId, initialDensity: CalendarDensityId, onSaved: () => void }) {
	const [range, setRange] = useState<CalendarHourRangeId>(initialRange)
	const [density, setDensity] = useState<CalendarDensityId>(initialDensity)
	const [error, setError] = useState<string | null>(null)
	const [saving, setSaving] = useState(false)

	async function handleChange(nextRange: CalendarHourRangeId, nextDensity: CalendarDensityId) {
		if (saving || (nextRange === range && nextDensity === density)) return
		const previousRange = range
		const previousDensity = density
		setRange(nextRange)
		setDensity(nextDensity)
		setError(null)
		setSaving(true)

		try {
			await savePreferences({ calendarHourRange: nextRange, calendarDensity: nextDensity })
			onSaved()
		} catch (e) {
			setRange(previousRange)
			setDensity(previousDensity)
			setError(e instanceof Error ? e.message : "Erreur")
		} finally {
			setSaving(false)
		}
	}
	
	return (
		<>
			<div className="flex flex-col">
				<span className={labelClass} aria-hidden="true">
					Plage d'heures
				</span>
				<div className="flex flex-1">
					<SegmentedControl label="Plage d'heures" options={HOUR_RANGE_VIEWS} value={range} onChange={(next) => handleChange(next, density)} />
				</div>
			</div>
			<div className="flex flex-col mt-3">
				<span className={labelClass} aria-hidden="true">
					Densité
				</span>
				<div className="flex flex-1">
					<SegmentedControl label="Densité" options={DENSITY_VIEWS} value={density} onChange={(next) => handleChange(range, next)} />
				</div>
			</div>
			{error && <p role="alert" className="mt-2 text-sm text-red-600">{error}</p>}
		</>
	)
}

