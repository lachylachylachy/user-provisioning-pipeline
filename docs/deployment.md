# Deployment

Entra-Flow's web app is a standard ASP.NET Core application. It keeps all state on disk under a
single **data directory** (`Storage__DataDirectory`, default `data`):

```
data/
  settings.json   your Entra connection + schema (client secret encrypted)
  audit.jsonl     append-only run history
  keys/           Data Protection keys (encrypt the secret) — keep these safe
  uploads/        uploaded CSVs
  output/         generated valid/error CSVs
```

> Back up (or persist on a volume) the **whole `data` directory**. If `keys/` is lost, the saved
> client secret can no longer be decrypted and must be re-entered.

## Docker (recommended)

```bash
ENTRAFLOW_ADMIN_PASSWORD='a-strong-password' \
ENTRAFLOW_API_KEY='optional-api-key' \
docker compose up --build -d
```

`docker-compose.yml` mounts a named volume `entraflow-data` at `/app/data`, so settings, audit,
and keys survive container restarts and rebuilds. The app listens on port **8080**.

Run a one-off container instead of compose:

```bash
docker build -t entra-flow .
docker run -d -p 8080:8080 -v entraflow-data:/app/data \
  -e Admin__Password='a-strong-password' \
  -e Admin__ApiKey='optional-api-key' \
  entra-flow
```

## Self-contained binary

Tagged releases publish single-file, self-contained binaries (no .NET install needed) for
linux-x64, win-x64, and osx-arm64. Download from the GitHub release, unzip, and run
`./EntraFlow.Web`. Set `Storage__DataDirectory` to a writable, backed-up path.

## TLS

Terminate TLS at a reverse proxy (nginx, Caddy, Azure Application Gateway, etc.) in front of the
container. The app sends HSTS outside Development. Do not expose it over plain HTTP on the internet.

## Secrets

- **Admin password / API key** — supply via environment variables or your platform's secret store;
  never commit them. The app warns at startup if the default password is still in use.
- **Entra client secret** — entered in the UI and stored encrypted via Data Protection. For
  multi-instance deployments, share the Data Protection key ring (e.g. a shared volume) or back it
  with **Azure Key Vault** (`Azure.Extensions.AspNetCore.DataProtection.Keys`) so every instance can
  decrypt it.

## Multiple instances

The default file-backed stores (settings/audit) assume a single instance. To scale out, mount
shared storage for `data/` or replace `ISettingsStore`/`IAuditLog` with a database-backed
implementation (the interfaces are designed for this).
