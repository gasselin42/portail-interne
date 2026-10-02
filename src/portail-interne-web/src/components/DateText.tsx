import { formatDate, formatWeekday } from "../utils/date"

type Props = {
	value: string
}

export function DateText({ value }: Props) {
	return (
		<time dateTime={value.slice(0, 10)}>
			<span className="block">{formatDate(value)}</span>
			<span className="block text-xs text-slate-500">{formatWeekday(value)}</span>
		</time>
	)
}