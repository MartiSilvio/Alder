---
title: Writing guidelines
description: Internal standard for writing and editing Alder documentation.
---

# Writing guidelines

Alder documentation is part of the product. It should read like the work of engineers who understand the system, respect the reader's intelligence, and care about precision. This page defines the standard for every document under `docs/`: what the writing is trying to achieve, how pages are structured, what level of detail belongs in them, and what kinds of drift must be corrected during review.

## Philosophy

Alder docs optimize for five things: truth, clarity, elegance, usefulness, and trust.

Truth comes first. The docs must describe the system as it is, not as it was intended to be, not as it may become, and not as a simplified story that is easier to tell. If the system has constraints, edge cases, or awkward tradeoffs, document them plainly.

Clarity is next. A reader should be able to understand what Alder does, how it behaves, and where its boundaries lie without reverse-engineering the prose. Clarity does not mean flattening every idea. It means presenting the right idea in the right terms.

Elegance matters because technical writing has a compounding effect. Strong prose makes difficult concepts easier to retain, improves confidence in the system, and reduces the need for corrective edits later. The goal is not to sound literary. The goal is to sound exact, composed, and deliberate.

Usefulness is the practical test. A page should help a reader evaluate, integrate, debug, or extend Alder. If a paragraph does not improve understanding or decision-making, it is probably ornamental.

Trust is the cumulative result. Good docs feel reliable. They do not overclaim, hedge unnecessarily, or hide complexity behind generic language. Readers should come away with the sense that the system is understood and that the documentation is a dependable guide to it.

Documentation should also reflect the real stature of the system being described. Understatement can mislead just as surely as hype. If a subsystem is broad, technically substantial, or unusually capable, the prose should acknowledge that scale through precision, depth, and confidence. The goal is proportional representation, not marketing.

Weak:

`Alder includes a Dynamic LINQ capability.`

Better:

`Alder provides a Dynamic LINQ system for runtime query composition across in-memory collections, query providers, and async streams.`

The stronger sentence makes a larger claim because the system supports a larger surface. That is not exaggeration. It is accurate hierarchy.

## Audience

Write for advanced external engineers.

That includes:

- experienced developers evaluating Alder
- engineers integrating Alder into products
- advanced users working with non-trivial features
- external contributors onboarding into the project

Assume intelligence. Assume technical fluency. Do not assume familiarity with Alder internals, repository layout, or implementation history.

Do not write down to the reader. Alder docs are not beginner tutorials unless the page is explicitly a tutorial. Even then, the writing should remain adult, direct, and technically serious.

## Documentation types (Diátaxis)

Alder uses Diátaxis as a practical boundary, not as a labeling exercise. Each page should know what kind of help it is providing.

**Explanation**

Explanation pages answer how and why. They establish architecture, design intent, tradeoffs, invariants, and mental models. They should help a strong engineer understand the system's shape and the reasoning behind it.

Explanation pages must not collapse into API inventories. They can survey a broad surface, but they should organize that surface around ideas: execution boundaries, binding contracts, result shapes, provider translation, configuration policy. If exhaustive operator or option coverage matters, split that material into reference.

**Reference**

Reference pages answer what exactly happens. They document contracts, configuration surfaces, guarantees, exact behavior, support matrices, and limits. Reference should be factual, dense, and easy to consult.

**How-to**

How-to pages solve one concrete task. They should get the reader from a known starting point to a known outcome with practical steps and realistic examples. They are not architecture essays and not API inventories.

**Tutorial**

Tutorials are guided onboarding. They introduce a sequence of concepts through deliberate practice. They may be slower and more explicit than other page types, but they still should not lapse into patronizing instruction.

Do not mix page types casually. An explanation page should not turn into a procedural checklist halfway through. A how-to page should not expand into a theory chapter. If a topic needs multiple modes of explanation, split it into multiple pages.

## Tone and voice

The tone should be confident, precise, calm, technically serious, and non-marketing.

Write as if the system is understood. State behavior directly. Prefer firm verbs: `is`, `uses`, `resolves`, `binds`, `executes`, `falls back`, `requires`, `rejects`, `guarantees`.

Serious engineering deserves calm authority. A page can sound proud of real work when the confidence is earned by accurate detail, clear boundaries, and good examples. Avoid both hype and embarrassed understatement. The desired tone is competence with technical self-respect.

Avoid hype, apology, and timidity.

Do not write:

- "powerful"
- "seamless"
- "super easy"
- "simply"
- "just"
- "currently" unless the temporal qualifier matters
- "may" when the behavior is actually deterministic

