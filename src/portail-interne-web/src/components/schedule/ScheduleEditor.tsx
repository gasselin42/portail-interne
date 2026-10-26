import { Fragment } from "react"
import { inputClass } from "../../ui/fields"
import { DAY_LABELS, formatDuration, rowError, weeklyMinutes, type DayDraft } from "../../utils/schedule"

type Props = {
	value: DayDraft[]
	onChange: (next: DayDraft[]) => void
}

const headerCell = "pb-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500"

export function ScheduleEditor({ value, onChange }: Props) {
	// Remplace seulement les champs donnés de la ligne `index` ; les autres lignes ne changent pas
	function update(index: number, patch: Partial<DayDraft>) {
		onChange(value.map((row, i) => (i === index ? { ...row, ...patch } : row)))
	}
	return (
		<section className="rounded-2xl border border-slate-200/80 bg-white p-6 shadow-sm shadow-slate-200/50">
			<table className="w-full text-sm">
				<thead>
					<tr>
						<th className={headerCell}>Jour</th>
						<th className={headerCell}>Début</th>
						<th className={headerCell}>Fin</th>
						<th className={headerCell}>Pause</th>
					</tr>
				</thead>
				<tbody>
					{value.map((row, i) => {
						const label = DAY_LABELS[row.dayOfWeek]
						const error = rowError(row)
						return (
							<Fragment key={row.dayOfWeek}>
								<tr>
									<td className="py-1.5 pr-3">
										<label className="flex items-center gap-2 font-medium text-slate-900">
											<input
												type="checkbox"
												checked={row.worked}
												onChange={(e) => update(i, { worked: e.target.checked })}
												className="size-4 accent-sky-600"
											/>
											{label}
										</label>
									</td>
									<td className="py-1.5 pr-3">
										<input
											type="time"
											aria-label={`Début ${label}`}
											value={row.startTime}
											disabled={!row.worked}
											onChange={(e) => update(i, { startTime: e.target.value })}
											className={inputClass}
										/>
									</td>
									<td className="py-1.5 pr-3">
										<input
											type="time"
											aria-label={`Fin ${label}`}
											value={row.endTime}
											disabled={!row.worked}
											onChange={(e) => update(i, { endTime: e.target.value })}
											className={inputClass}
										/>
									</td>
									<td className="w-24 py-1.5">
										<input
											type="number"
											aria-label={`Pause ${label}`}
											min={0}
											step={5}
											value={row.breakMinutes}
											disabled={!row.worked}
											onChange={(e) => update(i, { breakMinutes: Number(e.target.value) })}
											className={inputClass}
										/>
									</td>
								</tr>
								{error && (
									<tr>
										<td colSpan={4} className="pb-2 text-xs text-red-600">
											{label} : {error}
										</td>
									</tr>
								)}
							</Fragment>
						)
					})}
				</tbody>
			</table>
			<p className="mt-4 border-t border-slate-200 pt-4 text-sm text-slate-600">
				Total : <strong className="text-slate-900">{formatDuration(weeklyMinutes(value))}</strong> par semaine
			</p>
		</section>
	)
}