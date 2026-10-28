import { useSearchParams } from "react-router-dom"
import { primaryButton } from "../ui/buttons"
import { PageHeader } from "../components/common/PageHeader"
import { LeaveRequests } from "../components/requests/LeaveRequests"
import { SegmentedControl } from "../components/common/SegmentedControl"
import { AdjustmentRequests } from "../components/requests/AdjustmentRequests"
import { AbsenceRequests } from "../components/requests/AbsenceRequests"

type Tab = "conges" | "ajustements" | "absences"

const TABS: { value: Tab, label: string }[] = [
	{ value: "conges", label: "Congés" },
	{ value: "ajustements", label: "Ajustements" },
	{ value: "absences", label: "Absences" },
]

export function LeavesPage() {
	const [searchParams, setSearchParams] = useSearchParams()
	const newOpen = searchParams.get("nouvelle") === "1"

	const tabParam = searchParams.get("onglet")

	const tab: Tab =
		tabParam === "ajustements" || tabParam === "absences"
			? tabParam
			: "conges"

	const openNew = () => setSearchParams({ onglet: tab, nouvelle: "1" })
	const closeNew = () => setSearchParams({ onglet: tab })

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				eyebrow="Demandes"
				title="Mes demandes"
				description="Tes congés, ajustements d'horaire et absences prévues."
				actions={
					<button type="button" onClick={openNew} className={primaryButton}>
						Nouvelle demande
					</button>
				}
			/>
			<div className="mb-6">
				<SegmentedControl
					label="Type de demande"
					options={TABS}
					value={tab}
					onChange={(t) => setSearchParams({ onglet: t })}
				/>
			</div>
			{tab === "conges" && <LeaveRequests newOpen={newOpen} onNewClose={closeNew} />}
			{tab === "ajustements" && <AdjustmentRequests newOpen={newOpen} onNewClose={closeNew} />}
			{tab === "absences" && <AbsenceRequests newOpen={newOpen} onNewClose={closeNew} />}
		</div>
	)
}
