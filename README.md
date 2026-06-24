# Entra-Flow

Validate user CSVs and provision them into **Microsoft Entra ID** — through a web UI, a REST
API, or a command-line tool. Drop in a CSV, see exactly which rows are valid, download the
results, and (when you're ready) create the users in your tenant.

Built on .NET 10. Safe by default: provisioning runs in **dry-run** until you deliberately turn it on.

```
CSV ──► validate (required fields, email format, whitelists, duplicates) ──► valid / errors
                                                                          └─► provision to Entra
```

## What's in the box

| Component | What it is |
|-----------|------------|
| **EntraFlow.Web** | ASP.NET Core + Blazor app: upload & review, Settings (your Entra connection), run History, and a REST API. |
| **EntraFlow.Cli** | Console tool that processes a folder of CSVs — ideal for scheduled/automated runs. |
| **EntraFlow.Core** | Reusable library: CSV reading, configurable validation, CSV + Microsoft Graph sinks. |

## Quick start (web app)

```bash
dotnet run --project src/EntraFlow.Web
```

Open the app, sign in (default `admin` / `change-me` — **change this**, see below), then:

1. **Settings** → enter your Entra **Tenant ID, Client ID, Client Secret** and click **Test
   connection**. (See [docs/entra-setup.md](docs/entra-setup.md) to create the app registration.)
2. **Provision** → upload a CSV. Review the valid/rejected breakdown and download the results.
3. When ready, set the sink to **Entra (Graph)**, turn **off dry-run**, and enable live
   provisioning to create the users.

### With Docker

```bash
ENTRAFLOW_ADMIN_PASSWORD='a-strong-password' docker compose up --build
# → http://localhost:8080
```

Data (settings, audit log, encryption keys, outputs) persists in the `entraflow-data` volume.

## Quick start (CLI)

```bash
cp your-users.csv data/input/
dotnet run --project src/EntraFlow.Cli
# results in data/output/<name>-valid-*.csv and <name>-errors-*.csv
```

Exit codes: `0` all valid · `2` some rows rejected · `1` run failed.

## Input format

A header row names the columns; **add as many columns as you like** — the schema is configurable.
The defaults expect:

```csv
Name,Email,Department,Role
Jane Doe,jane.doe@company.com,IT,Admin
```

Default validation: `Name`, `Email`, `Department`, `Role` are required, `Email` must be a valid
address, and emails must be unique within a file. Add fields, mark them required, enforce a format,
or restrict to an allowed list — all from **Settings** (web) or the `Schema` config section (CLI).

## Configuration

The web app stores your Entra connection and schema in `data/settings.json` (the client secret is
**encrypted at rest** via ASP.NET Core Data Protection). Operational settings come from
configuration / environment variables:

| Setting | Purpose |
|---------|---------|
| `Admin__Username` / `Admin__Password` | Web sign-in. **Change the default password.** |
| `Admin__ApiKey` | Optional key for `/api` access via the `X-Api-Key` header. |
| `Storage__DataDirectory` | Where data is persisted (default `data`). |

CLI configuration precedence (highest last): `appsettings.json` → environment variables
(`EntraFlow__InputFolder`, `Entra__Sink`, …) → command line (`--EntraFlow:InputFolder=…`).

## REST API

All endpoints live under `/api` and require the `X-Api-Key` header or an admin session.

| Method & path | Purpose |
|---------------|---------|
| `POST /api/runs` | Upload a CSV (`multipart/form-data`, field `file`); returns the run summary. |
| `GET /api/runs` | Recent runs from the audit log. |
| `GET /api/runs/{id}` | A single run. |
| `GET /api/settings` | Current settings (secret omitted). |
| `PUT /api/settings` | Update settings (empty secret = keep existing). |
| `POST /api/settings/test-connection` | Verify Entra credentials. |

```bash
curl -X POST -H "X-Api-Key: $KEY" -F "file=@users.csv" http://localhost:8080/api/runs
```

## Project layout

```
src/EntraFlow.Core   reusable domain library (validation, sinks, Graph client)
src/EntraFlow.Cli    console host
src/EntraFlow.Web    Blazor web app + REST API
tests/               xUnit tests
docs/                setup, deployment, operations guides
```

## Development

```bash
dotnet build EntraFlow.slnx
dotnet test
```

## More docs

- [docs/entra-setup.md](docs/entra-setup.md) — create the Entra app registration (for IT teams)
- [docs/deployment.md](docs/deployment.md) — Docker, volumes, secrets, Key Vault
- [docs/operations.md](docs/operations.md) — logs, audit, troubleshooting
- [SECURITY.md](SECURITY.md) · [CONTRIBUTING.md](CONTRIBUTING.md) · [CHANGELOG.md](CHANGELOG.md)

## License

[MIT](LICENSE).
