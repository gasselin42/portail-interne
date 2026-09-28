# src/

Code applicatif du portail :

- `PortailInterne.Api/` — ASP.NET Core Web API (.NET 7, EF Core + SQLite, JWT)
- `portail-interne-web/` — React (Vite + TypeScript)

## Lancer en local

### API

```bash
cd src/PortailInterne.Api
dotnet run --launch-profile http
```

- Écoute sur `http://localhost:5222`
- Au démarrage, les migrations EF Core sont appliquées et un compte admin est créé (voir `Data/DbSeeder.cs`)
- La base SQLite `portail-interne.db` est créée localement (ignorée par git)

### Frontend

Le fichier `.env.development` est ignoré par git : le créer avant le premier lancement.

```bash
cd src/portail-interne-web
echo "VITE_API_URL=http://localhost:5222" > .env.development
npm install
npm run dev
```

- Disponible sur `http://localhost:5173` (seule origine autorisée par la politique CORS de l’API)
