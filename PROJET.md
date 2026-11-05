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
|---------|-------|-------|
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
| **Admin global** | Tout ce que fait un Admin, sur tous les sites ; gérer les sites ; changer le site d’un employé ; nommer d’autres Admins globaux |
| **Admin** | Créer/désactiver des comptes, gérer rôles, horaires et approbations des employés de **son site** |
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
- Un Admin ne gère que les employés de **son site** ; l’Admin global gère tous les sites (R23, introduit en phase 2.5).

---

## 5. Modules fonctionnels (cible)

### 5.1 Annuaire interne

- Liste des employés actifs (nom, poste, équipe/département, contact).
- Recherche (nom, équipe, poste) ; filtre par langue parlée.
- L’annuaire montre **tous les sites** ; un filtre par site permet de n’afficher qu’un site (gardé dans l’URL).
- Fiche employé (détails de contact, manager optionnel).
- Coordonnées **professionnelles** publiques (numéro du site + poste, cellulaire pro, prénom d’usage, langues parlées, local, date d’embauche) et **personnelles** privées (cellulaire, email perso, adresse, anniversaire, contacts d’urgence), saisies par l’employé dans « Mon profil » (R28).
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
- **Blocs personnels** : l’employé modèle son horaire avec des blocs à lui (ex. « Travailler sur le projet X »).
  - Un bloc peut être **souple** : un meeting d’équipe peut le chevaucher sans conflit ; le bloc reste affiché, pâli, à côté du meeting, et son propriétaire est notifié (R21).
  - Chaque employé choisit la **couleur** de chacun de ses événements, dans une palette fixe (R22).
- Meeting **de groupe** avec plusieurs employés.
- Meetings **récurrents** (quotidiens, hebdomadaires, mensuels) avec exceptions : modifier ou annuler une occurrence, les suivantes ou toute la série.
- **Libre / occupé** : à la création d’un meeting, on voit si les participants sont disponibles, sans jamais voir le détail de leurs événements.
- **Trouver un créneau** : le portail propose les premiers moments où tous les participants sont libres, en tenant compte des horaires, des fuseaux, des congés et des blocs (R27).
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
- **Base de connaissances** : articles rédigés par l’équipe IT, suggérés pendant qu’on écrit un ticket ; « Ça règle mon problème » évite de créer le ticket.

### 5.6 Notifications

Types prévus (non exhaustif) :

- Congé approuvé / refusé
- Meeting de groupe créé / conflit d’horaire
- Tentative de meeting avec personne en congé (blocage + message)
- Ticket IT assigné / mis à jour
- Rappel « prévoir un slot » pour une tâche IT

Canal initial : **notifications in-app** (table + cloche / liste). Email = bonus ultérieur.
Phase 5 : **temps réel** (SignalR) — la cloche se met à jour sans recharger la page.

### 5.7 Horaires de travail

- Un Manager ou un Admin définit l’**horaire type** hebdomadaire d’un employé (ex. lundi au jeudi, 8h–17h, 30 min de pause).
- L’horaire se répète chaque semaine sans intervention ; un changement = un **nouvel horaire** avec une date d’entrée en vigueur (le passé n’est jamais réécrit).
- Les jours absents de l’horaire sont **non travaillés**, ce ne sont pas des congés.
- Chaque employé appartient à un **site** (fuseau horaire IANA + jours fériés propres).
  - Un Admin ne gère que les employés de son site ; un employé et son manager sont toujours du même site (R23).
  - Seul l’Admin global change le site d’un employé, et seulement s’il n’a ni manager ni subordonnés.
- Les **jours fériés** du site sont non travaillés par défaut ; un manager peut faire travailler quelqu’un ce jour-là.
- Un **ajustement d’horaire** modifie une seule date (partir à 16h, venir un vendredi, travailler un férié) sans passer par Congés.
  - Demandé par l’employé → approbation du manager (même file que les congés).
  - Créé par le manager → approuvé directement.
  - Aucune banque d’heures : le portail enregistre les heures prévues, la paie reste hors périmètre.