Do not soften clear behavior with nervous prose. "Alder typically tries to..." is weaker than "Alder tries typed dispatch first, then falls back to reflection."

Do not exaggerate either. The docs should never read like product marketing or internal advocacy.

Preserve product hierarchy. Alder is the product and runtime platform. A feature page can describe one subsystem in depth, but it should not redefine the whole product through that feature.

Bad:

`Alder turns strings into LINQ.`

Better:

`Dynamic LINQ is Alder's runtime query-composition system.`

The better sentence gives the feature stature while keeping Alder larger than any one subsystem.

## Writing style

Aim for elegant technical prose.

That means:

- strong conceptual nouns
- direct verbs
- controlled sentence rhythm
- concise paragraphs with real density
- memorable phrasing where it sharpens understanding

Prefer concepts such as:

- semantic boundary
- execution path
- type surface
- resolution phase
- binding contract
- runtime model
- cache invalidation
- fallback path
- public surface
- deployment constraint

Avoid mechanically simplified prose that sounds correct but lands weakly.

Weak:

`Binding assigns semantic meaning to parsed syntax.`

Better:

`Alder's binder is the semantic boundary between syntax and execution.`

Weak:

`This helps users catch errors.`

Better:

`Early binding surfaces semantic errors before execution begins.`

Weak:

`The cache is checked when context changes.`

Better:

`Cache reuse is gated by changes to the context's type surface.`

Weak:

`Alder uses a compiled backend when available.`

Better:

`Synchronous evaluation dispatches to the compiled backend only when a compiler is configured.`

Do not treat sentence variety as decoration. A page made entirely of short declarative sentences sounds generated. A page made entirely of dense, compound sentences becomes hard to scan. Vary cadence deliberately.

Concise is not the same as thin. A short sentence can carry weight. A short paragraph that says almost nothing is merely compressed filler.

## Prefer affirmative prose over defensive contrast

Documentation should usually state what a feature does, supports, enables, or fits. Lead with the capability itself. Let scope and confidence come from accurate explanation and examples.

Use negative phrasing when it is carrying real information:

- limits by design
- compatibility boundaries
- safety warnings
- unsupported scenarios
- operational constraints

Do not write around imagined criticism. Avoid rebuttal-style phrasing such as:

- `is not just`
- `does not merely`
- `rather than`
- `not only ... but also`
- `instead of`

These forms often make correct material sound argumentative, apologetic, or unsure.

Bad:

`Dynamic LINQ is not a narrow helper around Where.`

Better:

`Dynamic LINQ supports filtering, ordering, projection, grouping, joins, and reusable query composition.`

Bad:

`IAsyncEnumerable does not use provider translation.`

Better:

`IAsyncEnumerable executes in process over compiled delegates.`

Bad:

`Compiled mode is not secondary.`

Better:

`Compiled mode is Alder's optimized synchronous execution path.`

In practice:

- prefer positive claims grounded in truth
- avoid rebutting imagined criticism
- avoid pick-me product language
- avoid sounding apologetic
- let capability emerge through explanation and examples

## Technical depth

Simplify wording, not ideas.

Preserve nuance. Preserve constraints. Preserve tradeoffs. Preserve difficult truths.

If a feature is partial, say so. If an execution path has a narrower surface, say so. If a guarantee depends on a version boundary, a runtime type shape, or a provider translation limit, say so. Strong documentation does not become "accessible" by omitting the parts that matter most to an experienced reader.

The right move is usually to make the idea sharper, not smaller.

When in doubt:

- remove redundancy before removing substance
- replace generic explanation with exact language
- keep the caveat if it affects behavior, correctness, integration, or trust
- preserve the scale of the feature when that scale is part of the truth

## Headings and structure

Headings should earn their place. A heading is useful when it helps a reader navigate an argument, a contract, or a task. It is not useful when it merely repeats a template.

Discourage generic headings such as:

- `Goal`
- `Purpose`
- `Context`
- `Overview` when it says nothing specific
- `Verify the result`

Prefer headings that teach something:

- `Resolved versus dynamic binding`
- `Instance resolution`
- `Context versioning`
- `Case sensitivity`
- `Limits by design`

Strong pages often use authored structure rather than template structure. Organize around the real concepts a reader must carry away.

Good:

- `Execution surfaces`
- `Provider boundaries`
- `Prepared plans`
- `Typed result shapes`

Weak:

