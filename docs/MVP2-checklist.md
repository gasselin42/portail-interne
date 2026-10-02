# Checklist MVP 2 — Congés + calendrier personnel

Référence produit : [PROJET.md](../PROJET.md) §5.2, §6, §7, §8, §9 (R5).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** MVP 1 validé (auth, rôles, annuaire).

## Préparation

- [ ] Relire le périmètre MVP 2 dans `PROJET.md` (inclus / exclu)
- [ ] Décider qui approuve : **Manager** (via `ManagerId`) et/ou **Admin**
- [ ] Figer les statuts : `EnAttente`, `Approuve`, `Refuse`, `Annule`
- [ ] Figer les types d’absence simples (ex. Congé, Maladie) — soldes avancés = optionnel / reporter

## Backend (API)

- [x] 1. Modèle `LeaveRequest` (+ enums Type / Status)
- [x] 2. Migration EF + relations vers `Employee`
- [x] 3. Seed : 1–2 demandes de test (ou données créées manuellement)
- [x] 4. Rôle / permission Manager (si pas encore dans Role) + seed d’un manager avec équipe
- [x] 5. Employé : créer une demande (dates, type, motif?)
- [x] 6. Employé : lister **ses** demandes + détail
- [x] 7. Employé : annuler une demande (règles : seulement si EnAttente, etc.)
- [x] 8. Manager/Admin : file d’approbation (équipe / global selon règle)
- [x] 9. Approuver / refuser (+ `ReviewedById`, `ReviewedAt`)
- [x] 10. Endpoint calendrier perso : absences sur une plage `[from, to]` (avec statut pour l’UI)
- [x] 11. Autorisation : un user ne lit/modifie que ce qui lui est permis

## Frontend (React)

- [x] 12. Navigation globale / **menu Accueil** : liens Congés, Calendrier (+ Admin si rôle) — remplacer le bouton temporaire du MVP 1
- [x] 13. Écran **Mes demandes** (liste + statuts)
- [x] 14. Écran **Nouvelle demande** (+ validation dates)
- [x] 15. Écran **File d’approbation** (Manager/Admin uniquement)
- [ ] 16. Calendrier personnel (vue jour / semaine simple)
- [ ] 17. Distinction visuelle : EnAttente vs Approuve vs Refuse (R5)
- [ ] 18. Accueil : premiers composants utiles (ex. prochaines absences / résumé calendrier de la semaine)

## Validation manuelle

- [ ] Demande créée → statut EnAttente visible au calendrier
- [ ] Approbation → affichage « congé » distinct
- [ ] Refus → plus d’affichage bloquant / statut clair
- [ ] Annulation côté demandeur (cas autorisé)
- [ ] Un employé ne voit pas les demandes des autres
- [ ] Un Manager ne voit que son équipe (si règle choisie)
- [ ] Admin peut approuver (si prévu)

## Bonus

- [ ] Soldes simples (jours restants)
- [ ] Tests API (création, approbation, isolation des données)
- [ ] Admin ou manager crée le congé maladie d'un autre employé

## Correctifs qualité (produit fini)

Objectif : aucun cas limite connu laissé de côté. Ordre recommandé ci-dessous.

- [x] Q1. **Session via `/api/auth/me`** (`fix/session-me`)
  - [x] Seul le token reste dans le `localStorage` ; rôle, `isManager`, `mustChangePassword` viennent de `/me`
  - [x] `SessionProvider` + `useSession()` (Context React)
  - [x] Gardes de route : écran de chargement pendant `/me` (pas de redirection par erreur au F5)
  - [x] Serveur injoignable ≠ déconnexion : écran d’erreur avec « Réessayer »
  - [x] Rafraîchir au démarrage, au retour sur l’onglet, après login, après changement de mot de passe, au logout
  - [x] Supprimer `ROLE_KEY`, `MUST_CHANGE_KEY`, `IS_MANAGER_KEY`, `isAdmin()`, `getMustChangePassword()`, `IsManager` du login
- [x] Q2. **Navigation globale** (`feat/navigation-sidebar`)
  - [x] Route de layout `AppLayout` + `<Outlet />` pour toutes les pages connectées
  - [x] Sidebar rétractable (icônes seules une fois réduite), tiroir ☰ sur mobile (Headless UI `Dialog`)
  - [x] Liens selon le rôle (À approuver, Comptes), lien actif mis en évidence (`isNavItemActive`, `aria-current`)
  - [x] Avatar + nom de l’utilisateur, Changer le mot de passe, Déconnexion
  - [x] État réduit/ouvert mémorisé, `aria-expanded`, navigable au clavier (focus visible)
  - [x] Tiroir fermé automatiquement au passage en grand écran (`matchMedia`)
  - [x] Accueil libéré des cartes de liens (prêt pour les widgets)
- [x] Q3. **Fuseau horaire de l’API** : « aujourd’hui » calculé en `America/Toronto`, pas selon la machine
  - [x] Réglage `App:TimeZone` + service `IAppClock` (vérifié au démarrage)
  - [x] Frontend aligné sur le même fuseau (`APP_TIME_ZONE`, `daysFromToday`)
- [x] Q4. **Dates lisibles** : `formatDate` (`Intl.DateTimeFormat("fr-CA")`) dans tous les affichages
- [ ] Q5. **`ConfirmDialog`** réutilisable pour remplacer les 3 `window.confirm` (Échap, focus piégé, variante destructive)
- [ ] Q6. **Choix du manager par son nom** (combobox, Headless UI)
  - [ ] Recherche via `GET /api/employees?search=` avec debounce + annulation des requêtes périmées
  - [ ] États : recherche en cours, aucun résultat, erreur API
  - [ ] Bouton pour retirer le manager ; texte tapé sans sélection = envoi bloqué
  - [ ] En modification : nom du manager actuel affiché, l’employé exclu des résultats
  - [ ] Résultats avec nom + poste (homonymes)
  - [ ] API : manager existant et actif, pas soi-même, pas de boucle ; nom du manager dans le détail admin
- [ ] Q7. **Pages de connexion et de changement de mot de passe** au même style que le reste de l’application
- [ ] Q8. **Nettoyages**
  - [ ] Type `PageError` partagé (au lieu d’être copié dans chaque page)
  - [x] Commiter `.vscode/settings.json`
- [ ] Q9. **Page 404**
- [ ] Q10. **Secrets hors du dépôt**
  - [ ] Clé JWT retirée de `appsettings.json` ; `dotnet user-secrets` en développement
  - [ ] Variable d’environnement en production (ex. `Jwt__Key`)
  - [ ] Nouvelle clé générée (l’ancienne reste visible dans l’historique Git)
  - [ ] L’API refuse de démarrer si la clé est absente ou trop courte
- [ ] Q11. **Passage à .NET 10** (LTS ; .NET 7 n’est plus supporté depuis mai 2024)
  - [ ] `TargetFramework` + paquets NuGet (EF Core, JWT, Swagger) mis à jour
  - [ ] Build, migrations et tests manuels OK
  - [ ] Envisager `TimeProvider` (intégré) à la place de `IAppClock`

## Notes / blocages

_(À remplir au fil de l’eau)_

-
