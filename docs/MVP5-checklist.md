# Checklist MVP 5 — Portail IT + assignation + créneau

Référence produit : [PROJET.md](../PROJET.md) §5.5, §6, §7, §8, §9 (R9, R10).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** MVP 1 (annuaire / rôles) ; idéalement MVP 3 (calendrier) pour le warning « prévoir un slot ».

## Préparation

- [ ] Relire le périmètre MVP 5 dans `PROJET.md`
- [ ] Figer les statuts : `Nouvelle`, `Assignee`, `EnCours`, `Resolue`, `Fermee`
- [ ] Introduire le rôle **ItAgent** (ou équivalent) + seed
- [ ] Clarifier permissions : vue globale = IT / Admin (R9)
- [ ] Décider : lien optionnel ticket ↔ événement calendrier

## Backend (API)

- [ ] 1. Modèle `ItTicket` (+ migration)
- [ ] 2. (Optionnel) `TicketComment` + migration
- [ ] 3. Brancher / réutiliser `Notification` (si créée en MVP 3)
- [ ] 4. Employé : créer un ticket (titre, description, catégorie?)
- [ ] 5. Employé : lister **ses** tickets + détail
- [ ] 6. IT/Admin : liste globale + filtres (statut, assigné)
- [ ] 7. Assignation à un agent (+ passage statut `Assignee`)
- [ ] 8. Transitions de statut (EnCours → Resolue → Fermee) — qui peut quoi
- [ ] 9. À l’assignation : notification assigné + **warning « prévoir un slot »** (R10)
- [ ] 10. Notification au demandeur (création / assignation / résolution)
- [ ] 11. (Optionnel) lier un créneau calendrier au ticket
- [ ] 12. Autorisation stricte (R9) : employé ≠ console IT

## Frontend (React)

- [ ] 13. Écran **Mes tickets IT**
- [ ] 14. Écran **Nouveau ticket**
- [ ] 15. Écran **Console IT** (toutes les demandes, assignation, statuts)
- [ ] 16. Centre de **notifications** (liste + lu / non lu) — finalisation
- [ ] 17. Accueil : panneau / widget notifications (aperçu + non lus) branché sur le centre de notifs
- [ ] 18. Warning visible pour l’assigné (« prévoir un slot »)
- [ ] 19. (Optionnel) bouton « planifier créneau » → meeting / événement

## Validation manuelle

- [ ] Employé crée une demande
- [ ] Agent IT voit toutes les demandes et assigne
- [ ] Assigné notifié + warning créneau
- [ ] Demandeur notifié
- [ ] Permissions respectées (employé hors console IT)
- [ ] Résolution / fermeture du ticket OK
- [ ] Admin a accès console IT (si prévu)

## Bonus

- [ ] Commentaires sur ticket
- [ ] Catégories / priorités
- [ ] Tests API (R9, R10, isolation)

## Notes / blocages

_(À remplir au fil de l’eau)_

-