- Une **absence** est un moment où l’on s’absente pendant une journée de travail, sans toucher au solde de congés ; il peut y en avoir plusieurs par jour, sans chevauchement.
  - **Prévue** (ex. rendez-vous de 10h à 12h) : demandée, puis approuvée par le manager.
  - **Non prévue** (urgence) : créée par « Je pars », approuvée d’office.
  - « Je pars » et « Je suis de retour » enregistrent les heures **réelles** ; le calendrier les affiche quand elles existent, sinon les heures prévues (R24).
  - Le manager ou un Admin du site peut confirmer le départ ou le retour à la place de l’employé.
  - Sans retour confirmé, l’absence se ferme à la fin de la journée de travail (heure prévue, sinon fin de journée), marquée « retour non confirmé ».
- La disponibilité d’une personne se calcule par couches : horaire type → férié → ajustement → absences → congé approuvé (→ meetings à la phase 3).

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
- Rôle **Admin global** ; Admin limité à son site ; manager du même site (R23)
- Annuaire : filtre par site
- Horaire type hebdomadaire versionné par date d’entrée en vigueur
- Ajustements d’horaire ponctuels (demande employé + approbation, ou création directe par le manager)
- **Grille horaire** du calendrier : axe des heures, heures de travail mises en évidence, passé grisé et ligne « maintenant »
- Calendrier perso et vue équipe semaine avec l’horaire effectif
- Congés : seuls les jours travaillés sont comptés

**Exclu :** temps supplémentaire constaté, banque d’heures, paie, quarts de nuit (passant minuit)

**Critères de done :**
- [x] Horaire lundi–jeudi → autres jours non travaillés, chaque semaine
- [x] Férié non travaillé automatiquement, sauf ajustement du manager
- [x] Départ anticipé demandé, approuvé, visible au calendrier
- [x] Congé sur une semaine complète → seuls les jours travaillés comptés
- [x] « Aujourd’hui » calculé dans le fuseau du site de l’employé
- [x] Un Admin ne voit et ne modifie que les employés de son site ; l’Admin global, tous

---

### Phase 3 — Meetings (solo / groupe) + conflits

**Objectif :** planifier des réunions avec règles de conflit et de congé.

**Inclus :**
- Création de meeting (titre, créneau, participants)
- Blocs personnels, fermes ou **souples** (R21), et couleur par événement (R22)
- Interdiction si un participant est en **congé approuvé**
- Détection de conflits d’horaire
- Priorité du meeting d’équipe + notification aux personnes en conflit
- Affichage des meetings dans la grille horaire (chevauchements côte à côte) et dans la vue mois
- **Libre / occupé** des participants à la création d’un meeting
- Endpoint calendrier : réponse par jour (horaire effectif, congés) + liste des meetings

**Exclu :** meetings récurrents (phase 3.5), réservation salle/équipement obligatoire, tickets IT

**Critères de done :**
- [ ] Bloc personnel OK (ferme et souple, couleur choisie)
- [ ] Meeting groupe OK
- [ ] Meeting d’équipe sur un bloc souple : aucun conflit, bloc pâli, propriétaire notifié
- [ ] Blocage si participant en congé
- [ ] Conflit → notification(s)
- [ ] Calendrier à jour pour tous les participants
- [ ] Un employé voit le libre / occupé d’un collègue, jamais le titre de ses événements

---

### Phase 3.1 — Coordonnées et profil

**Objectif :** séparer ce que tout le monde peut voir de ce qui reste privé, avant que des visiteurs voient l’annuaire dans la démo publique.

