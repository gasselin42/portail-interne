# Checklist de la phase 3 — Meetings, blocs personnels + conflits

Référence produit : [PROJET.md](../PROJET.md) §5.3, §6, §7, §8, §9 (R6, R7, R17, R21, R22).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** phase 2 (congés + calendrier perso) et phase 2.5 (horaires) — les règles de conflit s’appuient sur l’**horaire effectif** (R16), qui inclut les congés **approuvés**.

## Préparation

- [ ] Relire le périmètre de la phase 3 dans `PROJET.md` (inclus / exclu)
- [x] Figer le modèle : `Meeting` + `MeetingParticipant` (+ `ResponseStatus?`)
  - `Meeting` : `Kind` (Meeting / PersonalBlock), titre, description, `StartAt` / `EndAt` en `DateTime` UTC, organisateur, `IsTeamMeeting`, `IsFlexible`, `CreatedAt`
  - `MeetingParticipant` : clé (`MeetingId`, `EmployeeId`), `Color?` ; l’organisateur a aussi sa ligne
  - Un événement tient dans une journée (fuseau de l’organisateur), par tranches de 15 min ; annuler = supprimer + notifier ; `ResponseStatus` = bonus, plus tard
- [x] Clarifier « meeting d’équipe » (`IsTeamMeeting`) et la priorité en cas de conflit (R7)
  - Meeting d’équipe = organisé par un manager (≥ 1 subordonné actif) ou un Admin ; fixé à la création, pas de case à cocher
  - Conflit = avertissement + confirmation, jamais un blocage (sauf congé approuvé, R6) ; chevaucher un meeting d’équipe = avertissement renforcé
  - Participants : tout employé actif, tous sites ; bouton « Ajouter mon équipe » pour un manager
  - Modifier / annuler : organisateur ou Admin du site de l’organisateur
- [x] Relire R21 (bloc souple) et R22 (couleur par personne) ; figer la palette (environ 8 couleurs, lisibles en mode sombre, sans les couleurs des congés)
  - Palette : `blue` (défaut), `indigo`, `cyan`, `teal`, `fuchsia`, `pink`, `orange`, `stone`
- [ ] Décider le canal de notif initial : table `Notification` in-app (emails = plus tard)
- [ ] Garder la récurrence (phase 3.5) en tête : le calendrier doit pouvoir mélanger meetings simples et occurrences calculées

## Backend (API)

- [x] 1. Modèles `Meeting` / `MeetingParticipant` (+ migration)
  - [x] `IsFlexible` sur le meeting (bloc personnel seulement) ; `Color?` par personne
- [x] 2. Modèle `Notification` (si pas déjà) + migration
- [x] 3. Créer un **bloc personnel** (sans participants), ferme ou souple
- [x] 4. Créer un meeting de groupe (liste de participants ; jamais souple)
- [x] 4b. Changer la couleur d’un événement dans son propre calendrier (palette fixe validée par l’API, R22)
- [x] 5. Règle R6 : refuser si un participant a un **congé approuvé** sur le créneau
- [x] 6. Détection de conflits d’horaire (chevauchement avec d’autres meetings)
- [x] 6b. Avertir si le meeting sort de l’horaire effectif d’un participant (non travaillé, férié, départ anticipé), conversion UTC → fuseau du site (R15)
- [x] 7. Priorité meeting d’équipe + création des notifications aux personnes en conflit (R7)
  - [x] Bloc souple chevauché par un meeting d’équipe : pas de conflit, bloc conservé, notification légère au propriétaire (R21)
  - [x] Meeting ordinaire sur un bloc souple : conflit normal
- [x] 8. Détail meeting + liste des participants
- [x] 9. Modifier / annuler (organisateur ou Admin du site ; bloc : propriétaire ; verrouillé dès le début)
- [x] 10. Nouveau format de l’endpoint calendrier sur `[from, to]` : une entrée par jour (horaire effectif, congés) + la liste des meetings
- [x] 10b. Endpoint **libre / occupé** : créneaux occupés de plusieurs employés sur une plage, sans titre, motif ni participants (R17) ; les blocs souples sont marqués « occupé souple » (R21)
  - [x] Participants de plusieurs sites : créneaux renvoyés en UTC (jours et heures de travail dans le fuseau de chaque employé)
