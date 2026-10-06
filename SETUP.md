# Setup

Step-by-step local setup. For the short version, see
[Getting started](README.md#getting-started).

---

## 1. Prerequisites

| Requirement | Check with |
|---|---|
| .NET SDK 8+ | `dotnet --version` |
| Node.js 18+ | `node -v` |
| Azure OpenAI deployment | Azure Portal |

Install the EF Core CLI if you do not have it:

```bash
dotnet tool install --global dotnet-ef
```

---

## 2. Get your Azure OpenAI details

In the Azure Portal, open your Azure OpenAI resource.

**Keys and Endpoint** gives you:

```text
Endpoint   https://<resource-name>.openai.azure.com
API Key    Key 1
```

**Deployments** (in Azure AI Foundry) gives you the deployment name. This is the
name you chose when deploying, not the model name.

---

## 3. Backend

```bash
cd backend/PresalesGPT.Api
dotnet restore
```

Store the credentials as user secrets so they never reach git:

```bash
dotnet user-secrets init

dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<resource>.openai.azure.com"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<key>"
dotnet user-secrets set "AzureOpenAI:Deployment" "<deployment-name>"
dotnet user-secrets set "AzureOpenAI:ApiVersion" "2024-10-21"
```

Create the database:

```bash
dotnet ef database update
```

Run:

```bash
dotnet run
```

Expected output:

```text
Now listening on: http://localhost:5217
```

Verify at `http://localhost:5217/swagger`. Eight controller groups should be
listed: Opportunities, Documents, Requirements, Clarifications, Proposals,
Coverage, Deck, Sow.

---

## 4. Frontend

In a second terminal:

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`.

If your backend is on a different port, update the constant at the top of
`src/api/client.ts`:

```ts
export const API_BASE_URL = "http://localhost:5217/api";
```

---

## 5. Tests

```bash
cd backend/PresalesGPT.Tests
dotnet test
```

33 tests covering `CoverageService`. See
[the test README](backend/PresalesGPT.Tests/README.md).

---

## 6. First run

1. Create an opportunity
2. Select it — the card border turns blue
3. Upload a text-based PDF, DOCX or TXT
4. Confirm the status reads `Extracted`
5. **Extract Requirements (AI)** — 10 to 60 seconds
6. Review the table, then **Approve All**
7. **Prepare Questions (AI)** — around 60 seconds
8. Review and approve the questions you would actually submit
9. **Generate Proposal (AI)** — one to three minutes for seven sections
10. Review and approve each section
11. **Run Coverage Check**
12. **Build Deck (AI)**, then **Download PowerPoint**
13. Once every proposal section is approved,
    **Draft Statement of Work (AI)**

---

## Troubleshooting

### `Unsupported parameter: 'max_tokens'`

Your deployment is a reasoning-family model. These require
`max_completion_tokens` and reject a custom `temperature`. Both are already
handled in the services — this error means an older copy of those files is in
place.

### Empty model response

Increase `max_completion_tokens`. Reasoning models consume tokens internally
before producing output, so a limit that looks generous can still leave nothing
for the response.

### `no such table: Clarifications` (or any other table)

A migration has not been applied:

```bash
dotnet ef migrations add AddMissingTable
dotnet ef database update
```

If `dotnet ef migrations add` reports no changes detected, the corresponding
`DbSet` is missing from `AppDbContext`.

### `401` from Azure OpenAI

Wrong API key, or the key belongs to a different resource than the endpoint.

### `404` from Azure OpenAI

Wrong deployment name. Check Azure AI Foundry — it is the deployment name, not
the model name.

### `429` from Azure OpenAI

Rate limit. Wait, or raise the quota on the deployment.

### CORS errors in the browser console

The backend must register CORS before mapping controllers:

```csharp
app.UseCors("AllowReact");
app.MapControllers();
```

### `EBUSY: resource busy or locked` on Windows

The project is inside a synced folder such as OneDrive, and the sync client is
locking files that Vite is trying to watch. Move the project outside the synced
folder:

```powershell
C:\dev\presales-gpt
```

Close all terminals and your editor first, or the move will fail with "in use".

### PowerPoint offers to repair the file

The generated OpenXML is malformed. Check the server log for an exception in
`PowerPointBuilder` — the export returns 500 with the message rather than
shipping a broken file, so a successful download with a repair prompt suggests a
structural problem in the slide XML rather than a crash.

### Extracted text is empty or garbled

The PDF is image-only. Text extraction reads embedded text; scanned documents
need an OCR pass, which is not implemented.

### `Koi approved requirement nahi mili`

Proposal generation requires at least one approved requirement. Approve some
first — this gate is intentional.

### Statement of Work is blocked

All proposal sections must be approved, not just some. This gate is stricter
than the others on purpose: a SOW is contractual.

---

## Resetting local data

```bash
cd backend/PresalesGPT.Api

rm presalesgpt.db
rm -rf Uploads

dotnet ef database update
```

On Windows PowerShell:

```powershell
Remove-Item presalesgpt.db
Remove-Item Uploads -Recurse
dotnet ef database update
```
