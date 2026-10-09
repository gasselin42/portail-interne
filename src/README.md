# src/

Code applicatif du portail :

- `PortailInterne.Api/` — ASP.NET Core Web API (.NET 7, EF Core + SQLite, JWT)
- `portail-interne-web/` — React (Vite + TypeScript)

## Lancer en local

### API

La clé de signature JWT n'est pas dans le dépôt. Avant le premier lancement, la générer dans les user-secrets (stockés hors du projet, chargés seulement en Development) :

```bash
cd src/PortailInterne.Api
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
dotnet run --launch-profile http
```

- En production, fournir la clé par la variable d'environnement `Jwt__Key` (le `__` remplace le `:`)
- L'API refuse de démarrer si la clé est absente ou fait moins de 32 octets

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
