# Portail Interne — Spécification complète du projet

> Document de référence du produit.  
> Mode de travail : l’IA guide par **étapes** ; le code n’est fourni **que sur demande**, pour renforcer l’autonomie.

---

## 1. Vision

Construire un **portail interne d’entreprise** qui centralise :

- l’**annuaire** des employés ;
- la **gestion des congés / absences** ;
- un **calendrier / planning** (grille horaire en vues jour, semaine et mois ; meetings individuels, de groupe et récurrents) ;
- la **réservation de salles et d’équipements** ;
- un **portail de demandes IT** (helpdesk) ;
- les **notifications** liées aux conflits, validations et assignations ;
- une **gestion des rôles** (admin vs employé, puis rôles spécialisés).

Objectif pédagogique : pratiquer un vrai produit .NET volumineux, découpé en phases, avec règles métier réalistes.

---

## 2. Principes de travail

1. **Pas de code donné par défaut** — uniquement objectifs, pistes et critères de validation.
2. Le code n’est fourni **que si demandé explicitement**.
3. Livrer par **phase** : chaque phase doit être demoable seule.
4. Documenter les décisions importantes dans ce fichier (ou dans `docs/`).
5. Priorité à la **clarté des règles métier** avant l’UI fancy.

---

## 3. Stack technique (décidée)

| Élément | Choix | Notes |
|--------|--------|-------|
| Backend | **ASP.NET Core Web API** | Controllers JSON, pas de vues Razor |
| Frontend | **React** (Vite recommandé) | SPA séparée qui consomme l’API |
| Auth | **JWT** (Bearer) | Tranché pour la phase 1 ; cookies possibles plus tard |
| BDD | **SQLite** (dev) | Simple sur Mac ; SQL Server possible plus tard |
| ORM | Entity Framework Core | Migrations |
| Tests API | xUnit | Règles critiques d’abord |
| Repo | Git | Déjà initialisé à la racine |

Architecture :

```text
React (UI)  ←HTTP JSON→  ASP.NET Core Web API  ←→  SQLite
```

> Toute déviation future doit être notée dans le journal de décisions (§12).

---

## 4. Utilisateurs et rôles

### 4.1 Rôles prévus (cible finale)

| Rôle | Capacités principales |
|------|------------------------|
| **Admin** | Créer/désactiver des comptes, gérer rôles, accès global |
| **Employé** | Annuaire, ses congés, son calendrier, créer des meetings, faire des demandes IT, réserver ressources selon règles |
| **Manager** | Approuver/refuser les congés de son équipe |
| **Agent IT** / **IT Lead** | Voir toutes les demandes IT, les assigner, les traiter |

### 4.2 Rôles à la phase 1

- **Admin**
- **Employé**

Les rôles Manager / Agent IT seront introduits aux phases concernées.

### 4.3 Règles d’accès transverses

- Un employé « au bas de l’échelle » **ne peut pas** créer de compte.
- Seul un **Admin** (phase 1) crée un nouvel employé / compte.
- À la création d’un compte : **mot de passe temporaire** généré.
- À la **1re connexion**, l’utilisateur **doit** changer ce mot de passe avant d’accéder au reste.
- Un compte **inactif** ne peut pas se connecter.

---

## 5. Modules fonctionnels (cible)

### 5.1 Annuaire interne

- Liste des employés actifs (nom, poste, équipe/département, contact).
- Recherche (nom, équipe, poste).
- Fiche employé (détails de contact, manager optionnel).
- Lien naturel avec congés, meetings, tickets IT (qui est qui).

### 5.2 Congés et absences

- Demande de congé / absence (période, type, commentaire).
- Statuts : `EnAttente`, `Approuve`, `Refuse`, `Annule` (liste exacte à figer à la phase 2).
- Approbation par un Manager (phase 2+).
- Affichage dans le calendrier :
  - congé **approuvé** → indication visuelle sur la journée (ex. bandeau / diagonale) ;
  - congé **en attente** → indication distincte (ex. « Demande en attente »).

### 5.3 Calendrier / planning

Le calendrier est l’élément central du portail.

