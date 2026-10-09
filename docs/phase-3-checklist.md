# Checklist de la phase 3 — Meetings (solo / groupe) + conflits

Référence produit : [PROJET.md](../PROJET.md) §5.3, §6, §7, §8, §9 (R6, R7).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** phase 2 (congés + calendrier perso) et phase 2.5 (horaires) — les règles de conflit s’appuient sur l’**horaire effectif** (R16), qui inclut les congés **approuvés**.

## Préparation

- [ ] Relire le périmètre de la phase 3 dans `PROJET.md` (inclus / exclu)
- [ ] Figer le modèle : `Meeting` + `MeetingParticipant` (+ `ResponseStatus?`)
- [ ] Clarifier « meeting d’équipe » (`IsTeamMeeting`) et la priorité en cas de conflit (R7)
- [ ] Décider le canal de notif initial : table `Notification` in-app (emails = plus tard)

## Backend (API)

- [ ] 1. Modèles `Meeting` / `MeetingParticipant` (+ migration)
- [ ] 2. Modèle `Notification` (si pas déjà) + migration
- [ ] 3. Créer meeting solo (organisateur seul)
- [ ] 4. Créer meeting groupe (liste de participants)
- [ ] 5. Règle R6 : refuser si un participant a un **congé approuvé** sur le créneau
- [ ] 6. Détection de conflits d’horaire (chevauchement avec d’autres meetings)
- [ ] 6b. Avertir si le meeting sort de l’horaire effectif d’un participant (non travaillé, férié, départ anticipé), conversion UTC → fuseau du site (R15)
- [ ] 7. Priorité meeting d’équipe + création des notifications aux personnes en conflit (R7)
- [ ] 8. Détail meeting + liste des participants
- [ ] 9. Modifier / annuler (organisateur ou Admin — règles à figer)
- [ ] 10. Enrichir l’endpoint calendrier : congés **+** meetings sur `[from, to]`
- [ ] 11. Endpoints notifications : lister / marquer lu (minimum viable)

## Frontend (React)

- [ ] 12. Écran **Créer un meeting** (titre, créneau, participants, flag équipe)
- [ ] 13. Écran **Détail meeting**
- [ ] 14. Calendrier enrichi (meetings + congés, légende claire)
- [ ] 15. Affichage des erreurs métier (congé, conflit) côté UI
- [ ] 16. Centre de notifs simple (badge / liste) — même si la phase 5 le pousse plus loin
- [ ] 17. Accueil : widget / badge **notifications** + lien meetings dans le menu

## Validation manuelle

- [ ] Meeting solo OK
- [ ] Meeting groupe OK
- [ ] Blocage si participant en congé approuvé
- [ ] Conflit → notification(s) visibles
- [ ] Meeting d’équipe prioritaire selon la règle choisie
- [ ] Calendrier à jour pour tous les participants
- [ ] Annulation retire l’événement du calendrier

## Bonus

- [ ] Réponse participant (Accepté / Décliné)
- [ ] Tests API (R6, R7, isolation)

## Notes / blocages

_(À remplir au fil de l’eau)_

-
