# Checklist de la phase 3.1 — Coordonnées et profil

Référence produit : [PROJET.md](../PROJET.md) §5.1, §6, §7, §8, §9 (R23, R28).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** phase 3 terminée. À faire **avant** la démo publique : aucune coordonnée personnelle ne doit pouvoir apparaître dans l’annuaire.

## Préparation

- [x] Relire R28 : ce qui est public, ce qui est privé, qui voit quoi, qui modifie quoi
- [x] Repérer tous les DTO et endpoints qui renvoient aujourd’hui `PhoneNumber` (annuaire, fiche, `/me`, comptes admin)
- [x] Figer le format de l’adresse : ligne 1, ligne 2 (app., bureau), ville, province / état, code postal, pays ; tout ou rien (une adresse partielle est refusée, la ligne 2 reste optionnelle)
- [x] Lister tous les endroits qui affichent un prénom (annuaire, fiche, combobox, avatar, file d’approbation, calendrier, notifications) : ils passeront tous par un seul « nom affiché »
- [x] Figer la liste des langues proposées (codes ISO 639-1 : fr, en, es, pt, it, de, ar, zh… ; libellés en français)
- [x] Figer le format des numéros : enregistrés au format international **E.164** (`+15145551234`), validés selon le pays, affichés au format national si le numéro est du même pays que le site de la personne qui regarde, sinon au format international ; poste = 1 à 6 chiffres, champ séparé
- [ ] Découvrir `libphonenumber-js` (React, `AsYouType`, `parsePhoneNumber`) et `libphonenumber-csharp` (NuGet `libphonenumber-csharp`, API) : mêmes règles des deux côtés

## Backend (API)

- [x] 1. `Site.MainPhoneNumber?` et `Site.CountryCode` (ISO 3166, ex. `CA`, `FR` ; pays par défaut des numéros du site) + migration ; Montréal = `CA` ; modifiables dans l’écran Sites (Admin global)
- [x] 1b. Validation commune des numéros (un seul service) : `libphonenumber-csharp`, numéro valide pour son pays, enregistré en E.164 ; message clair sinon
  - [x] Migration des numéros existants vers E.164 (pays du site de l’employé) ; un numéro impossible à convertir est listé dans la sortie de la migration et laissé vide plutôt qu’enregistré faux
- [x] 2. `Employee.PhoneExtension?` ; `PhoneNumber` renommé `WorkPhone?` dans la migration, **données conservées** (les numéros existants deviennent des téléphones pro)
- [x] 3. Modèles `EmployeePrivateInfo` (1 ligne par employé, au besoin) et `EmergencyContact` (0 à 2 par employé) + migration
  - [x] Adresse dans `EmployeePrivateInfo` : code postal validé selon le pays (Canada : `A1A 1A1`, lettres interdites refusées ; États-Unis : `12345` ou `12345-6789` ; France : `12345` ; autres pays : texte libre en majuscules)
  - [x] Contact d’urgence : nom, lien et téléphone obligatoires ; `SortOrder` 1 ou 2, unique par employé
- [x] 3b. `Employee.PreferredName?`, `OfficeLocation?` (100 caractères max), `HireDate` + migration
  - [x] Clé de recherche du prénom affiché (`DisplayFirstNameKey` : prénom d’usage, sinon prénom) ; la recherche porte sur le prénom **et** le prénom d’usage ; le tri suit le nom affiché
  - [x] Employés existants : `HireDate` = jour de création du compte dans le fuseau du site (au démarrage), à corriger par l’Admin ; « obligatoire à la création » se fait à l’étape 7
- [x] 3c. `EmployeeLanguage` + migration ; codes validés contre la liste fixe (`LanguageLabels.Normalize`) ; filtre `?language=` de l’annuaire (l’URL web gardera `?langue=`)
- [x] 3d. Anniversaire dans `EmployeePrivateInfo` : mois + jour validés ensemble (29 février permis), `ShowBirthday` faux par défaut
  - [x] Endpoint « anniversaires de la semaine » (`GET /api/employees/birthdays`, lundi → dimanche) : employés actifs du site de la personne connectée, `ShowBirthday` vrai seulement, semaine calculée dans le fuseau du site ; 29 février affiché le 28 les années non bissextiles
- [x] 4. Annuaire, fiche publique et `/me` : numéro du site + poste, téléphone pro, nom affiché, langues, local, date d’embauche ; **aucun** champ personnel dans ces DTO
- [x] 5. Endpoint **coordonnées personnelles** d’un employé (lecture) : `GET /api/employees/{id}/private`
  - [x] L’employé lui-même, un Admin de son site, l’Admin global : tout
  - [x] Son manager direct (employé actif) : contacts d’urgence seulement, `details` à null (ni adresse, ni cellulaire, ni email perso)
  - [x] Tout autre : 404
