# Security

Entra-Flow handles identity data and can create users in your Entra tenant. Please treat it as a
privileged tool.

## Reporting a vulnerability

Please report security issues privately — open a [GitHub security advisory](../../security/advisories/new)
or email the maintainers — rather than filing a public issue. We aim to acknowledge reports within a
few business days.

## Deploying securely

- **Change the default admin password** (`Admin__Password`). The app warns at startup if you don't.
- **Always run behind TLS** (reverse proxy). Never expose it over plain HTTP on the internet.
- **Protect the `data/` directory**, especially `keys/` (decrypts the stored client secret) and
  `settings.json`. Restrict file permissions and back it up securely.
- **Least privilege:** grant the app registration only `User.ReadWrite.All`.
- **Use dry-run** until you have validated your data; enabling live writes requires two explicit
  toggles plus a Graph sink.
- **Rotate the client secret** on the schedule your policy requires.

## How secrets are handled

- The Entra **client secret** is encrypted at rest with ASP.NET Core Data Protection and is never
  returned by the UI or the `GET /api/settings` endpoint.
- **Emails/UPNs are masked** in log output.
- Admin credentials and the API key are read from configuration / environment and are never written
  to `settings.json`.

## Scope

This is a provisioning utility, not a security product. You are responsible for the environment it
runs in (network exposure, OS hardening, backups, and access control).
