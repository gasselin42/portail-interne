export function daysFromToday(offset: number): string {
	const d = new Date()
	d.setDate(d.getDate() + offset) // gère tout seul les changements de mois / année
	return d.toLocaleDateString("en-CA")
}