import { useLayoutEffect, useRef, useState, type ChangeEvent } from "react"
import { inputClass } from "../../ui/fields"
import parsePhoneNumberFromString, { AsYouType, getCountries, getCountryCallingCode, type CountryCode } from "libphonenumber-js"
import { ExpandMoreOutlined } from "@mui/icons-material"

const PRIMARY_COUNTRIES: CountryCode[] = ["CA", "US", "FR"]

const countryNames = new Intl.DisplayNames(["fr"], { type: "region" })

const OTHER_COUNTRIES = getCountries()
	.filter((c) => !PRIMARY_COUNTRIES.includes(c))
	.sort((a, b) => countryNames.of(a)!.localeCompare(countryNames.of(b)!, "fr"))

type Props = {
	id: string
	/** Le numéro en E.164 (« +15145551234 »), ou "" s'il n'y en a pas. */
	value: string
	/** Le pays du menu quand `value` est vide, d'habitude celui du site. */
	defaultCountry: CountryCode
	/** Reçoit l'E.164 si le numéro est valide, "" s'il est vide, sinon le texte tapé (l'API le refusera). */
	onChange: (value: string) => void
}

/** "CA" → 🇨🇦 : chaque lettre devient sa « lettre régionale » Unicode. */
function flagOf(country: string): string {
	return String.fromCodePoint(
		...[...country].map((letter) => 0x1f1e6 + letter.charCodeAt(0) - 65),
	)
}

/** Garde les chiffres, et le « + » s'il est au début. */
function clean(text: string): string {
	const digits = text.replace(/\D/g, "")
	return text.trimStart().startsWith("+") ? `+${digits}` : digits
}

/** Nombre de chiffres (et de « + ») dans un texte. */
function countDigits(text: string): number {
	return text.replace(/[^\d+]/g, "").length
}

export function PhoneInput({ id, value, defaultCountry, onChange }: Props) {
	const parseNumber = parsePhoneNumberFromString(value)
	const [countryCode, setCountryCode] = useState<CountryCode>(parseNumber?.country ?? defaultCountry)
	const [phoneNumber, setPhoneNumber] = useState(parseNumber?.formatNational() ?? value)
	const [error, setError] = useState<string | null>(null)

	const inputRef = useRef<HTMLInputElement>(null)
	/** Combien de chiffres doivent précéder le curseur après le prochain formatage. */
	const caretDigits = useRef<number | null>(null)

	// Après chaque formatage : replace le curseur après le même nombre de chiffres qu'avant.
	useLayoutEffect(() => {
		const input = inputRef.current
		if (input === null || caretDigits.current === null) return

		let remaining = caretDigits.current
		let position = 0
		while (position < phoneNumber.length && remaining > 0) {
			if (/[\d+]/.test(phoneNumber[position])) remaining--
			position++
		}
		input.setSelectionRange(position, position)
		caretDigits.current = null
	}, [phoneNumber])

	/** Formate le texte pour le pays, met le menu et le champ à jour, et prévient le parent. */
	function apply(text: string, country: CountryCode) {
		const cleaned = clean(text)
		// Avec un « + », la librairie trouve le pays elle-même (coller « +33 6 12… » choisit la France).
		const asYou = new AsYouType(cleaned.startsWith("+") ? undefined : country)
		const formatted = asYou.input(cleaned)
		const finalCountry = asYou.getCountry() ?? country
		const number = asYou.getNumber()

		setCountryCode(finalCountry)
		setPhoneNumber(formatted)

		if (cleaned === "") onChange("")
		else if (number?.isValid()) onChange(number.number)
		else onChange(cleaned)
	}

	function handleInput(e: ChangeEvent<HTMLInputElement>) {
		let text = e.target.value
		let caret = e.target.selectionStart ?? text.length
		setError(null)

		// Effacer un espace ou un trait d'union seul ne changerait rien : on efface le chiffre d'avant.
		const removedOnlySeparator = text.length < phoneNumber.length && countDigits(text) === countDigits(phoneNumber)
		if (removedOnlySeparator) {
			let i = caret - 1
			while (i >= 0 && !/\d/.test(text[i])) i--
			if (i >= 0) {
				text = text.slice(0, i) + text.slice(i + 1)
				caret = i
			}
		}

		caretDigits.current = countDigits(text.slice(0, caret))
		apply(text, countryCode)
	}

	function handleCountry(country: CountryCode) {
		// Un numéro international collé ne garde que sa partie nationale quand on choisit un autre pays.
		const national = phoneNumber.startsWith("+")
			? (parsePhoneNumberFromString(phoneNumber)?.nationalNumber ?? clean(phoneNumber).slice(1))
			: phoneNumber
		setError(null)
		apply(national, country)
	}

	function handleBlur() {
		const cleaned = clean(phoneNumber)
		const valid = parsePhoneNumberFromString(cleaned, countryCode)?.isValid() === true
		setError(cleaned !== "" && !valid ? "Numéro incomplet ou invalide pour ce pays." : null)
	}

	return (
		<div>
			<div className="flex gap-2">
				<div className="relative w-24 shrink-0">
					<select
						id={`${id}-country`}
						aria-label="Pays"
						value={countryCode}
						onChange={(e) => handleCountry(e.target.value as CountryCode)}
						className={`${inputClass} appearance-none pr-8 text-transparent`}
					>
						{PRIMARY_COUNTRIES.map((country) => (
							<option key={country} value={country} className="text-slate-900">
								{flagOf(country)} +{getCountryCallingCode(country)} {countryNames.of(country)}
							</option>
						))}
						<hr />
						{OTHER_COUNTRIES.map((country) => (
							<option key={country} value={country} className="text-slate-900">
								{flagOf(country)} +{getCountryCallingCode(country)} {countryNames.of(country)}
							</option>
						))}
					</select>
					<span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-sm">
						{flagOf(countryCode)} +{getCountryCallingCode(countryCode)}
					</span>
					<span className="pointer-events-none absolute inset-y-0 right-2 flex items-center">
						<ExpandMoreOutlined fontSize="small" className="text-slate-500" />
					</span>
				</div>
				<input
					ref={inputRef}
					id={id}
					type="tel"
					autoComplete="tel"
					value={phoneNumber}
					onChange={handleInput}
					onBlur={handleBlur}
					aria-invalid={error !== null}
					aria-describedby={error !== null ? `${id}-error` : undefined}
					className={`${inputClass} min-w-0 flex-1`}
				/>
			</div>
			{error && (
				<p id={`${id}-error`} className="mt-1 text-xs text-red-600">
					{error}
				</p>
			)}
		</div>
	)
}