- **Grille horaire** (axe des heures) : les événements sont placés et dimensionnés selon leur durée, comme dans Outlook ou Google Calendar.
  - **Vue semaine** sur grand écran (lundi → dimanche).
  - **Vue jour** sur mobile : les flèches changent de jour au lieu de semaine.
  - **Vue mois** : grille de 6 semaines × 7 jours, avec des jours avant et après chaque mois (si le 1er tombe du lundi au mercredi, la grille commence le lundi de la semaine précédente) ; congés en bandeaux qui traversent les jours, meetings en lignes courtes (« 10h Réunion », 3 au maximum puis « + N autres ») ; un clic sur un jour ouvre la vue jour.
  - Rangée **« toute la journée »** en haut de chaque jour : congés, fériés.
  - **Passé grisé** : jours passés entièrement, aujourd’hui jusqu’à l’heure exacte, avec une **ligne « maintenant »** mise à jour chaque minute (fuseau du site).
  - À l’ouverture, la grille défile jusqu’à l’heure actuelle.
  - La vue et la date affichées sont gardées dans l’URL (F5, Retour, lien partagé).
- Ajout d’événements / meetings dans l’horaire.
- Meeting **de groupe** avec plusieurs employés.
- Meetings **récurrents** (quotidiens, hebdomadaires, mensuels) avec exceptions : modifier ou annuler une occurrence, les suivantes ou toute la série.
- **Libre / occupé** : à la création d’un meeting, on voit si les participants sont disponibles, sans jamais voir le détail de leurs événements.
- Règles :
  - on **ne peut pas** planifier un meeting avec une personne **en congé approuvé** ;
  - si conflit d’horaire pour un ou plusieurs participants à un meeting d’équipe : le **meeting d’équipe a priorité** → les personnes en conflit reçoivent une **notification** ;
  - un meeting (ou créneau) exige que la **salle** et/ou l’**équipement** demandés soient **disponibles sur tout le créneau**, sinon **refus**.

### 5.4 Réservation de salles et équipements

- Catalogue de salles et d’équipements.
- Réservation liée à un créneau (meeting ou réservation autonome).
- Vérification de disponibilité sur l’intervalle `[début, fin]`.
- Conflit de ressource → réservation / meeting refusé.

### 5.5 Portail de demandes IT

- N’importe quel employé peut créer une demande (panne, accès, matériel, etc.).
- Statuts typiques : `Nouvelle`, `Assignee`, `EnCours`, `Resolue`, `Fermee` (à figer à la phase 5).
- Seuls les profils **autorisés** (Agent IT / IT Lead / Admin) voient **toutes** les requêtes.
- Assignation à un technicien / employé.
- La personne assignée reçoit un **avertissement** : elle doit **prévoir un créneau** dans son horaire pour la tâche.
- Le demandeur initial est **notifié** des changements (assignation, résolution, etc.).

### 5.6 Notifications

Types prévus (non exhaustif) :

- Congé approuvé / refusé
- Meeting de groupe créé / conflit d’horaire
- Tentative de meeting avec personne en congé (blocage + message)
- Ticket IT assigné / mis à jour
- Rappel « prévoir un slot » pour une tâche IT

Canal initial : **notifications in-app** (table + cloche / liste). Email = bonus ultérieur.

### 5.7 Horaires de travail

- Un Manager ou un Admin définit l’**horaire type** hebdomadaire d’un employé (ex. lundi au jeudi, 8h–17h, 30 min de pause).
- L’horaire se répète chaque semaine sans intervention ; un changement = un **nouvel horaire** avec une date d’entrée en vigueur (le passé n’est jamais réécrit).
- Les jours absents de l’horaire sont **non travaillés**, ce ne sont pas des congés.
- Chaque employé appartient à un **site** (fuseau horaire IANA + jours fériés propres).
- Les **jours fériés** du site sont non travaillés par défaut ; un manager peut faire travailler quelqu’un ce jour-là.
- Un **ajustement d’horaire** modifie une seule date (partir à 16h, venir un vendredi, travailler un férié) sans passer par Congés.
  - Il peut contenir une **plage d’absence** dans la journée (ex. rendez-vous de 10h à 12h), sans toucher au solde de congés.
  - Demandé par l’employé → approbation du manager (même file que les congés).
  - Créé par le manager → approuvé directement.
  - Aucune banque d’heures : le portail enregistre les heures prévues, la paie reste hors périmètre.
