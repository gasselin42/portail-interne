import { useState } from "react"
import { useSearchParams } from "react-router-dom"
import { formatDate, formatLongDate, todayIso } from "../../utils/date"
import { useAsync } from "../../hooks/useAsync"
import { deleteHoliday, listHolidays, type Holiday } from "../../api/holidays"
import { iconButton, rowIconButton, rowIconDangerButton, secondaryButton } from "../../ui/buttons"
import { ChevronLeft, ChevronRight, DeleteOutlined, EditOutlined } from "@mui/icons-material"
import { sectionTitle } from "../../ui/fields"
import { ErrorBanner } from "../feedback/Banner"
import { HolidayDialog } from "./HolidayDialog"
import { ConfirmDialog } from "../dialogs/ConfirmDialog"

export function HolidaysSection({ siteId }: { siteId: number }) {
	const [searchParams, setSearchParams] = useSearchParams()

	let year = Number(searchParams.get("annee"))
	if (!Number.isInteger(year) || year < 2000 || year > 2100)
		year = Number(todayIso().slice(0, 4))

	const { data, error, loading, retry } = useAsync((signal) => listHolidays(siteId, year, signal), [siteId, year])

	const holidays = data ?? []
	const [creating, setCreating] = useState(false)
	const [editing, setEditing] = useState<Holiday | null>(null)
	const [deleting, setDeleting] = useState<Holiday | null>(null)
	const [deleteError, setDeleteError] = useState<string | null>(null)

	return (
		<section className="mt-8 overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50">
			<div className=" px-4 py-3">
				<div className="items-center flex flex-row justify-between">
					<h2 className={sectionTitle}>Jours fériés</h2>
					<div className="flex items-center gap-1">
						<button
							type="button"
							onClick={() => setSearchParams({ annee: String(year - 1) })}
							aria-label="Année précédente"
							className={iconButton}
						>
							<ChevronLeft fontSize="large" />
						</button>
						<p className="font-semibold text-base">{year}</p>
						<button
							type="button"
							onClick={() => setSearchParams({ annee: String(year + 1) })}
							aria-label="Année suivante"
							className={iconButton}
						>
							<ChevronRight fontSize="large" />
						</button>
						<button
							type="button"
							disabled={year < Number(todayIso().slice(0, 4))}
							onClick={() => setCreating(true)}
							className={secondaryButton}
						>
							Ajouter
						</button>
					</div>
				</div>
				{deleteError &&
					<div className="p-4">
						<ErrorBanner message={deleteError} onDismiss={() => setDeleteError(null)} dismissible />
					</div>
				}
				{error ? (
					<div className="p-4">
						<ErrorBanner message={error} onRetry={retry} />
					</div>
				) : loading ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des fériés…</p>
				) : holidays.length === 0 ? (
					<div className="flex flex-col px-6 py-12 gap-3 items-center">
						<p className="text-center text-sm font-medium text-slate-500">Aucun jour férié en {year}</p>
					</div>
				) : (
					<ul className="mt-2">
						{holidays.map((holiday) => (
							<li
								key={holiday.id}
								className="flex items-center justify-between gap-3 border-b border-slate-100 py-3 last:border-0"
							>
								<div>
									<p className="text-sm font-medium text-slate-900">{holiday.name}</p>
									<time dateTime={holiday.date} className="text-xs text-slate-500">
										{formatLongDate(holiday.date)}
									</time>
								</div>
								{holiday.isEditable ? (
									<div className="flex items-center gap-1">
										<button
											type="button"
											onClick={() => setEditing(holiday)}
											aria-label={`Modifier ${holiday.name}`}
											title="Modifier"
											className={rowIconButton}
										>
											<EditOutlined fontSize="small" />
										</button>
										<button
											type="button"
											onClick={() => setDeleting(holiday)}
											aria-label={`Supprimer ${holiday.name}`}
											title="Supprimer"
											className={rowIconDangerButton}
										>
											<DeleteOutlined fontSize="small" />
										</button>
									</div>
								) : (
									<span className="text-xs text-slate-400">Passé</span>
								)}
							</li>
						))}
					</ul>
				)}
			</div>
			<HolidayDialog
				open={creating || editing !== null}
				siteId={siteId}
				holiday={editing}
				onClose={() => {setCreating(false); setEditing(null)}}
				onSaved={(selectedDate) => {
					setCreating(false)
					setEditing(null)
					const selectedYear = selectedDate.slice(0, 4)
					if (selectedYear !== String(year))
						setSearchParams({ annee: selectedYear })
					else
						retry()
				}}
			/>
			{deleting &&
				<ConfirmDialog
					title="Supprimer ce férié ?"
					message={`Supprimer le férié ${deleting.name} du ${formatDate(deleting.date)}`}
					confirmLabel="Supprimer"
					busyLabel="Suppression…"
					destructive
					open={deleting !== null}
					onClose={() => setDeleting(null)}
					onConfirm={async () => {
						setDeleteError(null)
						try {
							await deleteHoliday(siteId, deleting.id)
							setDeleting(null)
							retry()
						} catch (e) {
							setDeleting(null)
							setDeleteError(e instanceof Error ? e.message : "Impossible de supprimer le férié.")
						}
					}}
				/>
			}
		</section>
	)
}