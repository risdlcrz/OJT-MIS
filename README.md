# OJT-MIS

On-the-Job Training Management Information System. It covers the whole intern
lifecycle: applicant requests and hiring, orientation, intern IDs with QR
attendance, daily time records, time adjustments, and the Certificate of
Completion.

| Part | Tech | Folder |
|---|---|---|
| API | ASP.NET Core (.NET 10), EF Core, SQL Server, Identity + JWT | `OJTMISApi/` |
| Web app | Vue 3, Vite, vue-router, Bootstrap 5 | `src/`, `pages/`, `assets/` |
| Tests | xUnit (API) | `OJTMISApi.Tests/` |

## Who uses it

| Role | What they do |
|---|---|
| **HR Admin** | Manages requests, applicants, hiring, interns, IDs, DTRs, certificates, schools, signatories and user accounts. |
| **Supervisor** | Sees only their own department: interns, DTRs, orientations, slot requests, and time adjustment reviews. |
| **Intern** | Scans the QR on their ID to log time, views their DTR, and requests documents and time adjustments. |

---

## Prerequisites

- **.NET 10 SDK** - `dotnet --version`
- **Node.js** `^22.18.0` or `>=24.12.0` - `node --version`
- **SQL Server** (Express or Developer is fine) that you can reach with Windows login
- Windows PowerShell, for `npm run dev` / `npm run stop`

## First-time setup

### 1. Install packages

```sh
npm install
```

### 2. Set your local secrets

Secrets are **never** stored in `appsettings.json`. Each developer keeps their
own in the .NET secret store (saved in your user profile, outside the repo):

```sh
# Required: signs login tokens. Use any long random string (64+ characters).
dotnet user-secrets set "Jwt:SigningKey" "<long random string>" --project OJTMISApi

# Required: password for the default HR Admin account (created on first run).
dotnet user-secrets set "Seed:AdminPassword" "<a strong password>" --project OJTMISApi

# Your SQL Server. The database is created on first run.
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=OJTMIS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" --project OJTMISApi

# Optional: Mailtrap sandbox token. Without it, emails are saved as files (see below).
dotnet user-secrets set "Mailtrap:ApiToken" "<token>" --project OJTMISApi
```

A secure way to make a random signing key in PowerShell:

```powershell
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

Check what you have set with `dotnet user-secrets list --project OJTMISApi`.

### 3. Run it

```sh
npm run dev
```

This starts the API on <http://localhost:5198> and the web app on
<http://localhost:5173>. Open **<http://localhost:5173>**. Stop both with
`Ctrl+C`, or `npm run stop` if something is left running.

On the first run the API creates the database, the roles, and the HR Admin
account `hradmin@ojtmis.local` with the password you set in step 2.

---

## How accounts work

New accounts need **two** things before they can sign in:

1. **HR approval.** Accounts HR creates (hiring an applicant, adding an intern
   or signatory, User Management) are approved automatically. Accounts that
   interns create themselves on the Sign Up screen show as
   **Pending HR approval** in Account > User Management until HR clicks approve.
2. **Email verification.** The activation email has a **Verify my email**
   link (and the temporary password). The sign-in page can resend it.
   Resetting the password through Forgot Password also verifies the email.

### Emails during development

- **With a Mailtrap token:** emails go to your Mailtrap sandbox inbox.
- **Without one:** each email is saved as an `.html` file in
  `OJTMISApi/App_Data/outbox/`. Open it in a browser to click the activation
  or reset link. (This folder is not committed.)

## Intern IDs and attendance

- HR generates an intern's ID in **Requests > ID Generation**. Each generated
  ID gets a new, random QR code that contains no personal data.
- Generating a new ID **cancels the old card**: its QR stops working.
- An intern logs time by scanning **their own** ID while signed in. Punches go
  AM in > AM out > PM in > PM out. Arriving after 8:00 AM is marked Late.
  Two scans less than 5 minutes apart are refused.
- All dates and times are Philippine time (`App:TimeZone`, default
  `Asia/Manila`), whatever time zone the server uses.

---

## Scripts

| Command | What it does |
|---|---|
| `npm run dev` | Starts the API and the web app together |
| `npm run stop` | Stops anything still holding ports 5198 / 5173 |
| `npm run lint` | Checks the Vue/JS code with ESLint |
| `npm test` | Runs the API tests (`OJTMISApi.Tests`) |
| `npm run build` | Builds the web app into `dist/` |
| `npm run check` | Lint + build + tests (the same checks CI runs) |

Database changes: add an EF Core migration from the `OJTMISApi` folder with
`dotnet ef migrations add <Name> -o Data/Migrations`. In development it is
applied automatically on the next start.

## Project layout

```
OJTMISApi/            ASP.NET Core API
  Controllers/        HTTP endpoints, one per area (Auth, Interns, Supervisor, ...)
  Services/           Business logic (attendance, accounts, email, tokens, ...)
  Models/             Database entities
  Data/Migrations/    EF Core migrations
  AppClock.cs         The single source of "now" (Philippine time)
