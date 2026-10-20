# Checklist de la phase 2.5 — Horaires de travail

Référence produit : [PROJET.md](../PROJET.md) §5.7, §6, §7, §8, §9 (R11–R16, R23).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** phase 2 terminée (congés, file d’approbation, calendrier personnel) + correctifs qualité Q1–Q9.

## Préparation

- [x] Relire le périmètre de la phase 2.5 dans `PROJET.md` (inclus / exclu)
- [x] Comprendre l’ordre des couches de disponibilité (R16) : horaire type → férié → ajustement → congé approuvé
- [x] Distinguer les deux types de date/heure :
  - [x] **Heure locale du site** (`DateOnly` + `TimeOnly`) : horaires, fériés, ajustements
  - [x] **Instant UTC** (`DateTimeOffset`) : meetings (phase 3), `CreatedAt`, `ReviewedAt`
- [x] Vérifier la conversion de fuseaux IANA en .NET (`TimeZoneInfo.FindSystemTimeZoneById("America/Toronto")`)
- [x] Choisir comment stocker `DateOnly` / `TimeOnly` dans SQLite avec EF Core (texte ISO, tri correct)

## Backend (API)

- [x] 1. Modèle `Site` (nom, fuseau IANA) + migration
  - [x] Migration : site par défaut « Montréal » (`America/Toronto`), tous les employés existants y sont rattachés
  - [x] `Employee.SiteId` obligatoire
  - [x] Validation : fuseau IANA reconnu, sinon 400 (POST / PUT /api/sites, Admin global)
- [x] 1b. Rôle **Admin global** et portée par site (R23)
  - [x] Nouveau rôle `GlobalAdmin` (libellé « Admin global ») ; l’admin de démo devient Admin global
  - [x] Seul un Admin global peut attribuer le rôle Admin global
  - [x] Endpoints admin (comptes, fiche, création) : un Admin ne voit et ne modifie que les employés de son site ; création = son site imposé
  - [x] Approbations de congés et congé maladie pour autrui (R20) : un Admin seulement pour son site
  - [x] Manager : doit être du même site (validation du manager + recherche d’employés filtrée par site)
  - [x] Changement de site : Admin global seulement ; refusé avec un message clair (ce qu’il faut corriger) si l’employé a un manager ou des subordonnés
  - [x] Tests API : Admin d’un autre site → 404 ; changement de site refusé / accepté
- [x] 2. « Aujourd’hui » calculé dans le fuseau du site de l’employé (remplace le fuseau fixe de Q3)
- [x] 3. Modèles `WorkSchedule` + `WorkScheduleDay` + migration
  - [x] Un horaire = une date d’entrée en vigueur (`EffectiveFrom`) + 0 à 7 jours
  - [x] Un jour = `DayOfWeek`, `StartTime`, `EndTime`, `BreakMinutes`
  - [x] Validation : `StartTime < EndTime` (pas de quart de nuit, voir R11), un seul bloc par jour, pause < durée du bloc
  - [x] Un horaire sans jour travaillé est refusé (ce serait une désactivation, pas un horaire)
- [x] 4. CRUD horaires (Admin : tous ; Manager : son équipe — Admin limité à son site avec 1b)
  - [x] `EffectiveFrom` ≥ aujourd’hui (le passé n’est jamais réécrit)
  - [x] Un horaire futur peut être modifié ou supprimé ; un horaire déjà en vigueur, non (on en crée un nouveau)
  - [x] Deux horaires d’un même employé ne peuvent pas avoir le même `EffectiveFrom`
- [x] 5. Modèle `Holiday` (site, date, nom) + migration
  - [x] Unicité (site, date)
  - [x] Seed : fériés du Québec 2026 et 2027
- [x] 6. CRUD fériés (Admin du site ; l’Admin global s’ajoute avec 1b)
- [x] 7. Modèle `ScheduleAdjustment` + migration
  - [x] Champs : employé, date, `StartTime?`, `EndTime?`, `BreakMinutes`, motif, statut, `CreatedById`, `ReviewedById?`, `ReviewedAt?`
  - [x] Heures nulles = « ne travaille pas ce jour-là » (Manager/Admin seulement)
  - [x] Un seul ajustement actif (EnAttente ou Approuve) par employé et par date
- [x] 7b. Modèle `Absence` + migration (R24)
  - [x] Heures prévues (`PlannedStart?`, `PlannedEnd?`) et réelles (`StartedAt?`, `EndedAt?`), `ReturnConfirmed`, motif, statut, `CreatedById`, `ReviewedById?`, `ReviewedAt?`
  - [x] Plusieurs absences par jour, sans chevauchement ; comprises dans les heures travaillées du jour
  - [x] Prévue : demandée par l’employé (EnAttente) ou créée par le manager / un Admin (approuvée) ; non prévue : approuvée d’office
- [x] 8. Créer un ajustement
  - [x] Employé : seulement pour lui, date ≥ aujourd’hui, statut `EnAttente`, heures obligatoires (jamais une journée à 0 h)
  - [x] Manager / Admin du site : statut `Approuve` directement (jamais pour soi-même)
  - [x] Mêmes validations d’heures que l’horaire type
- [ ] 8b. **Absences** (R24)
  - [ ] Demander une absence prévue (date ≥ aujourd’hui) ; annuler si EnAttente
  - [ ] « Je pars » : démarre l’absence prévue et approuvée du moment, sinon crée une absence non prévue (approuvée d’office)
  - [ ] « Je suis de retour » : ferme l’absence en cours à l’heure réelle (retour en avance ou en retard)
  - [ ] Le manager ou un Admin du site peut confirmer le départ ou le retour à la place de l’employé
  - [ ] Fermeture automatique en fin de journée de travail sans retour confirmé (heure prévue, sinon fin de journée) ; marquée « retour non confirmé »
