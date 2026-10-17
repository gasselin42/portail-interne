type Props = {
	workingDays: number | null
	unpaidDates: string[] | null
	compact?: boolean
}

export function LeaveDaysNote({ workingDays, unpaidDates, compact = false }: Props) {
	if (workingDays === null) return null

	return (
		<span className="block text-xs text-slate-500">
			{!compact && `${workingDays} jour${workingDays > 1 ? "s" : ""} ouvrable${workingDays > 1 ? "s" : ""}`}
			{unpaidDates !== null && unpaidDates.length > 0 && <span className="font-medium text-amber-700">{`${!compact ? " · " : ""}dont ${unpaidDates.length} sans solde`}</span>}
		</span>
	)
}