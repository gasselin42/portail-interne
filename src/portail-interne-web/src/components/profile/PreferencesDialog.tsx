import { Modal } from "../dialogs/Modal"
import { linkClasses, secondaryButton } from "../../ui/buttons"
import { Link } from "react-router-dom"
import { KeyOutlined } from "@mui/icons-material"

type Props = {
	open: boolean
	onClose: () => void
}

export function PreferencesDialog({ open, onClose }: Props) {
	return (
		<Modal open={open} title="Préférences" onClose={onClose}>
			<Link to="/change-password" onClick={onClose} className={linkClasses(false, false)}>
				<KeyOutlined fontSize="small" />Changer mon mot de passe
			</Link>
			<div className="mt-6 flex justify-end gap-3">
				<button type="button" onClick={onClose} className={secondaryButton}>
					Fermer
				</button>
			</div>
		</Modal>
	)
}
