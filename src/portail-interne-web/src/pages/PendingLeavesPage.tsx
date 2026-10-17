import { useState, useRef } from "react"
import { ErrorBanner } from "../components/feedback/Banner"
import { listLeavesPending, LEAVE_TYPE_LABELS } from "../api/leaves"
import { DateText } from "../components/common/DateText"
import { ConfirmDialog } from "../components/dialogs/ConfirmDialog"
import { PageHeader } from "../components/common/PageHeader"
import { useAsync } from "../hooks/useAsync"
import { useLeaveReview } from "../hooks/useLeaveReview"
import { LeaveReviewButtons } from "../components/leaves/LeaveReviewButtons"
import { LeaveDaysNote } from "../components/leaves/LeaveDaysNote"

export function PendingLeavesPage() {
	const { data, loading, error, retry } = useAsync((signal) => listLeavesPending(signal), [])
	const [reviewingIds, setReviewingIds] = useState<number[]>([])
	const leaves = (data ?? []).filter((l) => !reviewingIds.includes(l.id))
	const contentRef = useRef<HTMLElement>(null)
	
	const review = useLeaveReview({
		onReviewed: (leave) => setReviewingIds((ids) => [...ids, leave.id]),
		onFailed: retry,
		focusAfterReview: () => contentRef.current?.focus()
	})

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				eyebrow="Congés"
				title="À approuver"
				description="Vois et accepte ou refuse les demandes de congés."
			/>

			{review.error && (
				<ErrorBanner
					message={review.error}
					dismissible
					onDismiss={review.dismissError}
				/>
			)}

			<section ref={contentRef} tabIndex={-1} className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50 outline-none">
				{error ? (
					<div className="p-4">
						<ErrorBanner message={error} onRetry={retry} />
					</div>
				) : loading ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des congés...</p>
				) : leaves.length === 0 ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Aucune demande en attente</p>
				) : (
					<div className="overflow-x-auto">
						<table className="w-full min-w-200 table-fixed border-collapse text-left">
							<thead>
								<tr className="border-b border-slate-200 bg-slate-50/80">
									<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
										Employé
									</th>
									<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
										Début
									</th>
									<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
										Fin
									</th>
									<th className="w-[15%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
										Type
									</th>
									<th className="px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
										Motif
									</th>
									<th className="w-24 px-3 py-3.5">
										<span className="sr-only">Actions</span>
									</th>
								</tr>
							</thead>
							<tbody>
								{leaves.map((leave) => (
									<tr
										key={leave.id}
										className="border-b border-slate-100 transition last:border-b-0 hover:bg-slate-50/70"
									>
										<td className="px-5 py-3.5 text-sm font-medium text-slate-900">
											{leave.employeeFirstName} {leave.employeeLastName}
										</td>
										<td className="px-5 py-3.5 text-sm font-semibold text-slate-700">
											<DateText value={leave.startDate} />
										</td>
										<td className="px-5 py-3.5 text-sm font-semibold text-slate-700">
											<DateText value={leave.endDate} />
										</td>
										<td className="px-5 py-3.5 text-sm text-slate-600">
											{LEAVE_TYPE_LABELS[leave.type]}
											<LeaveDaysNote workingDays={leave.workingDays} unpaidDates={leave.unpaidDates} />
										</td>
										<td className="px-5 py-3.5 text-sm wrap-break-word whitespace-normal text-slate-700">
											{leave.reason || "-"}
										</td>
										<td className="px-3 py-3.5 text-center">
											<LeaveReviewButtons
												leave={leave}
												disabled={review.reviewingId === leave.id}
												onApprove={() => review.approve(leave)}
												onDecline={() => review.requestDecline(leave)}
											/>
										</td>
									</tr>
								))}
							</tbody>
						</table>
					</div>
				)}
			</section>
			<p role="status" className="sr-only">{review.announcement}</p>
			<ConfirmDialog {...review.declineDialog} />
		</div>
	)
}
