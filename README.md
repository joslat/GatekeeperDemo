# Gatekeeper PartnerDesk Control Room

An Avalonia desktop demonstration of an indirect prompt-injection attack crossing a real MCP boundary, the
resulting attempted misuse of database and email tools, and the protections applied by AgentEval Gatekeeper.

The application is designed for a live audience: the left column keeps the fixed system topology visible while
request/response routes light up; the right column shows a curated AI Events story, a security-only view, raw
debug narration, and the imported `.Evals` report.

## Run it

Requirements: Windows, .NET SDK 10, and a desktop session. The default path is deterministic and offline; it does
not require model credentials.

```powershell
dotnet restore GatekeeperDemo.slnx
dotnet run --project src\GatekeeperDemo.App\GatekeeperDemo.App.csproj
```

Use the guided strip from left to right: choose one of the four numbered demos, optionally customize Evil MCP and
Gatekeeper, review the request, then press **Run selected demo**. The purple **Compare demos 2 ↔ 4 · A/B** action
runs the core comparison. **Run batch .Evals** drives the imported `AgentEval.PartnerDeskDemo.Evals` project for
1–25 samples per demo.
Blue identifies the exact verified demo currently configured; the purple A/B control is an experiment action,
not a fifth selected mode. Hover over any button, toggle, gate, or topology component for a short explanation.

The default user request is kept identical across the numbered demos. You can edit it before a run. The evil
MCP switch and Gatekeeper master/individual gates create a custom run; containment automatically enables its
required result-admission evidence source.

## What is real and what is simulated

- Real: the PartnerIntel server is a child process speaking MCP over stdio.
- Real: the imported PartnerDesk agent, AgentEval Gatekeeper middleware, gates, evidence, and containment store.
- Real: the model's proposed tool calls are captured before Gatekeeper and compared with what executed.
- Deterministic: the default offline model trajectory is scripted. It proves gate behavior, not that every model
  will follow the hostile addendum.
- Simulated: database reads use a synthetic in-memory register; email writes only to a run-local fake outbox.
- Not shown: private model chain-of-thought. “Agent working” means an observable request is in flight, not that
  hidden reasoning was captured.

No SMTP, production database, or outbound network effect is used by the offline demo. Bounded previews are used
for display and replay artifacts; email bodies are not retained by the effect ledger.

## Projects

- `src/AgentEval.PartnerDeskDemo` — pinned upstream PartnerDesk sample plus presentation-neutral event and
  individual-gate extension points.
- `src/AgentEval.PartnerDeskDemo.Evals` — pinned upstream evaluation project.
- `src/GatekeeperDemo.Core` — serialized run coordinator, ordered event store, evidence summary, A/B result,
  integrity-checked artifact, and pure replay.
- `src/GatekeeperDemo.App` — Avalonia control room.
- `tests/AgentEval.PartnerDeskDemo.Tests` — upstream behavioral suite, adapted only for this repository layout.
- `tests/GatekeeperDemo.Core.Tests` — GUI-driven runtime, custom-gate, artifact, replay, and Evals integration.
- `tests/GatekeeperDemo.App.Tests` — control-room state, validation, selection, and audience-label regressions.

See [architecture](docs/Architecture.md), [presenter runbook](docs/Presenter-Runbook.md), and the
[implementation review](docs/Implementation-Review.md).

## Verify

```powershell
dotnet build GatekeeperDemo.slnx
dotnet test GatekeeperDemo.slnx --no-build
```

The imported source revision and local import boundary are recorded in
[`eng/upstream/UPSTREAM.md`](eng/upstream/UPSTREAM.md); its MIT license is preserved alongside it.