- [x] 10c. Absence (R24) qui chevauche un meeting, prévue ou réelle (départ, retour tardif) : meeting conservé ; organisateur et employé notifiés (« Sam manquera le début de … ») ; manager notifié d’un départ non prévu ou d’un retour non confirmé
- [x] 10d. Congé approuvé qui chevauche un meeting : meeting conservé ; employé participant → organisateur notifié ; employé organisateur → participants notifiés (et son manager pour un meeting d’équipe) ; modifier un meeting sans changer l’heure ne revérifie les congés et les heures de travail que des participants ajoutés
- [x] 11. Endpoints notifications : lister / marquer lu (minimum viable)

## Frontend (React)

- [x] 12. Écran **Créer un événement** (titre, créneau, participants optionnels, flag équipe, souple si aucun participant, couleur)
  - [x] Libre / occupé des participants sur la journée choisie, d’un coup d’œil
  - [x] Participants de plusieurs sites : créneaux affichés dans le fuseau de la personne qui regarde
- [x] 13. Écran **Détail meeting**
  - [x] Ouverture depuis la grille ou la vue mois (`?evenement=ID`), détail complet
  - [x] Couleur personnelle modifiable (R22)
  - [x] Annuler (organisateur ou Admin), notification aux participants
  - [x] Modifier avec le même formulaire, avertissement 409 et confirmation
- [x] 14. Calendrier enrichi (meetings + congés, légende claire)
  - [x] Meetings placés dans la grille selon leur durée ; chevauchements affichés côte à côte
  - [x] Couleur choisie par l’employé ; bloc souple chevauché affiché pâli à côté du meeting
  - [x] Libre / occupé : « occupé souple » visuellement distinct, sans dépendre de la couleur seule
  - [x] Vue mois : « 10h Titre », 3 au maximum puis « + N autres »
- [x] 14b. **Densité** de la grille horaire (Compacte / Normale / Aérée), préférence stockée dans l’API comme la plage d’heures : plus de place par heure quand il y a plusieurs réunions ou tâches
- [x] 15. Affichage des erreurs métier (congé, conflit) côté UI
- [x] 16. Centre de notifs simple (badge / liste) — même si la phase 5 le pousse plus loin
  - [x] Page `/notifications` : non lues mises en avant, date relative, ouvrir = lue + lien, « Tout marquer comme lu », « Voir plus »
  - [x] Cloche (ordinateur) : menu des 5 dernières ; mobile : lien dans l'en-tête
  - [x] Compte de non lues partagé, rafraîchi chaque minute et au retour sur l'onglet
- [x] 17. Accueil : widget / badge **notifications** + lien meetings dans le menu

## Validation manuelle

- [x] Bloc personnel ferme et souple OK, couleur choisie conservée
- [x] Meeting groupe OK
- [x] Blocage si participant en congé approuvé
- [x] Conflit → notification(s) visibles
- [x] Meeting d’équipe prioritaire selon la règle choisie
- [x] Meeting d’équipe sur un bloc souple : aucun conflit, bloc pâli, notification au propriétaire
- [x] Un participant change la couleur d’un meeting : les autres ne voient aucun changement
- [x] Calendrier à jour pour tous les participants
- [x] Annulation retire l’événement du calendrier
- [x] Un employé voit le libre / occupé d’un collègue, jamais le titre de ses événements

## Bonus

- [x] Réponse participant (Accepté / Décliné)
- [x] Tests API (R6, R7, R21, isolation)
- [x] Convertisseur UTC sur les `DateTime` déjà en base (`CreatedAt`, `ReviewedAt`…) : le JSON doit finir par `Z`, sinon une heure affichée serait décalée (comme `StartAt` / `EndAt` des meetings)

## Notes / blocages

_(À remplir au fil de l’eau)_

-