- La disponibilité d’une personne se calcule par couches : horaire type → férié → ajustement → congé approuvé (→ meetings à la phase 3).

---

## 6. Roadmap par phase

### Phase 1 — Fondations : Auth, rôles, annuaire

**Objectif :** comptes sécurisés + annuaire consultable.

**Inclus :**
- Login / logout
- Rôles Admin / Employé
- Création d’employé par Admin + mot de passe temporaire
- Forçage du changement de mot de passe à la 1re connexion
- Activation / désactivation de compte
- Annuaire (liste, recherche, fiche)

**Exclu :** congés, calendrier, meetings, salles, tickets IT

**Critères de done :**
- [ ] Admin se connecte
- [ ] Admin crée un employé et obtient le mdp temporaire (affiché une fois)
- [ ] Nouvel employé forcé de changer le mdp
- [ ] Ensuite accès à l’annuaire
- [ ] Employé ne peut pas accéder aux pages Admin
- [ ] Compte inactif = connexion refusée
- [ ] Recherche annuaire OK

---

### Phase 2 — Congés + calendrier personnel

**Objectif :** demander / approuver des absences et les voir sur un calendrier jour par jour.

**Inclus :**
- CRUD demandes de congé (côté employé)
- Workflow d’approbation (Manager ou Admin selon choix)
- Calendrier perso : vue semaine (grand écran), vue jour (mobile) et vue mois
- Congés dans une rangée « toute la journée » : approuvé (plein) vs en attente (hachuré), distincts sans dépendre de la couleur seule
- Soldes simples : jours ouvrables (lun.–ven.) en attendant les jours travaillés de la phase 2.5 ; au-delà du solde, les jours sont **sans solde** (calculés, jamais stockés)
- Congé maladie déclaré par un Manager (son équipe) ou un Admin pour un autre employé, approuvé directement

**Exclu :** meetings de groupe, salles, tickets IT

**Critères de done :**
- [x] Demande créée → statut EnAttente visible au calendrier
- [x] Approbation → affichage « congé » distinct
- [x] Refus → plus d’affichage bloquant / statut clair
- [x] Un utilisateur ne voit/modifie que ce qui lui est permis

---

### Phase 2.5 — Horaires de travail

**Objectif :** savoir qui travaille quand, chaque semaine, sans saisie répétée.

**Inclus :**
- Sites avec fuseau horaire + jours fériés par site
- Horaire type hebdomadaire versionné par date d’entrée en vigueur
- Ajustements d’horaire ponctuels (demande employé + approbation, ou création directe par le manager)
- **Grille horaire** du calendrier : axe des heures, heures de travail mises en évidence, passé grisé et ligne « maintenant »
- Calendrier perso et vue équipe semaine avec l’horaire effectif
- Congés : seuls les jours travaillés sont comptés

**Exclu :** temps supplémentaire constaté, banque d’heures, paie, quarts de nuit (passant minuit)

**Critères de done :**
- [ ] Horaire lundi–jeudi → autres jours non travaillés, chaque semaine
- [ ] Férié non travaillé automatiquement, sauf ajustement du manager
- [ ] Départ anticipé demandé, approuvé, visible au calendrier
- [ ] Congé sur une semaine complète → seuls les jours travaillés comptés
- [ ] « Aujourd’hui » calculé dans le fuseau du site de l’employé

---

### Phase 3 — Meetings (solo / groupe) + conflits

**Objectif :** planifier des réunions avec règles de conflit et de congé.

**Inclus :**
- Création de meeting (titre, créneau, participants)
- Interdiction si un participant est en **congé approuvé**
- Détection de conflits d’horaire
- Priorité du meeting d’équipe + notification aux personnes en conflit
- Affichage des meetings dans la grille horaire (chevauchements côte à côte) et dans la vue mois
- **Libre / occupé** des participants à la création d’un meeting
- Endpoint calendrier : réponse par jour (horaire effectif, congés) + liste des meetings

