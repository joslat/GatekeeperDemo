# PartnerDesk upstream provenance

- Repository: https://github.com/AgentEvalHQ/AgentEval
- Commit: `345d5b62766d556f5619c1d41d1e31946615e586`
- Imported on: 2026-08-26
- License: MIT; see `AgentEval-LICENSE.txt`.

Imported source directories:

- `samples/AgentEval.PartnerDeskDemo` → `src/AgentEval.PartnerDeskDemo`
- `samples/AgentEval.PartnerDeskDemo.Evals` → `src/AgentEval.PartnerDeskDemo.Evals`
- `samples/AgentEval.PartnerDeskDemo.Tests` → `tests/AgentEval.PartnerDeskDemo.Tests`

Local changes are intentionally kept in ordinary source control so the UI can instrument the runtime. Project references to the upstream monorepo were replaced with the published `AgentEval` package at the matching `0.28.0-beta` release. Behavioral contracts, manifests, data, gates, MCP boundary, and deterministic tests remain source-identifiable.