**Inclus :**
- Numéro principal du site + poste de l’employé ; cellulaire professionnel optionnel (les numéros existants y sont migrés)
- Cellulaire et email personnels, adresse domiciliaire, 0 à 2 contacts d’urgence, dans des tables à part (R28)
- Prénom d’usage, langues parlées (filtre de l’annuaire), local, date d’embauche (ancienneté sur la fiche)
- Anniversaire privé par défaut ; widget « Anniversaires de la semaine » à l’accueil pour ceux qui l’affichent
- Page **Mon profil** : l’employé consulte sa fiche et modifie ses coordonnées personnelles

**Exclu :** infos de paie, pièces d’identité, informations médicales

**Critères de done :**
- [ ] L’annuaire affiche « 514 555-1234, poste 2045 » et jamais une coordonnée personnelle
- [ ] Un employé modifie son contact d’urgence dans Mon profil
- [ ] Son manager voit le contact d’urgence, pas son cellulaire personnel ; un collègue ne voit ni l’un ni l’autre
- [ ] « Alex » s’affiche partout pour Alexandra ; chercher « Alexandra » ou « Alex » la trouve
- [ ] Filtrer l’annuaire sur « anglais » ne montre que les employés qui le parlent

---

### Phase 3.5 — Meetings récurrents

**Objectif :** planifier une fois une réunion qui revient (ex. réunion d’équipe chaque lundi).

**Inclus :**
- Règle de récurrence : quotidienne, hebdomadaire (jours choisis), mensuelle ; intervalle (« toutes les 2 semaines ») ; fin par date ou par nombre d’occurrences
- Occurrences calculées à la volée sur `[from, to]` (pas de copie en base), exceptions stockées
- Modifier / annuler : cette occurrence, cette occurrence et les suivantes, ou toute la série
- Règles R6–R8 et horaire effectif vérifiés pour chaque occurrence
- Récurrence en heure locale : une réunion à 10h reste à 10h après un changement d’heure
- **Trouver un créneau** pour un meeting simple ou récurrent (R27)

**Exclu :** invitations externes (iCal / email), salles récurrentes (phase 4)

**Critères de done :**
- [ ] Réunion chaque lundi à 10h visible toutes les semaines, sans saisie répétée
- [ ] Une occurrence déplacée ou annulée n’affecte pas les autres
- [ ] « Cette occurrence et les suivantes » coupe la série proprement
- [ ] Une occurrence en conflit avec un congé approuvé est signalée
- [ ] 10h reste 10h avant et après le passage à l’heure normale
- [ ] « Trouver un créneau » propose 3 moments où tout le monde est libre, fuseaux compris

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
- Base de connaissances IT suggérée à la création d’un ticket
- Notifications en **temps réel** (SignalR)

**Critères de done :**
- [ ] Employé crée une demande
- [ ] Agent IT voit toutes les demandes et assigne
- [ ] Assigné notifié + warning créneau
- [ ] Demandeur notifié
- [ ] Permissions respectées
- [ ] En tapant « VPN », l’article correspondant est suggéré
- [ ] Une notification apparaît sans recharger la page

---

### Phase 6 — Continuité

**Objectif :** le portail continue de fonctionner quand quelqu’un s’absente ou quitte l’entreprise.

**Inclus :**
- **Remplaçant d’approbation** optionnel pour une période ; Admin du site notifié quand une demande attend un manager absent sans remplaçant (R25)
- **Date de départ** : désactivation du compte à la date prévue, retrait des meetings futurs, demandes en attente annulées (R26)

**Exclu :** transfert de dossiers, organigramme

**Critères de done :**
- [ ] Manager en congé : son remplaçant approuve les demandes de l’équipe, sauf les siennes
- [ ] Sans remplaçant : l’Admin du site est notifié de la demande en attente
- [ ] Date de départ atteinte : connexion refusée, plus dans aucun meeting futur, sans action manuelle

---

### Jalon — Démo publique

**Objectif :** une personne qui découvre le projet (ex. recruteur) le teste en ligne en 30 secondes, sans rien installer.

