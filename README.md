# PresalesGPT

An AI-assisted presales platform that turns a tender document into a traceable,
review-gated set of deliverables.

Upload an RFP. The system extracts structured requirements, drafts the
clarification questions worth asking before you bid, generates a grounded
proposal, validates it with deterministic code, and produces a customer deck and
a Statement of Work from the parts a human has approved.

> **Design position:** this tool does not replace presales judgement. Every AI
> step is followed by a human gate, and the final release decision is made by
> code, not by the model.

---

## Contents

- [Why this exists](#why-this-exists)
- [What it produces](#what-it-produces)
- [Architecture](#architecture)
- [Pipeline](#pipeline)
- [Getting started](#getting-started)
- [API reference](#api-reference)
- [Engineering the AI behaviour](#engineering-the-ai-behaviour)
- [Guardrails](#guardrails)
- [Validation results](#validation-results)
- [Tests](#tests)
- [Project structure](#project-structure)
- [Roadmap](#roadmap)
- [Notes and limitations](#notes-and-limitations)

---

## Why this exists

Responding to a tender is slow and error-prone. A presales engineer reads a
hundred-page document, pulls out the obligations, writes the response, and hopes
nothing was missed. The failure modes are well known:

1. **A requirement is missed.** Bids get disqualified for this.
2. **Something is promised that cannot be delivered.** A price, a date, a
   certification, a performance number invented under deadline pressure.
3. **The clarification window is wasted.** Questions are usually submitted once,
   to a deadline, and the answers go to every competing bidder. A weak question
   burns the only chance to resolve an ambiguity.

Generic AI writing tools make the second problem worse. They produce confident,
fluent text that reads well and commits to things nobody agreed to.

PresalesGPT is built around the opposite assumption: **the model is useful for
drafting, and untrustworthy for committing.** So it drafts, and humans commit.

---

## What it produces

| Stage | Who does it | Output |
|---|---|---|
| Document ingestion | System | Extracted text from PDF, DOCX or TXT |
| Requirement extraction | AI | Structured requirements with source references |
| Requirement review | **Human** | Approved requirement set |
| Clarification questions | AI | Questions worth submitting, graded by impact |
| Question review | **Human** | Approved questions, exported without internal notes |
| Proposal generation | AI | Seven grounded proposal sections |
| Section review | **Human** | Approved sections |
| Coverage validation | **Code** | Release readiness verdict |
| Customer deck | AI + code | A .pptx with mixed layouts and speaker notes |
| Statement of Work | AI | Nine contractual sections, drafted in legal register |
| Export | System | Markdown and PowerPoint, watermarked while in draft |

Gates differ by deliverable, deliberately:

- **Proposal** needs at least one approved requirement.
- **Deck** needs at least one approved proposal section.
- **Statement of Work** needs **every** proposal section approved. It is
  contractual, so un-reviewed text must not reach it.

---

## Architecture

```text
┌──────────────────────────────────────────────────────────────┐
│                    React + TypeScript                        │
│                                                              │
│  Opportunities │ Documents │ Requirements │ Clarifications    │
│  Proposal │ Coverage │ Deck │ Statement of Work               │
└──────────────────────────────┬───────────────────────────────┘
                               │ REST
┌──────────────────────────────▼───────────────────────────────┐
│                     ASP.NET Core Web API                     │
│                                                              │
│  Opportunities  Documents  Requirements  Clarifications       │
│  Proposals  Coverage  Sow  Deck                               │
└───┬────────────┬──────────────┬──────────────┬───────────────┘
    │            │              │              │
┌───▼──────┐ ┌───▼──────────┐ ┌─▼──────────┐ ┌─▼──────────────┐
│ SQLite   │ │TextExtraction│ │ Azure      │ │ CoverageService│
│(EF Core) │ │PdfPig /      │ │ OpenAI     │ │ (no AI)        │
│          │ │OpenXML       │ │ (REST)     │ │                │
│          │ └──────────────┘ └────────────┘ │PowerPointBuilder│
│          │                                 │ (no AI)        │
└──────────┘                                 └────────────────┘
```

**Two components contain no AI on purpose.** `CoverageService` decides whether a
proposal may be released; `PowerPointBuilder` renders the .pptx. Both are
deterministic — see [Guardrails](#guardrails).

### Stack

| Layer | Technology |
|---|---|
| Frontend | React 19, TypeScript, Vite, Axios |
| Backend | ASP.NET Core, Entity Framework Core |
| Database | SQLite (Postgres-ready) |
| Document parsing | PdfPig, DocumentFormat.OpenXml |
| Deck generation | OpenXML, hand-built slide layouts |
| AI | Azure OpenAI Chat Completions, JSON mode |
| Tests | xUnit, FluentAssertions |

---

## Pipeline

```text
  Upload tender document
      │
      ▼
  Text extraction  ──────────► page-marked plain text
      │
      ▼
  AI requirement extraction    strict JSON schema
      │                        source reference per requirement
      ▼
  ┌─────────────────┐
  │  HUMAN GATE 1   │  edit · approve · delete
  └────────┬────────┘
           │
           ├──────────────► AI clarification questions
           │                     │  graded Blocking / Important / Minor
           │                     ▼
           │               ┌─────────────────┐
           │               │  HUMAN GATE 2   │  approve what you will submit
           │               └────────┬────────┘
           │                        │
           │                        ▼
           │                  Export questions
           │                  (internal rationale stripped)
           │
           │  only approved requirements pass
           ▼
  AI proposal generation      seven sections, one call each
      │
      ▼
  ┌─────────────────┐
  │  HUMAN GATE 3   │  edit · regenerate · approve
  └────────┬────────┘
           │
           ├──────────────► AI deck  ──► .pptx with speaker notes
           │                (needs ≥ 1 approved section)
           │
           ├──────────────► AI Statement of Work
           │                (needs ALL sections approved)
           │                     │
           │                     ▼
           │               ┌─────────────────┐
           │               │  HUMAN GATE 4   │  + legal review warning
           │               └─────────────────┘
           ▼
  Deterministic coverage check
      │                       no AI involved
      ▼
  ┌─────────────────┐
  │  RELEASE GATE   │  blockers must be empty
  └────────┬────────┘
           │
           ▼
      Export
```

---

## Getting started

### Prerequisites

- .NET SDK 8 or later
- Node.js 18 or later
- An Azure OpenAI deployment

### Backend

```bash
cd backend/PresalesGPT.Api

dotnet restore
dotnet ef database update
dotnet run
```

Runs on `http://localhost:5217`. Swagger at `/swagger`.

### Configuration

Use user secrets so the key never reaches source control:

```bash
dotnet user-secrets init

dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<resource>.openai.azure.com"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<key>"
dotnet user-secrets set "AzureOpenAI:Deployment" "<deployment-name>"
dotnet user-secrets set "AzureOpenAI:ApiVersion" "2024-10-21"
```

`Deployment` is the name you gave the deployment, not the model name.

### Frontend

```bash
cd frontend

npm install
npm run dev
```

Runs on `http://localhost:5173`.

### Walkthrough

1. Create an opportunity
2. Upload a tender document (PDF, DOCX or TXT)
3. **Extract Requirements (AI)**
4. Review the table, then **Approve All** or approve selectively
5. **Prepare Questions (AI)** — review and approve what you would submit
6. **Generate Proposal (AI)** — one to three minutes
7. Review each section, then approve
8. **Run Coverage Check**
9. **Build Deck (AI)** and, once every section is approved,
   **Draft Statement of Work (AI)**
10. Export

---

## API reference

### Opportunities

```http
GET    /api/Opportunities
POST   /api/Opportunities
```

### Documents

```http
POST   /api/Documents/upload
GET    /api/Documents/by-opportunity/{opportunityId}
GET    /api/Documents/{id}/text
DELETE /api/Documents/{id}
```

### Requirements

```http
POST   /api/Requirements/extract/{documentId}
GET    /api/Requirements/by-opportunity/{opportunityId}
PUT    /api/Requirements/{id}
POST   /api/Requirements/{id}/approve
DELETE /api/Requirements/{id}
```

### Clarifications

```http
POST   /api/Clarifications/generate/{opportunityId}
GET    /api/Clarifications/by-opportunity/{opportunityId}
PUT    /api/Clarifications/{id}
POST   /api/Clarifications/{id}/approve
DELETE /api/Clarifications/{id}
GET    /api/Clarifications/export/{opportunityId}
```

### Proposals

```http
POST   /api/Proposals/generate/{opportunityId}
POST   /api/Proposals/regenerate-section/{sectionId}
GET    /api/Proposals/by-opportunity/{opportunityId}
PUT    /api/Proposals/{id}
POST   /api/Proposals/{id}/approve
GET    /api/Proposals/export/{opportunityId}
```

### Coverage

```http
GET    /api/Coverage/{opportunityId}
```

### Deck

```http
POST   /api/Deck/generate/{opportunityId}
GET    /api/Deck/by-opportunity/{opportunityId}
GET    /api/Deck/export/{opportunityId}
```

### Statement of Work

```http
POST   /api/Sow/generate/{opportunityId}
GET    /api/Sow/by-opportunity/{opportunityId}
PUT    /api/Sow/{id}
POST   /api/Sow/{id}/approve
GET    /api/Sow/export/{opportunityId}
```

---

## Engineering the AI behaviour

The first version of the extractor worked, in the sense that it returned JSON
and did not crash. It was also wrong in ways that would have caused real damage.
Five rounds of fixes are recorded in full in
[docs/PROMPT-ENGINEERING.md](docs/PROMPT-ENGINEERING.md). The summary:

| # | Problem | Fix | Result |
|---|---|---|---|
| 1 | Timeline rows and bid logistics extracted as requirements | Named the exclusions explicitly | 41 → 26 requirements |
| 2 | Mandatory flag inferred from importance | Bound the flag to document evidence | 35 → 11 mandatory |
| 3 | Internal triage metadata leaking into customer prose | Scoped the metadata explicitly | Leak eliminated |
| 4 | Wrong company name in the proposal | Prompt guard **and** a deterministic check | Blocker raised |
| 5 | Vendor names silently dropped | Told it the ban applies only to absent names | Names preserved |

Two of those five were caught only by reading the output as its reader would.
No structural check would have found them.

### Tested against two independent fixtures

| | BizTalk / Azure | AWS / Retail |
|---|---|---|
| Industry | Scientific instruments | Retail and e-commerce |
| Requirements in document | 26 | 31 |
| Requirements extracted | **26** | **31** |
| Mandatory in document | 11 | 13 |
| Mandatory flagged | **11** | **13** |
| Timeline rows extracted | 0 | 0 |
| Bid logistics extracted | 0 | 0 |
| Invented requirements | 0 | 0 |
| Category accuracy | 100% | 100% |

The second fixture matters more than the first. The prompt was tuned against
BizTalk; AWS is a different industry, a different domain and a larger document.
Precision and recall held at 100% on both, which is evidence the prompt
generalises rather than fits one document.

---

## Guardrails

### Fabrication bans

The proposal generator is explicitly prohibited from inventing:

```text
- Prices, rates, day counts or effort estimates
- Specific dates, durations or timelines
- Team sizes or named individuals
- Client references, case studies or past project names
- Certifications, partnership tiers or accreditations
- Performance guarantees or service level numbers
- Product names that the requirements do not imply
```

The Statement of Work adds a longer list: liability caps, warranty periods,
indemnities, penalties, notice periods, governing law and jurisdiction.

When the model lacks a value, it must emit a marked placeholder:

```text
[Commercial team to confirm]
[Delivery lead to confirm]
[Legal to confirm]
```

Placeholders are counted by the coverage checker and treated as release
blockers. An unresolved gap cannot quietly reach a customer.

### Validation is deterministic

The coverage checker is plain C#. Regex and set comparison. **No model call.**

Asking an LLM to grade its own output is circular: the same reasoning that
produced an error will usually endorse it. The checks that gate release are the
ones that cannot be talked out of a verdict.

Seven checks run on every report:

| Check | Catches |
|---|---|
| Uncovered requirements | An approved requirement no section addresses |
| Uncovered **mandatory** | The disqualification case |
| Unresolved placeholders | Gaps still needing human input |
| Unapproved sections | Review not completed |
| **Claimed-only** | Model listed a code as covered, but it appears nowhere in the text |
| Unknown codes | Model cited a requirement code that does not exist |
| **Customer name mismatch** | The proposal names a company absent from the source documents |

The *claimed-only* check exists because of a specific model behaviour: listing
`TR-05` in `coveredRequirements` while never addressing TR-05 in the prose.
Taking credit without doing the work.

The *customer name* check exists because of a real bug — see fix 4 above.

### Internal notes never leave the building

Two kinds of internal data are supplied to the model and must never reach a
customer:

- **Requirement metadata** — mandatory, priority, category. Internal triage.
- **Clarification rationale** — why each question matters, written bluntly
  about commercial and delivery consequences.

The first is handled in the prompt. The second is handled in code: the
clarification export endpoint simply never writes the rationale field.

### Human gates

```text
AI extracts requirements  →  human approves which ones count
AI drafts questions       →  human approves what to submit
AI writes sections        →  human approves each section
Code validates coverage   →  blockers must be empty before release
```

Export always succeeds, but an unapproved proposal is watermarked:

```text
> DRAFT - 4 section(s) still awaiting review. Not for customer release.
```

The Statement of Work carries a stronger warning, on screen and in the export:

```text
This draft has not been reviewed by Legal. It is not a contractual offer and
must not be issued to a customer without legal review.
```

---

## Validation results

Against the AWS fixture, 31 of 31 requirements approved:

```text
Coverage             100%   (31/31)
Uncovered mandatory  0
Claimed-only         0
Unknown codes        0
Placeholders         18
Sections pending     5
Verdict              Not ready for customer release
```

### Reasoning the models got right

Cases worth noting, because they are the kind of thing a careful presales
engineer does and a careless one does not:

**Caught an arithmetic contradiction in the tender.** The AWS fixture states a
mandatory exit date of 31 January 2028, and separately gives a timeline placing
decommissioning at Week 94 from an issue date of 7 September 2026 — roughly five
months past the deadline. The contradiction was accidental; it was introduced
when the fixture was written and never noticed. The model found it by computing
across two sections, and asked which of the two should govern rather than
guessing.

**Refused to assert an unprovable saving.** On the 25 per cent cost reduction
requirement, the proposal states that "the requirements alone do not establish
that the saving is achievable", and routes the figure to the commercial team.

**Kept retirement distinct from exclusion.** Where a requirement asks which
components should be retired, the proposal states across four sections that
retirement recommendations "will be presented for your approval rather than
treated as exclusions from the migration scope" — closing a common scope
dispute before it opens.

**Separated two retention obligations.** One requirement asks for 90-day
searchable message tracking; another for seven-year immutable audit logs. The
proposal and the SOW both note these as distinct rather than conflating them.

**Distinguished an excluded approach from excluded scope.** With no exclusions
stated in the proposal, the SOW wrote that not using manual console changes or
public internet connectivity are "excluded implementation approaches, not
exclusions from delivery scope."

**Declined to borrow an authority.** The SOW notes that a programme director's
authority to sign completion certificates "shall not be used to identify change
approval authorities without confirmation." Conflating two authorities is
exactly the ambiguity that becomes a contractual dispute.

---

## Tests

`CoverageService` — the component that decides whether a proposal is safe to
send — has 33 unit tests.

```bash
cd backend/PresalesGPT.Tests
dotnet test
```

| Area | Tests |
|---|---|
| Release verdict | 2 |
| Uncovered requirements | 3 |
| Coverage percentage | 4 |
| Claimed-only detection | 3 |
| Word boundaries | 2 |
| Unknown codes | 1 |
| Placeholders | 3 |
| Section approval | 1 |
| Customer identity | 7 |
| Code list parsing | 5 |
| Ordering | 2 |

The AI services are deliberately not unit tested. Their correctness depends on
model behaviour, which is not deterministic and not meaningful to assert in a
unit test. Those are measured against the fixture documents instead. Testing
both the same way would do neither well.

See [backend/PresalesGPT.Tests/README.md](backend/PresalesGPT.Tests/README.md)
for the tests worth reading.

---

## Project structure

```text
presales-gpt/
├── backend/
│   ├── PresalesGPT.Api/
│   │   ├── Controllers/
│   │   │   ├── OpportunitiesController.cs
│   │   │   ├── DocumentsController.cs
│   │   │   ├── RequirementsController.cs
│   │   │   ├── ClarificationsController.cs
│   │   │   ├── ProposalsController.cs
│   │   │   ├── CoverageController.cs
│   │   │   ├── DeckController.cs
│   │   │   └── SowController.cs
│   │   ├── Services/
│   │   │   ├── TextExtractionService.cs
│   │   │   ├── AiRequirementService.cs
│   │   │   ├── ClarificationService.cs
│   │   │   ├── ProposalService.cs
│   │   │   ├── SowService.cs
│   │   │   ├── DeckService.cs
│   │   │   ├── CoverageService.cs       # deterministic, no AI
│   │   │   └── PowerPointBuilder.cs     # deterministic, no AI
│   │   ├── Models/
│   │   ├── Data/
│   │   └── Program.cs
│   └── PresalesGPT.Tests/
│       ├── CoverageServiceTests.cs
│       └── TestData.cs
├── frontend/
│   └── src/
│       ├── components/
│       │   ├── AppShell.tsx
│       │   ├── OpportunityPanel.tsx
│       │   ├── DocumentPanel.tsx
│       │   ├── RequirementsTable.tsx
│       │   ├── ClarificationsPanel.tsx
│       │   ├── ProposalActions.tsx
│       │   ├── CoverageReportPanel.tsx
│       │   ├── ProposalSections.tsx
│       │   ├── DeckPanel.tsx
│       │   ├── SowPanel.tsx
│       │   ├── TextPreview.tsx
│       │   └── Banner.tsx
│       ├── api/
│       ├── types/
│       ├── usePresales.ts
│       ├── styles.ts
│       └── App.tsx
├── docs/
│   ├── ARCHITECTURE.md
│   └── PROMPT-ENGINEERING.md
└── README.md
```

---

## Roadmap

- **Bid/No-Bid assessment** ahead of requirement extraction. The tool currently
  assumes the decision to bid has been taken; in practice that decision comes
  first and is where most wasted presales effort is avoided.
- **DOCX export** with heading styles and a generated table of contents
- **Knowledge base and retrieval** so sections can be grounded in previously
  approved proposal content rather than requirements alone
- **Postgres and Blob Storage** to replace SQLite and local file storage
- **Microsoft Entra ID** authentication with reviewer and approver roles
- **Regression harness** that scores extraction against the fixture answer keys
  on every prompt change, so prompt edits stop being manual inspection
- **Scanned PDF support** via OCR

---

## Notes and limitations

**Scanned PDFs are not supported.** Text extraction reads embedded text. An
image-only PDF yields nothing and will need an OCR path.

**Long documents are truncated** at 30,000 characters for requirement
extraction and 24,000 for clarifications. Chunked extraction with
de-duplication is the correct fix.

**No authentication.** The API is open. The approval fields in the data model
are already shaped for role-based access, but it is not implemented.

**Local storage.** Uploaded files are written to disk and the database is
SQLite. Both are appropriate for local development and neither is appropriate
for production.

**Reasoning models behave differently.** The Azure OpenAI request uses
`max_completion_tokens` rather than `max_tokens`, and omits `temperature`,
because reasoning-family deployments reject the older parameters. Token limits
must also account for internal reasoning tokens, or the response comes back
empty.

**Output requires review.** The guardrails reduce fabrication; they do not
eliminate it. Nothing here should reach a customer without a human having read
it end to end.
