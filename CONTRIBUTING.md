# Contributing

Thanks for your interest in Entra-Flow!

## Getting started

```bash
git clone <repo>
cd Entra-Flow
dotnet build EntraFlow.slnx
dotnet test
```

Requirements: the **.NET 10 SDK**.

## Project layout

- `src/EntraFlow.Core` — domain logic (validation, sinks, Graph client). No UI/host concerns.
- `src/EntraFlow.Cli` — console host.
- `src/EntraFlow.Web` — Blazor app + REST API.
- `tests/` — xUnit tests.

New behaviour generally belongs in **Core** (so the CLI, web, and API all benefit) behind the
existing seams: `IUserReader`, `IUserValidator`, `IUserSink`, `ISettingsStore`, `IAuditLog`.

## Pull requests

1. Branch off `main`.
2. Keep changes focused; match the surrounding style (nullable enabled, file-scoped namespaces,
   records for data, XML doc comments on public types).
3. **Add or update tests.** `dotnet test` must pass.
4. Update docs/README when behaviour or configuration changes.
5. Don't commit secrets or the `data/` directory.

CI builds and tests every PR.

## Commit messages

Short imperative subject; explain the *why* in the body when it isn't obvious.
