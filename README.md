# Sustain Monitor (.NET)

GBI-aligned sustainability tracking for construction companies. A Project Manager raises CO-tickets with evidence, a Superior approves them, every approval re-scores the project against the official GBI points, and once 80% of the targeted points are confirmed the CEO or PM can run the GBI analysis. A result of 50+ points requests an e-certificate that a platform admin approves, for use in tenders.

> The e-cert is a **GBI-aligned self-assessment e-certificate issued by Sustain Monitor**. It is not an official Green Building Index certificate; only GBI's Certifiers and Accreditation Panel grant those. Do not label it "GBI certified" or use the GBI logo.

## Projects

| Project | What it does |
| --- | --- |
| `src/SustainMonitor.Domain` | Entities, GBI catalogue (NREB V1.1, Interiors V1.01), scoring engine, progress + projected score, CO-ticket workflow by role, e-cert lifecycle, company rules. No dependencies. |
| `src/SustainMonitor.Infrastructure` | EF Core 10 + SQLite, ASP.NET Core Identity, evidence file storage, database seeding. |
| `src/SustainMonitor.Api` | Minimal API for the whole flow (below). |
| `src/SustainMonitor.Web` | Blazor Server website: every screen for CEO, Project Manager, Superior and platform admin. |
| `deploy/` | Docker Compose + Caddy (automatic HTTPS), backup and restore scripts. See [DEPLOY.md](DEPLOY.md). |
| `tests/SustainMonitor.Domain.Tests` | 42 xUnit tests for every business rule. |
| `src/SustainMonitor.Api/SustainMonitor.Api.http` | The whole flow as runnable requests, all four roles. |
| `scripts/setup.ps1` | First-time setup on Windows. |

## Roles

| Action | CEO | Project Manager | Superior | Platform admin |
| --- | --- | --- | --- | --- |
| Register company | Yes | | | |
| Verify company | | | | Yes |
| Invite PM / Superior | Yes | | | |
| Create project, submit baseline | | Yes | | |
| Create CO-ticket, upload evidence, send for review | | Yes | | |
| Approve (award points) or reject ticket | | | Yes | |
| Run GBI analysis (progress ≥ 80%) | Yes | Yes | | |
| Approve / decline / revoke e-cert | | | | Yes |
| View e-certs | Yes | Yes | | |

## Try it on GitHub (no server needed)

**Automatic test deployment.** Every push runs two GitHub Actions:
- **CI** — builds everything and runs all unit and integration tests.
- **Test deployment** — builds the Docker images, starts the API and website on GitHub's machine, runs `scripts/smoke-test.sh` against them (health, admin sign-in, GBI tools, company registration and verification, website pages), then shuts down.

See the results under the repository's **Actions** tab.

**Click through it in GitHub Codespaces.** On the repository page: **Code → Codespaces → Create codespace on main**. When it opens, run:

```bash
bash scripts/dev-run.sh
```

Open the **Ports** tab → port **5181** (Website). Sign in as `admin@sustainmonitor.local` / `Admin2026Pass`, then use *Register your company* on the sign-in page to try the CEO, Project Manager and Superior flow. Stop with Ctrl+C. Data is kept inside the codespace only.

## First run

Requires the .NET 10 SDK (`dotnet --version` → 10.x; Visual Studio 2026 includes it).

```powershell
cd sustain-monitor-dotnet
powershell -ExecutionPolicy Bypass -File scripts\setup.ps1

# two terminals:
dotnet run --project src\SustainMonitor.Api --launch-profile https   # https://localhost:7180
dotnet run --project src\SustainMonitor.Web --launch-profile https   # https://localhost:7181  ← open this
```

In Visual Studio: right-click the solution → *Configure Startup Projects* → *Multiple startup projects* → Start both `SustainMonitor.Api` and `SustainMonitor.Web`.

`setup.ps1` restores, builds, runs the tests, saves the platform admin to user-secrets, creates the first EF Core migration and trusts the dev HTTPS certificate. Then open:

- API docs (try any endpoint): https://localhost:7180/scalar
- Health: https://localhost:7180/health
- Whole flow: open `src/SustainMonitor.Api/SustainMonitor.Api.http` and send the requests in order (set `@adminEmail` / `@adminPassword` at the top to what you gave the script).

First start creates `sustainmonitor.db` (WAL mode), seeds both GBI tools, the roles PlatformAdmin / CEO / ProjectManager / Superior, and the platform admin.

## Authentication and security

