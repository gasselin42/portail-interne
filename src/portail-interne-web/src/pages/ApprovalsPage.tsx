import { useState, useRef } from "react"
import { ErrorBanner } from "../components/feedback/Banner"
import { ConfirmDialog } from "../components/dialogs/ConfirmDialog"
import { PageHeader } from "../components/common/PageHeader"
import { useAsync } from "../hooks/useAsync"
import { useApprovalReview } from "../hooks/useApprovalReview"
import { ApprovalButtons } from "../components/approvals/ApprovalButtons"
import { LeaveDaysNote } from "../components/leaves/LeaveDaysNote"
import { approvalLabel, approvalWhen, loadApprovals, type ApprovalItem } from "../utils/approvals"

const KIND_BADGE: Record<ApprovalItem["kind"], string> = {
	leave: "bg-sky-50 text-sky-700",
	adjustment: "bg-violet-50 text-violet-700",
	absence: "bg-amber-50 text-amber-800",
}

export function ApprovalsPage() {
	const { data, loading, error, retry } = useAsync((signal) => loadApprovals(signal), [])
	const [reviewedKeys, setReviewedKeys] = useState<string[]>([])
	const items = (data ?? []).filter((item) => !reviewedKeys.includes(item.key))
	const contentRef = useRef<HTMLElement>(null)

	const review = useApprovalReview({
		onReviewed: (item) => setReviewedKeys((keys) => [...keys, item.key]),
		onFailed: retry,
		focusAfterReview: () => contentRef.current?.focus(),
	})

	return (
		<div className="mx-auto max-w-6xl px-6 py-10">
			<PageHeader
				eyebrow="Équipe"
				title="À approuver"
				description="Congés, ajustements d'horaire et absences en attente de ta décision."
			/>

			{review.error && (
				<ErrorBanner message={review.error} dismissible onDismiss={review.dismissError} />
			)}

			<section
				ref={contentRef}
				tabIndex={-1}
				className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm shadow-slate-200/50 outline-none"
			>
				{error ? (
					<div className="p-4">
						<ErrorBanner message={error} onRetry={retry} />
					</div>
				) : loading ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Chargement des demandes…</p>
				) : items.length === 0 ? (
					<p className="px-6 py-12 text-center text-sm text-slate-500">Aucune demande en attente</p>
				) : (
					<div className="overflow-x-auto">
						<table className="w-full min-w-200 table-fixed border-collapse text-left">
							<thead>
								<tr className="border-b border-slate-200 bg-slate-50/80">
									<th className="w-[20%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
										Employé
									</th>
									<th className="w-[20%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
										Type
									</th>
									<th className="w-[20%] px-5 py-3.5 text-xs font-semibold tracking-wide text-slate-500 uppercase">
										Quand
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
								{items.map((item) => (
									<tr
										key={item.key}
										className="border-b border-slate-100 transition last:border-b-0 hover:bg-slate-50/70"
									>
										<td className="px-5 py-3.5 text-sm font-medium text-slate-900">
											{item.employeeName}
										</td>
										<td className="px-5 py-3.5 text-sm text-slate-600">
											<span className={`rounded-full px-2 py-0.5 text-xs font-medium ${KIND_BADGE[item.kind]}`}>{approvalLabel(item)}</span>
										</td>
										<td className="px-5 py-3.5 text-sm font-semibold text-slate-700">
											<p>{approvalWhen(item)}</p>
											{item.kind === "leave" ? (
												<LeaveDaysNote workingDays={item.leave.workingDays} unpaidDates={item.leave.unpaidDates} />
											) : item.kind === "adjustment" ? (
												<span className="block text-xs text-slate-500">Pause {item.adjustment.breakMinutes} minutes</span>
											) : null}
											{!item.canApprove && (
												<span className="block text-xs text-amber-800">Trop tard pour approuver</span>
											)}
										</td>
										<td className="px-5 py-3.5 text-sm wrap-break-word whitespace-normal text-slate-700">
											{item.reason || "-"}
										</td>
										<td className="px-3 py-3.5 text-center">
											<ApprovalButtons
												item={item}
												disabled={review.reviewingKey === item.key}
												onApprove={() => review.approve(item)}
												onDecline={() => review.requestDecline(item)}
											/>
										</td>
									</tr>
								))}
							</tbody>
						</table>
					</div>
				)}
			</section>
			<p role="status" className="sr-only">
				{review.announcement}
			</p>
			<ConfirmDialog {...review.declineDialog} />
		</div>
	)
}
