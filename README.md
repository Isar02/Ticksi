# Ticksi

A web application for event tickets: organizers publish events, visitors buy tickets by card and receive them with a QR code.

ASP.NET Core 8 Web API (Clean Architecture, CQRS with MediatR, EF Core, SQL Server) · Angular 19 with Angular Material · Stripe in test mode.

| Folder | Contents |
|---|---|
| `Backend/` | API, Application, Domain, Infrastructure and Tests projects (`TicksiApp.sln`) |
| `Angular/` | Web application |

## Requirements

- .NET SDK 8 (pinned in `global.json`)
- Node.js 20 (20.11.1 or later) or 22 and later
- SQL Server LocalDB, installed with Visual Studio, or any SQL Server with its connection string
- Google Chrome for the web application's tests

## Run

1. Add Stripe test keys from the Stripe dashboard as user secrets. Without them the application runs; only card payments fail.

   ```bash
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project Backend/API
   dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..." --project Backend/API
   ```

2. Start the API on `https://localhost:5001` (Swagger at `/swagger`). On start it applies the migrations to `TicksiDb` on LocalDB and, in Development and Staging, adds the demo data.

   ```bash
   dotnet run --project Backend/API --launch-profile https
   ```

   If the browser refuses the certificate, run `dotnet dev-certs https --trust` once.

3. Start the web application on `http://localhost:4200`.

   ```bash
   cd Angular
   npm ci
   npm start
   ```

## Configuration

The API reads `appsettings.json`, then `appsettings.{Environment}.json`, then user secrets (Development only) and environment variables, written as `Section__Key` (for example `ConnectionStrings__DefaultConnection`). Staging and Production take the connection string, the JWT key, the allowed origins and the Stripe keys from environment variables.

| Key | Purpose | Development |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Database | LocalDB, `TicksiDb` |
| `Jwt:Key` | Token signing key, at least 32 characters | development key |
| `Jwt:Issuer`, `Jwt:Audience` | Token issuer and audience | `Ticksi.Api`, `Ticksi.Client` |
| `Jwt:AccessTokenMinutes`, `Jwt:SessionIdleMinutes` | Access token lifetime, idle session window | 15, 30 |
| `ClientApp:AllowedOrigins` | Origins allowed by CORS | `http://localhost:4200` |
| `Stripe:SecretKey`, `Stripe:PublishableKey` | Stripe test keys | user secrets |
| `Stripe:WebhookSecret` | Signing secret for `POST /api/payments/webhook`, optional | — |
| `Events:TimeZone` | Time zone of event dates | `Europe/Sarajevo` |
| `FileUpload:MaxFileSizeBytes` | Largest upload | 5 MB |
| `FileUpload:AllowedImageTypes` | Accepted image types | `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp` |
| `FileUpload:EventPosterPath`, `FileUpload:CategoryPosterPath` | Upload folders under `wwwroot` | `images/events`, `images/categories` |
| `Seeding:DemoData`, `Seeding:DemoPassword` | Demo data on start | on, `Demo123!` |

The web application keeps the API address (`apiUrl`) and the event time zone (`eventTimeZone`) in `Angular/src/environments/`; `npm run build -- --configuration staging` or `production`, run in `Angular/`, picks the matching file.

## Paying in test mode

Pay with card `4242 4242 4242 4242`, any future expiry date and any security code. The order page confirms the payment with the API itself. To receive Stripe's webhooks locally as well, run `stripe listen --forward-to https://localhost:5001/api/payments/webhook` and set `Stripe:WebhookSecret` to the secret it prints.

## Demo accounts

In Development and Staging every demo account has the password `Demo123!`.

| Role | Email |
|---|---|
| Admin | admin@ticksi.com |
| Organizer | organizer@ticksi.com |
| User | user@ticksi.com |

## Tests

```bash
dotnet test Backend/TicksiApp.sln
```

Unit tests run on EF Core InMemory; integration tests run the API against temporary LocalDB databases that are removed afterwards.

```bash
cd Angular
npm test -- --watch=false
```