**Exclu :** meetings récurrents (phase 3.5), réservation salle/équipement obligatoire, tickets IT

**Critères de done :**
- [ ] Meeting solo OK
- [ ] Meeting groupe OK
- [ ] Blocage si participant en congé
- [ ] Conflit → notification(s)
- [ ] Calendrier à jour pour tous les participants
- [ ] Un employé voit le libre / occupé d’un collègue, jamais le titre de ses événements

---

### Phase 3.5 — Meetings récurrents

**Objectif :** planifier une fois une réunion qui revient (ex. réunion d’équipe chaque lundi).

**Inclus :**
- Règle de récurrence : quotidienne, hebdomadaire (jours choisis), mensuelle ; intervalle (« toutes les 2 semaines ») ; fin par date ou par nombre d’occurrences
- Occurrences calculées à la volée sur `[from, to]` (pas de copie en base), exceptions stockées
- Modifier / annuler : cette occurrence, cette occurrence et les suivantes, ou toute la série
- Règles R6–R8 et horaire effectif vérifiés pour chaque occurrence
- Récurrence en heure locale : une réunion à 10h reste à 10h après un changement d’heure

**Exclu :** invitations externes (iCal / email), salles récurrentes (phase 4)

**Critères de done :**
- [ ] Réunion chaque lundi à 10h visible toutes les semaines, sans saisie répétée
- [ ] Une occurrence déplacée ou annulée n’affecte pas les autres
- [ ] « Cette occurrence et les suivantes » coupe la série proprement
- [ ] Une occurrence en conflit avec un congé approuvé est signalée
- [ ] 10h reste 10h avant et après le passage à l’heure normale

---

### Phase 4 — Salles et équipements

**Objectif :** aucune réservation de créneau acceptée si la ressource demandée n’est pas libre sur tout l’intervalle.

**Inclus :**
- Gestion du catalogue salles / équipements (Admin)
- Réservation liée au meeting (ou réservation seule)
- Algorithme de disponibilité sur `[début, fin]`
- Refus clair si indisponible

**Critères de done :**
- [ ] Double booking impossible sur la même ressource
- [ ] Meeting avec salle requise refusé si salle prise
- [ ] Idem équipements
- [ ] Libération de ressource si annulation

---

### Phase 5 — Portail IT + assignation + créneau

**Objectif :** circuit de demandes IT relié à l’annuaire et au planning.

**Inclus :**
- Création de ticket par employé
- Vue globale pour rôles IT
- Assignation à une personne
- Warning « prévoir un slot » pour l’assigné
- Notification au demandeur
- (Optionnel) lien ticket ↔ événement calendrier

**Critères de done :**
- [ ] Employé crée une demande
- [ ] Agent IT voit toutes les demandes et assigne
- [ ] Assigné notifié + warning créneau
- [ ] Demandeur notifié
- [ ] Permissions respectées

---

### Plus tard (idées, non prioritaires)

- Emails / webhooks
- Soldes de congés avancés + types d’absence
- Organigramme graphique
- Pièces jointes sur tickets
- Mobile responsive poussé
- Audit log admin
- Import CSV d’employés
- Temps supplémentaire constaté (déclaration + approbation), banque d’heures
- Export des jours sans solde pour la paie
- Quarts de nuit (horaire passant minuit) et plusieurs blocs par jour

---

## 7. Modèle de données (cible, évolutif)

> Noms indicatifs — à affiner à chaque phase.

### Phase 1
- **Employee** : Id, FirstName, LastName, Email, JobTitle, Department/Team, Phone?, ManagerId?, IsActive, CreatedAt
- **UserAccount** : Id, EmployeeId, Email (unique), PasswordHash, Role, MustChangePassword, IsActive, CreatedAt, LastLoginAt?
- **Role** : enum ou table (`Admin`, `Employee`, puis `Manager`, `ItAgent`, …)

### Phase 2
- **LeaveRequest** : Id, EmployeeId, StartDate, EndDate, Type, Status, Reason?, ReviewedById?, ReviewedAt?, CreatedAt

