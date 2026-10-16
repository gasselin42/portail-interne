import { useState, useRef } from "react"
import { Link } from "react-router-dom"
import { LEAVE_TYPE_LABELS, listLeavesPending } from "../../api/leaves"
import { useAsync } from "../../hooks/useAsync"
import { formatRange } from "../../utils/date"
import { ErrorBanner } from "../feedback/Banner"
import { Widget } from "./Widget"
import { textLink } from "../../ui/buttons"
import { useLeaveReview } from "../../hooks/useLeaveReview"
import { LeaveReviewButtons } from "../leaves/LeaveReviewButtons"
import { ConfirmDialog } from "../dialogs/ConfirmDialog"
import { useMediaQuery } from "../../hooks/useMediaQuery"

export function PendingApprovalsWidget() {
	const { data, error, loading, retry} = useAsync((signal) => listLeavesPending(signal), [])
	const [reviewedIds, setReviewedIds] = useState<number[]>([])
	const [leavingIds, setLeavingIds] = useState<number[]>([])
	const contentRef = useRef<HTMLDivElement>(null)
	
	const pending = (data ?? []).filter((l) => !reviewedIds.includes(l.id))
	const remaining = pending.filter((l) => !leavingIds.includes(l.id))
	const shownIds = remaining.slice(0, 3).map((l) => l.id)
	const shown = pending.filter((l) => leavingIds.includes(l.id) || shownIds.includes(l.id))
	const others = remaining.length - shownIds.length

	const reduceMotion = useMediaQuery("(prefers-reduced-motion: reduce)")

	const review = useLeaveReview({
		onReviewed: (leave) =>
			reduceMotion
				? setReviewedIds((ids) => [...ids, leave.id])
				: setLeavingIds((ids) => [...ids, leave.id]),
		onFailed: retry,
		focusAfterReview: () => contentRef.current?.focus()
	})

	const animateEntry = leavingIds.length > 0 || reviewedIds.length > 0

	return (
		<Widget title="À approuver" link={{ to: "/approvals", label: "Ouvrir la file" }}>
			<div ref={contentRef} tabIndex={-1} className="outline-none">
				{review.error && (
					<ErrorBanner
						message={review.error}
						dismissible
						onDismiss={review.dismissError}
					/>
				)}
				
				{error ? (
					<ErrorBanner message={error} onRetry={retry} />
				) : loading ? (
					<p role="status" className="text-center text-sm text-slate-500">
						Chargement…
					</p>
				) : pending.length === 0 ? (
					<p className="text-sm text-slate-500 py-2 text-center">Aucune demande en attente ✓</p>
				) : (
					<>
						<ul>
							{shown.map((leave) => {
								const leaving = leavingIds.includes(leave.id)

								return (
									<li 
										key={leave.id}
										onTransitionEnd={(e) => {
											// Une seule fois par ligne : la fin du repli, sur le <li> lui-même (pas un enfant)
											if (leaving && e.target === e.currentTarget && e.propertyName === "grid-template-rows") {
												setReviewedIds((ids) => [...ids, leave.id])
											}
										}}
										className={`group grid transition-[grid-template-rows,opacity] duration-300 ease-out motion-reduce:transition-none ${
												leaving ? "grid-rows-[0fr] opacity-0" : "grid-rows-[1fr]"
											} ${animateEntry ? "starting:grid-rows-[0fr] starting:opacity-0" : ""}`}
									>
										<div className="min-h-0 overflow-hidden">
											<div className="flex items-center justify-between py-2 gap-3 border-t border-slate-100 group-first:border-t-0">
												<div className="min-w-0">
													<p className="text-sm font-medium text-slate-900 truncate">{leave.employeeFirstName} {leave.employeeLastName}</p>
													<p className="text-xs text-slate-500 truncate">{LEAVE_TYPE_LABELS[leave.type]} · {formatRange(leave.startDate, leave.endDate)}</p>
												</div>
												<LeaveReviewButtons
													leave={leave}
													disabled={review.reviewingId === leave.id || leavingIds.includes(leave.id)}
													onApprove={() => review.approve(leave)}
													onDecline={() => review.requestDecline(leave)}
												/>
											</div>
										</div>
									</li>
								)
							})}
						</ul>
						{others > 0 && (
							<div className="mt-2 text-center">
								<Link to="/approvals" className={textLink}>+{others} {others > 1 ? "autres demandes" : "autre demande"}</Link>
							</div>
						)}
					</>
				)}
				<p role="status" className="sr-only">{review.announcement}</p>
				<ConfirmDialog {...review.declineDialog} />
			</div>
		</Widget>
	)
}