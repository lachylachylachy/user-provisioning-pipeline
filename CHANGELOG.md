# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/), and this project adheres to semantic versioning.

## [1.0.0] - 2026-06-24

First business-ready release. Entra-Flow grows from a CSV validator into a full provisioning tool.

### Added
- **Microsoft Entra provisioning** via Microsoft Graph (`GraphUserSink`), safe by default
  (dry-run + disabled until explicitly enabled).
- **Web UI + REST API** (ASP.NET Core + Blazor): upload & review CSVs, a Settings page to enter
  your own Entra connection with a **Test connection** check, run **History**, and `/api` endpoints.
- **Configurable schema** — add fields and rules (required, email format, allowed values, unique
  field) without code, from config or the Settings page.
- **Audit log** of every run (JSON-lines) and **PII masking** in logs.
- **Secret-at-rest** encryption (ASP.NET Core Data Protection) for the Entra client secret.
- **Admin authentication** (cookie) and optional API key for programmatic access.
- **Packaging**: Dockerfile + docker-compose, self-contained release binaries, version stamping,
  and a tag-triggered release workflow.
- Documentation: Entra app-registration guide, deployment, operations, SECURITY, CONTRIBUTING.

### Changed
- `IUserSink` / the pipeline are now async to support network-bound sinks.
- CLI configuration precedence fixed so environment variables and command-line args override
  `appsettings.json` as documented.

### Notes
- This release builds on the original CSV validate-and-split pipeline, which remains the default
  (Sink = CSV).
