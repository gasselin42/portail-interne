import { CheckCircleOutlineOutlined, HighlightOff } from "@mui/icons-material";
import { rowIconDangerButton, rowIconSuccessButton } from "../../ui/buttons";
import { approvalOf, type ApprovalItem } from "../../utils/approvals";

type Props = {
	item: ApprovalItem
	disabled: boolean
	onApprove: () => void
	onDecline: () => void
}

export function ApprovalButtons({ item, disabled, onApprove, onDecline }: Props) {
	return (
		<div className="inline-flex gap-1">
			<button
				type="button"
				onClick={onApprove}
				disabled={disabled || !item.canApprove}
				title={item.canApprove ? "Accepter" : "Trop tard pour approuver : refus seulement"}
				aria-label={`Accepter la demande ${approvalOf(item)} de ${item.employeeName}`}
				className={rowIconSuccessButton}
			>
				<CheckCircleOutlineOutlined fontSize="small" />
			</button>
			<button
				type="button"
				onClick={onDecline}
				disabled={disabled}
				title="Refuser"
				aria-label={`Refuser la demande ${approvalOf(item)} de ${item.employeeName}`}
				className={rowIconDangerButton}
			>
				<HighlightOff fontSize="small" />
			</button>
		</div>
	)
}