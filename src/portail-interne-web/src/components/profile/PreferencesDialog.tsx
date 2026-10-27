import { useState } from "react"
import { Modal } from "../dialogs/Modal"
import { linkClasses, secondaryButton } from "../../ui/buttons"
import { Link } from "react-router-dom"
import { KeyOutlined } from "@mui/icons-material"
import { sectionTitle } from "../../ui/fields"
import { ErrorBanner } from "../feedback/Banner"
import { useAsync } from "../../hooks/useAsync"
import { CalendarHourRange, getPreferences, savePreferences, type CalendarHourRangeId } from "../../api/preferences"
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
				<HourRangeChoice initial={data.calendarHourRange} onSaved={onSaved} />
			}
		</div>
	)
}

const HOUR_RANGE_VIEWS: { value: CalendarHourRangeId, label: string }[] = [
	{ value: CalendarHourRange.WorkHours, label: "Mes heures de travail" },
	{ value: CalendarHourRange.FullDay, label: "Journée complète" }
]

function HourRangeChoice({ initial, onSaved }: { initial: CalendarHourRangeId, onSaved: () => void }) {
	const [value, setValue] = useState<CalendarHourRangeId>(initial)
	const [error, setError] = useState<string | null>(null)
	const [saving, setSaving] = useState(false)

	async function handleChange(next: CalendarHourRangeId) {
		if (saving || next === value) return
		const previous = value
		setValue(next)
		setError(null)
		setSaving(true)

		try {
			await savePreferences({ calendarHourRange: next })
			onSaved()
		} catch (e) {
			setValue(previous)
			setError(e instanceof Error ? e.message : "Erreur")
		} finally {
			setSaving(false)
		}
	}
	
	return (
		<>
			<SegmentedControl label="Plage d'heures" options={HOUR_RANGE_VIEWS} value={value} onChange={handleChange} />
			{error && <p role="alert" className="mt-2 text-sm text-red-600">{error}</p>}
		</>
	)
}

