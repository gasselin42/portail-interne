import { CloseOutlined } from "@mui/icons-material"

type Tone = "error" | "success"

const TONES: Record<Tone, { box: string; button: string; role: "alert" | "status" }> = {
	error: {
		box: "border-red-200 bg-red-50 text-red-700",
		button: "text-red-500 hover:bg-red-100 hover:text-red-800",
		role: "alert",
	},
	success: {
		box: "border-emerald-200 bg-emerald-50 text-emerald-700",
		button: "text-emerald-500 hover:bg-emerald-100 hover:text-emerald-800",
		role: "alert",
	},
}

type BannerProps = {
	message: string
	/** Si true : bouton × pour fermer. Si false : erreur bloquante, non fermable. */
	dismissible?: boolean
	onDismiss?: () => void
}

function Banner({ tone, message, dismissible = false, onDismiss }: BannerProps & { tone: Tone }) {
	const style = TONES[tone]
	
	return (
		<div 
			role={style.role}
			className={`mb-4 flex items-start gap-3 rounded-lg border px-4 py-3 text-sm ${style.box}`}
		>
			<p className="flex-1 text-center">{message}</p>
			{dismissible && onDismiss && (
				<button
					type="button"
					aria-label="Fermer le message"
					className={`shrink-0 rounded px-1.5 leading-none transition ${style.button}`}
					onClick={onDismiss}
				>
					<CloseOutlined fontSize="small" />
				</button>
			)}
		</div>
	)
}

export function ErrorBanner(props: BannerProps) {
	return <Banner tone="error" {...props} />
}

export function SuccessBanner(props: BannerProps) {
	return <Banner tone="success" {...props} />
}
