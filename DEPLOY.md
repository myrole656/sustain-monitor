# Deploying Sustain Monitor

Two apps run together:

| App | What it is | Exposed to the internet? |
| --- | --- | --- |
| `SustainMonitor.Web` | Blazor Server website people use | Yes (HTTPS) |
| `SustainMonitor.Api` | REST API + SQLite database + evidence files | No — only the web app calls it |

Pick **one** of the two options below. Option A (Docker) is recommended.

---

## Before the first deployment (on your laptop)

1. Run `scripts\setup.ps1` once. It builds, runs the tests and **creates the first EF Core migration** in `src/SustainMonitor.Infrastructure/Data/Migrations`.
2. Commit that `Migrations` folder. From now on, every database change is a new migration:
   ```powershell
   dotnet ef migrations add <Name> --project src/SustainMonitor.Infrastructure --startup-project src/SustainMonitor.Api --output-dir Data/Migrations
   ```
   The API applies pending migrations automatically when it starts.

---

## Option A — Linux server with Docker (recommended)

Works on any Ubuntu/Debian VM (DigitalOcean, AWS Lightsail, Azure VM, a local server). 2 GB RAM is enough to start.

### 1. Prepare the server

```bash
# Docker + compose plugin
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER   # log out and in again

# Firewall: only SSH, HTTP and HTTPS
sudo ufw allow OpenSSH && sudo ufw allow 80 && sudo ufw allow 443 && sudo ufw enable
```

Point your domain's DNS **A record** (e.g. `sustainmonitor.yourcompany.my`) at the server's public IP.

### 2. Get the code and configure

```bash
git clone <your repo url> /opt/sustainmonitor
cd /opt/sustainmonitor/deploy
cp .env.example .env
nano .env        # DOMAIN, ACME_EMAIL, ADMIN_EMAIL, ADMIN_PASSWORD, SMTP_*
```

### 3. Start

```bash
docker compose up -d --build
docker compose logs -f api     # watch the first start: migrations, GBI tools, roles, admin
```

Open `https://<DOMAIN>` — Caddy fetches the HTTPS certificate automatically on the first request (allow up to a minute).

Then:
1. Sign in as the platform admin.
2. **Remove `ADMIN_PASSWORD` from `.env`** (the admin already exists) and run `docker compose up -d`.

### 4. Updating to a new version

```bash
cd /opt/sustainmonitor && git pull
cd deploy && docker compose up -d --build
```

Database migrations run on API start. Users stay signed in (keys are kept in volumes).

Using images from CI instead of building on the server: set `IMAGE_PREFIX=ghcr.io/<org>/<repo>` and `IMAGE_TAG=<commit sha or latest>` in `.env`, then `docker compose pull && docker compose up -d`.

### 5. Backups

```bash
cd /opt/sustainmonitor/deploy
./backup.sh                          # database (safe while running) + evidence + keys → deploy/backups/
crontab -e                           # daily at 02:15:
# 15 2 * * * cd /opt/sustainmonitor/deploy && ./backup.sh >> backups/backup.log 2>&1
./restore.sh 20261126-021500         # restore a backup by its timestamp
```

Copy `deploy/backups/` off the server too (e.g. `rclone` to cloud storage). Backups are kept 30 days.

### Useful commands

| Task | Command |
| --- | --- |
| Status | `docker compose ps` |
| Logs | `docker compose logs -f web` / `api` / `caddy` |
| Site up? | `curl -I https://<DOMAIN>` (expect `200`) |
| Restart | `docker compose restart` |
| Stop | `docker compose down` (data stays in volumes) |

---

## Option B — Windows Server (IIS)

### 1. Server setup (once)

1. Install the **ASP.NET Core 10 Hosting Bundle** from https://dotnet.microsoft.com/download/dotnet/10.0 and restart IIS (`iisreset`).
2. In *Server Manager → Add Roles and Features*, enable **Web Server (IIS) → Application Development → WebSocket Protocol** (Blazor Server needs it).
3. Create folders: `D:\SustainMonitor\api`, `D:\SustainMonitor\web`, `D:\SustainMonitor\data`, `D:\SustainMonitor\web-keys`.

### 2. Publish (on your laptop)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish-iis.ps1
```

Copy `publish\api\*` → `D:\SustainMonitor\api` and `publish\web\*` → `D:\SustainMonitor\web`. Edit the two `appsettings.Production.json` files (paths, admin, SMTP).

### 3. IIS sites

| Site | Physical path | Binding | App pool |
| --- | --- | --- | --- |
| `SustainMonitor.Api` | `D:\SustainMonitor\api` | `http`, `127.0.0.1`, port **8081** (local only) | No Managed Code |
| `SustainMonitor.Web` | `D:\SustainMonitor\web` | `https`, port 443, your domain + certificate | No Managed Code |

- Give both app pool identities (`IIS AppPool\SustainMonitor.Api`, `IIS AppPool\SustainMonitor.Web`) **Modify** rights on `D:\SustainMonitor\data` and `D:\SustainMonitor\web-keys` respectively.
- In the API's app pool advanced settings set **Start Mode = AlwaysRunning** and **Idle Time-out = 0**.
- Add an `http` → `https` redirect on the web site (URL Rewrite) or bind only HTTPS.
- Certificate: use your company certificate, or win-acme (https://www.win-acme.com) for free Let's Encrypt certificates.

### 4. Updates

Stop both sites → copy the new `publish\api` and `publish\web` (keep `appsettings.Production.json`) → start. Migrations run on API start.

### 5. Backups

Back up `D:\SustainMonitor\data` daily (Windows Server Backup or a scheduled `robocopy`). Stop the API site briefly, or copy `sustainmonitor.db` with `sqlite3 sustainmonitor.db ".backup backup.db"` while it runs.

---

## Settings reference

| Setting (`appsettings` / env var with `__`) | Default | Meaning |
| --- | --- | --- |
| `ConnectionStrings:Default` | `Data Source=sustainmonitor.db` | SQLite file |
| `Storage:EvidenceFolder` | `App_Data/evidence` | Uploaded evidence |
| `DataProtection:KeysFolder` | (in memory) | **Set in production** so tokens/sessions survive restarts |
| `Seed:AdminEmail` / `Seed:AdminPassword` | — | First platform admin (created once) |
| `Smtp:*` | — | Email; when `Host` is empty emails go to the log |
| `Analysis:ProgressThreshold` | `0.8` | Progress needed to run the GBI analysis |
| `Auth:AccessTokenMinutes` / `Auth:RefreshTokenDays` | `60` / `14` | Sign-in lifetime |
| `RateLimits:AuthPerMinute` / `PublicPerMinute` | `20` / `30` | Per IP |
| `Cors:AllowedOrigins` | `[]` | Only needed if a browser app calls the API directly |
| `UseHttpsRedirection` | `true` | Set `false` behind a proxy that terminates HTTPS (Docker images do) |
| Web: `Api:BaseUrl` | `https://localhost:7180/` | Where the web app finds the API |

## Security checklist

- [ ] HTTPS only; the API is not reachable from the internet.
- [ ] `ADMIN_PASSWORD` removed after the first start.
- [ ] `DataProtection:KeysFolder` set (Docker: done) and included in backups.
- [ ] Daily backups copied off the server; a restore tested once.
- [ ] SMTP set up so invites, password resets and decisions reach people.
- [ ] Server OS updates on (`unattended-upgrades` / Windows Update).
