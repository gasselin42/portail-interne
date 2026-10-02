# Portail Interne — Spécification complète du projet

> Document de référence du produit.  
> Mode de travail : l’IA guide par **étapes** ; le code n’est fourni **que sur demande**, pour renforcer l’autonomie.

---

## 1. Vision

Construire un **portail interne d’entreprise** qui centralise :

- l’**annuaire** des employés ;
- la **gestion des congés / absences** ;
- un **calendrier / planning** (vue jour, meetings individuels et de groupe) ;
- la **réservation de salles et d’équipements** ;
- un **portail de demandes IT** (helpdesk) ;
- les **notifications** liées aux conflits, validations et assignations ;
- une **gestion des rôles** (admin vs employé, puis rôles spécialisés).

Objectif pédagogique : pratiquer un vrai produit .NET volumineux, versionné par MVP, avec règles métier réalistes.

---

## 2. Principes de travail

1. **Pas de code donné par défaut** — uniquement objectifs, pistes et critères de validation.
2. Le code n’est fourni **que si demandé explicitement**.
3. Livrer par **MVP** : chaque phase doit être demoable seule.
4. Documenter les décisions importantes dans ce fichier (ou dans `docs/`).
5. Priorité à la **clarté des règles métier** avant l’UI fancy.

---

## 3. Stack technique (décidée)

| Élément | Choix | Notes |
|--------|--------|-------|
| Backend | **ASP.NET Core Web API** | Controllers JSON, pas de vues Razor |
| Frontend | **React** (Vite recommandé) | SPA séparée qui consomme l’API |
| Auth | **JWT** (Bearer) | Tranché pour MVP 1 ; cookies possibles plus tard |
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

### 4.2 Rôles au MVP 1

- **Admin**
- **Employé**

Les rôles Manager / Agent IT seront introduits aux phases concernées.

### 4.3 Règles d’accès transverses

- Un employé « au bas de l’échelle » **ne peut pas** créer de compte.
- Seul un **Admin** (MVP 1) crée un nouvel employé / compte.
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
- Statuts : `EnAttente`, `Approuve`, `Refuse`, `Annule` (liste exacte à figer au MVP 2).
- Approbation par un Manager (MVP 2+).
- Affichage dans le calendrier :
  - congé **approuvé** → indication visuelle sur la journée (ex. bandeau / diagonale) ;
  - congé **en attente** → indication distincte (ex. « Demande en attente »).

### 5.3 Calendrier / planning

- Vue **jour par jour** du planning personnel.
- Ajout d’événements / meetings dans l’horaire.
- Meeting **de groupe** avec plusieurs employés.
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
- Statuts typiques : `Nouvelle`, `Assignee`, `EnCours`, `Resolue`, `Fermee` (à figer au MVP 5).
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

Canal MVP : **notifications in-app** (table + cloche / liste). Email = bonus ultérieur.

### 5.7 Horaires de travail

- Un Manager ou un Admin définit l’**horaire type** hebdomadaire d’un employé (ex. lundi au jeudi, 8h–17h, 30 min de pause).
- L’horaire se répète chaque semaine sans intervention ; un changement = un **nouvel horaire** avec une date d’entrée en vigueur (le passé n’est jamais réécrit).
- Les jours absents de l’horaire sont **non travaillés**, ce ne sont pas des congés.
- Chaque employé appartient à un **site** (fuseau horaire IANA + jours fériés propres).
- Les **jours fériés** du site sont non travaillés par défaut ; un manager peut faire travailler quelqu’un ce jour-là.
- Un **ajustement d’horaire** modifie une seule date (partir à 16h, venir un vendredi, travailler un férié) sans passer par Congés.
  - Demandé par l’employé → approbation du manager (même file que les congés).
  - Créé par le manager → approuvé directement.
  - Aucune banque d’heures : le portail enregistre les heures prévues, la paie reste hors périmètre.
- La disponibilité d’une personne se calcule par couches : horaire type → férié → ajustement → congé approuvé (→ meetings au MVP 3).

---

## 6. Roadmap par MVP

### MVP 1 — Fondations : Auth, rôles, annuaire

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

### MVP 2 — Congés + calendrier personnel

**Objectif :** demander / approuver des absences et les voir sur un calendrier jour par jour.

**Inclus :**
- CRUD demandes de congé (côté employé)
- Workflow d’approbation (Manager ou Admin selon choix)
- Calendrier perso : jours avec congé approuvé vs en attente (distinction visuelle)
- Soldes simples (optionnel si trop lourd : reporter)

