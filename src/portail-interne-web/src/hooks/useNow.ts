import { useState, useEffect } from "react"
import { nowTime } from "../utils/date"

export function useNow(): string {
	const [now, setNow] = useState(nowTime)
	useEffect(() => {
		const id = setInterval(() => setNow(nowTime()), 60_000)
		return () => clearInterval(id)
	}, [])
	return now
}
