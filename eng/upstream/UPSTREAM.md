# PartnerDesk upstream provenance

- Repository: https://github.com/AgentEvalHQ/AgentEval
- Commit: `345d5b62766d556f5619c1d41d1e31946615e586`
- Imported on: 2026-08-26
- License: MIT; see `AgentEval-LICENSE.txt`.

Imported source directories:

- `samples/AgentEval.PartnerDeskDemo` → `src/AgentEval.PartnerDeskDemo`
- `samples/AgentEval.PartnerDeskDemo.Evals` → `src/AgentEval.PartnerDeskDemo.Evals`
- `samples/AgentEval.PartnerDeskDemo.Tests` → `tests/AgentEval.PartnerDeskDemo.Tests`

Local changes are intentionally kept in ordinary source control so the UI can instrument the runtime. Project references to the upstream monorepo were replaced with the published `AgentEval` package, now at `0.35.0-beta` (was `0.28.0-beta` at import). That bump is what carries AgentEval task 4.3: `PartnerDeskChecks.cs` and `AdmittedChecksSelfTest.cs` were imported from the same upstream directory and project AgentEval's four containment questions through `BenchmarkArm` / `BenchmarkRunner` / `AdmittedCheck`, none of which exist in `0.28.0-beta`. They are the first eval types in this repository; every other file listed above is unchanged by the port, which added code and deleted none. Behavioral contracts, manifests, data, gates, MCP boundary, and deterministic tests remain source-identifiable.
