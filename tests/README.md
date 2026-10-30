# tests/

Tests automatisés de l'API, avec xUnit.

## Lancer les tests

À la racine du dépôt :

    dotnet test

## Projets

- **PortailInterne.Api.Tests**
  - `LeaveBalanceTests` : calcul du solde de congés (`LeaveBalance.ComputeUsage`) — jours travaillés fournis (R14), dépassement et jours sans solde, ordre de création, années civiles (R19)
  - `LeavesApiTests` : tests d'intégration des congés — création, chevauchements (Q16), isolation des données, approbation (Manager / Admin), congé maladie déclaré pour autrui (R20), solde et aperçu (R19), seuls les jours travaillés comptent — horaire, férié, journée retirée, lun.–ven. sans horaire, demande sans aucun jour travaillé refusée (R14)
  - `WorkScheduleRulesTests` : validation d'un horaire type (`WorkScheduleRules.Validate`) — au moins un jour, pas de doublon, fin après début, pause valide (R11)
  - `AdjustmentAndAbsenceRulesTests` : validation d'un ajustement d'horaire (jour non travaillé, heures, pause — R13) et d'une absence dans la journée (prévue, non prévue, retour en avance ou en retard — R24)
  - `AbsenceStateTests` : où en est une absence (`AbsenceState.Compute`) — prévue sans clic (suit le plan), retour en avance ou en retard, « Je pars » sans retour (en cours puis fermée en fin de journée, non confirmée), jour non travaillé (R24)
  - `AbsencesApiTests` : tests d'intégration des absences prévues — en attente (employé) ou approuvée (manager), comprise dans les heures travaillées, sans chevauchement, état « en cours » dans la liste, annulation impossible une fois commencée, approbation ou refus par le manager (une demande en cours s'approuve ; terminée, jour devenu congé, heures changées ou urgence qui chevauche : refus seulement) (R24)
  - `PresenceApiTests` : « Je pars » et « Je suis de retour » — urgence approuvée d'office, rendez-vous prévu démarré à 30 min près, pas deux départs, pas de retour sans départ, hors des heures de travail, demande en attente ignorée, manager à la place de l'employé (R24)
  - `EffectiveScheduleTests` : horaire effectif d'un jour (`EffectiveSchedule.ComputeDay`) — horaire type, jour hors horaire, non défini, férié, ajustement (même un férié), ajustement en attente non appliqué, congé par-dessus tout (R16)
  - `CalendarApiTests` : `GET /api/calendar` de bout en bout — chaque couche de l'horaire effectif (horaire, ajustement approuvé ou en attente, congé, férié), changement d'horaire en cours de période, absences du jour (approuvées triées avec leur état, en attente à part, aucune un jour de congé), isolation, période invalide (R16, R24)
  - `AdjustmentsApiTests` : tests d'intégration des ajustements — demande en attente (employé), approuvé d'office (manager / Admin, jamais pour soi), jour non travaillé réservé au manager, un seul actif par date, annulation selon le rôle, approbation ou refus par le manager / l'Admin du site (jamais sa propre demande, pas d'approbation d'une date passée) (R13)
  - `AuthorizationMatrixTests` : qui lit et gère l'horaire, les ajustements et les absences de Sam, et qui approuve ses demandes, et ce que dit `CanManage` sur sa fiche — lui-même, son manager, l'Admin de son site, l'Admin global, un collègue, l'Admin d'un autre site (étape 14, R23)
  - `ApprovalsApiTests` : file d'approbation `GET /api/approvals` — ajustements et absences en attente seulement, triés, approuvables ou non (passé, terminée), jamais ses propres demandes, portée manager / Admin du site / Admin global, employé désactivé exclu
  - `WorkSchedulesApiTests` : tests d'intégration des horaires — création (Manager / Admin), date passée, doublon (409), droits de lecture, modification et suppression d'un horaire futur, historique jamais réécrit (R11)
  - `HolidaysApiTests` : tests d'intégration des jours fériés — lecture par tous, écriture par l'Admin du site seulement, date passée, doublon par site (409), férié passé figé, site sans employé (R12, R23)
  - `SiteScopeApiTests` : portée par site (R23) — un Admin ne gère que son site (comptes, fiches, approbations, horaires), l'Admin global gère tout ; rôle Admin global, création sur son site, manager du même site, changement de site, création des sites (fuseau IANA)
  - `SiteTimeZoneTests` : « aujourd'hui » dans le fuseau du site de chaque employé (Montréal vs Paris), fuseau renvoyé par `/me` (R15)
  - `TeamApiTests` : vue équipe `GET /api/team/week` — selon le réglage du site (Mon équipe, Tout le site, Personne), l'appelant en premier, le manager et l'Admin toujours avec les détails, présence seulement sinon (ni ajustement ou absence en attente, ni motif), manager désactivé, Admin global sur un site au choix, période de 7 jours au plus, `CanSeeTeam` de `/me` (étape 19)
  - `PreferencesApiTests` : préférences personnelles `api/me/preferences` — valeurs par défaut sans ligne en base, enregistrer puis modifier (une seule ligne), isolation entre employés, plage d'heures inconnue refusée, connexion requise (étape 16)
  - `ApiFactory` : démarre la vraie API en mémoire pour un test — base SQLite neuve (migrations), date fixée au mercredi 14 octobre 2026, équipe de test (Alex admin, Mark manager de Sam, Sam, Jane hors équipe)
