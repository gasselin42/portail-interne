import { MEETING_COLORS, type MeetingColor } from "../../api/meetings"
import { labelClass } from "../../ui/fields"
import { MEETING_COLOR_STYLES } from "../../ui/meetingColors"

type Props = {
	legend: string
	value: MeetingColor
	onChange: (color: MeetingColor) => void
	disabled?: boolean
}

export function ColorPicker({ legend, value, onChange, disabled }: Props) {
	return (
		<fieldset className="flex flex-1" disabled={disabled}>
			<legend className={labelClass}>{legend}</legend>
			<div className="flex flex-1 flex-wrap gap-2 justify-center mt-1">
				{MEETING_COLORS.map((c) => (
					<label key={c} title={MEETING_COLOR_STYLES[c].label}>
					<input
						type="radio"
						name="color"
						value={c}
						checked={value === c}
						onChange={() => onChange(c)}
						aria-label={MEETING_COLOR_STYLES[c].label}
						className="peer sr-only"
					/>
					<span className={`block size-8 rounded-full ${MEETING_COLOR_STYLES[c].swatch} ring-offset-2 peer-checked:ring-2 peer-checked:ring-slate-900 peer-focus-visible:ring-2 peer-focus-visible:ring-sky-400 peer-disabled:opacity-50`} />
				</label>
				))}
			</div>
		</fieldset>
	)
}