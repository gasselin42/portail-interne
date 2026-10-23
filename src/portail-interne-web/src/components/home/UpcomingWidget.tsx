import { Widget } from "./Widget"
import { UpcomingList } from "../upcoming/UpcomingList"

export function UpcomingWidget() {
	return (
		<Widget title="À venir" link={{ to: "/leaves", label: "Voir mes congés" }}>
			<UpcomingList />
		</Widget>
	)
}