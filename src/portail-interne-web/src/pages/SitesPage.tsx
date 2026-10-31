import { useState } from "react"
import { Navigate, Link } from "react-router-dom"
import { isGlobalAdmin } from "../session/permission"
import { useSession } from "../session/useSession"
import { useAsync } from "../hooks/useAsync"
import { listSites, TEAM_VISIBILITY_LABELS } from "../api/sites"
import { PageHeader } from "../components/common/PageHeader"
import { secondaryButton } from "../ui/buttons"
import { ErrorBanner } from "../components/feedback/Banner"
import { SiteDialog } from "../components/sites/SiteDialog"

export function SitesPage() {
	const [open, setOpen] = useState(false)

	const { me } = useSession()

	const { data, loading, error, retry } = useAsync((signal) => listSites(signal), [])

	if (!isGlobalAdmin(me)) {
		return <Navigate to={`/admin/sites/${me?.siteId}`} replace />
	}

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				title="Sites"
				actions={
					<button type="button" className={secondaryButton} onClick={() => setOpen(true)}>
						Nouveau site
					</button>
				}
			/>

			<section className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
				{error ? (
					<div className="p-4">
						<ErrorBanner message={error} onRetry={retry} />
					</div>
				) : loading ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des sites…</p>
				) : (
					<ul>
						{(data ?? []).map((site) => (
							<li key={site.id} className="border-b border-slate-100 last:border-0">
								<Link
									to={`/admin/sites/${site.id}`}
									className="flex items-center justify-between gap-3 px-5 py-4 hover:bg-slate-50"
								>
									<div>
										<p className="text-sm font-medium text-slate-900">{site.name}</p>
										<p className="text-xs text-slate-500">{site.timeZoneId}</p>
									</div>
									<p className="text-xs text-slate-600">
										Visibilité : {TEAM_VISIBILITY_LABELS[site.teamVisibility]}
									</p>
								</Link>
							</li>
						))}
					</ul>
				)}
			</section>
			<SiteDialog open={open} site={null} onClose={() => setOpen(false)} onSaved={() => { setOpen(false); retry() }} />
		</div>
	)
}