### Phase 2.5
- **Site** : Id, Name, TimeZoneId (IANA) — `Employee.SiteId` obligatoire
- **WorkSchedule** : Id, EmployeeId, EffectiveFrom, CreatedById, CreatedAt
- **WorkScheduleDay** : WorkScheduleId, DayOfWeek, StartTime, EndTime, BreakMinutes
- **Holiday** : Id, SiteId, Date, Name
- **ScheduleAdjustment** : Id, EmployeeId, Date, StartTime?, EndTime?, BreakMinutes, Reason?, Status, CreatedById, ReviewedById?, ReviewedAt?, CreatedAt

### Phase 3
- **CalendarEvent / Meeting** : Id, Title, Description?, StartAt, EndAt, OrganizerId, IsTeamMeeting, CreatedAt
- **MeetingParticipant** : MeetingId, EmployeeId, ResponseStatus?

### Phase 3.5
- **MeetingSeries** : Id, OrganizerId, Title, Description?, IsTeamMeeting, TimeZoneId, FirstStartLocal, DurationMinutes, Frequency (Daily/Weekly/Monthly), Interval, DaysOfWeek?, Until? / Count?, CreatedAt
- **MeetingSeriesParticipant** : SeriesId, EmployeeId
- **MeetingOccurrenceException** : SeriesId, OriginalStartLocal, IsCancelled, StartAt?, EndAt?, Title? (occurrence déplacée ou modifiée)

### Phase 4
- **Room** : Id, Name, Capacity?, IsActive
- **Equipment** : Id, Name, Type?, IsActive
- **ResourceReservation** : Id, ResourceType (Room/Equipment), ResourceId, StartAt, EndAt, MeetingId?, ReservedById

### Phase 5
- **ItTicket** : Id, Title, Description, Category?, Status, CreatedById, AssignedToId?, CreatedAt, UpdatedAt, ResolvedAt?
- **TicketComment** (optionnel)

### Transverse
- **Notification** : Id, UserId, Type, Message, IsRead, CreatedAt, RelatedEntityType?, RelatedEntityId?

---

## 8. Écrans React + endpoints API (par phase)

Les écrans sont côté **React**. L’API expose les routes `/api/...` correspondantes.

### Phase 1
**Écrans React :**
1. Login
2. Changer le mot de passe (forcé)
3. Annuaire (liste + recherche)
4. Fiche employé
5. Admin — créer employé / compte
6. Admin — liste des comptes

**Endpoints API (indicatif) :**
- `POST /api/auth/login`
- `POST /api/auth/logout` (si cookies) ou invalidation côté client (si JWT)
- `POST /api/auth/change-password`
- `GET  /api/auth/me`
- `GET  /api/employees` (+ query recherche)
- `GET  /api/employees/{id}`
- `POST /api/admin/employees` (crée employé + compte, renvoie mdp temporaire une fois)
- `GET  /api/admin/accounts`
- `PATCH /api/admin/accounts/{id}` (actif/inactif, rôle si besoin)

### Phase 2
7. Mes demandes de congé
8. Nouvelle demande
9. (Manager) File d’approbation
10. Calendrier personnel : vue semaine (grand écran), vue jour (mobile), vue mois

### Phase 2.5
10a. (Manager/Admin) Horaire d’un employé
10b. Demander un ajustement + mes ajustements
10c. (Manager) Vue équipe semaine
10d. (Admin) Sites et jours fériés
10e. Calendrier en grille horaire (heures de travail, passé grisé, ligne « maintenant »)

### Phase 3
11. Créer un meeting
12. Détail meeting
13. Calendrier enrichi (meetings + congés) + libre / occupé à la création

### Phase 3.5
13a. Créer un meeting récurrent (règle + aperçu en clair : « Chaque lundi à 10h, jusqu’au 31 déc. »)
13b. Modifier / annuler : cette occurrence, les suivantes ou toute la série

### Phase 4
14. Catalogue salles / équipements
15. Réservation / sélection ressource à la création de meeting

