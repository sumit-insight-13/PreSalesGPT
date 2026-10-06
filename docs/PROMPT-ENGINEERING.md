# Prompt engineering log

A record of what the prompts did wrong, what changed, and what the numbers did
afterwards. Kept because the prompt changes are where most of the actual
engineering went.

Five fixes are recorded. Two of them were caught only by reading the output as
its reader would — no structural check would have found them.

---

## Test fixtures

Two tender documents with known answer keys.

### Fixture A — BizTalk migration (Azure, scientific instruments)

| Prefix | Category | Count |
|---|---|---|
| BR | Business | 5 |
| TR | Technical | 8 |
| SR | Security | 5 |
| CR | Compliance | 4 |
| CM | Commercial | 4 |
| | **Total** | **26** |

11 requirements carry the literal phrase *"This is a mandatory requirement"*.

### Fixture B — Data centre exit (AWS, retail)

| Prefix | Category | Count |
|---|---|---|
| BR | Business | 6 |
| TR | Technical | 11 |
| SR | Security | 6 |
| CR | Compliance | 4 |
| CM | Commercial | 4 |
| | **Total** | **31** |

13 requirements carry the mandatory phrase.

**Deliberate traps in both:** a project timeline table, an evaluation criteria
table, and a paragraph of bid submission instructions. None of these are
requirements. A correct extractor returns zero of them.

Fixture B was written after the prompt had been tuned on Fixture A. Different
industry, different domain, larger document. It exists to test whether the
prompt generalises or merely fits the first document.

---

## Extraction: v1

### Prompt

```text
You are a presales requirement analyst. Read the customer document and extract
requirements. Return ONLY valid JSON, no markdown, no explanation.
Format: {"requirements":[{...}]}
Only extract requirements that are actually stated in the document.
Do not invent requirements.
```

### Result on Fixture A

```text
41 requirements
35 flagged mandatory
```

### Failure 1 — over-extraction

Fifteen entries were not requirements.

The timeline table became six requirements:

> REQ-030 — Complete the current-state assessment and migration wave plan with a
> target completion of Week 6.

The submission instructions became six more:

> REQ-040 — Include named curricula vitae for all proposed key personnel.

These are milestones and bid logistics. Passing them to a proposal generator
produces a document that explains, to the customer, how to submit a bid to
itself.

The instruction "only extract requirements actually stated in the document" did
not help, because these *were* stated in the document. The prompt never defined
what a requirement is.

### Failure 2 — mandatory inflation

Document marks 11. Model flagged 35.

REQ-030, a Week 6 milestone, was flagged `isMandatory: true`. The document says
nothing about it being mandatory.

The model was inferring mandatory status from perceived importance.

This matters more than it looks. In a bid, *mandatory* means *disqualification
risk*. A flag applied to everything conveys nothing, and the team loses the
ability to prioritise the eleven items that can actually lose the deal.

---

## Extraction: v2

### Change 1 — define what a requirement is not

```text
WHAT COUNTS AS A REQUIREMENT
A requirement is a statement that obliges the supplier to deliver, build,
provide or comply with something in the proposed solution or the delivery of
that solution.

DO NOT EXTRACT the following, even though they appear in the document:
- Project timeline tables, milestone schedules or target completion weeks
- Evaluation criteria and their weightings
- Bid submission logistics, for example submission deadlines, file formats,
  sealed envelopes, number of references to attach, or CVs to include
- Background, introduction and context narrative
- Contact details, document references and revision history

If a statement only describes WHEN the bid is due or HOW the bid must be
packaged, it is not a requirement. Leave it out.
```

Naming the exact failure categories worked where a general instruction had not.

### Change 2 — bind the mandatory flag to evidence

```text
MANDATORY FLAG
Set isMandatory to true ONLY when the document explicitly marks that specific
requirement as mandatory, for example by stating "this is a mandatory
requirement", "mandatory", "must comply", "shall", or by listing it under a
heading of mandatory requirements.

If the document uses softer wording such as "should", "may", "is desirable",
"preferred", or gives no indication at all, set isMandatory to false.

Do not infer mandatory status from how important the requirement sounds.
When in doubt, set it to false.
```

Two parts did the work: an explicit list of qualifying phrases, and an explicit
ban on inferring from importance.

### Change 3 — reuse the document's identifiers

```text
CODES
If the document already uses requirement identifiers such as BR-01, TR-04 or
SR-02, reuse that exact identifier as the code. Only generate REQ-001 style
codes when the document has no identifiers of its own.
```

Not an accuracy fix, a reviewability fix. Cross-checking `TR-04` against the
tender takes seconds. Cross-checking `REQ-009` requires reading both documents
in parallel.