Après la phase 3.1 (aucune coordonnée personnelle visible dans l’annuaire) ; les meetings de la phase 3 rendent la démo parlante. Mise à jour à chaque phase.

**Inclus :**
- Hébergement en ligne (HTTPS) de l’API et de React
- Mode démo (configuration) : bandeau « Démo », boutons « Se connecter comme Employé / Manager / Admin » sur la page de connexion
- Données de démo générées **par rapport à aujourd’hui** (le calendrier n’est jamais vide), remises à zéro chaque nuit
- Actions bloquées en démo : mot de passe, rôle et désactivation des comptes de démo

**Critères de done :**
- [ ] Lien public → connecté en un clic avec chaque rôle
- [ ] Le calendrier de la semaine courante contient des congés et des meetings
- [ ] Le lendemain, les modifications d’un visiteur ont disparu

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
- **Role** : enum ou table (`Admin`, `Employee`, puis `GlobalAdmin`, `Manager`, `ItAgent`, …)

### Phase 2
- **LeaveRequest** : Id, EmployeeId, StartDate, EndDate, Type, Status, Reason?, ReviewedById?, ReviewedAt?, CreatedAt

### Phase 2.5
- **Site** : Id, Name, TimeZoneId (IANA) — `Employee.SiteId` obligatoire
- **WorkSchedule** : Id, EmployeeId, EffectiveFrom, CreatedById, CreatedAt
- **WorkScheduleDay** : WorkScheduleId, DayOfWeek, StartTime, EndTime, BreakMinutes
- **Holiday** : Id, SiteId, Date, Name
- **ScheduleAdjustment** : Id, EmployeeId, Date, StartTime?, EndTime?, BreakMinutes, Reason?, Status, CreatedById, ReviewedById?, ReviewedAt?, CreatedAt
- **Absence** : Id, EmployeeId, Date, PlannedStart?, PlannedEnd?, StartedAt?, EndedAt?, ReturnConfirmed, Reason?, Status, CreatedById, ReviewedById?, ReviewedAt?, CreatedAt

### Phase 3
- **CalendarEvent / Meeting** : Id, Title, Description?, StartAt, EndAt, OrganizerId, IsTeamMeeting, IsFlexible (bloc personnel seulement), CreatedAt
- **MeetingParticipant** : MeetingId, EmployeeId, ResponseStatus?, Color? (choisie par chaque personne, organisateur compris)

### Phase 3.1
- `Site.MainPhoneNumber?`, `Site.CountryCode` (ISO 3166) ; numéros enregistrés en E.164 ; `Employee.PhoneExtension?` ; `Employee.PhoneNumber` renommé `WorkMobilePhone?` (données conservées)
- `Employee.PreferredName?` (+ clé de recherche), `Employee.OfficeLocation?`, `Employee.HireDate` (DateOnly)
- **EmployeeLanguage** : EmployeeId, LanguageCode (ISO 639-1, liste fixe) — clé composée
- **EmployeePrivateInfo** : EmployeeId (clé), PersonalMobilePhone?, PersonalEmail?, AddressLine1?, AddressLine2?, City?, Region?, PostalCode?, Country?, BirthMonth?, BirthDay?, ShowBirthday, UpdatedAt
- **EmergencyContact** : Id, EmployeeId, Name, Relationship, PhoneNumber, SortOrder (1 ou 2)

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

### Phase 5 (suite)
- **KnowledgeArticle** : Id, Title, Body, TitleKey (recherche sans accents), IsPublished, AuthorId, HelpfulCount, CreatedAt, UpdatedAt

### Phase 6
- **ApprovalDelegation** : Id, ManagerId, DelegateId, StartDate, EndDate, CreatedAt
- `Employee.DepartureDate?` (DateOnly, heure locale du site)
- Décisions d’approbation : `ReviewedOnBehalfOfId?`

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
10d. (Admin global) Sites ; (Admin du site) jours fériés
10e. Calendrier en grille horaire (heures de travail, passé grisé, ligne « maintenant »)

