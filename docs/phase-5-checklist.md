# Checklist de la phase 5 — Portail IT + assignation + créneau

Référence produit : [PROJET.md](../PROJET.md) §5.5, §6, §7, §8, §9 (R9, R10).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** phase 1 (annuaire / rôles) ; idéalement la phase 3 (calendrier) pour le warning « prévoir un slot ».

## Préparation

- [ ] Relire le périmètre de la phase 5 dans `PROJET.md`
- [ ] Figer les statuts : `Nouvelle`, `Assignee`, `EnCours`, `Resolue`, `Fermee`
- [ ] Introduire le rôle **ItAgent** (ou équivalent) + seed
- [ ] Clarifier permissions : vue globale = IT / Admin (R9)
- [ ] Décider : lien optionnel ticket ↔ événement calendrier

## Backend (API)

- [ ] 1. Modèle `ItTicket` (+ migration)
- [ ] 2. (Optionnel) `TicketComment` + migration
- [ ] 3. Brancher / réutiliser `Notification` (si créée en phase 3)
- [ ] 4. Employé : créer un ticket (titre, description, catégorie?)
- [ ] 5. Employé : lister **ses** tickets + détail
- [ ] 6. IT/Admin : liste globale + filtres (statut, assigné)
- [ ] 7. Assignation à un agent (+ passage statut `Assignee`)
- [ ] 8. Transitions de statut (EnCours → Resolue → Fermee) — qui peut quoi
- [ ] 9. À l’assignation : notification assigné + **warning « prévoir un slot »** (R10)
- [ ] 10. Notification au demandeur (création / assignation / résolution)
- [ ] 11. (Optionnel) lier un créneau calendrier au ticket
- [ ] 12. Autorisation stricte (R9) : employé ≠ console IT
- [ ] 12b. Base de connaissances : modèle `KnowledgeArticle` + migration
  - [ ] CRUD réservé aux rôles IT ; brouillon / publié ; les employés ne voient que les articles publiés
  - [ ] Recherche insensible aux accents et à la casse (même approche que `TextKey`)
  - [ ] Endpoint de suggestions à partir du titre d’un ticket (5 au maximum)
  - [ ] « Ça règle mon problème » : compteur `HelpfulCount` (tickets évités)
- [ ] 12c. Notifications en **temps réel** (SignalR)
  - [ ] Hub authentifié par le JWT (passé en `access_token` dans l’URL, seul cas où le navigateur ne peut pas envoyer l’en-tête)
  - [ ] Chaque notification créée est poussée à son destinataire seulement (tous ses onglets ouverts)
  - [ ] Le temps réel ne remplace pas la lecture : à la reconnexion, la liste est relue depuis l’API (aucune notification perdue si la connexion tombe)

## Frontend (React)

- [ ] 13. Écran **Mes tickets IT**
- [ ] 14. Écran **Nouveau ticket**
- [ ] 15. Écran **Console IT** (toutes les demandes, assignation, statuts)
- [ ] 16. Centre de **notifications** (liste + lu / non lu) — finalisation
- [ ] 17. Accueil : panneau / widget notifications (aperçu + non lus) branché sur le centre de notifs
- [ ] 18. Warning visible pour l’assigné (« prévoir un slot »)
- [ ] 19. (Optionnel) bouton « planifier créneau » → meeting / événement
- [ ] 20. **Base de connaissances** : liste + recherche, page d’un article, éditeur pour l’IT
- [ ] 21. Suggestions d’articles dans **Nouveau ticket** pendant la saisie du titre ; « Ça règle mon problème » ferme le formulaire
- [ ] 22. Connexion SignalR ouverte à la connexion, fermée à la déconnexion ; badge et liste mis à jour sans recharger

## Validation manuelle

- [ ] Employé crée une demande
- [ ] Agent IT voit toutes les demandes et assigne
- [ ] Assigné notifié + warning créneau
- [ ] Demandeur notifié
- [ ] Permissions respectées (employé hors console IT)
- [ ] Résolution / fermeture du ticket OK
- [ ] Admin a accès console IT (si prévu)
- [ ] En tapant « VPN » dans un ticket, l’article VPN est suggéré ; « Ça règle mon problème » n’ouvre aucun ticket
- [ ] Un brouillon d’article est invisible pour un employé
- [ ] Deux navigateurs : un meeting créé dans l’un fait apparaître la notification dans l’autre sans recharger
- [ ] Wi-Fi coupé puis rétabli : les notifications reçues entre-temps apparaissent

## Bonus

- [ ] Commentaires sur ticket
- [ ] Catégories / priorités
- [ ] Tests API (R9, R10, isolation)

## Notes / blocages

_(À remplir au fil de l’eau)_

-