### Results

| Metric | v1 (A) | v2 (A) | Key (A) | v2 (B) | Key (B) |
|---|---|---|---|---|---|
| Requirements | 41 | **26** | 26 | **31** | 31 |
| Mandatory | 35 | **11** | 11 | **13** | 13 |
| Timeline rows | 6 | **0** | 0 | **0** | 0 |
| Bid logistics | 6 | **0** | 0 | **0** | 0 |
| Invented | 0 | **0** | 0 | **0** | 0 |
| Category accuracy | 100% | **100%** | — | **100%** | — |

Precision and recall both 100% on both fixtures.

Fixture B also carried four requirements marked *"This requirement is
desirable"*. All four were assigned `Low` priority; nothing else was. The
priority rule held as well as the mandatory rule.

---

## A deliberate judgement call

Several requirements use the word *must* but carry no explicit mandatory marker.
v2 flags them `false`.

In strict tender convention, *must* is binding, so a case exists for flagging
them `true`.

The current behaviour is intentional. The flag answers a narrow question: *did
the customer explicitly mark this as mandatory?* A reviewer can promote any
requirement during the approval step. Over-flagging would restore exactly the
signal loss that v1 suffered from.

Recorded as a decision rather than an oversight.

---

## Proposal generation: fabrication bans

Proposals commit the supplier to things. A fabricated price or invented client
reference is a commercial and legal problem, not a quality problem. The bans are
therefore explicit rather than implied.

```text
GROUNDING RULES
Write only what the supplied requirements support. You must not invent any of
the following:
- Prices, rates, day counts or effort estimates
- Specific dates, durations or timelines
- Team sizes or named individuals
- Client references, case studies or past project names
- Certifications, partnership tiers or accreditations
- Performance guarantees or service level numbers
- Product names that the requirements do not imply

If the section would normally contain such information and you do not have it,
write a clearly marked placeholder such as [Commercial team to confirm] or
[Delivery lead to confirm] instead of inventing a value.
```

The placeholder mechanism matters as much as the ban. Without a defined escape
hatch, a model asked not to invent a price will often produce vaguer prose that
still implies one. Given an explicit placeholder format, it produces a gap the
coverage checker can count and block on.

A style rule was added alongside:

```text
Avoid marketing superlatives such as world-class, cutting-edge, best-in-class
or industry leading. Be specific and factual rather than promotional.
```

---

## Fix 3 — the metadata leak

Found by reading generated output, not by any automated check.

The *Understanding of Requirements* section contained:

> "Although classified as non-mandatory, this requirement sets out your
> expectations for payment terms."

The requirement metadata — mandatory, priority, category — is passed to the
model so it can weight emphasis. It is internal triage. Telling a customer their
requirement was internally graded as non-mandatory is a commercial own goal.

### Fix

```text
CONFIDENTIALITY OF INTERNAL METADATA
The requirement metadata supplied to you, namely the mandatory flag, the
priority and the category, is our internal working classification. It must
never appear in the proposal text in any form.

Never write phrases such as "this requirement is classified as non-mandatory",
"although marked as non-mandatory", "this is a high priority requirement" or
"categorised as technical".

Use the metadata only to decide how much emphasis to give a topic. The customer
must never see how we have internally triaged their requirements.

You may refer to something as mandatory only when the customer's own
requirement wording states it, not because our metadata says so.
```

The user prompt was also changed to label the fields as internal at the point of
supply:

```text
[BR-02] (internal metadata - category Business, priority High, mandatory yes)
The proposed solution must eliminate dependency on...
```

Leak eliminated. The same section now reads:

> "Commercial acceptance (CM-03) — You require payments to be tied to acceptance
> of milestones."

### What this taught

The bug was invisible to every automated check in the system. Coverage was
100%, no requirement was missed, no value was fabricated, the JSON was valid.
The output was simply wrong for its audience.

Some failure classes only surface when the output is read the way its reader
will read it.

---

## Fix 4 — the wrong customer

An opportunity created under one company name had a different company's tender
uploaded against it. The generated Executive Summary opened:

> "You are seeking to migrate **NewGen's** workloads from two data centres..."

The tender was issued by Meridian Retail Group. NewGen appears nowhere in it.

This was not a hallucination. The opportunity record supplies `CustomerName` to
the prompt, and the model used it. The data was wrong; the model was obedient.

Coverage was 100%. Nothing was fabricated. No other check caught it.

### Fix, part one — the prompt