### Phase 3
11. Créer un meeting
12. Détail meeting
13. Calendrier enrichi (meetings + congés) + libre / occupé à la création

### Phase 3.1
13d. Mon profil (fiche, prénom d’usage, langues, coordonnées personnelles, adresse, anniversaire, contacts d’urgence)
13f. Accueil : widget « Anniversaires de la semaine »
13e. Formulaire employé et fiche : poste, cellulaire pro, section personnelle (Admin du site) ; numéro principal dans l’écran Sites

### Phase 3.5
13a. Créer un meeting récurrent (règle + aperçu en clair : « Chaque lundi à 10h, jusqu’au 31 déc. »)
13b. Modifier / annuler : cette occurrence, les suivantes ou toute la série
13c. « Trouver un créneau » dans l’écran de création (R27)

### Phase 4
14. Catalogue salles / équipements
15. Réservation / sélection ressource à la création de meeting

### Phase 5
16. Mes tickets IT
17. Nouveau ticket
18. Console IT (toutes les demandes, assignation)
19. Centre de notifications (temps réel)
20. Base de connaissances : liste / article ; (IT) rédiger un article ; suggestions dans « Nouveau ticket »

### Phase 6
21. (Manager) Désigner un remplaçant
22. (Admin du site) Date de départ dans la fiche employé

