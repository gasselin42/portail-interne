import { useState } from "react"
import { Navigate, useParams, Link } from "react-router-dom"
import { useSession } from "../session/useSession"
import { listSites } from "../api/sites"
import { useAsync } from "../hooks/useAsync"
import { isGlobalAdmin } from "../session/permission"
import { ErrorBanner } from "../components/feedback/Banner"
import { primaryButton, secondaryButton } from "../ui/buttons"
import { PageHeader } from "../components/common/PageHeader"
import { SiteDialog } from "../components/sites/SiteDialog"
import { HolidaysSection } from "../components/sites/HolidaysSection"
import { TeamVisibilitySection } from "../components/sites/TeamVisibilitySection"

export function SitePage() {
	const [open, setOpen] = useState(false)

	const { me } = useSession()

	const { id } = useParams()
	const siteId = id ? Number(id) : null

	const { data, loading, error, retry } = useAsync((signal) => listSites(signal), [])

	const globalAdmin = isGlobalAdmin(me)
	if (!globalAdmin && siteId !== me?.siteId) {
		return <Navigate to={`/admin/sites/${me?.siteId}`} replace />
	}

	const site = data?.find((s) => s.id === siteId)

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			{error ? (
				<div className="p-4">
					<ErrorBanner message={error} onRetry={retry} />
				</div>
			) : loading ? (
				<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des sites…</p>
			) : !site ? (
				<div className="flex flex-col px-6 py-12 gap-3 items-center">
					<p className="text-center text-sm font-medium text-slate-500">Site introuvable</p>
					<Link to="/admin/sites" className={`${secondaryButton}`}>Tous les sites</Link>
				</div>
			) : (
				<>
					<PageHeader
						eyebrow="Site"
						title={site.name}
						description={`Fuseau horaire : ${site.timeZoneId}`}
						actions={
							globalAdmin && (
								<>
									<Link to="/admin/sites" className={secondaryButton}>
										Tous les sites
									</Link>
									<button type="button" className={primaryButton} onClick={() => setOpen(true)}>
										Modifier
									</button>
								</>
							)
						}
					/>

					<HolidaysSection siteId={site.id} />
					<TeamVisibilitySection site={site} />

					<SiteDialog
						open={open}
						site={site}
						onClose={() => setOpen(false)}
						onSaved={() => {
							setOpen(false)
							retry()
						}}
					/>
				</>
			)
		}
		</div>
	)
}