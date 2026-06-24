# Operations

## Logs

Entra-Flow uses standard ASP.NET Core / .NET logging (console by default). Adjust levels in
`appsettings.json` under `Logging`. Provisioning steps log at `Information`; failures at
`Error`/`Warning`.

**PII:** email addresses / UPNs are **masked** in logs (`ja****@company.com`). Full values appear
only in the authenticated UI and in downloaded CSVs.

## Audit trail

Every run — UI, API, dry-run or live — is appended to `data/audit.jsonl` (one JSON record per line)
and shown on the **History** page. Each entry records the timestamp, signed-in user, source file,
valid/rejected counts, sink mode, dry-run flag, and per-record outcomes.

```bash
tail -n 5 data/audit.jsonl
```

Treat this file as compliance evidence: include it in backups and restrict access.

## Health & smoke test

```bash
# unauthenticated home should redirect to /login
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/        # 302

# API requires a key (or admin cookie)
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/api/runs            # 401
curl -s -H "X-Api-Key: $KEY" http://localhost:8080/api/runs                        # 200 []
```

## Troubleshooting

| Symptom | Likely cause / fix |
|---------|--------------------|
| **Test connection fails: authentication failed** | Wrong tenant/client/secret, or the secret expired. Recreate the secret and re-enter it. |
| **Test connection OK, but provisioning fails with 403** | The app registration is missing `User.ReadWrite.All` **with admin consent**. See [entra-setup.md](entra-setup.md). |
| **`userPrincipalName` rejected by Graph** | The UPN domain must be a verified domain in your tenant. |
| **Saved secret stops working after redeploy** | The Data Protection `keys/` directory was not persisted. Mount it on a volume and re-enter the secret. |
| **Upload rejected: "exceeds the limit"** | File has more than `Storage__MaxRowsPerRun` rows (default 100k). Split the file or raise the limit. |
| **Everything is dry-run** | Live writes require **both** "Enable live provisioning" on **and** dry-run off, with Sink = Graph or Both. |

## Backups

Back up the entire `data/` directory: settings (encrypted secret), `keys/` (needed to decrypt it),
and `audit.jsonl` (history). Test a restore periodically.
