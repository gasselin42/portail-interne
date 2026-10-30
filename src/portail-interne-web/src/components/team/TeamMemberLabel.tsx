import type { TeamMember } from "../../api/team"
import { Avatar } from "../employees/Avatar"

export function TeamMemberLabel({ member }: { member: TeamMember }) {
	return (
		<div className="flex items-center gap-3">
			<Avatar size="sm" firstName={member.firstName} lastName={member.lastName} />
			<div>
				<span className="text-sm font-medium text-slate-900">{member.firstName} {member.lastName}</span>
				<span className="block text-xs text-slate-500">{member.jobTitle}</span>
			</div>
		</div>
	)
}