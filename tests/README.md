# tests/

Tests automatisés de l'API, avec xUnit.

## Lancer les tests

À la racine du dépôt :

    dotnet test

## Projets

- **PortailInterne.Api.Tests**
  - `LeaveBalanceTests` : calcul du solde de congés (`LeaveBalance.ComputeUsage`) — jours ouvrables, dépassement et jours sans solde, ordre de création, années civiles (R19)
  - `LeavesApiTests` : tests d'intégration des congés — création, chevauchements (Q16), isolation des données, approbation (Manager / Admin), congé maladie déclaré pour autrui (R20), solde et aperçu (R19)
  - `WorkScheduleRulesTests` : validation d'un horaire type (`WorkScheduleRules.Validate`) — au moins un jour, pas de doublon, fin après début, pause valide (R11)
  - `WorkSchedulesApiTests` : tests d'intégration des horaires — création (Manager / Admin), date passée, doublon (409), droits de lecture, modification et suppression d'un horaire futur, historique jamais réécrit (R11)
  - `SiteTimeZoneTests` : « aujourd'hui » dans le fuseau du site de chaque employé (Montréal vs Paris), fuseau renvoyé par `/me` (R15)
  - `ApiFactory` : démarre la vraie API en mémoire pour un test — base SQLite neuve (migrations), date fixée au mercredi 14 octobre 2026, équipe de test (Alex admin, Mark manager de Sam, Sam, Jane hors équipe)
