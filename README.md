# Northwind.App.Backend

En moderne ASP.NET Core REST API backend-applikation, der fungerer som en demo og reference-implementering af bedste praksis. Bygget med .NET 10, Entity Framework Core og JWT-autentificering demonstrerer dette projekt en komplet web-API med Docker-understøttelse og cloud-deployment.

## 🌟 Funktioner

- ✅ **RESTful API** - Komplette CRUD-operationer for Northwind database-entiteter
- ✅ **JWT-autentificering** - Sikre endpoints med access- og refresh-tokens
- ✅ **Entity Framework Core** - SQLite-database med code-first tilgang
- ✅ **OpenAPI/Swagger** - Interaktiv API-dokumentation
- ✅ **Struktureret Logging** - Serilog med konsol-output til containers
- ✅ **Health Checks** - Kubernetes-klar liveness og readiness probes
- ✅ **Problem Details (RFC 7807)** - Konsistente fejlsvar
- ✅ **Docker-understøttelse** - Multi-stage build med sikkerhedsbedste praksis
- ✅ **Cloud Ready** - Deployed på Render.com med automatiske deployments

## 🚀 Live Demo

API'et er deployed og tilgængeligt på:

**🔗 [https://northwind-backend-b088.onrender.com](https://northwind-backend-b088.onrender.com)**

> **⚠️ Bemærkning om Render.com Free Tier:**  
> Denne applikation hostes på Render's gratis tier, som automatisk lukker ned efter 15 minutters inaktivitet. Den første forespørgsel efter inaktivitet kan tage **30-50 sekunder** at svare, mens tjenesten starter op igen. Efterfølgende forespørgsler vil være hurtige. Dette er normal adfærd for free-tier deployments.

### Hurtige Links

- **Swagger UI**: [https://northwind-backend-b088.onrender.com/swagger](https://northwind-backend-b088.onrender.com/swagger)
- **Health Check**: [https://northwind-backend-b088.onrender.com/health/live](https://northwind-backend-b088.onrender.com/health/live)
- **API Version**: [https://northwind-backend-b088.onrender.com/version](https://northwind-backend-b088.onrender.com/version)

## 🛠️ Teknologi Stack

- **[.NET 10](https://dotnet.microsoft.com/)** - Nyeste .NET framework
- **[ASP.NET Core Web API](https://docs.microsoft.com/aspnet/core/)** - Højtydende web framework
- **[Entity Framework Core](https://docs.microsoft.com/ef/core/)** - Moderne ORM til .NET
- **[SQLite](https://www.sqlite.org/)** - Letvægts filbaseret database
- **[JWT Bearer Authentication](https://jwt.io/)** - Industristandard token-autentificering
- **[Serilog](https://serilog.net/)** - Struktureret logging-bibliotek
- **[Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)** - OpenAPI/Swagger-værktøjer
- **[Docker](https://www.docker.com/)** - Containeriserings-platform
- **[Render.com](https://render.com/)** - Cloud-platform til deployment

## 📚 API Endpoints

### System Endpoints

| Endpoint        | Method   | Beskrivelse                   | Kræver Auth |
| --------------- | -------- | ----------------------------- | ----------- |
| `/`             | GET      | Omdirigerer til Swagger UI    | Nej         |
| `/health`       | GET      | Basis health check            | Nej         |
| `/health/live`  | GET/HEAD | Liveness probe                | Nej         |
| `/health/ready` | GET/HEAD | Readiness probe               | Nej         |
| `/version`      | GET      | API version                   | Nej         |
| `/config`       | GET      | Runtime-konfiguration         | Nej         |
| `/test`         | GET      | Echo test endpoint            | Nej         |
| `/test/error`   | GET      | Test fejlhåndtering           | Nej         |
| `/swagger`      | GET      | API-dokumentation             | Nej         |

### Autentificerings-Endpoints

| Endpoint               | Method | Beskrivelse                 | Kræver Auth |
| ---------------------- | ------ | --------------------------- | ----------- |
| `/api/auth/login`      | POST   | Login med credentials       | Nej         |
| `/api/auth/refresh`    | POST   | Forny access token          | Nej         |
| `/api/auth/logout`     | POST   | Logout aktuel session       | Ja          |
| `/api/auth/logout-all` | POST   | Logout alle sessioner       | Ja          |
| `/api/auth/me`         | GET    | Hent aktuel brugerinfo      | Ja          |

**Demo Credentials:**
```json
{
  "username": "admin",
  "password": "admin"
}
```
or
```json
{
  "username": "user",
  "password": "user"
}
```

### Offentlige Kunde-Endpoints (Ingen Autentificering)

| Endpoint                             | Method | Beskrivelse                                           | Query Parameters                            |
| ------------------------------------ | ------ | ----------------------------------------------------- | ------------------------------------------- |
| `/api/public/customers`              | GET    | Hent alle kunder (pagineret)                          | `skip` (standard: 0), `take` (standard: 1000) |
| `/api/public/customers-with-revenue` | GET    | Hent alle kunder med omsætningsinfo (sorteret)        | `skip` (standard: 0), `take` (standard: 1000) |
| `/api/public/customers/{id}`         | GET    | Hent kunde efter ID                                   | -                                           |
| `/api/public/customers/{id}/orders`  | GET    | Hent kunde med ordrer                                 | `maxOrders` (standard: 10)                  |
| `/api/public/customers`              | POST   | Opret ny kunde                                        | -                                           |
| `/api/public/customers/{id}`         | PUT    | Opdater kunde                                         | -                                           |
| `/api/public/customers/{id}`         | PATCH  | Delvist opdater kunde                                 | -                                           |
| `/api/public/customers/{id}`         | DELETE | Slet kunde                                            | -                                           |

### Beskyttede Kunde-Endpoints (Kræver Autentificering)

| Endpoint         | Method | Beskrivelse                       | Query Parameters                            |
| ---------------- | ------ | --------------------------------- | ------------------------------------------- |
| `/api/customers` | GET    | Hent alle kunder (kræver JWT)     | `skip` (standard: 0), `take` (standard: 1000) |

## 🏃 Kom i Gang

### Forudsætninger

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/)
- (Valgfrit) [Docker Desktop](https://www.docker.com/products/docker-desktop)
- (Valgfrit) [Visual Studio Code](https://code.visualstudio.com/) eller [Visual Studio 2022](https://visualstudio.microsoft.com/)

### Lokal Udvikling

1. **Klon repository**
   ```bash
   git clone https://github.com/devcronberg/Northwind.App.Backend.git
   cd Northwind.App.Backend
   ```

2. **Gendan dependencies**
   ```bash
   dotnet restore
   ```

3. **Kør applikationen**
   ```bash
   dotnet run
   ```

4. **Åbn Swagger UI**
   
   Naviger til: [http://localhost:5000/swagger](http://localhost:5000/swagger)

### Test API'et

#### Eksempel 1: Hent alle kunder (ingen autentificering)
```bash
curl http://localhost:5000/api/public/customers
```

#### Eksempel 2: Login og få JWT token
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'
```

Svar:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123...",
  "expiresIn": 3600
}
```

#### Eksempel 3: Tilgå beskyttet endpoint med JWT
```bash
curl http://localhost:5000/api/customers \
  -H "Authorization: Bearer DIN_ACCESS_TOKEN"
```

## 🐳 Docker

### Byg Docker Image
```bash
docker build -t northwind-backend .
```

### Kør Container
```bash
docker run -p 8080:8080 northwind-backend
```

API'et vil være tilgængeligt på [http://localhost:8080](http://localhost:8080)

### Docker Image Detaljer

- **Multi-stage build** - Optimeret til størrelse og sikkerhed
- **Non-root bruger** - Kører som `appuser` (UID 1001)
- **Størrelse** - Cirka 220MB (kun runtime)
- **Base images**:
  - Build: `mcr.microsoft.com/dotnet/sdk:10.0`
  - Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0`

## ☁️ Deployment til Render.com

Dette projekt inkluderer en `render.yaml` konfigurationsfil til nem deployment til [Render.com](https://render.com/).

### Hvad er Render.com?

Render er en moderne cloud-platform, der gør det nemt at deploye web-applikationer, API'er, databaser og mere. Den tilbyder:

- **Free Tier** - Perfekt til demoer og små projekter
- **Automatiske Deployments** - Deployer automatisk når du pusher til GitHub
- **Docker-understøttelse** - Native Docker container deployment
- **HTTPS som standard** - Gratis SSL-certifikater
- **Health Checks** - Indbygget overvågning
- **Zero DevOps** - Ingen server-administration påkrævet

### Deploy til Render

1. **Push til GitHub**
   ```bash
   git add .
   git commit -m "Initial commit"
   git push origin main
   ```

2. **Opret Render-konto**
   - Tilmeld dig på [render.com](https://render.com)
   - Tilslut din GitHub-konto

3. **Deploy med Blueprint**
   - Klik "New +" → "Blueprint"
   - Vælg dette repository
   - Render vil detektere `render.yaml` og konfigurere automatisk
   - Klik "Apply"

4. **Vent på deployment** (5-10 minutter første gang)
   - Build logs viser Docker build fremskridt
   - Tjenesten vil være tilgængelig på `https://your-app-name.onrender.com`

### Render Free Tier Adfærd

Den gratis tier inkluderer:
- ✅ 750 timer/måned runtime
- ✅ Automatisk HTTPS
- ✅ Automatiske deployments fra GitHub
- ⚠️ Lukker ned efter 15 minutters inaktivitet
- ⚠️ Cold start tager 30-50 sekunder

**Tip:** Til produktion, opgrader til en betalt plan ($7/måned) for at eliminere nedlukning og få:
- Altid-på tjeneste (ingen cold starts)
- Mere RAM og CPU
- Hurtigere builds
- Support

## ⚙️ Konfiguration

Konfiguration håndteres gennem `appsettings.json` og miljøvariabler.

### JWT Indstillinger

```json
{
  "Jwt": {
    "Secret": "YourSecretKeyHere",
    "Issuer": "Northwind.App.Backend",
    "Audience": "Northwind.App.Frontend",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Miljøvariabler (til Produktion)

```bash
# JWT Konfiguration
Jwt__Secret=your-production-secret-key
Jwt__Issuer=Northwind.App.Backend
Jwt__Audience=Northwind.App.Frontend

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

**⚠️ Sikkerhedsbemærkning:** Commit aldrig secrets til Git. Brug miljøvariabler eller secret management services i produktion.

## 📁 Projektstruktur

```
Northwind.App.Backend/
├── Controllers/                      # API Controllers
│   ├── SystemController.cs           # System endpoints
│   ├── AuthController.cs             # Autentificering
│   ├── CustomersController.cs        # Beskyttede endpoints
│   └── PublicCustomersController.cs  # Offentlige endpoints
├── Models/
│   ├── EF/                           # Entity Framework modeller
│   │   ├── NorthwindContext.cs       # Database context
│   │   ├── Customer.cs               # Kunde entitet
│   │   ├── Order.cs                  # Ordre entitet
│   │   └── [andre entiteter]
│   └── MVC/                          # Legacy mappestruktur
├── Assets/
│   └── Northwind.db                  # SQLite database
├── Program.cs                        # Applikations entry point
├── appsettings.json                  # Konfiguration
├── Dockerfile                        # Docker build konfiguration
├── .dockerignore                     # Docker ekskluderinger
├── render.yaml                       # Render.com deployment config
└── .github/
    └── copilot-instructions.md       # AI assistent instruktioner
```

## 🔐 Autentificering

Dette API bruger JWT (JSON Web Tokens) til autentificering.

### Autentificerings-flow

1. **Login** - POST credentials til `/api/auth/login`
2. **Modtag Tokens** - Få `accessToken` og `refreshToken`
3. **Brug Access Token** - Inkluder i `Authorization: Bearer {token}` header
4. **Forny Token** - Når access token udløber, brug refresh token til at få en ny
5. **Logout** - Invalider tokens via `/api/auth/logout`

### Brug JWT i Swagger

1. Klik "Authorize" knappen i Swagger UI
2. Indtast: `Bearer DIN_ACCESS_TOKEN`
3. Klik "Authorize"
4. Test beskyttede endpoints

### Token Udløb

- **Access Token**: 60 minutter (konfigurerbar)
- **Refresh Token**: 7 dage (konfigurerbar)

## 🧪 Test

### Brug af Swagger UI

Den nemmeste måde at teste API'et på:

1. Naviger til `/swagger`
2. Prøv de offentlige endpoints (ingen autentificering nødvendig)
3. Login via `/api/auth/login` for at få et JWT token
4. Klik "Authorize" og indsæt tokenet
5. Test beskyttede endpoints

### Brug af cURL

Se eksempler i "Test API'et" sektionen ovenfor.

### Brug af Postman

1. Importer API'et ved at indsætte Swagger JSON URL'en:
   ```
   https://northwind-backend-b088.onrender.com/swagger/v1/swagger.json
   ```
2. Opret et environment med `baseUrl` variabel
3. Test endpoints med autentificerings-flow

## 📝 Demonstrerede Best Practices

Dette projekt demonstrerer:

- ✅ **Clean Architecture** - Separation of concerns
- ✅ **Async/Await** - Korrekte async programmeringsmønstre
- ✅ **Fejlhåndtering** - Problem Details (RFC 7807) standard
- ✅ **Sikkerhed** - JWT autentificering, non-root Docker bruger
- ✅ **Logging** - Struktureret logging med Serilog
- ✅ **Dokumentation** - OpenAPI/Swagger med XML kommentarer
- ✅ **Health Checks** - Kubernetes-klar probes
- ✅ **CORS** - Konfigureret til cross-origin requests
- ✅ **Docker** - Multi-stage builds, layer caching
- ✅ **Cloud Native** - Container-klar, 12-factor app principper

## 🤝 Bidrag

Dette er et demo-projekt til læringsformål. Du er velkommen til at:

- Forke repository
- Oprette feature branches
- Indsende pull requests
- Rapportere issues
- Foreslå forbedringer

## 📄 Licens

Dette projekt er open source og tilgængeligt til uddannelsesformål.

## 🙏 Anerkendelser

- **Northwind Database** - Klassisk sample database fra Microsoft
- **ASP.NET Core Team** - For det fremragende framework
- **Render.com** - For nem cloud hosting

## 📞 Kontakt & Support

- **Repository**: [https://github.com/devcronberg/Northwind.App.Backend](https://github.com/devcronberg/Northwind.App.Backend)
- **Live Demo**: [https://northwind-backend-b088.onrender.com](https://northwind-backend-b088.onrender.com)
- **Dokumentation**: Tilgængelig på `/swagger` endpoint

---

**God Kodning! 🚀**

*Dette er en demo-applikation til uddannelsesformål. Til produktion, implementer ordentlig brugerstyring, database persistens, rate limiting og sikkerhedshærdning.*
