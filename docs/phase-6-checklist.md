# Checklist de la phase 6 — Continuité

Référence produit : [PROJET.md](../PROJET.md) §6, §7, §8, §9 (R4, R23, R25, R26).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** phase 3 (meetings, notifications). Les approbations (congés, ajustements, absences) existent depuis les phases 2 et 2.5.

## Préparation

- [ ] Relire R25 (remplaçant d’approbation) et R26 (date de départ)
- [ ] Lister tous les endroits où « qui peut approuver » est décidé aujourd’hui (`AccessScope.CanManageAsync` : manager direct ou Admin du site) : le remplaçant s’y ajoute à un seul endroit
- [ ] Décider comment les actions « à la date de départ » s’exécutent : tâche de fond (`BackgroundService`) une fois par jour et par fuseau de site, plus une vérification à la connexion pour ne jamais dépendre de l’heure de la tâche

## Backend (API)

- [ ] 1. Modèle `ApprovalDelegation` (manager, remplaçant, début, fin) + migration
  - [ ] Remplaçant : employé actif du même site (R23), jamais le manager lui-même
  - [ ] Fin ≥ début ≥ aujourd’hui ; pas deux délégations qui se chevauchent pour un même manager
- [ ] 2. CRUD délégations (le manager pour lui-même ; un Admin du site pour n’importe quel manager du site)
- [ ] 3. Droit d’approuver : manager direct, Admin du site (déjà en place) **et** remplaçant actif à cette date, sauf pour ses propres demandes
- [ ] 3b. Notification aux Admins du site à la création d’une demande quand le manager est en congé approuvé (ou parti) et n’a aucun remplaçant actif
- [ ] 4. File d’approbation et endpoints approuver / refuser : le remplaçant y voit les demandes de l’équipe pendant sa période
  - [ ] Décision enregistrée avec `ReviewedOnBehalfOfId` quand un remplaçant approuve
- [ ] 5. `Employee.DepartureDate?` + migration ; saisie par un Admin du site (R23)
  - [ ] Date ≥ aujourd’hui (fuseau du site) ; modifiable ou annulable avant la date
  - [ ] Réponse de la saisie : liste des subordonnés et des meetings futurs organisés (avertissement, pas un refus)
- [ ] 6. Connexion refusée dès la date de départ (R4), même si la tâche de fond n’est pas encore passée
- [ ] 7. Tâche de fond à la date de départ
  - [ ] Retrait des meetings futurs où la personne est participante (organisateur notifié)
  - [ ] Annulation de ses meetings futurs avec participants (participants notifiés) ; suppression de ses blocs personnels futurs
  - [ ] Annulation de ses demandes en attente (congés, ajustements, absences)
  - [ ] Délégations où elle est remplaçante : terminées (le manager concerné est notifié)
  - [ ] Rejouable sans effet en double si elle tourne deux fois
- [ ] 8. Tests API : remplaçant (période, pas ses propres demandes), notification à l’Admin, départ (connexion, meetings, demandes)

## Frontend (React)

- [ ] 9. (Manager) Écran **Mon remplaçant** : choisir un collègue du site et une période ; délégations à venir et en cours
- [ ] 10. File d’approbation : demandes reçues comme remplaçant marquées « au nom de Mark »
- [ ] 11. Détail d’une demande : « Approuvé par Sam au nom de Mark »
- [ ] 12. Fiche employé (Admin du site) : champ **Date de départ**, avec l’avertissement listant subordonnés et meetings
- [ ] 13. Annuaire et combobox : un employé parti n’apparaît plus ; une personne avec un départ prévu affiche « Départ le 14 nov. » sur sa fiche

## Validation manuelle

- [ ] Mark (manager) désigne Sam du 3 au 7 nov. : Sam approuve un congé de Jane pendant cette période, pas avant ni après
- [ ] Sam ne peut pas approuver sa propre demande pendant sa délégation
- [ ] Mark en congé sans remplaçant : Jane fait une demande, l’Admin du site reçoit une notification et peut approuver
- [ ] Départ de Jane fixé à demain : demain, connexion refusée, Jane absente de tous les meetings futurs, ses congés en attente annulés
- [ ] Départ annulé la veille : rien ne se passe le lendemain
- [ ] Départ d’un manager : une demande d’un de ses subordonnés notifie l’Admin du site

## Bonus

- [ ] Rappel au manager 3 jours avant le départ d’un subordonné qui a encore des subordonnés
- [ ] Transfert des meetings organisés à un autre employé au lieu de les annuler

## Notes / blocages

_(À remplir au fil de l’eau)_

-
