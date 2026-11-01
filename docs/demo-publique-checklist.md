# Checklist du jalon — Démo publique

Référence produit : [PROJET.md](../PROJET.md) §6 (Jalon — Démo publique), §8.

Stack figée : **ASP.NET Core Web API + React + SQLite**.  
Coche au fur et à mesure. Le code n’est fourni que sur demande.

**Quand :** après n’importe quelle phase ; recommandé après la phase 3 (les meetings rendent la démo parlante), puis mis à jour à chaque phase.

## Préparation

- [ ] Choisir l’hébergement : API .NET + fichier SQLite persistant + site statique pour React (comparer offre gratuite, mise en veille, HTTPS, domaine)
- [ ] Décider le domaine / sous-domaine et l’URL de l’API (CORS)
- [ ] Lister les secrets à fournir en production (clé JWT ≥ 32 octets, chaîne de connexion) : jamais dans le dépôt
- [ ] Choisir des photos de démo libres de droits ou des avatars générés (aucune vraie personne)

## Backend (API)

- [ ] 1. Configuration `Demo:Enabled` (désactivée par défaut, activée seulement en ligne)
- [ ] 2. **Générateur de données de démo** relatif à aujourd’hui
  - [ ] 2 sites (fuseaux différents), une quinzaine d’employés, 2 équipes avec managers
  - [ ] Horaires variés, fériés, congés passés / en cours / à venir (approuvés, en attente, refusés)
  - [ ] Meetings et blocs personnels sur la semaine courante et la suivante ; quelques notifications non lues
  - [ ] Toujours cohérent avec les règles (R6, R14, R23…) : passer par les mêmes validations que l’API
- [ ] 3. Remise à zéro chaque nuit : base recréée puis régénérée (tâche de fond, heure creuse)
- [ ] 4. Comptes de démo par rôle (Employé, Manager, Admin, Admin global) ; endpoint de connexion « comme » un rôle, actif seulement en mode démo
- [ ] 5. Actions bloquées sur les comptes de démo en mode démo : mot de passe, rôle, désactivation, date de départ (message clair, pas une erreur générique)
- [ ] 6. Limitation de débit sur la connexion et les créations (un visiteur ne peut pas remplir la base)
- [ ] 7. En-têtes de sécurité, HTTPS forcé, journalisation des erreurs

## Frontend (React)

- [ ] 8. Page de connexion en mode démo : boutons « Se connecter comme… » avec une phrase par rôle (« Manager : approuve les congés de son équipe »)
- [ ] 9. Bandeau discret « Démo — les données sont remises à zéro chaque nuit » sur toutes les pages
- [ ] 10. Build de production pointant vers l’URL de l’API en ligne (variable d’environnement Vite)

## Mise en ligne

- [ ] 11. Déploiement de l’API et de React ; vérifier CORS, photos (endpoint protégé) et fuseaux en ligne
- [ ] 12. README : lien vers la démo, capture d’écran, comptes de démo expliqués

## Validation manuelle

- [ ] Depuis un téléphone, en navigation privée : lien → connecté comme Manager en un clic
- [ ] Le calendrier de la semaine courante contient congés, meetings et horaires, même plusieurs semaines après le déploiement
- [ ] Un visiteur crée un congé ; le lendemain, il a disparu
- [ ] Impossible de changer le mot de passe d’un compte de démo
- [ ] En local, sans `Demo:Enabled`, aucun bouton de démo n’apparaît et l’endpoint de connexion « comme » répond 404

## Bonus

- [ ] Visite guidée au premier passage (3 ou 4 étapes : calendrier, congés, meetings)
- [ ] Badge de statut (démo en ligne / hors ligne) dans le README

## Notes / blocages

_(À remplir au fil de l’eau)_

-