### Phase 5
16. Mes tickets IT
17. Nouveau ticket
18. Console IT (toutes les demandes, assignation)
19. Centre de notifications

---

## 9. Règles métier — synthèse

| # | Règle |
|---|--------|
| R1 | Seul un Admin (ou rôle autorisé) crée un compte employé |
| R2 | Nouveau compte → mot de passe temporaire + `MustChangePassword = true` |
| R3 | Tant que le mdp n’est pas changé → API refuse le reste ; React redirige vers changement de mdp |
| R4 | Compte inactif → login refusé |
| R5 | Congé en attente vs approuvé : affichages calendrier distincts |
| R6 | Pas de meeting avec un participant en congé **approuvé** |
| R7 | Conflit d’horaire sur meeting d’équipe → priorité au meeting d’équipe + notification |
| R8 | Si salle/équipement demandé : doit être libre sur **tout** le créneau, sinon refus |
| R9 | Tickets IT : vue globale réservée aux rôles IT / Admin |
| R10 | Assignation ticket → warning « prévoir un slot » pour l’assigné + notif demandeur |
| R11 | Horaire type récurrent, versionné par `EffectiveFrom` ≥ aujourd’hui ; jours absents = non travaillés ; un bloc par jour, sans passer minuit |
| R12 | Jour férié du site = non travaillé, sauf ajustement créé par un Manager/Admin |
| R13 | Ajustement demandé par l’employé = EnAttente jusqu’à approbation ; créé par Manager/Admin = approuvé ; jamais de journée à 0 h demandée par l’employé ; peut contenir une plage d’absence dans la journée ; aucune banque d’heures |
| R14 | Un congé ne compte que les jours travaillés ; une demande sans aucun jour travaillé est refusée |
| R15 | Horaires, fériés et ajustements en heure locale du site ; instants (meetings, horodatages) en UTC |
| R16 | Disponibilité = horaire type → férié → ajustement approuvé → congé approuvé (la couche suivante l’emporte) |
| R17 | Libre / occupé : un employé voit les créneaux occupés de ses collègues, jamais le titre, le motif ni les participants |
| R18 | Meeting récurrent : chaque occurrence respecte R6–R8 ; une modification vise une occurrence, les suivantes ou toute la série ; la série suit l’heure locale du fuseau de l’organisateur |
| R19 | Solde annuel de congés (20 jours par défaut, modifiable par l’Admin par employé, année civile) : les congés en attente et approuvés de l’année consomment le solde dans l’**ordre de création** des demandes ; les jours au-delà sont sans solde (calculés à l’affichage, jamais stockés) ; la maladie ne consomme pas le solde |
| R20 | Congé maladie pour autrui : Admin (tous) ou Manager (équipe directe), jamais pour soi ; approuvé directement ; `CreatedById` conservé |

---

## 10. Structure du dépôt (cible)

```text
portail-interne/
├── PROJET.md                      ← ce document (source de vérité produit)
├── README.md
├── docs/
│   ├── phase-1-checklist.md
│   ├── phase-2-checklist.md
│   ├── phase-2.5-checklist.md
│   ├── phase-3-checklist.md
│   ├── phase-3.5-checklist.md
│   ├── phase-4-checklist.md
│   └── phase-5-checklist.md
├── src/
│   ├── PortailInterne.Api/        ← ASP.NET Core Web API (.NET)
│   └── portail-interne-web/       ← React (Vite)
├── tests/
│   └── PortailInterne.Api.Tests/  ← xUnit
└── assets/
    └── mockups/
```

> Les dossiers `src/` et `tests/` applicatifs seront créés au démarrage concret de la phase 1.

---

## 11. Checklist de la phase 1 — étapes de réalisation (sans code)

### Backend (API)
1. Créer la solution + projet **Web API** dans `src/` et vérifier Swagger / démarrage
2. Modèles `Employee` + `UserAccount` (+ enum Role)
3. Configurer **SQLite** + EF Core
4. Migrations / schéma initial
5. Seed : 1 admin + 1–2 employés de test
6. Hash des mots de passe
7. Auth : login (+ schéma JWT ou cookies) + endpoint `me`
8. `MustChangePassword` : endpoint changement de mdp + refus des autres routes si flag = true
9. Autorisation par rôles (`Admin` vs `Employee`)
10. Endpoints admin : créer employé (mdp temporaire **une fois**), lister, activer/désactiver
11. Endpoints annuaire : liste + recherche + fiche

