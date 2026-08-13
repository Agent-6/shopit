# Preview run doc — shopit (frontend portal + Aspire backend)

## Reproduce artifacts (fresh checkout)

No secret files need copying: `projects/portal/src/environments/*.ts` and `proxy.conf.json`
are committed.

**Proxy target (proxy.conf.json)** — `proxy.conf.json` forwards the SPA's `/api/**` calls to
the Yarp gateway. The committed target is `https://host.docker.internal:5001`, which is
correct when the SPA runs inside Docker. When the SPA runs on the host (the preview setup),
copy the file as-is but change the target to `https://localhost:5001` — Aspire's DCP binds
the gateway to loopback only, so `host.docker.internal` (the LAN IP) is refused from the
host. The Angular dev server reads this file at startup; restart `ng serve` after editing.

Install dependencies from `src/frontend`:

```bash
cd src/frontend
pnpm install   # add CI=true if run from a non-interactive shell
```

Windows notes (Git Bash):

- If `node_modules` contains symlinks created inside the Linux dev container, Node cannot
  resolve them (`require('@angular/build')` fails, `ng build` reports
  "Could not find the '@angular/build:application' builder's node package"). Delete the
  directory and reinstall: `rm -rf node_modules && CI=true pnpm install`. Fresh pnpm
  installs create Windows-native links.
- pnpm must be installed globally first (`npm install -g pnpm@10.29.3`).

## Run the backend (Aspire)

The SPA needs the backend for data and login (OIDC discovery, token exchange, the Identity
API). Prerequisites: Docker Desktop running (`docker info` must succeed) and the .NET SDK.

```bash
cd src/backend
dotnet run --project ShopIt.AppHost --launch-profile https
```

The `https` profile is required: the Auth API then binds `https://localhost:7234`, which the
SPA's `auth.config.ts` and the Identity API's OpenIddict issuer both expect. The stack
starts three Postgres instances, Kafka, Seq, the Auth/Identity/Tenancy APIs and a Yarp
gateway on `https://localhost:5001` (http 5000). First start pulls container images and
migrates/seeds the databases.

**Always stop any existing backend before starting a new one** — never reuse a running
AppHost. A fresh `dotnet run` picks up the latest code changes and avoids port conflicts.
If ports 5000/5001/7234 are already listening, stop the stack first: kill the
`ShopIt.AppHost.exe` tree and the `dcp.exe` trees with `taskkill /PID <pid> /T /F`
(Git Bash: `taskkill //PID <pid> //T //F`), then confirm the ports are free with
`netstat -ano | grep -E ":(5000|5001|7234)\s"`. This also shuts down the Postgres/Kafka/Seq
containers DCP started.

Health checks (after the new backend is up): `curl -sk https://localhost:7234/.well-known/openid-configuration` returns
200; `curl -sk https://localhost:5001/api/identity/users/me/permissions` returns 401
(unauthenticated) rather than a connection error.

Observability (DCP binds fixed host ports even though Docker maps random ones):
- Mailpit (dev inbox) web UI: http://localhost:8025 — API: http://localhost:8025/api/v1/messages
- Seq log viewer: http://localhost:55718

Note: after a backend restart the auth signing keys rotate, so a browser session with a
pre-restart access token gets 401s and lands on the SPA's 403 page — log out and back in.

Detached start on Windows (PowerShell), logging to `<log>` and `<log>.err` (different
files!):

```powershell
powershell -NoProfile -Command "(Start-Process -FilePath 'C:\Program Files\dotnet\dotnet.exe' -ArgumentList @('run','--project','<abs path to>\ShopIt.AppHost','--launch-profile','https') -RedirectStandardOutput '<log>' -RedirectStandardError '<log>.err' -WindowStyle Hidden -PassThru).Id"
```

## Run the frontend dev server

**Always stop any lingering dev server on port 4200 before starting a new one** — never
reuse an old `ng serve`. A fresh start picks up the latest code and proxy config.
If `netstat -ano | grep -E ":4200\s"` shows a LISTENING pid, kill that node process tree
(Git Bash: `taskkill //PID <pid> //T //F`) and confirm the port is free.

Default port is 4200 (`start:portal`):

```bash
cd src/frontend
pnpm start:portal
# or, if 4200 is occupied by another worktree's server:
pnpm exec ng serve portal --host 0.0.0.0 --port 4201 --poll 2000
```

Detached start on Windows (PowerShell), logging to `<log>` and `<log>.err`. The
`-WorkingDirectory` is required — `npm run` must resolve `package.json` in `src/frontend`:

```powershell
powershell -NoProfile -Command "(Start-Process -FilePath 'npm.cmd' -ArgumentList 'run','start:portal' -WorkingDirectory '<abs path to>\src\frontend' -RedirectStandardOutput '<log>' -RedirectStandardError '<log>.err' -WindowStyle Hidden -PassThru).Id"
```

Note: this wrapper may appear to "hang" while the dev server holds the console handle —
that is normal; check `<log>` for the `Local: http://localhost:4200/` line instead.

With the backend up, the full loop works from the browser: the auth guard starts the OIDC
code flow (`https://localhost:7234/Account/Login` → consent → `auth-callback`), and the
proxy serves `/api/**` from the gateway.
