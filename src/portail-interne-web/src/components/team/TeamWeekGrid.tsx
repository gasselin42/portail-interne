import type { TeamMember } from "../../api/team"
import { useSession } from "../../session/useSession"
import { formatDayHeader, todayIso } from "../../utils/date"
import { TeamDayCell } from "./TeamDayCell"
import { TeamMemberLabel } from "./TeamMemberLabel"

export function TeamWeekGrid({ days, members }: { days: string[], members: TeamMember[]}) {
	const today = todayIso()
	const { me } = useSession()

	return (
		<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
			<div className="overflow-x-auto">
				<table className="min-w-200 table-fixed w-full">
					<thead>
						<tr className="border-b border-slate-200 bg-slate-50/80">
							<th className="w-56 px-4 py-3.5 text-left text-xs font-semibold tracking-wide text-slate-500 uppercase">Employé</th>
							{days.map((day) => (
								<th
									key={day}
									aria-current={day === today ? "date" : undefined}
									className="group px-3 py-3.5 text-left"
								>
									<time
										dateTime={day}
										className="text-sm font-medium text-slate-700 group-aria-[current=date]:font-semibold group-aria-[current=date]:text-sky-700"	
									>
										{formatDayHeader(day)}
									</time>
								</th>
							))}
						</tr>
					</thead>
					<tbody>
						{members.map((m) => (
							<tr key={m.employeeId} className={`${m.employeeId === me?.employeeId ? "bg-sky-100/40" : ""} border-b border-slate-100 last:border-0`}>
								<th scope="row" className="text-left font-normal px-4 py-3">
									<TeamMemberLabel member={m} />
								</th>
								{days.map((day) => {
									const weekDay = m.days.find((d) => d.date === day)
									return (
										<td key={day} className={`px-3 py-3 border-l border-slate-100 ${day === today ? "bg-sky-100/40" : ""}`}>
											{weekDay && <TeamDayCell day={weekDay} />}
										</td>
									)
								})}
							</tr>
						))}
					</tbody>
				</table>
			</div>
		</div>
	)
}