# Checklist de la phase 2 — Congés + calendrier personnel

Référence produit : [PROJET.md](../PROJET.md) §5.2, §6, §7, §8, §9 (R5).

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Prérequis :** phase 1 validée (auth, rôles, annuaire).

## Préparation

- [ ] Relire le périmètre de la phase 2 dans `PROJET.md` (inclus / exclu)
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

- [x] 12. Navigation globale / **menu Accueil** : liens Congés, Calendrier (+ Admin si rôle) — remplacer le bouton temporaire de la phase 1
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
- [x] Q5. **`ConfirmDialog`** réutilisable pour remplacer les 3 `window.confirm` (Échap, focus piégé, variante destructive)
- [x] Q5b. **Mot de passe temporaire dans une boîte de dialogue**
  - [x] Composant `Modal` (coquille commune), utilisé par `ConfirmDialog`
  - [x] `TemporaryPasswordDialog` : fermeture par « Terminé » seulement, `data-autofocus` sur Copier, `select-all`
  - [x] Copie : échec géré (message), « Copié ✓ » pendant 2 s
  - [x] `AdminAccountsPage` et `CreateEmployeePage` : encadré jaune remplacé, `key` pour un état neuf à chaque mot de passe
  - [x] Boîtes enchaînées après la fin de l’animation (`afterLeave`) : le focus n’est plus volé par la confirmation
- [x] Q6. **Choix du manager par son nom** (combobox, Headless UI)
  - [x] API : manager existant et actif, pas soi-même, pas de boucle ; nom du manager dans le détail admin
  - [x] API : poste et téléphone réellement optionnels (`string?`), `ManagerId` en `int?` (plus de « 0 » magique)
  - [x] API : endpoint de recherche léger `GET /api/employees/lookup` (`search`, `limit`, `excludeTeamOf`) → `{ items, hasMore }`
  - [x] API : `excludeTeamOf` exclut l’employé et toute son équipe (directe et indirecte) : aucune boucle proposable
  - [x] Composant **réutilisable** `EmployeeCombobox` (manager ici ; participants phase 3, assignation phase 5)
  - [x] Recherche avec debounce + annulation des requêtes périmées (`AbortController`)
  - [x] États : recherche en cours, aucun résultat, erreur API, « Affine ta recherche » si résultats coupés
  - [x] Résultats riches : avatar à initiales, nom, poste et département (homonymes)
  - [x] Bouton pour retirer le manager ; le champ revient toujours à la sélection (aucun texte orphelin)
  - [x] En modification : nom du manager actuel affiché dès l’ouverture
- [x] Q7. **Pages de connexion et de changement de mot de passe** au même style que le reste de l’application
  - [x] API : erreurs de saisie en 400 avec message (plus de déconnexion sur un mauvais mot de passe)
  - [x] `Brand`, `AuthCard` (aussi utilisée par `FullPageStatus`), `PasswordInput` (bouton bascule accessible)
  - [x] `autoComplete` pour les gestionnaires de mots de passe ; validation avant l’envoi
  - [x] Changement forcé : carte centrée + déconnexion ; volontaire : page dans le menu + message de succès
- [ ] Q8. **Nettoyages**
  - [ ] Type `PageError` partagé (au lieu d’être copié dans chaque page)
  - [x] Classes de boutons partagées (`src/ui/buttons.ts`)
  - [x] Commiter `.vscode/settings.json`
- [ ] Q9. **Page 404**
- [x] Q10. **Secrets hors du dépôt**
  - [x] Clé JWT retirée de `appsettings.json` ; `dotnet user-secrets` en développement
  - [x] Variable d’environnement en production (ex. `Jwt__Key`)
  - [x] Nouvelle clé générée (l’ancienne reste visible dans l’historique Git)
  - [x] L’API refuse de démarrer si la clé est absente ou trop courte
- [ ] Q11. **Passage à .NET 10** (LTS ; .NET 7 n’est plus supporté depuis mai 2024)
  - [ ] `TargetFramework` + paquets NuGet (EF Core, JWT, Swagger) mis à jour
  - [ ] Build, migrations et tests manuels OK
  - [ ] Envisager `TimeProvider` (intégré) à la place de `IAppClock`
- [ ] Q12. **Recherche et tri d’employés insensibles aux accents et à la casse** (« helene » trouve « Hélène » ; « doe » et « Émond » triés comme un humain l’attend)
  - [ ] Noms affichés **tels que saisis** (aucune capitalisation automatique : « van der Berg », « McDonald », « D’Amours »)
  - [ ] Clés normalisées stockées à côté (minuscules, sans accents, apostrophes unifiées), calculées automatiquement à chaque enregistrement
  - [ ] Migration + remplissage des employés existants
  - [ ] Tri (Annuaire, `lookup`) et recherche (`ApplySearch`) sur les clés normalisées
  - [ ] Vérifier les noms composés et les apostrophes

## Notes / blocages

_(À remplir au fil de l’eau)_

-
