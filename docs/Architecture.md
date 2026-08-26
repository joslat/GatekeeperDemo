# Architecture and trust model

## Runtime path

```text
User
  ↕ request / answer
PartnerDesk agent ↔ deterministic or live model
  ↕ proposed tool call captured before gates
Gatekeeper composition
  ├─ MCP result admission + containment → real PartnerIntel MCP child process
  ├─ partner scope gate                 → synthetic register tool
  └─ recipient-domain contract          → local fake email outbox
```

`PhaseOutcome` remains authoritative. It combines independent evidence from model proposals, Gatekeeper findings,
and tool-effect ledgers. The GUI event stream is a non-authoritative projection: observer exceptions are isolated
and cannot change Gatekeeper or tool behavior.

## Ordered event pipeline

The imported demo emits presentation-neutral actor IDs and semantic event types. `ControlRoomEventStore` adds a
run ID, strict sequence, monotonic elapsed time, and UTC time. The Avalonia view model projects those events onto
the fixed topology and three different audiences:

- **Story** — curated semantic events in run order.
- **Security** — untrusted, risky, blocked, withheld, and failed events only.
- **Debug** — every semantic event plus the imported demo's original narration and exceptions.

Color has consistent meaning: green is safe/executed-as-intended, red is untrusted or an unsafe effect, amber is
blocked/withheld. Text badges and icons carry the same state so color is never the only cue.

## Gate dependencies

The master switch controls whether any Gatekeeper middleware is installed. Database scope, email recipient, and
MCP result admission can be selected independently. Containment requires result admission because the result
finding is the evidence that justifies containing the source; the UI and runtime both validate that dependency.

Canonical scenes retain the upstream phase oracle. A custom combination is intentionally labeled custom and is
reported from its raw evidence; it does not borrow a canonical pass/fail claim.

## Replay and provenance

A completed run creates a schema-versioned `RunArtifact` containing bounded question/answer text, evidence totals,
and ordered semantic presentation events. Raw Debug narration (which may contain hostile supplier text or an
exception stack) is deliberately excluded from persistence. SHA-256 integrity is verified on import. `RunReplay`
only moves an index over that immutable event list; it cannot start an agent, MCP process, database tool, or email
tool.

The original sample, Evals project, tests, and license are imported from the pinned upstream commit documented in
`eng/upstream/UPSTREAM.md`. Local changes are limited to repository paths and reusable observation/configuration
seams needed by the desktop control room.

## MAF safety audit

The completed repository receives a MAF Doctor grade **B**, with zero anti-pattern errors, zero prompt-lint
findings, and zero fan-out starvation risks. Its cost scanner reports heuristic `RunAsync` matches in wrappers,
MCP server loops, and tests; the actual `ChatClientAgent` configuration sets `MaxOutputTokens = 16000`. The remaining
observability warning reflects the sample's explicit `AgentTrace` plus control-room event pipeline rather than an
OpenTelemetry exporter, which is a deliberate offline-demo boundary.

## Concurrency and cancellation

Real runs are serialized by `PartnerDeskRunCoordinator`. A run owns its MCP session, containment state, event
store, and fake outbox. Cancellation propagates to the agent and MCP calls. The UI disables competing actions and
keeps already-recorded effects visible after cancellation.
