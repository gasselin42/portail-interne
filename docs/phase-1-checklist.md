# Checklist de la phase 1 — Auth, rôles, annuaire

Référence produit : [PROJET.md](../PROJET.md) §6 et §11.

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

## Préparation

- [x] Relire le périmètre de la phase 1 dans `PROJET.md` (inclus / exclu)
- [x] Stack UI/backend : **Web API + React** (plus MVC/Razor)
- [x] BDD : **SQLite**
- [x] Auth : **JWT** (Bearer)

## Backend (API)

- [x] 1. Créer solution + projet Web API dans `src/` — vérifier démarrage / Swagger
- [x] 2. Modèles `Employee` + `UserAccount` (+ rôle)
- [x] 3. Configurer SQLite + EF Core
- [x] 4. Migrations / schéma initial
- [x] 5. Seed : 1 admin + 1–2 employés de test
- [x] 6. Hash des mots de passe
- [x] 7. Auth : login + `GET /api/auth/me`
- [x] 8. `MustChangePassword` + endpoint changement de mdp + blocage des autres routes
- [x] 9. Autorisation par rôles (Admin vs Employee)
- [x] 10. Admin : créer employé + mdp temporaire (renvoyé une fois)
- [x] 11. Admin : lister / activer-désactiver + Annuaire API (liste, recherche, fiche)

## Frontend (React)

- [x] 12. Créer l’app React (Vite) dans `src/portail-interne-web/`
- [x] 13. Brancher l’API (base URL) + CORS côté API
- [x] 14. Écran Login + erreurs
- [x] 15. Écran changement de mdp forcé + redirection si flag
- [x] 16. Écrans Admin (créer + liste comptes)
- [x] 17. Écrans Annuaire + fiche employé

## Approfondissement (toujours dans la phase 1)

- [x] 20. Admin : modifier un employé (poste, département, téléphone, photo) après la création
- [x] 21. Lien « Changer mon mot de passe » utilisable même sans `MustChangePassword`
- [x] 22. Session expirée : un 401 déconnecte et renvoie vers `/login`
- [x] 23. Fiche employé : afficher le nom du manager, pas seulement son id

## Validation manuelle

- [x] Admin se connecte
- [x] Admin crée un employé → voit le mdp temporaire
- [x] Nouvel employé doit changer son mdp avant l’annuaire
- [x] Après changement, accès OK
- [x] Employé ne peut pas appeler / ouvrir les routes Admin
- [x] Compte inactif ne peut pas se connecter
- [x] Recherche annuaire fonctionne

## Bonus

- [ ] 19. Tests API automatisés (login, mdp temporaire, rôles)

## Notes / blocages

- Accueil phase 1 : lien Admin minimal OK. Menu global + widgets (calendrier, notifications) reportés → **phase 2** (nav/accueil), **phase 3** (badge notifs), **phase 5** (widget notifs final).
-