**Exclu :** meetings de groupe, salles, tickets IT

**Critères de done :**
- [ ] Demande créée → statut EnAttente visible au calendrier
- [ ] Approbation → affichage « congé » distinct
- [ ] Refus → plus d’affichage bloquant / statut clair
- [ ] Un utilisateur ne voit/modifie que ce qui lui est permis

---

### MVP 2.5 — Horaires de travail

**Objectif :** savoir qui travaille quand, chaque semaine, sans saisie répétée.

**Inclus :**
- Sites avec fuseau horaire + jours fériés par site
- Horaire type hebdomadaire versionné par date d’entrée en vigueur
- Ajustements d’horaire ponctuels (demande employé + approbation, ou création directe par le manager)
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

### MVP 3 — Meetings (solo / groupe) + conflits

**Objectif :** planifier des réunions avec règles de conflit et de congé.

**Inclus :**
- Création de meeting (titre, créneau, participants)
- Interdiction si un participant est en **congé approuvé**
- Détection de conflits d’horaire
- Priorité du meeting d’équipe + notification aux personnes en conflit
- Affichage des meetings dans le calendrier

**Exclu :** réservation salle/équipement obligatoire, tickets IT

**Critères de done :**
- [ ] Meeting solo OK
- [ ] Meeting groupe OK
- [ ] Blocage si participant en congé
- [ ] Conflit → notification(s)
- [ ] Calendrier à jour pour tous les participants

---

### MVP 4 — Salles et équipements

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

### MVP 5 — Portail IT + assignation + créneau

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

### Post-MVP (idées, non prioritaires)

- Emails / webhooks
- Soldes de congés avancés + types d’absence
- Organigramme graphique
- Pièces jointes sur tickets
- Mobile responsive poussé
- Audit log admin
- Import CSV d’employés
- Temps supplémentaire constaté (déclaration + approbation), banque d’heures
- Quarts de nuit (horaire passant minuit) et plusieurs blocs par jour

---

## 7. Modèle de données (cible, évolutif)

> Noms indicatifs — à affiner à chaque MVP.

### MVP 1
- **Employee** : Id, FirstName, LastName, Email, JobTitle, Department/Team, Phone?, ManagerId?, IsActive, CreatedAt
- **UserAccount** : Id, EmployeeId, Email (unique), PasswordHash, Role, MustChangePassword, IsActive, CreatedAt, LastLoginAt?
- **Role** : enum ou table (`Admin`, `Employee`, puis `Manager`, `ItAgent`, …)

### MVP 2
- **LeaveRequest** : Id, EmployeeId, StartDate, EndDate, Type, Status, Reason?, ReviewedById?, ReviewedAt?, CreatedAt

### MVP 2.5
- **Site** : Id, Name, TimeZoneId (IANA) — `Employee.SiteId` obligatoire
- **WorkSchedule** : Id, EmployeeId, EffectiveFrom, CreatedById, CreatedAt
- **WorkScheduleDay** : WorkScheduleId, DayOfWeek, StartTime, EndTime, BreakMinutes
- **Holiday** : Id, SiteId, Date, Name
- **ScheduleAdjustment** : Id, EmployeeId, Date, StartTime?, EndTime?, BreakMinutes, Reason?, Status, CreatedById, ReviewedById?, ReviewedAt?, CreatedAt

### MVP 3
- **CalendarEvent / Meeting** : Id, Title, Description?, StartAt, EndAt, OrganizerId, IsTeamMeeting, CreatedAt
- **MeetingParticipant** : MeetingId, EmployeeId, ResponseStatus?

### MVP 4
- **Room** : Id, Name, Capacity?, IsActive
- **Equipment** : Id, Name, Type?, IsActive
- **ResourceReservation** : Id, ResourceType (Room/Equipment), ResourceId, StartAt, EndAt, MeetingId?, ReservedById

### MVP 5
- **ItTicket** : Id, Title, Description, Category?, Status, CreatedById, AssignedToId?, CreatedAt, UpdatedAt, ResolvedAt?
- **TicketComment** (optionnel)

### Transverse
- **Notification** : Id, UserId, Type, Message, IsRead, CreatedAt, RelatedEntityType?, RelatedEntityId?

---

## 8. Écrans React + endpoints API (par phase)