OJTMISApi.Tests/      xUnit tests for the API
src/
  router/             Routes and the sign-in / role guard
  components/         Layout, notification bell, shared widgets
  services/api.js     All calls to the API
  utils/              Access rules, navigation, helpers
pages/                One .vue file per screen (URL = file name, e.g. /dtr.html)
assets/               CSS, images, menu JSON
.github/workflows/    CI
```

Who may open which page is defined in `src/utils/access.js`. A new page needs
an entry there and a route in `src/router/index.js`.

## Continuous integration

Every push and pull request runs `.github/workflows/ci.yml`: the API is built
and tested, and the web app is linted and built. No database or secrets are
needed (tests use an in-memory database).

---

## Deploying

The API reads secrets from environment variables in production (`__` replaces `:`):

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | Production database |
| `Jwt__SigningKey` | Token signing key (32+ bytes, keep it secret) |
| `Seed__AdminPassword` | Only needed to create the first HR Admin |
| `Mailtrap__ApiToken` | Email delivery |
| `Mailtrap__AppBaseUrl` | Public web address used in email links |
| `AllowedHosts` | Your domain, e.g. `ojtmis.example.gov.ph` |
| `Cors__AllowedOrigins__0` | The web app's address, if served from another origin |

Notes:

- **Migrations:** outside Development the API does not change the database on
  start-up. Run `dotnet OJTMISApi.dll --migrate` once after each deployment
  (it migrates, seeds and exits), or set `Database__MigrateOnStartup=true` for
  a single-server setup. The API refuses to start if migrations are pending.
- **HTTPS:** outside Development the API redirects to HTTPS and sends HSTS.
- **Reverse proxy** (IIS, nginx, a load balancer): set
  `ReverseProxy__Enabled=true` (and `ReverseProxy__KnownProxies__0`) so rate
  limits and logs see the real client address.
- **Web app:** `npm run build`, then serve `dist/` with a fallback to
  `index.html` for unknown paths (the app uses `*.html` page addresses). Set
  `VITE_API_URL` at build time if the API is on a different address than `/api`.

## Troubleshooting

| Problem | Fix |
|---|---|
| `Jwt:SigningKey is missing or shorter than 32 bytes` | Set it with `dotnet user-secrets` (step 2). |
| `Could not apply database migrations` | Check `ConnectionStrings:DefaultConnection` and that SQL Server is running. |
| `Port 5198 is already in use` | `npm run stop`, then `npm run dev` again. |
| Can't sign in: "Verify your email first" | Open the activation email (or `App_Data/outbox/`), or use "Resend verification email". |
| Can't sign in: "waiting for HR approval" | HR approves it in Account > User Management. |
| "Too many attempts" | Sign-in and email requests are rate limited per address; wait the time shown. |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for branch and commit message conventions.
