import { useEffect, useState, type SubmitEvent } from "react"
import { Link, useNavigate, useParams } from "react-router-dom"
import { createEmployee, Role, type RoleId, Departement, type DepartementId, getAdminEmployee, updateEmployee } from "../api/admin"
import { AccountCircleOutlined } from "@mui/icons-material"
import { ErrorBanner } from "../components/ErrorBanner"

export function CreateEmployeePage() {
	const [firstName, setFirstName] = useState<string>("")
	const [lastName, setLastName] = useState<string>("")
	const [email, setEmail] = useState<string>("")
	const [departement, setDepartement] = useState<DepartementId | null>(null)
	const [role, setRole] = useState<RoleId | null>(null)
	const [jobTitle, setJobTitle] = useState<string>("")
	const [phoneNumber, setPhoneNumber] = useState<string>("")
	const [managerId, setManagerId] = useState<number>()
	const [photo, setPhoto] = useState<File | null>(null)
	const [photoPreview, setPhotoPreview] = useState<string | null>(null)

	const [enCours, setEnCours] = useState<boolean>(false)
	const [erreur, setErreur] = useState<string | null>(null)

	const [temporaryPassword, setTemporaryPassword] = useState<string>("")

	const { id } = useParams()
	const employeId = id ? Number(id) : null
	const enEdition = employeId !== null

	const navigate = useNavigate()

	useEffect(() => {
		if (!enEdition || employeId === null) return

		getAdminEmployee(employeId)
			.then((employee) => {
				setFirstName(employee.firstName)
				setLastName(employee.lastName)
				setEmail(employee.email)
				setDepartement(employee.departement as DepartementId)
				setRole(employee.role as RoleId)
				setJobTitle(employee.jobTitle)
				setPhoneNumber(employee.phoneNumber)
				setManagerId(employee.managerId ?? undefined)
				setPhotoPreview(employee.photoUrl)
			})
			.catch((e) => setErreur(e instanceof Error ? e.message : "Employé introuvable"))
	}, [enEdition, employeId])

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setErreur(null)
		setEnCours(true)
		try {
			if (departement === null) {
				setErreur("Veuillez choisir un département")
				return;
			}
			else if (role === null) {
				setErreur("Veuillez choisir un rôle")
				return;
			}

			if (enEdition && employeId !== null) {
				await updateEmployee(employeId, {
					firstName,
					lastName,
					departement,
					role,
					jobTitle,
					phoneNumber,
					managerId
				}, photo)
				navigate("/admin/accounts")
				return
			}
			
			const data = await createEmployee({
				firstName,
				lastName,
				email,
				departement: departement as number,
				role: role as number,
				jobTitle,
				phoneNumber,
				managerId
			}, photo)

			setTemporaryPassword(data.temporaryPassword)
		} catch (e) {
			setErreur(e instanceof Error ? e.message : 'Erreur à la création du compte')
		} finally {
			setEnCours(false)
		}
	}

	function handlePhoto(file: File | undefined) {
		if (photoPreview) URL.revokeObjectURL(photoPreview)
		if (!file) {
			setPhoto(null)
			setPhotoPreview(null)
			return
		}
		setPhoto(file)
		setPhotoPreview(URL.createObjectURL(file))
	}

	function slugify(value: string): string {
		return value
			.normalize('NFD')
			.replace(/\p{M}/gu, '')
			.toLowerCase()
			.replace(/[^a-z0-9]+/g, '')
	}

	function buildEmail(firstName: string, lastName: string): string {
		const prenom = slugify(firstName)
		const nom = slugify(lastName)
		if (!prenom || !nom) return ''
		return `${prenom}.${nom}@portail.local`
	}

	return (
		<div className="min-h-screen bg-gradient-to-b from-slate-50 via-slate-50 to-white">
			<div className="mx-auto max-w-2xl px-6 py-10">
				<header className="mb-8 flex flex-wrap items-end justify-between gap-4">
					<div>
						<p className="text-sm font-medium text-sky-700">Administration</p>
						<h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-900">
							{enEdition ? "Modifier l'employé" : "Nouvel employé"}
						</h1>
						<p className="mt-1 text-sm text-slate-500">
							Créer un employé et noter le mot de passe temporaire
						</p>
					</div>
					<Link
						to="/admin/accounts"
						className="rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-50"
					>
						Retour aux comptes
					</Link>
				</header>

				{erreur && (
					<ErrorBanner
						message={erreur}
						dismissible
						onDismiss={() => setErreur(null)}
					/>
				)}

				{temporaryPassword && (
					<div className="mb-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
						<p className="font-medium">Mot de passe temporaire (affiché une seule fois)</p>
						<p className="mt-1 font-mono text-base tracking-wide">{temporaryPassword}</p>
					</div>
				)}

				<section className="overflow-hidden rounded-2xl border border-slate-200 bg-white p-6 shadow-sm shadow-slate-200/50">
					<form onSubmit={handleSubmit} className="space-y-5">
						<div>
							<label htmlFor="photo" className="mb-1.5 block text-sm font-medium text-slate-700">
								Photo
							</label>
							<div className="flex items-center gap-4">
								{photoPreview ? (
									<img src={photoPreview} alt="" className="h-16 w-16 rounded-full object-cover" />
								) : (
									<AccountCircleOutlined className="text-slate-400" sx={{ fontSize: 64 }} />
								)}
								<input
									id="photo"
									type="file"
									accept="image/jpeg,image/png,image/webp"
									onChange={(e) => handlePhoto(e.target.files?.[0])}
									className="text-sm texte-slate-600"
								/>
							</div>
						</div>
						<div className="grid gap-5 sm:grid-cols-2">
							<div>
								<label htmlFor="firstName" className="mb-1.5 block text-sm font-medium text-slate-700">
									Prénom
								</label>
								<input
									id="firstName"
									type="text"
									required
									value={firstName}
									onChange={(e) => {
										const next = e.target.value
										setFirstName(next)
										if (!enEdition) setEmail(buildEmail(next, lastName))
									}}
									className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
								/>
							</div>
							<div>
								<label htmlFor="lastName" className="mb-1.5 block text-sm font-medium text-slate-700">
									Nom
								</label>
								<input
									id="lastName"
									type="text"
									required
									value={lastName}
									onChange={(e) => {
										const next = e.target.value
										setLastName(next)
										if (!enEdition) setEmail(buildEmail(firstName, next))
									}}
									className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
								/>
							</div>
						</div>

						<div>
							<label htmlFor="email" className="mb-1.5 block text-sm font-medium text-slate-700">
								Email
							</label>
							<input
								id="email"
								type="email"
								required
								value={email}
								readOnly={enEdition}
								onChange={(e) => setEmail(e.target.value)}
								className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100 read-only:cursor-default read-only:border-slate-200 read-only:bg-slate-100 read-only:text-slate-500 read-only:shadow-none read-only:focus:ring-0"
							/>
						</div>

						<div className="grid gap-5 sm:grid-cols-2">
							<div>
								<label htmlFor="departement" className="mb-1.5 block text-sm font-medium text-slate-700">
									Département
								</label>
								<select
									id="departement"
									value={departement ?? ""}
									onChange={(e) =>
										setDepartement(
											e.target.value === "" ? null : (Number(e.target.value) as DepartementId),
										)
									}
									className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
								>
									<option value="">Choisir...</option>
									{Object.entries(Departement).map(([label, id]) =>(
										<option key={id} value={id}>{label}</option>
									))}
								</select>
							</div>
							<div>
								<label htmlFor="role" className="mb-1.5 block text-sm font-medium text-slate-700">
									Rôle
								</label>
								<select
									id="role"
									value={role ?? ""}
									onChange={(e) =>
										setRole(
											e.target.value === "" ? null : (Number(e.target.value) as RoleId),
										)
									}
									className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
								>
									<option value="">Choisir...</option>
									{Object.entries(Role).map(([label, id]) =>(
										<option key={id} value={id}>{label}</option>
									))}
								</select>
							</div>
						</div>

						<div>
							<label htmlFor="jobTitle" className="mb-1.5 block text-sm font-medium text-slate-700">
								Titre de poste
							</label>
							<input
								id="jobTitle"
								type="text"
								value={jobTitle}
								onChange={(e) => setJobTitle(e.target.value)}
								className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
							/>
						</div>

						<div className="grid gap-5 sm:grid-cols-2">
							<div>
								<label htmlFor="phoneNumber" className="mb-1.5 block text-sm font-medium text-slate-700">
									Téléphone
								</label>
								<input
									id="phoneNumber"
									type="tel"
									value={phoneNumber}
									onChange={(e) => setPhoneNumber(e.target.value)}
									className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
								/>
							</div>
							<div>
								<label htmlFor="managerId" className="mb-1.5 block text-sm font-medium text-slate-700">
									ID manager (optionnel)
								</label>
								<input
									id="managerId"
									type="text"
									value={managerId ?? ""}
									onChange={(e) => 
										setManagerId(e.target.value === "" ? undefined : Number(e.target.value))
									}
									className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-sky-400 focus:ring-2 focus:ring-sky-100"
								/>
							</div>
						</div>

						<button
							type='submit'
							disabled={enCours}
							className="w-full rounded-lg bg-sky-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-sky-700 disabled:cursor-not-allowed disabled:opacity-50"
						>
							{enCours ? "Enregistrement..." : enEdition ? "Enregistrer" : "Créer l'employé"}
						</button>
					</form>
				</section>
			</div>
		</div>
	)
}