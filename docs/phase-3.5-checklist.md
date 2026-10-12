# Checklist de la phase 3.5 — Meetings récurrents

Référence produit : [PROJET.md](../PROJET.md) §5.3, §6, §7, §8, §9 (R6–R8, R15, R18).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** phase 3 (meetings simples, conflits, libre / occupé, grille horaire enrichie).

## Préparation

- [ ] Relire le périmètre de la phase 3.5 dans `PROJET.md` (inclus / exclu)
- [ ] Comprendre le principe : on stocke la **règle** et les **exceptions**, jamais une copie de chaque occurrence
- [ ] Lire la notion de `RRULE` (RFC 5545) : on en implémente un sous-ensemble (`FREQ`, `INTERVAL`, `BYDAY`, `UNTIL`, `COUNT`)
- [ ] Figer le cas du « 31 du mois » en récurrence mensuelle (mois sans 31 : occurrence sautée ou dernier jour du mois)
- [ ] Figer l’horizon de vérification des conflits pour une série sans fin (ex. 12 mois)

## Backend (API)

- [ ] 1. Modèles `MeetingSeries`, `MeetingSeriesParticipant`, `MeetingOccurrenceException` (+ migration)
- [ ] 2. Service d’**expansion** : occurrences d’une série sur `[from, to]`, exceptions appliquées
  - [ ] Calcul en heure locale du fuseau de l’organisateur, puis conversion en UTC (10h reste 10h après un changement d’heure)
  - [ ] Quotidienne, hebdomadaire (jours choisis), mensuelle ; intervalle ; fin par date ou par nombre
- [ ] 3. Créer une série
  - [ ] Validation : au moins une occurrence, intervalle ≥ 1, jours choisis pour une hebdomadaire
  - [ ] R6–R8 et horaire effectif vérifiés sur chaque occurrence de l’horizon ; occurrences en conflit listées dans la réponse
- [ ] 4. Modifier / annuler **cette occurrence** (exception)
- [ ] 5. Modifier / annuler **cette occurrence et les suivantes** (la série est coupée en deux)
- [ ] 6. Modifier / annuler **toute la série** (que faire des exceptions déjà créées : règle à figer)
- [ ] 7. Endpoint calendrier et libre / occupé : occurrences incluses, mélangées aux meetings simples
- [ ] 8. Notifications : participants avertis d’une série créée, modifiée ou annulée
- [ ] 9. Autorisation : organisateur ou Admin pour modifier ; participants en lecture

## Frontend (React)

- [ ] 10. Écran **Créer un meeting** : option « Répéter » (fréquence, intervalle, jours, fin)
  - [ ] Aperçu en clair : « Toutes les 2 semaines le lundi et le mercredi à 10h, jusqu’au 31 déc. »
  - [ ] Liste des occurrences en conflit avant d’enregistrer
- [ ] 11. Icône de récurrence sur les occurrences (grille horaire et vue mois)
- [ ] 12. Boîte de dialogue au moment de modifier ou d’annuler : **Cette occurrence / Les suivantes / Toutes**
- [ ] 13. Détail d’une occurrence : règle de la série affichée, lien vers la série

## Validation manuelle

- [ ] Réunion chaque lundi à 10h visible toutes les semaines, sans saisie répétée
- [ ] Une occurrence déplacée au mardi n’affecte pas les autres lundis
- [ ] « Cette occurrence et les suivantes » : les occurrences passées restent inchangées
- [ ] Une occurrence qui tombe sur un congé approuvé d’un participant est signalée
- [ ] 10h reste 10h avant et après le passage à l’heure normale (novembre)
- [ ] Une série « 10 occurrences » s’arrête bien après la 10e
- [ ] Mensuelle le 31 : comportement conforme à la règle figée

## Bonus

- [ ] Mensuelle « le 2e mardi du mois »
- [ ] Tests API : expansion (changement d’heure, fin par nombre, exceptions), découpe d’une série

## Notes / blocages

_(À remplir au fil de l’eau)_

-
