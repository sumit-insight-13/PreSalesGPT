# Architecture

How the system is put together, and why the structural choices were made.

---

## Principle

One idea shapes everything below:

> The model is useful for drafting. It is not trustworthy for committing.

So every AI step produces a draft, every draft passes a human gate, and the
final release decision is made by deterministic code.

---

## Component view

```text
┌────────────────────────────────────────────────────────────────┐
│                    React + TypeScript (Vite)                   │
│                                                                │
│  AppShell: sidebar · progress rail · header                    │
│  Opportunities  Documents  Requirements  Clarifications        │
│  Proposal  Coverage  Deck  Statement of Work                   │
└──────────────────────────────┬─────────────────────────────────┘
                               │  REST / JSON
┌──────────────────────────────▼─────────────────────────────────┐
│                      ASP.NET Core Web API                      │
│                                                                │
│  ┌────────────┐ ┌────────────┐ ┌──────────────┐ ┌───────────┐  │
│  │Opportunit- │ │ Documents  │ │ Requirements │ │Clarificat-│  │
│  │ies         │ │            │ │              │ │ions       │  │
│  └────────────┘ └─────┬──────┘ └──────┬───────┘ └─────┬─────┘  │
│  ┌────────────┐ ┌─────▼──────┐ ┌──────▼───────┐ ┌─────▼─────┐  │
│  │ Proposals  │ │TextExtract-│ │AiRequirement │ │Clarificat-│  │
│  │            │ │ionService  │ │Service       │ │ionService │  │
│  └─────┬──────┘ └────────────┘ └──────────────┘ └───────────┘  │
│  ┌─────▼──────┐ ┌────────────┐ ┌──────────────┐ ┌───────────┐  │
│  │Proposal    │ │  Coverage  │ │     Deck     │ │    Sow    │  │
│  │Service     │ │ Controller │ │  Controller  │ │Controller │  │
│  └────────────┘ └─────┬──────┘ └──────┬───────┘ └─────┬─────┘  │
│                 ┌─────▼──────┐ ┌──────▼───────┐ ┌─────▼─────┐  │
│                 │ Coverage   │ │DeckService + │ │SowService │  │
│                 │ Service    │ │PowerPoint    │ │           │  │
│                 │ (no AI)    │ │Builder(no AI)│ │           │  │
│                 └────────────┘ └──────────────┘ └───────────┘  │
└──────────┬───────────────────────────┬─────────────────────────┘
           │                           │
   ┌───────▼────────┐          ┌───────▼────────┐
   │  Azure OpenAI  │          │ SQLite (EF)    │
   │  REST API      │          │                │
   └────────────────┘          │ Opportunities  │
                               │ Documents      │
   ┌────────────────┐          │ Requirements   │
   │  Local disk    │          │ Clarifications │
   │  Uploads/      │          │ ProposalSection│
   └────────────────┘          │ SowSections    │
                               │ DeckSlides     │
                               └────────────────┘
```

Five services call the model. Two do not, and that is the point:
`CoverageService` decides release readiness, `PowerPointBuilder` renders the
.pptx. Both are deterministic.

---

## Data model

```text
Opportunity
    │
    ├──< Document
    │       │
    │       └──< Requirement          (documentId, opportunityId)
    │
    ├──< Clarification
    ├──< ProposalSection
    ├──< SowSection
    └──< DeckSlide
```

### Requirement

```text
Code              BR-02, TR-04 - reused from the source document
Description       the requirement text
Category          Business | Technical | Security | Compliance | Commercial
Priority          High | Medium | Low
IsMandatory       evidence-bound, see prompt engineering log
SourceReference   "Page 3"
IsApproved        the human gate
```

`SourceReference` exists so a reviewer can verify a requirement against the
original document without re-reading it end to end. Traceability is a
requirement of the tool, not a nicety.

### Clarification