Les écrans sont côté **React**. L’API expose les routes `/api/...` correspondantes.

### MVP 1
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

### MVP 2
7. Mes demandes de congé
8. Nouvelle demande
9. (Manager) File d’approbation
10. Calendrier personnel (vue jour / semaine simple)

### MVP 2.5
10a. (Manager/Admin) Horaire d’un employé
10b. Demander un ajustement + mes ajustements
10c. (Manager) Vue équipe semaine
10d. (Admin) Sites et jours fériés

### MVP 3
11. Créer un meeting
12. Détail meeting
13. Calendrier enrichi (meetings + congés)

### MVP 4
14. Catalogue salles / équipements
15. Réservation / sélection ressource à la création de meeting

### MVP 5
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
| R13 | Ajustement demandé par l’employé = EnAttente jusqu’à approbation ; créé par Manager/Admin = approuvé ; jamais de journée à 0 h demandée par l’employé ; aucune banque d’heures |
| R14 | Un congé ne compte que les jours travaillés ; une demande sans aucun jour travaillé est refusée |
| R15 | Horaires, fériés et ajustements en heure locale du site ; instants (meetings, horodatages) en UTC |
| R16 | Disponibilité = horaire type → férié → ajustement approuvé → congé approuvé (la couche suivante l’emporte) |

---

## 10. Structure du dépôt (cible)

```text
portail-interne/
├── PROJET.md                      ← ce document (source de vérité produit)
├── README.md
├── docs/
│   ├── MVP1-checklist.md
│   ├── MVP2-checklist.md
│   ├── MVP2.5-checklist.md
│   ├── MVP3-checklist.md
│   ├── MVP4-checklist.md
│   └── MVP5-checklist.md
├── src/
│   ├── PortailInterne.Api/        ← ASP.NET Core Web API (.NET)
│   └── portail-interne-web/       ← React (Vite)
├── tests/
│   └── PortailInterne.Api.Tests/  ← xUnit
└── assets/
    └── mockups/
```

> Les dossiers `src/` et `tests/` applicatifs seront créés au démarrage concret du MVP 1.

---

## 11. Checklist MVP 1 — étapes de réalisation (sans code)

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
18. Tests manuels bout en bout (voir §6 MVP 1)
19. Bonus : tests API automatisés (login, mdp temporaire, rôles)

---

## 12. Journal de décisions

| Date | Décision |
|------|----------|
| 2026-09-11 | Produit = mélange annuaire + congés/calendrier + meetings + salles/équipements + helpdesk IT |
| 2026-09-11 | Travail en autonomie guidée (étapes sans code par défaut) |
| 2026-09-11 | Découpage en 5 MVP |
| 2026-09-11 | Nouveau dépôt hors `tuto dotnet` : `~/Desktop/portail-interne` |
| 2026-09-11 | Doc produit centralisée dans `PROJET.md` |
| 2026-09-11 | Stack = **ASP.NET Core Web API + React** (dès le MVP 1) |
| 2026-09-11 | BDD dev = **SQLite** |
| 2026-09-11 | Auth MVP 1 = **JWT** (pas cookies) |
| 2026-09-28 | Congé : date de début ≥ aujourd'hui. Maladie : rétroactif permis jusqu'à 14 jours. Toujours EnAttente à la création
| 2026-09-28 | Création d'un congé pour autrui (Admin/Manager) : reportée, ajoutée dans les bonus du MVP2, nécessite CreatedById
| 2026-09-30 | Ajout du MVP 2.5 — Horaires de travail, avant les meetings (les conflits s'appuieront sur l'horaire effectif)
| 2026-09-30 | Départ anticipé = ajustement d'horaire approuvé par le manager, pas un congé ; aucune banque d'heures, paie hors périmètre
| 2026-09-30 | Fuseaux horaires dès le MVP 2.5 via `Site` (modèle de données) ; temps supplémentaire constaté reporté en Post-MVP

---

## 13. État d’avancement

| MVP | Statut |
|-----|--------|
| MVP 1 — Auth + annuaire | 🔜 À démarrer |
| MVP 2 — Congés + calendrier | ⏳ |
| MVP 2.5 — Horaires de travail | ⏳ |
| MVP 3 — Meetings | ⏳ |
| MVP 4 — Ressources | ⏳ |
| MVP 5 — Helpdesk IT | ⏳ |

---

*Dernière mise à jour : 2026-09-11*