```text
CUSTOMER IDENTITY
The customer name is supplied to you as CUSTOMER. Treat it as a label only. If
the requirements themselves refer to a different organisation name, the
requirements are authoritative and the supplied label is likely to be wrong.

When the two disagree, do not name either organisation. Write "you" and "your
organisation" throughout instead. Never merge the two names and never invent a
third.

Never introduce any company, brand, product, or organisation name that does not
appear in the requirements or in the CUSTOMER field.
```

The user prompt now labels the field as unreliable at the point of supply:

```text
CUSTOMER (label only, may be unreliable): NewGen
```

After the change, the same section opens:

> "**Your organisation** needs to migrate an estate of 412 virtual machines..."

Neither name used. Where the source is untrustworthy, naming nobody beats
naming the wrong party.

### Fix, part two — a deterministic check

A prompt guard is not a guarantee. `CoverageService` gained a customer identity
check that runs on every report:

```csharp
check.FoundInSourceDocuments =
    hasSource && NameAppearsIn(sourceDocumentText, name);

check.IsMismatch = !check.FoundInSourceDocuments
                   && check.AppearsInProposal;
```

The matcher strips company suffixes — Group, Limited, Ltd, Holdings — so
"Meridian Retail Group Limited" still matches "Meridian Retail". A test stops
that relaxation going too far: "Northern Retail Group" must not match "Meridian
Retail Group" merely because both contain "Retail".

Two blockers result:

```text
The customer name 'NewGen' appears in 1 proposal section(s) but does not
appear anywhere in the source documents.

The customer name 'NewGen' on this opportunity does not appear in the source
documents. Confirm the correct document has been uploaded.
```

The second fires even when the prompt guard worked — because the underlying
data problem is still there.

### What this taught

When a failure is expensive, fix it twice: once where it is generated, once
where it is verified. The prompt reduces the chance; the check catches what gets
through.

---

## Fix 5 — vendor names silently dropped

A Statement of Work was internally inconsistent about the same estate:

| Section | Wording |
|---|---|
| Scope of Services | "11 in-scope databases" |
| Scope of Services | "26 in-scope relational database instances" |
| Deliverables | "26-instance database estate" |
| Acceptance Criteria | "26 SQL Server instances" |
| Roles | "26 relational database instances identified for consolidation" |

Four names for one thing, in a single contract. The requirements say
*11 Oracle databases* and *26 Microsoft SQL Server instances*.

In a contract, "11 databases" and "11 Oracle databases" are different things.
Stripping the vendor weakens specificity and invites a later dispute about which
eleven.

The cause was the grounding rule. It bans inventing product names the
requirements do not imply. The model generalised that into avoiding product
names altogether.

### Fix

```text
PRODUCT AND VENDOR NAMES
Where the source names a specific product, vendor or system, carry that name
through exactly and use the same wording in every section.
"11 Oracle databases" must not become "11 databases", "11 in-scope databases"
or "11 relational databases".
"26 Microsoft SQL Server instances" must not become "26 database instances" or
"26 relational database instances".
The rule against inventing product names applies only to names absent from the
source. It is not a reason to remove names that are present.
```

The last line is the fix. The other lines merely describe the symptom.

Applied to `SowService` and `DeckService`. After the change, all nine SOW
sections use the same wording, and PagerDuty and Microsoft Teams — which had
become "existing incident-management and collaboration channels" — also
returned.

### What this taught

A negative instruction has a blast radius. "Do not invent X" was read as "avoid
X". Where a ban has a legitimate exception, state the exception in the same
breath as the ban.

---

## Deck layouts: a quota, not a suggestion

The deck generator was given four layouts — Bullets, Stats, TwoColumn, Callout —
with guidance on when each suited the content.

It produced **13 slides, all Bullets**.

The guidance described the layouts as available. It never made using them
required, so the model took the default path every time.

### Fix

```text
You MUST meet this quota across the deck:
- At least 2 slides with layout "Stats"
- At least 2 slides with layout "TwoColumn"
- At least 1 slide with layout "Callout"
- The rest may be "Bullets"

Before you write the slides, scan the proposal and decide which content fits
which layout. Do not write the deck first and then label it.

If you find yourself writing a fourth consecutive Bullets slide, stop and ask
whether the content would land better as Stats or TwoColumn.
```

The prompt was also given concrete examples drawn from the domain, rather than
abstract descriptions:

```text
"Stats" - this proposal contains many: counts of servers, databases and stores,
percentage targets, downtime limits, recovery objectives, retention periods,
concurrency figures.

"TwoColumn" - this proposal contains several: what is in scope against what
remains to be confirmed, supplier responsibilities against customer
responsibilities, a risk against its mitigation, operational retention against
audit retention.
```

### Result

