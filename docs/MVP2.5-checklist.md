# Checklist MVP 2.5 — Horaires de travail

Référence produit : [PROJET.md](../PROJET.md) §5.7, §6, §7, §8, §9 (R11–R16).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** MVP 2 terminé (congés, file d’approbation, calendrier personnel) + correctifs qualité Q1–Q9.

## Préparation

- [ ] Relire le périmètre MVP 2.5 dans `PROJET.md` (inclus / exclu)
- [ ] Comprendre l’ordre des couches de disponibilité (R16) : horaire type → férié → ajustement → congé approuvé
- [ ] Distinguer les deux types de date/heure :
  - [ ] **Heure locale du site** (`DateOnly` + `TimeOnly`) : horaires, fériés, ajustements
  - [ ] **Instant UTC** (`DateTimeOffset`) : meetings (MVP 3), `CreatedAt`, `ReviewedAt`
- [ ] Vérifier la conversion de fuseaux IANA en .NET (`TimeZoneInfo.FindSystemTimeZoneById("America/Toronto")`)
- [ ] Choisir comment stocker `DateOnly` / `TimeOnly` dans SQLite avec EF Core (texte ISO, tri correct)

## Backend (API)

- [ ] 1. Modèle `Site` (nom, fuseau IANA) + migration
  - [ ] Migration : site par défaut « Montréal » (`America/Toronto`), tous les employés existants y sont rattachés
  - [ ] `Employee.SiteId` obligatoire
  - [ ] Validation : fuseau IANA reconnu, sinon 400
- [ ] 2. « Aujourd’hui » calculé dans le fuseau du site de l’employé (remplace le fuseau fixe de Q3)
- [ ] 3. Modèles `WorkSchedule` + `WorkScheduleDay` + migration
  - [ ] Un horaire = une date d’entrée en vigueur (`EffectiveFrom`) + 0 à 7 jours
  - [ ] Un jour = `DayOfWeek`, `StartTime`, `EndTime`, `BreakMinutes`
  - [ ] Validation : `StartTime < EndTime` (pas de quart de nuit, voir R11), un seul bloc par jour, pause < durée du bloc
  - [ ] Un horaire sans jour travaillé est refusé (ce serait une désactivation, pas un horaire)
- [ ] 4. CRUD horaires (Admin : tous ; Manager : son équipe)
  - [ ] `EffectiveFrom` ≥ aujourd’hui (le passé n’est jamais réécrit)
  - [ ] Un horaire futur peut être modifié ou supprimé ; un horaire déjà en vigueur, non (on en crée un nouveau)
  - [ ] Deux horaires d’un même employé ne peuvent pas avoir le même `EffectiveFrom`
- [ ] 5. Modèle `Holiday` (site, date, nom) + migration
  - [ ] Unicité (site, date)
  - [ ] Seed : fériés du Québec 2026 et 2027
- [ ] 6. CRUD fériés (Admin uniquement)
- [ ] 7. Modèle `ScheduleAdjustment` + migration
  - [ ] Champs : employé, date, `StartTime?`, `EndTime?`, `BreakMinutes`, motif, statut, `CreatedById`, `ReviewedById?`, `ReviewedAt?`
  - [ ] Heures nulles = « ne travaille pas ce jour-là » (Manager/Admin seulement)
  - [ ] Un seul ajustement actif (EnAttente ou Approuve) par employé et par date
- [ ] 8. Créer un ajustement
  - [ ] Employé : seulement pour lui, date ≥ aujourd’hui, statut `EnAttente`, heures obligatoires (jamais une journée à 0 h)
  - [ ] Manager/Admin : pour son équipe / tous, statut `Approuve` directement
  - [ ] Mêmes validations d’heures que l’horaire type
- [ ] 9. Annuler un ajustement (employé : si EnAttente ; manager : tant que la date n’est pas passée)
- [ ] 10. File d’approbation : y ajouter les ajustements en attente (approuver / refuser, mêmes règles d’équipe que les congés)
- [ ] 11. Service **horaire effectif** : pour un employé et une plage `[from, to]`, renvoie chaque jour avec :
  - [ ] les heures prévues (ou « non travaillé ») et la source : `Horaire`, `Ferie`, `Ajustement`, `Conge`, `NonDefini`
  - [ ] l’ajustement en attente éventuel (affiché sans être appliqué, comme un congé en attente)
  - [ ] le nom du férié le cas échéant
- [ ] 12. Endpoint calendrier : intégrer l’horaire effectif à la réponse existante
- [ ] 13. Congés : ne compter que les jours travaillés (R14)
  - [ ] Demande qui ne couvre aucun jour travaillé → refusée avec un message clair
  - [ ] Nombre de jours travaillés affiché dans la demande et la file d’approbation
- [ ] 14. Autorisation : employé = lecture de son horaire ; manager = son équipe ; admin = tout

## Frontend (React)

- [ ] 15. Écran **Horaire d’un employé** (Manager/Admin)
  - [ ] Grille 7 jours : coché / non coché, début, fin, pause
  - [ ] Date d’entrée en vigueur ; historique des horaires passés en lecture seule
  - [ ] Total d’heures par semaine affiché en direct
- [ ] 16. Calendrier personnel : jours travaillés, non travaillés, fériés et ajustements visuellement distincts
- [ ] 17. **Demander un ajustement** (employé) + liste de mes ajustements avec statut
- [ ] 18. File d’approbation : ajustements à côté des congés (type clairement indiqué)
- [ ] 19. Vue **équipe semaine** (Manager) : qui travaille quand, d’un coup d’œil
- [ ] 20. Écran Admin **Sites et fériés** (créer un site avec fuseau, gérer les fériés par année)
- [ ] 21. État « Horaire non défini » visible partout où il s’applique (fiche employé, calendrier, vue équipe)

## Validation manuelle

- [ ] Horaire lundi–jeudi → vendredi, samedi, dimanche non travaillés au calendrier
- [ ] Nouvel horaire avec date future → l’ancien s’applique jusqu’à la veille, le nouveau ensuite
- [ ] Noël → non travaillé pour tout le site, sans aucune action
- [ ] Manager ajoute un ajustement le jour de Noël → l’employé travaille ce jour-là, les autres non
- [ ] Employé demande à partir à 16h → en attente au calendrier → approuvé → horaire du jour = fin 16h
- [ ] Congé du lundi au dimanche pour un employé à 3 jours/semaine → 3 jours comptés
- [ ] Congé uniquement sur des jours non travaillés → refusé
- [ ] Employé d’un site à Paris : « aujourd’hui » change à minuit heure de Paris, pas de Montréal
- [ ] Un employé ne voit pas l’horaire des autres ; un manager seulement celui de son équipe

## Bonus

- [ ] Copier les fériés d’une année vers la suivante (les dates mobiles comme Pâques restent à corriger)
- [ ] Horaire « copié depuis » un collègue
- [ ] Tests API : couches de disponibilité (R16), changement d’heure, comptage des jours de congé

## Notes / blocages

_(À remplir au fil de l’eau)_

-
