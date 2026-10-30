import { useSearchParams } from "react-router-dom"
import { PageHeader } from "../components/common/PageHeader"
import { addDays, formatLongDate, formatRange, isIsoDate, iterateDays, startOfWeek, todayIso } from "../utils/date"
import { useSession } from "../session/useSession"
import { isGlobalAdmin } from "../session/permission"
import { useAsync } from "../hooks/useAsync"
import { getTeamWeek } from "../api/team"
import { iconButton, secondaryButton } from "../ui/buttons"
import { ChevronLeft, ChevronRight } from "@mui/icons-material"
import { listSites } from "../api/sites"
import { inputClass } from "../ui/fields"
import { ErrorBanner } from "../components/feedback/Banner"
import { TeamWeekGrid } from "../components/team/TeamWeekGrid"
import { useMediaQuery } from "../hooks/useMediaQuery"
import { TeamDayList } from "../components/team/TeamDayList"

export function TeamPage() {
	const [searchParams, setSearchParams] = useSearchParams()
	const { me } = useSession()

	const isDesktop = useMediaQuery("(min-width: 48rem)")

	const today = todayIso()

	let date = searchParams.get("date")
	if (date === null || !isIsoDate(date)) date = today

	const from = startOfWeek(date)
	const to = addDays(from, 6)
	const days = iterateDays(from, 7)
	const containsToday = isDesktop ? from <= today && today <= to : date === today

	let siteId: number | undefined = Number(searchParams.get("site"))
	if (!(isGlobalAdmin(me) && Number.isInteger(siteId) && siteId > 0)) siteId = undefined

	const {
		data: team,
		error: teamError,
		loading: teamLoading,
		retry: teamRetry,
	} = useAsync((signal) => getTeamWeek(from, to, siteId, signal), [from, to, siteId])

	const members = team ?? []

	const {
		data: sites,
		error: sitesError,
		retry: sitesRetry,
	} = useAsync((signal) => (isGlobalAdmin(me) ? listSites(signal) : Promise.resolve([])), [])

	function goTo(newDate: string) {
		const params: Record<string, string> = { date: newDate }
		if (siteId !== undefined) params.site = String(siteId)
		setSearchParams(params)
	}

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				eyebrow="Planning"
				title="Équipe"
				description="Qui travaille quand, cette semaine."
			/>

			<div className="mb-4 flex flex-wrap items-center justify-between gap-3">
				<h2 aria-live="polite" className="text-lg font-semibold text-slate-900">
					{isDesktop ? formatRange(from, to) : formatLongDate(date)}
				</h2>
				<div className="flex items-center gap-1">
					<button
						type="button"
						onClick={() => goTo(addDays(date, isDesktop ? -7 : -1))}
						aria-label={isDesktop ? "Semaine précédente" : "Jour précédent"}
						className={iconButton}
					>
						<ChevronLeft fontSize="large" />
					</button>
					<button
						type="button"
						onClick={() => goTo(addDays(date, isDesktop ? 7 : 1))}
						aria-label={isDesktop ? "Semaine suivante" : "Jour suivant"}
						className={iconButton}
					>
						<ChevronRight fontSize="large" />
					</button>
					<button
						type="button"
						disabled={containsToday}
						onClick={() => goTo(today)}
						className={secondaryButton}
					>
						Aujourd'hui
					</button>
					{sites && sites.length > 0 &&
						<select
							aria-label="Site"
							value={siteId ?? me?.siteId}
							onChange={(e) => {
								setSearchParams({ site: e.target.value, date: date })
							}}
							className={inputClass}
						>
							{sites.map((site) => (
								<option key={site.id} value={site.id}>
									{site.name}
								</option>
							))}
						</select>
					}
				</div>
			</div>

			{sitesError &&
				<ErrorBanner message={sitesError} onRetry={sitesRetry} />
			}

			<div className="mb-3 min-h-6">
				{teamError ? (
					<ErrorBanner message={teamError} onRetry={teamRetry} />
				) : teamLoading ? (
					<p role="status" className="text-center text-sm text-slate-500">
						Chargement…
					</p>
				) : members.length === 0 ? (
					<p role="status" className="text-center text-sm text-slate-500">
						Aucun employé actif sur ce site.
					</p>
				) : (
					isDesktop
						? <TeamWeekGrid days={days} members={members} />
						: <TeamDayList day={date} members={members} />
				)}
			</div>
		</div>
	)
}
