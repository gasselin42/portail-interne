# Checklist MVP 4 — Salles et équipements

Référence produit : [PROJET.md](../PROJET.md) §5.4, §6, §7, §8, §9 (R8).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** MVP 3 (meetings) — réservations souvent liées à un meeting ; réservation seule possible.

## Préparation

- [ ] Relire le périmètre MVP 4 dans `PROJET.md`
- [ ] Figer : `Room`, `Equipment`, `ResourceReservation`
- [ ] Clarifier : réservation **liée au meeting** vs **réservation seule**
- [ ] Règle R8 : ressource libre sur **tout** l’intervalle `[début, fin]`, sinon refus clair

## Backend (API)

- [ ] 1. Modèles `Room` + `Equipment` (+ migration)
- [ ] 2. Modèle `ResourceReservation` (+ migration, lien `MeetingId?`)
- [ ] 3. Seed : quelques salles + équipements
- [ ] 4. Admin : CRUD catalogue salles (actif/inactif)
- [ ] 5. Admin : CRUD catalogue équipements (actif/inactif)
- [ ] 6. Algorithme de disponibilité sur `[StartAt, EndAt]` (pas de chevauchement)
- [ ] 7. Réserver une salle / un équipement (avec ou sans meeting)
- [ ] 8. Création / update de meeting avec ressource requise → refus si prise (R8)
- [ ] 9. Annulation meeting / réservation → **libération** de la ressource
- [ ] 10. Endpoint « ressources disponibles » pour un créneau donné

## Frontend (React)

- [ ] 11. Écran Admin **Catalogue** salles / équipements
- [ ] 12. À la création / édition de meeting : sélection ressource + feedback indispo
- [ ] 13. (Optionnel) écran réservation seule
- [ ] 14. Affichage sur le calendrier / détail meeting : salle / équipement associés

## Validation manuelle

- [ ] Double booking impossible sur la même ressource
- [ ] Meeting avec salle requise refusé si salle prise
- [ ] Idem équipements
- [ ] Libération de ressource si annulation
- [ ] Ressource inactive non réservable
- [ ] Message d’erreur clair côté UI

## Bonus

- [ ] Capacité salle vs nombre de participants
- [ ] Tests API (chevauchements, libération)

## Notes / blocages

_(À remplir au fil de l’eau)_

-
