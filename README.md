# Portail Interne

Outil interne d’entreprise : annuaire, congés, calendrier / meetings, réservation de salles & équipements, portail de demandes IT.

## Stack

- **Backend :** ASP.NET Core Web API + EF Core + SQLite  
- **Frontend :** React (Vite)  
- **Auth :** JWT (Bearer)

## Documentation

Toute la spécification produit est dans :

👉 **[PROJET.md](./PROJET.md)**

Checklists opérationnelles :

- **[docs/MVP1-checklist.md](./docs/MVP1-checklist.md)** — Auth, rôles, annuaire
- **[docs/MVP2-checklist.md](./docs/MVP2-checklist.md)** — Congés + calendrier personnel
- **[docs/MVP3-checklist.md](./docs/MVP3-checklist.md)** — Meetings + conflits
- **[docs/MVP4-checklist.md](./docs/MVP4-checklist.md)** — Salles et équipements
- **[docs/MVP5-checklist.md](./docs/MVP5-checklist.md)** — Portail IT

## Structure (cible)

```text
portail-interne/
├── PROJET.md
├── README.md
├── docs/
├── src/
│   ├── PortailInterne.Api/       # Web API .NET
│   └── portail-interne-web/      # React
├── tests/
└── assets/mockups/
```

## Mode de travail

1. Avancer **MVP par MVP**
2. L’IA donne des **étapes**, pas le code (sauf demande explicite)
3. Valider chaque MVP avec sa checklist avant de passer au suivant
