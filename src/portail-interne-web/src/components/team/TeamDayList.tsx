import type { TeamMember } from "../../api/team"
import { useSession } from "../../session/useSession"
import { TeamDayCell } from "./TeamDayCell"
import { TeamMemberLabel } from "./TeamMemberLabel"

type Props = {
	day: string
	members: TeamMember[]
}

export function TeamDayList({ day, members }: Props) {
	const { me } = useSession()

	return (
		<div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
			<ul>
				{members.map((m) => {
					const effectiveDay = m.days.find((d) => d.date === day)
					return (
						<li key={m.employeeId} className={`flex justify-between items-center gap-3 px-4 py-3 border-b border-slate-100 last:border-0 ${m.employeeId === me?.employeeId ? "bg-sky-100/40" : ""}`}>
							<TeamMemberLabel member={m} />
							<div className="text-right">
								{effectiveDay && <TeamDayCell day={effectiveDay} />}
							</div>
						</li>
					)
				})}
			</ul>
		</div>
	)
}