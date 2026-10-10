import { useEffect, useRef, useState, type SubmitEvent } from "react"
import { Link, useNavigate, useParams } from "react-router-dom"
import {
	createEmployee,
	Role,
	type RoleId,
	Departement,
	type DepartementId,
	DEPARTEMENT_LABELS,
	getAdminEmployee,
	updateEmployee,
} from "../api/admin"
import { AccountCircleOutlined } from "@mui/icons-material"
import { ErrorBanner } from "../components/Banner"
import { TemporaryPasswordDialog } from "../components/TemporaryPasswordDialog"
import { EmployeeCombobox } from "../components/EmployeeCombobox"
import { focusRing, primaryButton, secondaryButton } from "../ui/buttons"
import type { EmployeeOption } from "../api/employees"
import { inputClass, labelClass } from "../ui/fields"
import { PageHeader } from "../components/PageHeader"

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function CreateEmployeePage() {
	const [firstName, setFirstName] = useState<string>("")
	const [lastName, setLastName] = useState<string>("")
	const [email, setEmail] = useState<string>("")
	const [departement, setDepartement] = useState<DepartementId | null>(null)
	const [role, setRole] = useState<RoleId | null>(null)
	const [jobTitle, setJobTitle] = useState<string>("")
	const [phoneNumber, setPhoneNumber] = useState<string>("")
	const [manager, setManager] = useState<EmployeeOption | null>(null)
	const [photo, setPhoto] = useState<File | null>(null)
	const [photoPreview, setPhotoPreview] = useState<string | null>(null)
	const [existingPhotoUrl, setExistingPhotoUrl] = useState<string | null>(null)
	const [photoError, setPhotoError] = useState<string | null>(null)

	const [enCours, setEnCours] = useState<boolean>(false)
	const [erreur, setErreur] = useState<string | null>(null)

	const [temporaryPassword, setTemporaryPassword] = useState<string>("")
	const [passwordDialogOpen, setPasswordDialogOpen] = useState(false)

	const { id } = useParams()
	const employeId = id ? Number(id) : null
	const enEdition = employeId !== null

	const navigate = useNavigate()
	const photoInputRef = useRef<HTMLInputElement>(null)

	useEffect(() => {
		if (!enEdition || employeId === null) return

		getAdminEmployee(employeId)
			.then((employee) => {
				setFirstName(employee.firstName)
				setLastName(employee.lastName)
				setEmail(employee.email)
				setDepartement(employee.departement)
				setRole(employee.role as RoleId)
				setJobTitle(employee.jobTitle)
				setPhoneNumber(employee.phoneNumber)
				setManager(
					employee.managerId !== null
						? {
								id: employee.managerId,
								firstName: employee.managerFirstName ?? "",
								lastName: employee.managerLastName ?? "",
							}
						: null,
				)
				setExistingPhotoUrl(employee.photoUrl)
			})
			.catch((e) => setErreur(e instanceof Error ? e.message : "Employé introuvable"))
	}, [enEdition, employeId])

	async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
		e.preventDefault()
		setErreur(null)

		try {
			if (firstName.trim() === "") {
				setErreur("Veuillez inscrire un prénom")
				return
			} else if (lastName.trim() === "") {
				setErreur("Veuillez inscrire un nom")
				return
			} else if (!EMAIL_PATTERN.test(email.trim())) {
				setErreur("Veuillez saisir un email valide")
				return
			} else if (departement === null) {
				setErreur("Veuillez choisir un département")
				return
			} else if (role === null) {
				setErreur("Veuillez choisir un rôle")
				return
			}

			setEnCours(true)

			if (enEdition && employeId !== null) {
				await updateEmployee(
					employeId,
					{
						firstName,
						lastName,
						departement,
						role,
						jobTitle,
						phoneNumber,
						managerId: manager?.id,
					},
					photo,
				)
				navigate("/admin/accounts")
				return
			}

			const data = await createEmployee(
				{
					firstName,
					lastName,
					email,
					departement: departement,
					role: role,
					jobTitle,
					phoneNumber,
					managerId: manager?.id,
				},
				photo,
			)

			setTemporaryPassword(data.temporaryPassword)
			setPasswordDialogOpen(true)
		} catch (e) {
			setErreur(e instanceof Error ? e.message : "Erreur à la création du compte")
		} finally {
			setEnCours(false)
		}
	}

	function handlePhoto(file: File | undefined) {
		setPhotoError(null)

		if (!file) {
			setPhoto(null)
			setPhotoPreview(null)
			return
		}

		if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
			setPhotoError("Format non supporté : choisis une image JPEG, PNG ou WebP.")
			if (photoInputRef.current) photoInputRef.current.value = ""
			return
		}
		
		if (file.size > 2 * 1024 * 1024) {
			setPhotoError("L'image ne doit pas dépasser 2 Mo.")
			if (photoInputRef.current) photoInputRef.current.value = ""
			return
		}
		
		if (photoPreview) URL.revokeObjectURL(photoPreview)
		setPhoto(file)
		setPhotoPreview(URL.createObjectURL(file))
	}

	function handleRemovePhoto() {
		if (photoPreview) URL.revokeObjectURL(photoPreview)
		setPhoto(null)
		setPhotoPreview(null)
		setPhotoError(null)
		if (photoInputRef.current) photoInputRef.current.value = ""
	}

	function slugify(value: string): string {
		return value
			.normalize("NFD")
			.replace(/\p{M}/gu, "")
			.toLowerCase()
			.replace(/[^a-z0-9]+/g, "")
	}

	function buildEmail(firstName: string, lastName: string): string {
		const prenom = slugify(firstName)
		const nom = slugify(lastName)
		if (!prenom || !nom) return ""
		return `${prenom}.${nom}@portail.local`
	}

	const shownPhoto = photoPreview ?? existingPhotoUrl

	return (
		<div className="mx-auto max-w-2xl px-6 py-10">
			<PageHeader
				eyebrow="Administration"
				title={enEdition ? "Modifier l'employé" : "Nouvel employé"}
				description={enEdition 
					? "Modifie les informations de l'employé." 
					: "Crée un employé et noter le mot de passe temporaire."
				}
				actions={
					<Link to="/admin/accounts" className={secondaryButton}>
						Retour aux comptes
					</Link>
				}
			/>

			{erreur && <ErrorBanner message={erreur} dismissible onDismiss={() => setErreur(null)} />}

			<section className="overflow-hidden rounded-2xl border border-slate-200 bg-white p-6 shadow-sm shadow-slate-200/50">
				<form noValidate onSubmit={handleSubmit} className="space-y-5">
					<p className="text-sm text-slate-500">
						Les champs marqués d’un <span className="text-red-600">*</span> sont obligatoires.
					</p>
					<div>
						<label htmlFor="photo" className={labelClass}>
							Photo
						</label>
						<div className="flex items-center gap-4">
							{shownPhoto ? (
								<img src={shownPhoto} alt="" className="h-16 w-16 rounded-full object-cover" />
							) : (
								<AccountCircleOutlined className="text-slate-400" sx={{ fontSize: 64 }} />
							)}
							<div className="min-w-0 flex-1">
								<div className="flex items-center gap-3">
									<input
										id="photo"
										type="file"
										ref={photoInputRef}
										accept="image/jpeg,image/png,image/webp"
										onChange={(e) => handlePhoto(e.target.files?.[0])}
										aria-describedby={photoError ? "photo-hint photo-error" : "photo-hint"}
										className={`block w-full text-sm text-slate-600 file:mr-4 file:rounded-lg file:border file:border-slate-200 file:bg-white file:px-4 file:py-2 file:text-sm file:font-medium file:text-slate-700 file:cursor-pointer file:shadow-sm file:transition hover:file:bg-slate-100 ${focusRing}`}
									/>
									{photo && (
										<button
											type="button"
											onClick={handleRemovePhoto}
											className={`shrink-0 text-sm text-slate-500 transition hover:text-red-600 ${focusRing}`}
										>
											Retirer
										</button>
									)}
								</div>

								<p id="photo-hint" className="mt-1.5 text-xs text-slate-500">
									JPEG, PNG ou WebP, 2 Mo maximum.
								</p>

								{photoError && (
									<p id="photo-error" role="alert" className="mt-1.5 text-sm text-red-600">
										{photoError}
									</p>
								)}
							</div>
						</div>
					</div>
					<div className="grid gap-5 sm:grid-cols-2">
						<div>
							<label
								htmlFor="firstName"
								className={labelClass}
							>
								Prénom
								<span className="ml-0.5 text-red-600" aria-hidden="true">
									*
								</span>
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
								className={inputClass}
							/>
						</div>
						<div>
							<label htmlFor="lastName" className={labelClass}>
								Nom
								<span className="ml-0.5 text-red-600" aria-hidden="true">
									*
								</span>
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
								className={inputClass}
							/>
						</div>
					</div>

					<div>
						<label htmlFor="email" className={labelClass}>
							Email
							<span className="ml-0.5 text-red-600" aria-hidden="true">
								*
							</span>
						</label>
						<input
							id="email"
							type="email"
							required
							value={email}
							readOnly={enEdition}
							onChange={(e) => setEmail(e.target.value)}
							className={`${inputClass} read-only:cursor-default read-only:border-slate-200 read-only:bg-slate-100 read-only:text-slate-500 read-only:shadow-none read-only:focus:ring-0`}
						/>
					</div>

					<div className="grid gap-5 sm:grid-cols-2">
						<div>
							<label
								htmlFor="departement"
								className={labelClass}
							>
								Département
								<span className="ml-0.5 text-red-600" aria-hidden="true">
									*
								</span>
							</label>
							<select
								id="departement"
								required
								value={departement ?? ""}
								onChange={(e) =>
									setDepartement(
										e.target.value === "" ? null : (Number(e.target.value) as DepartementId),
									)
								}
								className={inputClass}
							>
								<option value="">Choisir...</option>
								{Object.entries(Departement).map(([, id]) => (
									<option key={id} value={id}>
										{DEPARTEMENT_LABELS[id]}
									</option>
								))}
							</select>
						</div>
						<div>
							<label htmlFor="role" className={labelClass}>
								Rôle
								<span className="ml-0.5 text-red-600" aria-hidden="true">
									*
								</span>
							</label>
							<select
								id="role"
								required
								value={role ?? ""}
								onChange={(e) =>
									setRole(e.target.value === "" ? null : (Number(e.target.value) as RoleId))
								}
								className={inputClass}
							>
								<option value="">Choisir...</option>
								{Object.entries(Role).map(([label, id]) => (
									<option key={id} value={id}>
										{label}
									</option>
								))}
							</select>
						</div>
					</div>

					<div>
						<label htmlFor="jobTitle" className={labelClass}>
							Titre de poste
						</label>
						<input
							id="jobTitle"
							type="text"
							value={jobTitle}
							onChange={(e) => setJobTitle(e.target.value)}
							className={inputClass}
						/>
					</div>

					<div className="grid gap-5 sm:grid-cols-2">
						<div>
							<label
								htmlFor="phoneNumber"
								className={labelClass}
							>
								Téléphone
							</label>
							<input
								id="phoneNumber"
								type="tel"
								value={phoneNumber}
								onChange={(e) => setPhoneNumber(e.target.value)}
								className={inputClass}
							/>
						</div>
						<div>
							<label htmlFor="manager" className={labelClass}>
								Manager
							</label>
							<EmployeeCombobox
								id="manager"
								value={manager}
								onChange={setManager}
								excludeTeamOf={employeId ?? undefined}
								clearLabel="Retirer le manager"
							/>
						</div>
					</div>

					<button type="submit" disabled={enCours} className={`${primaryButton} w-full`}>
						{enCours ? "Enregistrement..." : enEdition ? "Enregistrer" : "Créer l'employé"}
					</button>
				</form>
			</section>
			<TemporaryPasswordDialog
				key={temporaryPassword}
				open={passwordDialogOpen}
				title="Compte créé"
				password={temporaryPassword}
				employeeName={`${firstName} ${lastName}`}
				onClose={() => {
					navigate("/admin/accounts")
				}}
			/>
		</div>
	)
}
