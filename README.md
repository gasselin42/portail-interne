# Portail Interne

Portail interne d’entreprise : annuaire des employés, demandes de congés et approbations, avec horaires de travail, réunions, réservation de salles et demandes TI à venir.

**API ASP.NET Core · React + TypeScript · EF Core · SQLite · JWT**

> 🚧 Projet en cours, construit phase par phase. La phase 1 est terminée et la phase 2 est en cours de finition.

## Pourquoi ce projet

Je connaissais déjà le C# grâce au développement de jeux vidéo, et React grâce au développement web. Ce projet sert à apprendre l’écosystème .NET côté serveur sur un vrai produit : API, base de données, authentification, logique d’affaires et règles d’accès.

L’IA m’a servi de mentor : elle propose les étapes et des pistes, puis révise mon travail. Le code est écrit à la main, et chaque décision est documentée dans [PROJET.md](./PROJET.md).

## Fonctionnalités

### Comptes et sécurité
- Connexion par jeton JWT, rôles **Employé** et **Admin** ; un employé devient manager dès qu’on lui assigne une équipe
- Création d’un compte par un admin avec un mot de passe temporaire, affiché une seule fois
- Changement de mot de passe obligatoire à la première connexion, appliqué côté API
- Activation et désactivation des comptes

### Annuaire
- Liste des employés avec recherche
- Fiche détaillée : poste, département, coordonnées, manager
- Choix du manager par son nom, avec recherche dans une liste déroulante

### Congés
- Nouvelle demande avec validation des dates, dans l’interface et dans l’API
- Suivi des demandes avec leur statut : en attente, approuvée, refusée, annulée
- Annulation par le demandeur
- File d’approbation pour les managers (leur équipe) et les admins

## Feuille de route

| Phase | Contenu | Statut |
|-------|---------|--------|
| Phase 1 | Authentification, rôles, annuaire | ✅ Terminé |
| Phase 2 | Congés, approbation, calendrier personnel | 🔨 En cours |
| Phase 2.5 | Horaires de travail, jours fériés par site | ⏳ À venir |
| Phase 3 | Réunions individuelles et de groupe, détection de conflits | ⏳ À venir |
| Phase 4 | Réservation de salles et d’équipements | ⏳ À venir |
| Phase 5 | Portail de demandes TI | ⏳ À venir |

Le détail de chaque phase se trouve dans [PROJET.md](./PROJET.md) et dans les checklists de [docs/](./docs).

## Stack

| Côté | Technologies |
|------|--------------|
| Backend | ASP.NET Core Web API, Entity Framework Core (migrations), SQLite |
| Frontend | React 19, TypeScript, Vite, Tailwind CSS, Headless UI, React Router |
| Auth | JWT Bearer, mots de passe hachés |
| Qualité | ESLint, Prettier, Conventional Commits |

## Lancer le projet

Les instructions complètes sont dans [src/README.md](./src/README.md). En bref :

```bash
# API (http://localhost:5222)
cd src/PortailInterne.Api
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
dotnet run --launch-profile http

# Frontend (http://localhost:5173), dans un autre terminal
cd src/portail-interne-web
echo "VITE_API_URL=http://localhost:5222" > .env.development
npm install
npm run dev
```

Au premier lancement, la base est créée avec un compte de démonstration : `admin@portail.local` / `Admin123!`.

## Méthode de travail

1. Chaque phase a sa checklist et doit être démontrable seule avant de passer à la suivante.
2. Les règles d’affaires sont décidées et notées avant de construire l’interface.
3. Chaque fonctionnalité est développée sur sa propre branche, avec des commits atomiques.

## Structure

```text
portail-interne/
├── PROJET.md                 # Spécification, règles d’affaires, décisions
├── docs/                     # Checklists par phase
└── src/
    ├── PortailInterne.Api/   # Web API .NET
    └── portail-interne-web/  # Application React
```