| Item | Setting |
| --- | --- |
| Sign-in | ASP.NET Core Identity bearer tokens: `POST /api/auth/login` → `accessToken` + `refreshToken`; renew with `POST /api/auth/refresh` |
| Token lifetime | Access 60 min, refresh 14 days (`Auth:AccessTokenMinutes`, `Auth:RefreshTokenDays`) |
| Passwords | 10+ characters with upper, lower and a digit |
| Lockout | 5 wrong passwords → locked 15 minutes |
| Sign-up | Open `/api/auth/register` is off. Companies register via `/api/companies/register`; staff are invited by their CEO |
| First login | Invited users get a temporary password and must call `POST /api/auth/change-password` before anything else (403 until then) |
| Forgot password | `POST /api/auth/forgotPassword` emails a reset code → `POST /api/auth/resetPassword` |
| Deactivated / suspended | CEO deactivating a user, or an admin suspending a company, locks those accounts; their existing tokens stop working immediately |
| Rate limits | Auth endpoints 20/min per IP, public endpoints (company registration, e-cert verify) 30/min (`RateLimits:*`) |
| Headers | `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, HSTS outside Development |
| CORS | Only origins in `Cors:AllowedOrigins` (Development allows the local web ports) |
| Who am I | `GET /api/auth/me` → roles, company, company status, must-change-password flag |

## Email and notifications

Every key event creates an in-app notification (`GET /api/notifications`, `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`) and sends an email:

| Event | Who is told |
| --- | --- |
| Company registered | Platform admins |
| Company verified / rejected / suspended | CEO |
| Staff invited | The new user (sign-in email + temporary password) |
| Ticket sent for review | Superiors |
| Ticket approved / rejected | The PM who raised it |
| Project rating band changed | CEO and PMs |
| E-cert requested | Platform admins |
| E-cert issued / declined / revoked | CEO and PMs |

Email goes through SMTP when `Smtp:Host` is set (use user-secrets for `Smtp:User` / `Smtp:Password`). When it isn't set, emails are written to the console log, so development works without a mail server.

## The flow through the API

All calls except company registration, login and `/api/verify` need `Authorization: Bearer <accessToken>` from `POST /api/auth/login`.

1. **Company registers** (no login): `POST /api/companies/register`
   `{ "companyName": "Binaan Hijau Sdn Bhd", "ssmNo": "202301012345", "cidbGrade": "G5", "ceoName": "Aminah", "ceoEmail": "ceo@binaan.my", "password": "..." }`
2. **Platform admin verifies**: `GET /api/admin/companies?status=Pending` → `POST /api/admin/companies/{id}/verify` (also `/reject`, `/suspend` with `{ "reason": "..." }`; `GET /api/admin/users`)
3. **CEO invites team**: `POST /api/company/users` `{ "fullName": "Hafiz", "email": "pm@binaan.my", "role": "ProjectManager", "temporaryPassword": "..." }` (and a `Superior`). Deactivate / reactivate: `POST /api/company/users/{userId}/deactivate` or `/reactivate`. Invited users change their password at first login.
4. **PM creates project**: `GET /api/tools` → `POST /api/projects` `{ "code": "KLT", "name": "Kelulut HQ", "toolId": 1, "gfaM2": 1200, "targetRating": "Gold" }`
5. **PM submits baseline** once: `POST /api/projects/{id}/baseline` `{ "stage": "SelfCheck", "scores": [ { "criterionId": 5, "status": "InProgress", "pointsTargeted": 15, "pointsAwarded": 7 }, ... ] }` → returns suggested tickets for each gap.
6. **PM creates CO-ticket**: `POST /api/projects/{id}/tickets` `{ "criterionId": 38, "title": "Install rainwater harvesting tank", "pointsAtStake": 3 }` (38 = NREB WE1 on a fresh database)
7. **PM works it**: `POST /api/tickets/{id}/move { "to": "InProgress" }` → upload `POST /api/tickets/{id}/evidence` (multipart `file`) → `POST /api/tickets/{id}/move { "to": "InReview" }`
8. **Superior decides**: approve `POST /api/tickets/{id}/move { "to": "Done", "pointsGained": 3 }` (re-scores the project and returns before/after) or reject `{ "to": "InProgress", "note": "Bills missing" }`
9. **Board and progress**: `GET /api/projects/{id}/tickets` (columns + allowed moves for your role), `GET /api/projects/{id}` (confirmed score, projected score, progress %, criteria), `GET /api/projects/{id}/history`
10. **CEO or PM runs analysis** at ≥ 80%: `POST /api/projects/{id}/analysis` → GBI points, rating, categories; requests the e-cert at 50+ points.
11. **Platform admin decides e-cert**: `GET /api/admin/ecerts?status=Requested` → `POST /api/admin/ecerts/{id}/approve` (or `/decline`, `/revoke` with `{ "reason": "..." }`)
12. **Tender board verifies** (no login): `GET /api/verify/{token}` — the QR code target.

Open self-registration through `/api/auth/register` is switched off; people join via company registration or a CEO invite.

## Rules enforced in code and tests

- Points are calculated only by `ScoringEngine`, using the official GBI criterion maxima and rating bands (Platinum 86+, Gold 76+, Silver 66+, Certified 50+).
- After the baseline, points change only through approved CO-tickets. Each approval inserts a new snapshot (`VersionNo + 1`); history is kept.
- A ticket's points at stake can never push a criterion past its GBI maximum, counting other open tickets on the same criterion.
- A ticket needs at least one evidence file before review. A Superior cannot approve a ticket they raised or work on.
- Confirmed score = baseline + approved tickets. Projected score = confirmed + open tickets' points (capped per criterion).
- Progress = confirmed points ÷ targeted points. Analysis unlocks at 80%.
- E-cert: requested at 50+ points, issued only by a platform admin, valid 3 years (GBI's re-verification cycle), superseded by newer ones, revocable, publicly verifiable.

## Website screens

| Who | Screens |
| --- | --- |
| Everyone | Sign in, register company, forgot password, public e-cert check (`/verify/{token}`), change password (forced at first sign-in), notifications |
| CEO | Dashboard, company & team (add / deactivate PM and Superior), project overview, run GBI analysis, view & print e-cert with QR |
| Project Manager | New project, baseline scoring per GBI criterion, new CO-ticket, upload evidence, send for review, run analysis |
| Superior | Ticket board with "your move" hints, approve (award points → instant re-score) or reject with a note |
| Platform admin | Overview, verify / reject / suspend companies, approve / decline / revoke e-certs, user list |

The web app talks only to the API; the browser never calls the API directly. E-certs print to PDF from the browser (*Print / Save as PDF*), with a QR code to the public check page.

## Not built yet

- AI summary and gap plan on analysis.
- Email confirmation of new addresses (accounts are trusted on registration/invite).
