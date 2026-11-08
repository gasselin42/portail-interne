import { OutsideHoursReason, type MeetingConflictResponse, type OutsideHoursReasonId } from "../../api/meetings"

const REASON_LABELS: Record<OutsideHoursReasonId, string> = {
	[OutsideHoursReason.NotWorking]: "jour non travaillé",
	[OutsideHoursReason.Holiday]: "jour férié",
	[OutsideHoursReason.OutsideHours]: "hors des heures de travail",
	[OutsideHoursReason.Absence]: "absence prévue",
}

export function MeetingConflictPanel({ conflict, myId, isEditing }: {conflict: MeetingConflictResponse, myId: number | undefined, isEditing: boolean}) {
	function personName(employeeId: number, firstName: string, lastName: string): string {
		return employeeId === myId ? "Toi" : (firstName + " " + lastName)
	}
	
	return (
		<div role="status" className="mt-2 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
			<p className="font-semibold">Avant de {isEditing ? "modifier" : "créer"} cet événement</p>
			{conflict.conflicts.length > 0 &&
				<>
					<p className="mt-3">Déjà un événement à cette heure :</p>
					<ul className="mt-1 list-disc space-y-0.5 pl-5">
						{conflict.conflicts.map((c) => (
							<li key={c.employeeId}>
								{personName(c.employeeId, c.firstName, c.lastName)}
								{" : "}
								{c.withTeamMeeting ? <strong>un meeting d'équipe</strong> : "un autre événement"}
							</li>
						))}
					</ul>
				</>
			}
			{conflict.outsideHours.length > 0 &&
				<>
					<p className="mt-3">En dehors de l'horaire de travail :</p>
					<ul className="mt-1 list-disc space-y-0.5 pl-5">
						{conflict.outsideHours.map((c) => (
							<li key={c.employeeId}>
								{personName(c.employeeId, c.firstName, c.lastName)}
								{" : "}
								{REASON_LABELS[c.reason]}
							</li>
						))}
					</ul>
				</>
			}
			<p className="mt-3">Tu peux changer le créneau ou {isEditing ? "modifier" : "créer"} l'événement quand même.</p>
		</div>
	)
}