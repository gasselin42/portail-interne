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
- [ ] 13. Écran **Mes demandes** (liste + statuts)
- [ ] 14. Écran **Nouvelle demande** (+ validation dates)
- [ ] 15. Écran **File d’approbation** (Manager/Admin uniquement)
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

## Notes / blocages

_(À remplir au fil de l’eau)_

-