| | Before | After |
|---|---|---|
| Slides | 13 | 15 |
| Bullets | 13 | 5 |
| Stats | 0 | 3 |
| TwoColumn | 0 | 6 |
| Callout | 0 | 1 |

The choices were sound rather than merely varied. Oracle and SQL Server were
placed in opposing columns — two estates with different constraints. Operational
retention and audit retention were paired the same way, preserving a
distinction the proposal had been careful to make.

### Guarding against a half-filled layout

A model that declares `Stats` and then supplies no stats would render an empty
slide. Rather than trust it, the parser falls back:

```csharp
return slide.Layout switch
{
    "Stats" when slide.Stats.Count == 0 => "Bullets",
    "TwoColumn" when slide.Columns.Count < 2 => "Bullets",
    "Callout" when string.IsNullOrWhiteSpace(slide.Callout) => "Bullets",
    _ => slide.Layout
};
```

### What this taught

An option the model is told it *may* use is an option it will not use. If a
behaviour matters, make it a requirement with a number attached, and give
domain-specific examples rather than abstract criteria.

---

## Clarification questions: what not to ask

Clarification questions are submitted once, to a deadline, and the answers are
circulated to every competing bidder. The cost of a weak question is higher than
it looks: it burns the only chance to resolve an ambiguity, and it can reveal a
gap in your own understanding to competitors.

The prompt therefore spends more words on what not to ask than on what to ask:

```text
DO NOT ASK
- Anything the document already answers. Read carefully before assuming
  silence.
- Questions whose answer you could reasonably assume and state as an assumption
  instead.
- Questions that expose uncertainty about your own capability, for example
  asking whether a common capability is really required.
- Broad questions such as "can you provide more detail on X".
- More than one distinct matter in a single question.
- Questions about the submission process, formats or deadlines.
```

And on phrasing:

```text
Where a requirement is ambiguous, state both readings and ask which applies.
That is more useful than asking what was meant.
```

An honesty rule was attached to the impact grading:

```text
Be honest. Marking everything blocking is as useless as marking nothing.
```

### Result

18 questions from the AWS fixture: 3 Blocking, 15 Important. None restated
something the document answers. None asked about submission logistics.

The first question found a contradiction that had been written into the fixture
by accident:

> The Week 94 decommissioning milestone in Section 8 falls after the mandatory
> 31 January 2028 exit date in BR-02, even if delivery starts at RFP issue.
> Please confirm whether bidders must replace the stated milestone schedule with
> an accelerated programme meeting BR-02, or whether the exit date will be
> revised.

Computed across two sections of the document, and phrased as a choice between
two readings rather than an open question.

---

## Reasoning-model parameters

Initial calls failed:

```json
{
  "error": {
    "message": "Unsupported parameter: 'max_tokens' is not supported with this model. Use 'max_completion_tokens' instead.",
    "code": "unsupported_parameter"
  }
}
```

Reasoning-family deployments require `max_completion_tokens` and reject a custom
`temperature`.

```csharp
var requestBody = new Dictionary<string, object>
{
    ["messages"] = messages,
    ["max_completion_tokens"] = 8000,
    ["response_format"] = new { type = "json_object" }
};
```

The token budget also needs headroom. These models spend tokens on internal
reasoning before emitting output, so a limit sized for the visible answer can
return an empty response. A specific error was added rather than letting it fail
silently.

Latency differs by task. Proposal sections take 10 to 35 seconds each.
Clarification questions take around 60 — the model is given the full document
plus every requirement, and finding contradictions across sections takes more
reasoning than restating them.

---

## What generalises

**Name the failure, not the goal.** "Do not invent requirements" achieved
nothing. Listing timeline tables and submission logistics by name fixed it.

**Bind judgement flags to evidence.** "Set this only when the document says X"
beats any amount of describing what the flag means.

**Give refusals somewhere to go.** A ban without an escape hatch produces
evasive prose. A ban with a placeholder format produces a countable gap.

**State the exception with the ban.** "Do not invent product names" was read as
"avoid product names". Negative instructions have a blast radius.

**An option is not an instruction.** Layouts the model *may* use went unused
until a quota was attached.

**Scope the context you supply.** Anything handed to the model may appear in its
output unless told otherwise. Internal metadata needs an explicit boundary.

**Read the output as its audience.** Two of the five fixes here passed every
structural check and were caught by reading a paragraph as a customer would.

**Fix expensive failures twice.** The wrong customer name got a prompt guard and
a deterministic check. The prompt reduces the chance; the check catches what
gets through.

**Validate with code, not with the model.** Every gate in this system —
coverage, placeholders, claimed-only, unknown codes, customer identity — is
deterministic. A model asked to grade its own work tends to approve it.
