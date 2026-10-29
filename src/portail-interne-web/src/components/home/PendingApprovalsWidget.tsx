import { useState, useRef } from "react"
import { Link } from "react-router-dom"
import { useAsync } from "../../hooks/useAsync"
import { ErrorBanner } from "../feedback/Banner"
import { Widget } from "./Widget"
import { textLink } from "../../ui/buttons"
import { useApprovalReview } from "../../hooks/useApprovalReview"
import { ApprovalButtons } from "../approvals/ApprovalButtons"
import { ConfirmDialog } from "../dialogs/ConfirmDialog"
import { useMediaQuery } from "../../hooks/useMediaQuery"
import { LeaveDaysNote } from "../leaves/LeaveDaysNote"
import { approvalLabel, approvalWhen, loadApprovals } from "../../utils/approvals"

export function PendingApprovalsWidget() {
	const { data, error, loading, retry } = useAsync((signal) => loadApprovals(signal), [])
	const [reviewedKeys, setReviewedKeys] = useState<string[]>([])
	const [leavingKeys, setLeavingKeys] = useState<string[]>([])
	const contentRef = useRef<HTMLDivElement>(null)

	const pending = (data ?? []).filter((item) => !reviewedKeys.includes(item.key))
	const remaining = pending.filter((item) => !leavingKeys.includes(item.key))
	const shownKeys = remaining.slice(0, 3).map((item) => item.key)
	const shown = pending.filter((item) => leavingKeys.includes(item.key) || shownKeys.includes(item.key))
	const others = remaining.length - shownKeys.length

	const reduceMotion = useMediaQuery("(prefers-reduced-motion: reduce)")

	const review = useApprovalReview({
		onReviewed: (item) =>
			reduceMotion
				? setReviewedKeys((keys) => [...keys, item.key])
				: setLeavingKeys((keys) => [...keys, item.key]),
		onFailed: retry,
		focusAfterReview: () => contentRef.current?.focus(),
	})

	const animateEntry = leavingKeys.length > 0 || reviewedKeys.length > 0

	return (
		<Widget title="À approuver" link={{ to: "/approvals", label: "Ouvrir la file" }}>
			<div ref={contentRef} tabIndex={-1} className="outline-none">
				{review.error && (
					<ErrorBanner message={review.error} dismissible onDismiss={review.dismissError} />
				)}

				{error ? (
					<ErrorBanner message={error} onRetry={retry} />
				) : loading ? (
					<p role="status" className="text-center text-sm text-slate-500">
						Chargement…
					</p>
				) : pending.length === 0 ? (
					<p className="py-2 text-center text-sm text-slate-500">Aucune demande en attente ✓</p>
				) : (
					<>
						<ul>
							{shown.map((item) => {
								const leaving = leavingKeys.includes(item.key)

								return (
									<li
										key={item.key}
										onTransitionEnd={(e) => {
											// Une seule fois par ligne : la fin du repli, sur le <li> lui-même (pas un enfant)
											if (
												leaving &&
												e.target === e.currentTarget &&
												e.propertyName === "grid-template-rows"
											) {
												setReviewedKeys((keys) => [...keys, item.key])
											}
										}}
										className={`group grid transition-[grid-template-rows,opacity] duration-300 ease-out motion-reduce:transition-none ${
											leaving ? "grid-rows-[0fr] opacity-0" : "grid-rows-[1fr]"
										} ${animateEntry ? "starting:grid-rows-[0fr] starting:opacity-0" : ""}`}
									>
										<div className="min-h-0 overflow-hidden">
											<div className="flex items-center justify-between gap-3 border-t border-slate-100 py-2 group-first:border-t-0">
												<div className="min-w-0">
													<p className="truncate text-sm font-medium text-slate-900">
														{item.employeeName}
													</p>
													<p className="truncate text-xs text-slate-500">
														{approvalLabel(item)} · {approvalWhen(item)}
													</p>
													{item.kind === "leave" &&
														<p className="truncate">
															<LeaveDaysNote
																workingDays={item.leave.workingDays}
																unpaidDates={item.leave.unpaidDates}
																compact
															/>
														</p>
													}
													{!item.canApprove && (
														<span className="truncate text-xs text-amber-800">Trop tard pour approuver</span>
													)}
												</div>
												<ApprovalButtons
													item={item}
													disabled={
														review.reviewingKey === item.key || leavingKeys.includes(item.key)
													}
													onApprove={() => review.approve(item)}
													onDecline={() => review.requestDecline(item)}
												/>
											</div>
										</div>
									</li>
								)
							})}
						</ul>
						{others > 0 && (
							<div className="mt-2 text-center">
								<Link to="/approvals" className={textLink}>
									+{others} {others > 1 ? "autres demandes" : "autre demande"}
								</Link>
							</div>
						)}
					</>
				)}
				<p role="status" className="sr-only">
					{review.announcement}
				</p>
				<ConfirmDialog {...review.declineDialog} />
			</div>
		</Widget>
	)
}