```text
Reference            Q01, Q02
Question             what is asked, customer-facing
Rationale            why the answer matters - INTERNAL, never exported
Category             Ambiguity | Gap | Conflict | Feasibility |
                     Commercial | Compliance
Impact               Blocking | Important | Minor
RelatedRequirements  codes the question bears on
IsApproved           the human gate
```

The rationale is written to be blunt about commercial consequences, which makes
it useful internally and unacceptable externally. The export endpoint simply
never writes the field — the separation is enforced in code, not left to a
prompt.

### ProposalSection and SowSection

```text
SectionOrder          1-7 for proposals, 1-9 for SOW
SectionName
Content               generated prose
CoveredRequirements   codes the model claims to address
Assumptions           proposal only
IsApproved            the human gate
IsEdited              whether a human changed the text
```

`CoveredRequirements` is a **claim**, not a fact. The coverage checker verifies
it against the section text. See [claimed-only](#claimed-only) below.

### DeckSlide

```text
Layout          Bullets | Stats | TwoColumn | Callout
Title
Bullets         newline-separated, used by the Bullets layout
LayoutData      JSON payload for the other three layouts
SpeakerNotes
SourceSection   which part of the proposal the slide draws from
```

`LayoutData` is a single JSON column rather than child tables. Stats and columns
are never queried independently, and the set of layouts is expected to keep
changing — a schema change per layout would be friction with no return.

---

## Request flows

### Document upload and extraction

```text
POST /api/Documents/upload
  │
  ├─ validate extension (.pdf .docx .txt) and size (20 MB)
  ├─ verify opportunity exists
  ├─ write file to Uploads/ under a GUID name
  ├─ insert Document row with status Processing
  │
  ├─ TextExtractionService.Extract
  │     .pdf   → PdfPig, page markers inserted
  │     .docx  → OpenXml body InnerText
  │     .txt   → direct read
  │
  ├─ success → status Extracted, text + page count stored
  └─ failure → status Failed, error message stored
```

The original filename is kept for display but never used on disk. Stored names
are GUIDs, which removes path traversal and collision concerns.

### Requirement extraction

```text
POST /api/Requirements/extract/{documentId}
  │
  ├─ load document, reject if no extracted text
  ├─ truncate text to 30,000 characters
  ├─ call Azure OpenAI with JSON response format
  ├─ strip markdown fences if present
  ├─ parse into typed objects, tolerating missing fields
  ├─ delete previous requirements for this document
  └─ insert new set with IsApproved = false
```

Re-extraction replaces rather than appends. Running it twice produces one set,
not two.

Parsing never trusts the shape of the response. Every field is read through a
helper that returns a default if the property is missing or the wrong type.

### Clarification questions

```text
POST /api/Clarifications/generate/{opportunityId}
  │
  ├─ load ALL extracted requirements, not only approved ones
  ├─ load full source document text (24,000 character cap)
  ├─ one model call
  ├─ normalise impact values against a known set
  ├─ assign Q01-style references where the model omitted one
  └─ replace any previous set
```

This is the one place that deliberately ignores the approval gate. A requirement
a reviewer rejected as unclear is exactly the kind of thing worth asking the
customer about.

Results are sorted Blocking → Important → Minor, so the questions that decide
whether a bid is possible appear first.

### Proposal generation

```text
POST /api/Proposals/generate/{opportunityId}
  │
  ├─ load APPROVED requirements only
  ├─ reject with 400 if none are approved
  ├─ delete existing sections
  │
  ├─ for each of 7 section definitions:
  │     build prompt (opportunity + approved requirements + brief)
  │     call Azure OpenAI
  │     on success → collect section
  │     on failure → record in failures[], continue
  │
  └─ insert all successful sections
```

One model call per section rather than one call for the whole document. Three
reasons: each section gets a focused brief, a single failure does not lose the
rest, and individual sections can be regenerated without touching the others.

Failures are collected and returned rather than aborting. A partial proposal
with a named failure is more useful than no proposal.

### Statement of Work

```text
POST /api/Sow/generate/{opportunityId}
  │
  ├─ load proposal sections
  ├─ reject with 400 if ANY section is unapproved
  ├─ load approved requirements as supporting context
  │
  └─ for each of 9 SOW section definitions:
        prompt carries the approved proposal as the only source of scope
```

The gate is stricter here than anywhere else in the system, and deliberately so.
A proposal is persuasive; a Statement of Work is contractual. Drafting one from
un-reviewed text would put unreviewed AI output into a document that can be
enforced.

The SOW prompt also differs in register: "the Supplier shall" rather than "we
will", parties as "the Supplier" and "the Customer" rather than named
organisations, and a longer prohibition list covering liability, warranties,
indemnities and jurisdiction.

### Deck generation and export

```text
POST /api/Deck/generate/{opportunityId}
  │
  ├─ load APPROVED proposal sections
  ├─ one model call, returns slides with a declared layout each
  ├─ fall back to Bullets where a layout was declared but not populated
  └─ store, with non-bullet content serialised into LayoutData

GET /api/Deck/export/{opportunityId}
  │
  ├─ deserialise LayoutData per slide
  ├─ insert a section divider whenever SourceSection changes
  └─ PowerPointBuilder renders the .pptx
```

The section dividers are inserted by the builder, not requested from the model.
Where the deck should break is a structural decision derived from the data, in
the same way the coverage verdict is.

---

## Coverage validation

The component that decides whether a proposal can be released. **It contains no
AI.**

### Why deterministic

Asking a model to check its own output is circular — the reasoning that produced
an error will usually endorse it. A second model adds cost and latency and a
second opinion that is also unverifiable.

Every gate here is regex and set comparison. Instant, free, reproducible, and
immune to persuasion.

### Checks

| Check | Method | Catches |
|---|---|---|
| Uncovered | code appears in no section, claimed or in text | a missed requirement |
| Uncovered mandatory | above, filtered to `IsMandatory` | disqualification risk |
| Placeholders | regex `\[[^\[\]]{3,120}\]` over content | unresolved human input |
| Unapproved sections | `IsApproved` false | review incomplete |
| Claimed-only | claimed in metadata, absent from prose | credit without work |
| Unknown codes | claimed code not in approved set | hallucinated reference |
| Customer identity | name absent from source documents | the wrong company named |

<a name="claimed-only"></a>

### The claimed-only check

A specific model behaviour: listing `TR-05` in `coveredRequirements` while never
mentioning TR-05 anywhere in the section text.

Detected by comparing two sources:

```csharp
// what the model claimed
var claimed = SplitCodes(section.CoveredRequirements);

// what actually appears in the prose, word-boundary matched
var mentioned = ContainsWholeWord(section.Content, requirement.Code);

coverage.IsClaimedOnly = claimedIn.Count > 0 && mentionedIn.Count == 0;
```

Word-boundary matching prevents `TR-1` matching inside `TR-10`:

```csharp
var pattern = $@"(?<![A-Za-z0-9]){Regex.Escape(code)}(?![A-Za-z0-9])";
```

Flagged for human verification rather than treated as failure — a section can
legitimately address a requirement without naming its code.

### The customer identity check

Added after a real failure: an opportunity created under one company name had a
different company's tender uploaded against it, and the proposal opened with the
wrong name.

The matcher tolerates company suffixes so "Meridian Retail Group Limited"
matches "Meridian Retail", but a test stops that going too far — "Northern
Retail Group" must not match "Meridian Retail Group" merely because both
contain "Retail".

Two distinct blockers, because the two situations need different responses:

- The name appears in the proposal but not the source → the document names the
  wrong party.
- The name appears in neither → the wrong document may have been uploaded.

### Verdict

```csharp
report.IsReadyForRelease = report.Blockers.Count == 0;
```

No weighting, no threshold, no score. Any blocker blocks. A release gate that
can be argued with is not a gate.

---

## Deck rendering

`PowerPointBuilder` writes OpenXML directly. No AI, no template file.

Shapes are positioned explicitly rather than inheriting placeholders from the
slide layout. That keeps the master and layout minimal, at the cost of doing the
arithmetic in code. All measurements are English Metric Units: 914400 EMU to the
inch, on a 16:9 canvas of 12192000 × 6858000.

Four content layouts plus a title slide and a section divider:

| Layout | Renders |
|---|---|
| Bullets | Accent-bulleted list |
| Stats | Two to four tinted cards, large figure over a short label |
| TwoColumn | Two tinted panels, accent heading over a bulleted list |
| Callout | Tinted band with a left accent bar and one large sentence |

Speaker notes are attached as a `NotesSlidePart` per slide, so the deck opens in
presenter view with the detail behind each slide already in place.

---

## Choices worth explaining

### REST over the Azure OpenAI SDK

The SDK's surface changes between versions and method names move. The REST
contract is stable. One `HttpClient` call, explicit request body, no dependency
that breaks on upgrade.

It also made the reasoning-model parameter difference a one-line change rather
than an SDK version hunt.

### SQLite

No Docker, no service to install, no connection string to configure. EF Core
means the move to Postgres is a provider swap and a fresh migration.

Appropriate for local development. Not appropriate for production.

### Section-at-a-time generation

Covered above: focused briefs, isolated failures, independent regeneration. The
cost is seven round trips instead of one, which for a document produced a few
times per deal is not a meaningful constraint.

The deck is the exception — a single call, because slide-to-slide coherence
matters more there than isolation does.

### Different gates for different deliverables

```text
Proposal   ≥ 1 approved requirement
Deck       ≥ 1 approved proposal section
SOW        ALL proposal sections approved
```

Graded by consequence. A deck built from a partial proposal is an incomplete
deck. A Statement of Work built from unreviewed text is a contractual exposure.

### Approval as a hard gate

Generation returns `400` when the gate is not met, rather than falling back to
unapproved content.

A silent fallback would make the approval step decorative. Making it a hard
failure is the only way the gate means anything.

### Two-tier storage

Extracted text lives in the database; original files live on disk. Text is
queried constantly. Originals are needed rarely, for audit.

The `by-opportunity` endpoint projects `TextLength` rather than the text itself,
so listing documents never pulls megabytes of prose.

---

## Frontend structure

```text
App.tsx           layout only, ~180 lines
usePresales.ts    all state and orchestration
components/       presentation only, no HTTP calls
api/              every request in one place
types/            shared interfaces
styles.ts         shared inline styles
index.css         what inline styles cannot express
```

No component makes an API call. Everything arrives through props and callbacks,
which makes each panel testable in isolation and keeps the request surface in
one file.

`styles.ts` and `index.css` split along a technical line rather than a stylistic
one: inline styles cannot express hover, focus or animation, so those live in
CSS. Everything else is inline, which means re-theming the application is a
single file change.

The progress rail in `AppShell` is derived from the data rather than tracked as
state. A stage is complete when it has produced something, so the rail reflects
reality rather than intent — the same principle as the coverage verdict.

---

## Known constraints

**Truncation.** Requirement extraction caps at 30,000 characters; clarifications
at 24,000. Long tenders lose their tail. Chunked extraction with de-duplication
across chunk boundaries is the correct fix.

**No OCR.** Image-only PDFs extract nothing. The document is marked `Failed`
with a readable reason rather than silently producing an empty requirement set.

**Local file storage.** Fine on one machine, wrong for anything shared. Blob
Storage with managed identity is the production shape.

**No authentication.** The API is open. Entra ID with reviewer and approver
roles is on the roadmap, and the approval fields in the data model are already
shaped for it.

**No regression harness.** Prompt changes are currently validated by running the
two fixtures and reading the output. Automating that scoring is the
highest-value remaining piece of work — without it, every prompt edit risks a
silent regression that only manual inspection would catch.
