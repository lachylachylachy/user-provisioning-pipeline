# Setting up the Entra app registration

Entra-Flow provisions users with the Microsoft Graph **client-credentials (app-only)** flow.
That means you create an **app registration** in your tenant and give Entra-Flow its Tenant ID,
Client ID, and a Client Secret. This is a one-time, ~5-minute task for an IT administrator.

## 1. Create the app registration

1. Sign in to the [Microsoft Entra admin center](https://entra.microsoft.com) as an admin.
2. **Identity → Applications → App registrations → New registration**.
3. Name it e.g. `Entra-Flow`. Leave "Supported account types" as **single tenant**. Register.
4. On the **Overview** page, copy the **Directory (tenant) ID** and **Application (client) ID**.

## 2. Grant the Graph permission

Entra-Flow needs to create users, which requires an **application** permission.

1. **API permissions → Add a permission → Microsoft Graph → Application permissions**.
2. Add **`User.ReadWrite.All`** (create/update users).
   - The connection test only reads directory info; provisioning needs write.
3. Click **Grant admin consent for &lt;your tenant&gt;** and confirm. The status should show a
   green check.

> Principle of least privilege: grant only `User.ReadWrite.All`. Do not add broader roles unless
> a later feature requires them.

## 3. Create a client secret

1. **Certificates & secrets → Client secrets → New client secret**.
2. Give it a description and expiry (e.g. 6–12 months). **Add**.
3. **Copy the secret Value immediately** — it is shown only once.

## 4. Enter the details in Entra-Flow

1. Open Entra-Flow → **Settings**.
2. Paste the **Tenant ID**, **Client ID**, and **Client Secret**.
3. Click **Test connection** — you should see *Connected to '&lt;your org&gt;'*.
4. Save.

## 5. Provision — safely

- Keep **Sink = CSV** (or **Both**) and **dry-run on** while you validate your data. Dry-run shows
  exactly which users *would* be created without touching the tenant.
- When you're confident: set **Sink = Entra (Graph)** (or **Both**), turn **dry-run off**, tick
  **Enable live provisioning**, and run again. New users get a strong temporary password and are
  required to change it at first sign-in.

## Notes on the user payload

By default these CSV columns map to Entra user properties:

| CSV field | Entra property |
|-----------|----------------|
| `Email`   | `userPrincipalName` + `mailNickname` (local part) |
| `Name`    | `displayName` |
| `Department` | `department` |
| `Role`    | `jobTitle` |

The `userPrincipalName` must be a domain you own in the tenant. Adjust the mapping and temporary
password policy in **Settings** / the `Entra:FieldMapping` config.

## Rotating the secret

Client secrets expire. When you create a new one, paste it into **Settings** and save — leaving the
field blank keeps the existing secret. Remove expired secrets from the app registration.
