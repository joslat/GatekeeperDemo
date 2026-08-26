# Implementation review against the proposal

## Implemented

| Proposal intent | Implementation evidence |
|---|---|
| Import PartnerDesk demo and `.Evals` first | Pinned sample, Evals, tests, MIT license, and provenance under `src`, `tests`, and `eng/upstream` |
| Avalonia two-column control room | Fixed topology and inspector on the left; Story/Security/Debug/.Evals timeline on the right |
| Show both information directions | Explicit request/response labels for user, model, MCP, database, and email lanes |
| Reactive green/red flow | Event-driven route brushes with safe, risky, blocked/withheld, and neutral semantics plus text badges |
| Evil MCP control | Persistent evil-mode switch, red/untrusted MCP state, and hostile-result events |
| Gatekeeper global and per-gate control | Master plus database, recipient, result-admission, and dependency-valid containment controls |
| With/without experiment | First-class A/B action and side-by-side actual-effect summary using the identical request |
| Turn-by-turn and real time | Live semantic event stream plus pure Previous/Next replay after completion |
| Debug and exception visibility | Full Debug tab; curated Story is the default; failures are recorded with exception detail |
| Drive `.Evals` from UI | Sample-count control, streamed arm progress, and original evaluation text report tab |
| Honest evidence | Separate proposed intent, enforced findings, and executed fake effects; canonical oracle retained |

## Improvements made during review

The original proposal was implementation-heavy. The finished experience applies the review's “story first,
debug on demand” correction:

1. Four numbered demos plus an A/B comparison beat are visible in the primary controls and documented in the
   runbook. Blue marks an exact demo preset; purple marks the A/B action rather than a selected mode.
2. Gates sit physically inline before their protected destinations; request and response lanes are labeled rather
   than implied by a generic connector.
3. Raw narration and exceptions moved behind the Debug tab, while curated AI Events remain the default surface.
4. A/B comparison is a primary action and a dedicated visual band, not an afterthought.
5. Color is reinforced with badges, icons, wording, and persistent node status for accessibility and projector use.
6. The UI never describes hidden chain-of-thought; it shows observable model requests, actions, findings, and
   outcomes.
7. Custom gate combinations cannot inherit a canonical phase oracle, preventing an attractive but false verdict.

## Second-pass application audit

The working desktop application was launched and exercised through the clean, unprotected attack, and protected
paths. The audit produced the following concrete corrections:

- Added explicit **On/Off** text to the Evil MCP switch and **ON · ENFORCING / OFF · BYPASSED** to Gatekeeper.
  The evil-face indicator now fades to red when armed and fades back to a muted state when disarmed.
- Added the missing editable user-request field. Blank requests now explain the problem and disable Run, A/B, and
  Evals as appropriate.
- Made demo selection unambiguous. A manual toggle change clears every blue preset highlight and labels the setup
  **Custom configuration**; changing demos also clears stale events and effect status.
- Aligned demo, action, cancel, and Evals controls to a common height and separated the evaluation harness from
  the presenter-story row. Every interactive control and topology component now has hover help, and clickable
  controls use the hand cursor.
- Increased idle-lane, arrow, direction-label, and gate-label contrast. Gates are prefixed **GATE** and show their
  effective On/Off state inline before the destination they protect.
- Split transient route animation from persistent database/email outcome color so an unsafe executed effect stays
  red after the arrow animation ends. A model answer is neutral evidence—not incorrectly presented as a safety
  verdict.
- Added observable “tool working” events before database/email execution, corrected aggregate completion counts
  across containment retries, and made coordinator shutdown wait for an active MCP run instead of racing disposal.
- Made audience pacing optional, added Follow latest, corrected replay to begin at the final live event, and added
  an explicit replay-start action. Empty Story, Security, and Debug states now explain what will appear.
- Added an Avalonia view-model regression project alongside the imported and Core suites. The final solution test
  pass covers 89 tests across all three layers.

## Deliberate boundaries

- The control room defaults to the deterministic trajectory for repeatable, credential-free presentation. Its
  explicitly labeled Live mode mirrors the reference code's `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY`, and
  `AZURE_OPENAI_DEPLOYMENT` inputs and offers only the demo's three fixed deployment names.
- “Turn by turn” is pure presentation replay after a completed real run. Pausing the protected execution inside an
  observer would let visualization timing alter the system being demonstrated, so it is intentionally avoided.
- Artifact export/import APIs are implemented in Core; the first desktop surface replays the most recent run in
  memory. File-picker UI can be added without changing the run model.

## Residual follow-ups

- Add export/import buttons around the existing integrity-checked artifact API.
- Add automated pixel snapshots for multiple DPI/theme combinations if the UI becomes productized beyond the
  Windows-first demo target.