- `What it is`
- `Core operations`
- `More examples`
- `Miscellaneous`

The structure should reveal the product's architecture, not merely sort paragraphs into bins.

Every page should open with a strong introductory paragraph after the title. That paragraph should orient the reader quickly: what the page covers, what problem space it belongs to, and what kind of help it provides. Avoid limp openings such as "This page describes..." unless the sentence carries real specificity.

Use lists where lists clarify structure. Do not default to bullets for every idea. If a short paragraph does the job better, write the paragraph.

## Use of internal implementation details

Internal implementation details belong in the docs only when they materially improve understanding.

Usually acceptable:

- public APIs such as `AlderEngine`, `AlderOptions`, and `AlderConfig`
- public-facing concepts that shape integration behavior
- internal terms that have become part of the documented mental model

Usually avoid:

- source file names
- repository layout
- private helper types
- emitter or generator internals
- class names that do not matter outside the implementation
- maintainer narration such as "implemented in" or "see file"

The question is not whether a detail is true. The question is whether the reader needs it to understand behavior, integration, or design. Most of the time, file paths and internal helper names answer a maintainer's question, not a user's.

## Examples and code snippets

Examples should be real, minimal, and plausible.

Prefer examples that are:

- copy-pasteable when practical
- aligned with the actual API surface
- small enough to scan quickly
- realistic enough to resemble production use

Avoid toy examples unless the concept genuinely requires the smallest possible shape. `Foo`, `Bar`, and contrived one-line snippets are often a sign that the prose has lost contact with actual integration work.

Code samples should demonstrate the thing the page is trying to teach and little else. Do not pad them with ceremony. Do not make them so abstract that they stop feeling like code someone would write.

Use editorial judgment. Examples should prove capability, not flood the page. One strong example per concept is usually better than several near-duplicates. Explanation pages need fewer, more carefully chosen examples; reference pages can be denser because their job is exact lookup. Too many snippets reduce impact by making the reader decide which ones matter.

Good examples do more than compile. They reveal product shape: a runtime boundary, a type contract, a provider limit, a configuration decision, a reusable pattern. If an example does not teach a distinct idea, merge it with a stronger example or remove it.

If an example includes a caveat, the caveat should be accurate and local. Do not make the reader infer hidden constraints from a suspiciously polished snippet.

## What to avoid

Do not allow the docs to drift into any of the following:

- AI filler language
- generic summaries that restate the heading
- repetitive sentence patterns
- speculative claims about future behavior
- unnecessary verbosity
- patronizing explanations
- maintainer-only notes inside user-facing docs
- benchmarks used as documentation substance
- markdown templates that produce lifeless pages
- headings that organize text without improving comprehension
- operator catalogs disguised as explanation pages
- feature pages that accidentally shrink Alder into the feature being discussed

Also avoid two common failure modes that look sophisticated but are not:

First, vague polish. This is prose that sounds smooth but stops making concrete claims.

Second, reference dumps. This is technically correct material with no editorial judgment, no hierarchy, and no help for the reader about what matters.

Neither is acceptable.

## Editing existing docs

When revising docs, preserve correct content and improve surgically.

Do not rewrite a page merely to impose a new phrasing preference. Avoid churn. Prefer incremental refinement that strengthens accuracy, structure, or prose without discarding working material.

When editing:

- preserve behaviorally correct content
- improve weak passages rather than rephrasing everything
- keep terminology consistent with nearby pages
- maintain voice across the docs set
- remove drift toward AI-generated rhythm or generic markdown templates
- tighten introductions and headings when they are flat
- delete internal-only clutter from user-facing pages unless it is doing real explanatory work

A good edit often removes less than expected. It clarifies a boundary, strengthens a key paragraph, sharpens a heading, and leaves the rest alone.

## Quality checklist

Before considering a page done, ask:

- Is it true?
- Is it clear?
- Is it useful?
- Is it elegant?
- Is it written for the right audience?
- Does it respect the page type?
- Does it preserve the important constraints and tradeoffs?
- Does it represent the feature's real scale without hype or understatement?
- Does the structure feel authored around ideas rather than assembled from a template?
- Would a strong engineer trust it?
- Does it avoid sounding generated?

If the answer to any of these is no, the page is not finished.

## Related pages

- [Architecture](/explanation/architecture/)
- [Binding system](/explanation/binding-system/)
- [Typed dispatch and AOT](/explanation/typed-dispatch/)
- [Configuration](/reference/configuration/)
- [Execution model](/reference/execution-model/)
