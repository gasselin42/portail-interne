import { CheckCircleOutlineOutlined, HighlightOff } from "@mui/icons-material";
import { rowIconDangerButton, rowIconSuccessButton } from "../../ui/buttons";
import type { LeaveRequestResponse } from "../../api/leaves";

type Props = {
	leave: LeaveRequestResponse
	disabled: boolean
	onApprove: () => void
	onDecline: () => void
}

export function LeaveReviewButtons({ leave, disabled, onApprove, onDecline }: Props) {
	return (
		<div className="inline-flex gap-1">
			<button
				type="button"
				onClick={onApprove}
				disabled={disabled}
				title="Accepter"
				aria-label={`Accepter la demande de ${leave.employeeFirstName} ${leave.employeeLastName}`}
				className={rowIconSuccessButton}
			>
				<CheckCircleOutlineOutlined fontSize="small" />
			</button>
			<button
				type="button"
				onClick={onDecline}
				disabled={disabled}
				title="Refuser"
				aria-label={`Refuser la demande de ${leave.employeeFirstName} ${leave.employeeLastName}`}
				className={rowIconDangerButton}
			>
				<HighlightOff fontSize="small" />
			</button>
		</div>
	)
}