import { useState } from "react"
import { VisibilityOutlined, VisibilityOffOutlined } from "@mui/icons-material"
import { fieldBase, labelClass } from "../ui/fields"
import { focusRing } from "../ui/buttons"

type Props = {
	id: string
	label: string
	value: string
	onChange: (value: string) => void
	autoComplete: "current-password" | "new-password"
	hint?: string
}

export function PasswordInput({
	id,
	label,
	value,
	onChange,
	autoComplete,
	hint
}: Props) {
	const [visible, setVisible] = useState(false)

	return (
		<div>
			<label htmlFor={id} className={labelClass}>{label}</label>
			<div className="relative">
				<input
					id={id}
					value={value}
					type={visible ? "text" : "password"}
					onChange={(e) => onChange(e.target.value)}
					autoComplete={autoComplete}
					autoCapitalize="none"
					autoCorrect="off"
					spellCheck={false}
					aria-describedby={hint ? `${id}-hint` : undefined}
					className={`${fieldBase} pl-3 pr-10`}
					required
				/>
				<button
					className={`absolute inset-y-0 right-0 flex items-center px-3 text-slate-400 transition hover:text-slate-600 ${focusRing}`}
					type="button"
					onClick={() => setVisible((v) => !v)}
					onMouseDown={(e) => e.preventDefault()}
					aria-label="Afficher le mot de passe"
					aria-pressed={visible}
				>
					{visible ? <VisibilityOffOutlined fontSize="small" /> : <VisibilityOutlined fontSize="small" />}
				</button>
			</div>
			{hint && <p id={`${id}-hint`} className="mt-1.5 text-xs text-slate-500">{hint}</p>}
		</div>
	)
}