### Démo publique
23. Page de connexion en mode démo (boutons par rôle) + bandeau « Démo »

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
| R7 | Meeting d’équipe = organisé par un manager (au moins un subordonné actif) ou un Admin, statut fixé à la création ; en cas de chevauchement il est prioritaire : avertissement renforcé à l’organisateur d’un meeting ordinaire, personnes en conflit notifiées ; un conflit ne bloque jamais (sauf congé approuvé, R6) |
| R8 | Si salle/équipement demandé : doit être libre sur **tout** le créneau, sinon refus |
| R9 | Tickets IT : vue globale réservée aux rôles IT / Admin |
| R10 | Assignation ticket → warning « prévoir un slot » pour l’assigné + notif demandeur |
| R11 | Horaire type récurrent, versionné par `EffectiveFrom` ≥ aujourd’hui ; jours absents = non travaillés ; un bloc par jour, sans passer minuit |
| R12 | Jour férié du site = non travaillé, sauf ajustement créé par un Manager/Admin |
| R13 | Ajustement demandé par l’employé = EnAttente jusqu’à approbation ; créé par Manager/Admin = approuvé ; jamais de journée à 0 h demandée par l’employé ; aucune banque d’heures |
| R14 | Un congé ne compte que les jours travaillés ; une demande sans aucun jour travaillé est refusée |
| R15 | Horaires, fériés et ajustements en heure locale du site ; instants (meetings, horodatages) en UTC |
| R16 | Disponibilité = horaire type → férié → ajustement approuvé → absences → congé approuvé (la couche suivante l’emporte) |
| R17 | Libre / occupé : un employé voit les créneaux occupés de ses collègues, jamais le titre, le motif ni les participants |
| R18 | Meeting récurrent : chaque occurrence respecte R6–R8 ; une modification vise une occurrence, les suivantes ou toute la série ; la série suit l’heure locale du fuseau de l’organisateur |
| R19 | Solde annuel de congés (20 jours par défaut, modifiable par l’Admin par employé, année civile) : les congés en attente et approuvés de l’année consomment le solde dans l’**ordre de création** des demandes ; les jours au-delà sont sans solde (calculés à l’affichage, jamais stockés) ; la maladie ne consomme pas le solde |
| R20 | Congé maladie pour autrui : Admin (son site) ou Manager (équipe directe), jamais pour soi ; approuvé directement ; `CreatedById` conservé |
| R21 | Bloc personnel **souple** : seul un meeting d’équipe peut le chevaucher sans conflit ; le bloc reste affiché, pâli, à côté du meeting, n’est jamais supprimé ni coupé, et son propriétaire est notifié ; il apparaît « occupé souple » dans le libre / occupé ; un meeting ordinaire sur un bloc souple = conflit normal |
| R22 | Couleur d’un événement : choisie par chaque personne dans son propre calendrier, parmi une palette fixe qui exclut les couleurs des congés ; couleur par défaut si aucune n’est choisie ; ne change jamais celle des autres participants |
| R23 | Portée par site : un Admin gère seulement les employés de son site (comptes, fiches, approbations, horaires, ajustements, congé maladie pour autrui) ; l’Admin global gère tous les sites, les sites eux-mêmes et le changement de site ; manager et subordonnés toujours du même site ; changement de site refusé tant que l’employé a un manager ou des subordonnés ; annuaire et meetings restent ouverts à tous les sites |
| R24 | Absence dans la journée : prévue (demandée, approuvée) ou non prévue (« Je pars », approuvée d’office) ; plusieurs par jour sans chevauchement ; « Je pars » et « Je suis de retour » enregistrent les heures réelles, affichées au calendrier ; confirmables aussi par le manager ou un Admin du site ; sans retour confirmé, fermeture à la fin de la journée de travail, marquée « retour non confirmé » ; un meeting chevauché n’est jamais annulé, l’organisateur et l’employé sont notifiés (phase 3) |
| R25 | Remplaçant d’approbation : un manager désigne un remplaçant pour une période (employé actif du même site, jamais lui-même) ; pendant la période, le remplaçant approuve les demandes de l’équipe, sauf les siennes ; un Admin du site peut toujours approuver (R23) ; sans remplaçant, une demande qui arrive pendant un congé approuvé du manager, ou pour un employé dont le manager est parti, notifie les Admins du site ; la décision garde qui a approuvé « au nom de » qui ; le manager garde ses droits pendant la période |
| R26 | Date de départ (heure locale du site, ≥ aujourd’hui, fixée par un Admin du site, modifiable ou annulable avant la date) : dès cette date, connexion refusée (R4), retrait des meetings futurs (organisateurs notifiés), annulation de ses meetings futurs avec participants (participants notifiés) et de ses demandes en attente ; à la saisie, avertissement listant ses subordonnés et ses meetings organisés ; tant que ses subordonnés n’ont pas de nouveau manager, leurs demandes notifient les Admins du site (R25) |
| R27 | Trouver un créneau : pour des participants et une durée, propose les 3 premiers créneaux sur les 14 prochains jours, par tranches de 15 min, jamais dans le passé, à l’intérieur de l’horaire effectif de chacun (dans son fuseau), sans congé, absence, meeting ni bloc ferme ; un bloc souple n’est ignoré que pour un meeting d’équipe (R21) ; sans résultat, message clair indiquant qui bloque le plus (« sans Sam : 4 créneaux ») |
| R28 | Coordonnées : **professionnelles** (email, numéro du site + poste, cellulaire professionnel, prénom d’usage, langues parlées, local, date d’embauche) visibles dans l’annuaire ; **personnelles** (cellulaire et email personnels, adresse domiciliaire, anniversaire, 0 à 2 contacts d’urgence) dans des tables à part, jamais renvoyées par l’annuaire ; lecture : l’employé, les Admins de son site (R23) et l’Admin global ; son manager direct voit seulement les contacts d’urgence (jamais l’adresse) ; modification : l’employé (« Mon profil ») ou un Admin de son site ; supprimées à la date de départ (R26) ; le prénom d’usage remplace le prénom partout où un nom est affiché, la recherche trouve les deux ; l’anniversaire (jour et mois, sans l’année) n’est montré aux collègues que si l’employé l’a choisi (29 février : affiché le 28 les années non bissextiles) |

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
│   ├── phase-3.1-checklist.md
│   ├── phase-3.5-checklist.md
│   ├── phase-4-checklist.md
│   ├── phase-5-checklist.md
│   ├── phase-6-checklist.md
│   └── demo-publique-checklist.md
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
| 2026-10-18 | Blocs personnels souples (R21) : seul un meeting d’équipe passe par-dessus, le bloc reste affiché pâli, « occupé souple » dans le libre / occupé ; couleur par événement et par personne, palette fixe (R22)
| 2026-10-18 | Absences séparées des ajustements (R24) : prévues ou non prévues (urgence), plusieurs par jour, « Je pars » / « Je suis de retour » avec heures réelles, fermeture automatique en fin de journée ; remplace la plage d’absence dans un ajustement
| 2026-10-18 | Retard sur une absence (R24, première version) : prolongation le jour même, appliquée sans approbation, manager informé ; meeting chevauché conservé, organisateur et employé notifiés
| 2026-10-18 | Portée par site (R23) : rôle Admin global ; Admin limité à son site ; manager du même site ; changement de site par l’Admin global seulement, refusé s’il y a un manager ou des subordonnés ; annuaire filtrable par site
| 2026-10-31 | Phase 3 : tout le monde crée des meetings avec tout employé actif (tous sites) ; meeting d’équipe = organisé par un manager (≥ 1 subordonné actif) ou un Admin, fixé à la création (`IsTeamMeeting`), aucune case à cocher ; bouton « Ajouter mon équipe » pour un manager
| 2026-10-31 | Conflit sur un meeting ordinaire : avertissement, l’organisateur confirme, personnes en conflit notifiées ; chevauchement d’un meeting d’équipe = avertissement renforcé (« prioritaire ») ; congé approuvé = refus (R6) ; modifier / annuler = organisateur ou Admin du site de l’organisateur
| 2026-10-31 | Ajouts : « Trouver un créneau » (phase 3.5, R27) ; base de connaissances IT et notifications temps réel SignalR (phase 5) ; nouvelle phase 6 — Continuité (remplaçant d’approbation optionnel + notification à l’Admin du site R25, date de départ R26) ; jalon Démo publique (recommandé après la phase 3)
| 2026-11-01 | Phase 3.1 — Coordonnées : pro publiques (numéro du site + poste, cellulaire pro) vs personnelles privées en tables à part (R28) ; l’employé les saisit dans « Mon profil » ; manager = contacts d’urgence seulement ; anciens numéros migrés en cellulaire pro ; adresse domiciliaire privée (jamais visible du manager) ; prénom d’usage, langues parlées, local, date d’embauche ; anniversaire privé par défaut avec widget d’accueil ; pas de pronoms ni d’infos médicales ; numéros formatés pendant la saisie selon le pays (`libphonenumber-js` / `libphonenumber-csharp`), enregistrés en E.164 ; avant la démo publique
| 2026-11-01 | Meeting hors de l’horaire effectif d’un participant (jour non travaillé, férié, hors des heures, absence approuvée) : avertissement dans le même 409 que les conflits, levé par `confirmConflicts` ; participants seulement (jamais l’organisateur ni un bloc personnel) ; horaire non défini = aucun avertissement
| 2026-11-01 | Notifications de conflit (R7, R21) : une seule notification par participant — l’invitation devient « conflit » (avec « Meeting d’équipe : il a priorité. » si d’équipe) ou « bloc souple chevauché » ; meeting d’équipe sur un meeting ordinaire d’un participant : l’organisateur de ce meeting ordinaire est prévenu (« Jane Test a un meeting d’équipe prioritaire pendant « Point projet » »), sans le titre du meeting d’équipe (R17)
| 2026-11-01 | Détail d’un meeting : organisateur, participants et Admin du site de l’organisateur ; bloc personnel : propriétaire seulement ; sinon 404 sans révéler l’existence (R17)
| 2026-11-01 | Modifier / annuler (étape 9) : organisateur ou Admin du site (bloc : propriétaire seulement) ; verrouillé dès que le meeting commence ; annuler = suppression définitive, participants notifiés ; modification revalidée comme une création (409 à confirmer, le meeting lui-même ignoré) ; participants restants notifiés seulement si l’heure ou le titre change, ajoutés invités, retirés prévenus, organisateur prévenu quand un Admin agit à sa place
| 2026-11-01 | Libre / occupé (étape 10b) : consultable par tout employé pour tout employé actif ; par jour, heures de travail ou « non travaillé » (congé, férié et jour de repos confondus) ou « non défini » ; créneaux « occupé » (meeting, bloc ferme), « occupé souple » (bloc souple), « absent » (absence approuvée) ; jamais de titre, de participants, de type de congé ni de motif (R17) ; tout en UTC
| 2026-11-02 | Absence qui chevauche un meeting (étape 10c, R24) : meetings de groupe seulement (blocs ignorés), jamais annulés ; notifiés à l’approbation (ou à la création approuvée d’office), au « Je pars » imprévu ou en avance, et au retour tardif ; une absence approuvée puis annulée prévient aussi ; destinataires : l’organisateur, et l’employé seulement si quelqu’un d’autre (manager, Admin, système) a agi ; manager prévenu d’un départ imprévu et d’un retour non confirmé ; retour tardif et non confirmé détectés par une tâche de fond, une seule fois par absence
| 2026-11-02 | Congé approuvé qui chevauche un meeting (étape 10d) : meeting conservé ; employé participant → organisateur notifié ; employé organisateur → participants notifiés, et son manager pour un meeting d’équipe ; une modification qui garde la même heure ne revérifie les congés et les heures de travail que des participants ajoutés (sinon l’organisateur en congé ne pouvait même plus corriger un titre) ; les congés passés (maladie rétroactive) ne notifient rien ; employé prévenu aussi, puisque c’est toujours quelqu’un d’autre qui approuve
| 2026-11-03 | Écran « Créer un événement » (étape 12) : grande modale ouverte depuis le calendrier (`?nouvel-evenement=1`) ; heures choisies dans des listes par tranches de 15 min (fin proposée en durées, jusqu’à minuit) ; libre / occupé en frise horaire, une ligne par personne, cliquable pour placer le début ; un 409 (conflits, hors horaire) s’affiche dans la modale avec « Créer quand même »
| 2026-11-05 | Créer un événement (étape 12, suite) : « Créer quand même » ne vaut que pour la requête exacte qui a reçu le 409 (toute modification du formulaire fait revérifier) ; jamais de créneau dans le passé (début à la prochaine tranche de 15 min, date minimale = premier jour qui en a encore) ; libre / occupé : plage d’heures = heures de travail de tous + le créneau (8 h – 18 h si aucune), affichée dans le fuseau de la personne qui regarde, 20 personnes au maximum (comme l’API) ; ordre des étapes front : 14 (calendrier enrichi) avant 13 (détail), pour ouvrir le détail depuis le calendrier
| 2026-10-12 | Ajout de la phase 3.5 — Meetings récurrents (occurrences calculées, exceptions stockées, R18)

---

## 13. État d’avancement

| Phase | Statut |
|-----|--------|
| Phase 1 — Auth + annuaire | ✅ Terminée |
| Phase 2 — Congés + calendrier | ✅ Terminée |
| Phase 2.5 — Horaires de travail | ✅ Terminée |
| Phase 3 — Meetings | ⏳ |
| Phase 3.1 — Coordonnées et profil | ⏳ |
| Phase 3.5 — Meetings récurrents | ⏳ |
| Phase 4 — Ressources | ⏳ |
| Phase 5 — Helpdesk IT | ⏳ |
| Phase 6 — Continuité | ⏳ |
| Jalon — Démo publique | ⏳ |

---

*Dernière mise à jour : 2026-10-31*