### Frontend (React)
12. Créer l’app React (Vite) dans `src/portail-interne-web/`
13. Configurer l’appel API (base URL, CORS côté API)
14. Écran Login + gestion erreurs
15. Écran changement de mdp forcé + garde de navigation
16. Écrans Admin (création + liste comptes)
17. Écrans Annuaire + fiche employé

### Validation
18. Tests manuels bout en bout (voir §6 phase 1)
19. Bonus : tests API automatisés (login, mdp temporaire, rôles)

---

## 12. Journal de décisions

| Date | Décision |
|------|----------|
| 2026-09-11 | Produit = mélange annuaire + congés/calendrier + meetings + salles/équipements + helpdesk IT |
| 2026-09-11 | Travail en autonomie guidée (étapes sans code par défaut) |
| 2026-09-11 | Découpage en 5 phases |
| 2026-09-11 | Nouveau dépôt hors `tuto dotnet` : `~/Desktop/portail-interne` |
| 2026-09-11 | Doc produit centralisée dans `PROJET.md` |
| 2026-09-11 | Stack = **ASP.NET Core Web API + React** (dès la phase 1) |
| 2026-09-11 | BDD dev = **SQLite** |
| 2026-09-11 | Auth phase 1 = **JWT** (pas cookies) |
| 2026-09-28 | Congé : date de début ≥ aujourd'hui. Maladie : rétroactif permis jusqu'à 14 jours. Toujours EnAttente à la création
| 2026-09-28 | Création d'un congé pour autrui (Admin/Manager) : reportée, ajoutée dans les bonus de la phase 2, nécessite CreatedById
| 2026-09-30 | Ajout de la phase 2.5 — Horaires de travail, avant les meetings (les conflits s'appuieront sur l'horaire effectif)
| 2026-09-30 | Départ anticipé = ajustement d'horaire approuvé par le manager, pas un congé ; aucune banque d'heures, paie hors périmètre
| 2026-09-30 | Fuseaux horaires dès la phase 2.5 via `Site` (modèle de données) ; temps supplémentaire constaté reporté à plus tard
| 2026-10-12 | Calendrier = grille horaire (vues jour, semaine, mois) ; vue jour sur mobile ; passé grisé + ligne « maintenant » ; grille construite en phase 2.5 (premier contenu à l’heure)
| 2026-10-12 | Phase 2 : congés dans une rangée « toute la journée » (R5), vue jour mobile et vue mois
| 2026-10-12 | Libre / occupé des collègues à la création de meeting (phase 3, R17) ; endpoint calendrier = réponse par jour + meetings
| 2026-10-16 | Bonus phase 2 : congé maladie pour autrui (approuvé directement, R20) ; soldes en jours ouvrables, dépassement = jours sans solde calculés (R19), couleurs distinctes avec hachures continues
| 2026-10-16 | Solde : 20 jours par défaut ; consommé dans l’ordre de création (la demande qui dépasse porte les jours sans solde)
| 2026-10-16 | Absence de quelques heures = ajustement d’horaire avec plage d’absence (phase 2.5), sans toucher au solde
| 2026-10-12 | Ajout de la phase 3.5 — Meetings récurrents (occurrences calculées, exceptions stockées, R18)

---

## 13. État d’avancement

| Phase | Statut |
|-----|--------|
| Phase 1 — Auth + annuaire | ✅ Terminée |
| Phase 2 — Congés + calendrier | 🔨 En cours |
| Phase 2.5 — Horaires de travail | ⏳ |
| Phase 3 — Meetings | ⏳ |
| Phase 3.5 — Meetings récurrents | ⏳ |
| Phase 4 — Ressources | ⏳ |
| Phase 5 — Helpdesk IT | ⏳ |

---

*Dernière mise à jour : 2026-10-12*