- [x] 9. Annuler un ajustement (employé : si EnAttente ; manager : tant que la date n’est pas passée)
- [ ] 10. File d’approbation : y ajouter les ajustements et les absences prévues en attente (approuver / refuser, mêmes règles d’équipe que les congés)
- [ ] 11. Service **horaire effectif** : pour un employé et une plage `[from, to]`, renvoie chaque jour avec :
  - [x] les heures prévues (ou « non travaillé ») et la source : `Horaire`, `Ferie`, `Ajustement`, `Conge`, `NonDefini`
  - [x] l’ajustement en attente éventuel (affiché sans être appliqué, comme un congé en attente)
  - [x] le nom du férié le cas échéant
  - [ ] les absences du jour (heures réelles sinon prévues, en cours, retour non confirmé) — avec l’étape 8b
- [x] 12. Endpoint calendrier `GET /api/calendar` : horaire effectif de chaque jour + congés (remplace `/api/leaves/calendar`)
- [ ] 13. Congés : ne compter que les jours travaillés (R14)
  - [ ] Demande qui ne couvre aucun jour travaillé → refusée avec un message clair
  - [ ] Nombre de jours travaillés affiché dans la demande et la file d’approbation
  - [ ] Solde et jours sans solde (R19) recalculés sur les jours travaillés au lieu de lun.–ven.
- [ ] 14. Autorisation : employé = lecture de son horaire ; manager = son équipe ; admin = son site ; Admin global = tout

## Frontend (React)

- [ ] 15. Écran **Horaire d’un employé** (Manager/Admin)
  - [ ] Grille 7 jours : coché / non coché, début, fin, pause
  - [ ] Date d’entrée en vigueur ; historique des horaires passés en lecture seule
  - [ ] Total d’heures par semaine affiché en direct
- [ ] 16. Calendrier en **grille horaire** (vues jour et semaine ; la vue mois reste en journées)
  - [ ] Axe des heures ; la rangée « toute la journée » de la phase 2 reste en haut (congés, fériés)
  - [ ] Heures de travail mises en évidence ; jours non travaillés, fériés et ajustements visuellement distincts
  - [ ] **Passé grisé** : jours passés entièrement, aujourd’hui jusqu’à l’heure exacte
  - [ ] **Ligne « maintenant »**, mise à jour chaque minute, dans le fuseau du site
  - [ ] Défilement automatique vers l’heure actuelle à l’ouverture
- [ ] 17. **Demander un ajustement ou une absence prévue** (employé) + liste de mes demandes avec statut
- [ ] 17b. **Bandeau de présence** en haut de toutes les pages : « Je pars » pendant une journée travaillée ; « Absent depuis 9h05 (retour prévu à 11h) — Je suis de retour » pendant une absence ; absences réelles au calendrier (en cours dessinée jusqu’à maintenant)
- [ ] 18. File d’approbation : ajustements à côté des congés (type clairement indiqué)
- [ ] 19. Vue **équipe semaine** (Manager) : qui travaille quand, d’un coup d’œil
- [ ] 20. Écran **Sites et fériés** (Admin global : créer un site avec fuseau ; Admin du site : gérer ses fériés par année)
- [ ] 20b. Portée par site dans l’UI (R23)
  - [ ] Rôle « Admin global » dans la page Comptes ; menu Sites visible pour lui seulement
  - [ ] Champ site de la fiche : modifiable par l’Admin global seulement ; erreur de changement de site affichée clairement
  - [ ] Annuaire : filtre par site (« Tous les sites » par défaut), gardé dans l’URL ; site affiché sur chaque carte
- [ ] 21. État « Horaire non défini » visible partout où il s’applique (fiche employé, calendrier, vue équipe)

## Validation manuelle

- [ ] Horaire lundi–jeudi → vendredi, samedi, dimanche non travaillés au calendrier
- [ ] La ligne « maintenant » avance seule, page laissée ouverte ; à minuit, le grisé passe au jour suivant
- [ ] Nouvel horaire avec date future → l’ancien s’applique jusqu’à la veille, le nouveau ensuite
- [ ] Noël → non travaillé pour tout le site, sans aucune action
- [ ] Manager ajoute un ajustement le jour de Noël → l’employé travaille ce jour-là, les autres non
- [ ] Employé demande à partir à 16h → en attente au calendrier → approuvé → horaire du jour = fin 16h
- [ ] Congé du lundi au dimanche pour un employé à 3 jours/semaine → 3 jours comptés
- [ ] Congé uniquement sur des jours non travaillés → refusé
- [ ] Employé d’un site à Paris : « aujourd’hui » change à minuit heure de Paris, pas de Montréal
- [ ] Un employé ne voit pas l’horaire des autres ; un manager seulement celui de son équipe
- [ ] Admin de Montréal : ne voit pas les comptes de Paris ; l’annuaire, oui
- [ ] Changer le site d’un employé qui a des subordonnés → refusé, message clair

## Bonus

- [ ] Copier les fériés d’une année vers la suivante (les dates mobiles comme Pâques restent à corriger)
- [ ] Horaire « copié depuis » un collègue
- [ ] Tests API : couches de disponibilité (R16), changement d’heure, comptage des jours de congé

## Notes / blocages

_(À remplir au fil de l’eau)_

-