- [ ] 6. Endpoint de modification (remplace l’ensemble : coordonnées + contacts) : l’employé ou un Admin de son site
- [ ] 7. Création d’employé : poste, téléphone pro, local, date d’embauche (obligatoire, peut être dans le futur) dans le formulaire ; coordonnées personnelles optionnelles
  - [ ] Qui modifie quoi : prénom d’usage, langues, coordonnées personnelles, anniversaire = l’employé ou un Admin du site ; local, date d’embauche = Admin du site
- [ ] 8. Tests API : collègue → 404 ; manager → contacts d’urgence sans adresse ni cellulaire perso ; adresse partielle refusée ; annuaire sans aucun champ personnel ; 3e contact refusé

## Frontend (React)

- [ ] 8b. Composant **`PhoneInput`** (dans `components/`, comme `PasswordInput`) : menu du pays (par défaut celui du site) + champ formaté pendant la saisie avec `AsYouType`
  - [ ] Seuls les chiffres sont tapés ; espaces et traits d’union insérés automatiquement selon le pays
  - [ ] Le curseur reste au bon endroit quand on corrige un chiffre au milieu ou qu’on efface
  - [ ] Coller `+33 6 12 34 56 78` choisit la France tout seul
  - [ ] Valeur envoyée à l’API en E.164 ; message d’erreur sous le champ si le numéro est incomplet
  - [ ] Utilisé partout : téléphone pro, cellulaire perso, contacts d’urgence, numéro principal du site
- [ ] 9. Annuaire et fiche : « 514 555-1234, poste 2045 » (lien `tel:` avec le poste) ; « Poste 2045 » seul si le site n’a pas de numéro
- [ ] 10. Page **Mon profil** (menu du compte) : ma fiche en lecture ; en modification, mon prénom d’usage, mes langues, mes coordonnées personnelles, mon adresse, mon anniversaire (case « Afficher à mes collègues ») et mes contacts d’urgence
- [ ] 11. Formulaire employé / fiche (Admin du site) : poste, téléphone pro, section « Coordonnées personnelles » repliée par défaut
- [ ] 12. Fiche vue par le manager : bloc « Contacts d’urgence » ; rien de personnel pour les collègues
- [ ] 13. Écran Sites : champ numéro principal
- [ ] 14. Nom affiché partout (une seule fonction) : prénom d’usage s’il existe ; initiales de l’avatar comprises
- [ ] 15. Fiche : langues, local, « Depuis le 3 mars 2024 (2 ans) »
- [ ] 16. Annuaire : filtre par langue, gardé dans l’URL comme le filtre par site
- [ ] 17. Accueil : widget **Anniversaires de la semaine** (masqué s’il est vide)

## Validation manuelle

- [ ] Annuaire : aucun cellulaire ni email personnel, pour aucun employé
- [ ] Jane ajoute un contact d’urgence dans Mon profil ; Mark (son manager) le voit sur sa fiche ; Sam (collègue) ne le voit pas
- [ ] Mark ne voit ni l’adresse ni le cellulaire personnel de Jane ; l’Admin du site, oui
- [ ] Une adresse sans code postal est refusée avec un message clair
- [ ] Changer le numéro principal du site met à jour l’affichage de tous ses employés
- [ ] Les numéros saisis avant la migration apparaissent comme téléphones pro
- [ ] Taper `5145551234` affiche `514 555-1234` au fil de la saisie ; avec la France, `0612345678` affiche `06 12 34 56 78`
- [ ] Corriger le 3e chiffre d’un numéro complet : le curseur ne saute pas à la fin
- [ ] Un numéro de Paris vu par un employé de Montréal s’affiche `+33 6 12 34 56 78`
- [ ] Jane met « Alex » comme prénom d’usage : annuaire, calendrier, file d’approbation et avatar affichent « Alex » ; chercher « Alexandra » la trouve
- [ ] Filtre « anglais » : seuls les employés qui le parlent
- [ ] Anniversaire saisi sans cocher « Afficher » : absent du widget ; coché : présent cette semaine-là
- [ ] Anniversaire le 29 février : affiché le 28 en 2027

## Bonus

- [ ] Rappel dans Mon profil si aucun contact d’urgence n’est saisi
- [ ] Données de démo : coordonnées personnelles fictives

## Notes / blocages

_(À remplir au fil de l’eau)_

- Langues proposées (2026-11-13) : fr Français, en Anglais, es Espagnol, pt Portugais, it Italien, de Allemand, ar Arabe, zh Chinois, ru Russe, vi Vietnamien, ht Créole haïtien, ja Japonais
- Numéros existants : un seul (Mark, « 123 456-7890 », invalide) → la migration E.164 doit le laisser vide et le signaler
- Noms construits côté API : messages de notification (`PersonInfo.FullName`) → prénom d’usage aussi