import { useState } from "react"
import { returnFromAbsence } from "../../api/absences"
import { getCalendar } from "../../api/calendar"
import { useAsync } from "../../hooks/useAsync"
import { useNow } from "../../hooks/useNow"
import { useSession } from "../../session/useSession"
import { primaryButton } from "../../ui/buttons"
import { formatTime, todayIso } from "../../utils/date"

export function ReturnBanner({ version }: { version: number}) {
	const [busy, setBusy] = useState(false)
	const [returnError, setReturnError] = useState<string | null>(null)

	const today = todayIso()
	const now = useNow()
	const { me } = useSession()

	const { data, retry } = useAsync((signal) => getCalendar(today, today, signal), [today, version])
	if (!data) return null

	const day = data.days[0]
	if (!day || (day.endTime && now >= day.endTime)) return null

	const absence = day.absences.find((a) => a.inProgress)
	if (!absence ) return null

	async function handleReturn() {
		setBusy(true)
		setReturnError(null)
		try {
			await returnFromAbsence(me!.employeeId)
			retry()
		} catch (e) {
			setReturnError(e instanceof Error ? e.message : "Impossible d'enregistrer ton retour")
		} finally {
			setBusy(false)
		}
}


	return (
		<div className="flex justify-between items-center gap-3 px-4 py-2 border-b border-amber-200 bg-amber-50 text-amber-800">
			<div>
				<p>Absent depuis {formatTime(absence.start)}</p>
				{returnError && <p className="text-xs">{returnError}</p>}
			</div>
			<button
				type="button"
				disabled={busy}
				onClick={handleReturn}
				className={primaryButton}
			>
				Je suis de retour
			</button>
		</div>
	)
}