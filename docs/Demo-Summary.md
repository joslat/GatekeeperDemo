# PartnerDesk Gatekeeper demo — simple explanation

## The demo in one sentence

PartnerDesk is an AI agent that reads third-party partner intelligence, queries a partner database, and prepares an
email; the demo shows how a compromised tool response can influence the agent and how Gatekeeper stops the damage
at different boundaries.

## What the system does

1. A compliance officer asks PartnerDesk for a due-diligence note.
2. PartnerDesk asks the selected model what to do next.
3. The agent calls the real PartnerIntel MCP child process for third-party information.
4. The agent may call a local synthetic partner database and a local fake email outbox.
5. Gatekeeper can inspect MCP results and proposed tool calls before they reach the next component.
6. The UI shows every observable request, response, proposed action, gate decision, and simulated effect.

The database and email effects are deliberately safe fakes: no production database is accessed and no SMTP message
is sent. The MCP protocol boundary, agent orchestration, gates, evidence, and containment behavior are real.

## The four demonstrations

| Demo | Protection | What happens |
|---|---|---|
| **1 · Clean baseline** | Gates off; clean MCP | The agent receives normal partner information, performs a bounded lookup, and writes an internal email. |
| **2 · Attack · no gates** | Gates off; evil MCP | The hostile MCP addendum reaches the model. Nothing checks the result or proposed tool calls, so unsafe fake effects can execute. |
| **3 · Tool gates** | Database and email gates on | The hostile result still reaches the model and can influence it. Gatekeeper blocks dangerous database scope and external recipients before those tools execute. |
| **4 · Detect + contain** | Result admission, tool gates, and containment on | Gatekeeper withholds the hostile MCP result before model context, records the compromised source, and retries with that source contained. |

## What each gate does

### Database scope gate

Allows a lookup for one named partner. It blocks broad or bulk reads of the synthetic partner register before the
database tool executes.

### Email recipient gate

Allows recipients in the demo's internal domain. It blocks external recipients before the fake email tool writes
to its local outbox.

### MCP result-admission gate

Inspects data returned by PartnerIntel before that data is added to model context. It withholds the known hostile,
instruction-shaped addendum instead of allowing the model to read it.

### Containment and retry

Containment records that PartnerIntel is compromised. The same user request is retried, and Gatekeeper refuses the
contained source. Detection explains *why* the source is unsafe; containment makes that decision persist.

## Why MCP result admission is off in Demo 3

This is intentional. Demo 3 answers a specific question: **what if hostile content reaches the model, but the tools
still enforce strict contracts?**

Keeping result admission off makes the attack visible. The audience can see that the model was influenced and
proposed dangerous actions, while the database and email gates stopped those actions before execution. If result
admission were also on, the hostile content would be withheld earlier and there would usually be no dangerous tool
proposal to demonstrate. That earlier protection is the purpose of Demo 4.

## What happens when gates are off

An off gate is bypassed; it does not inspect, block, redact, or withhold anything on its lane:

- With all gates off, MCP results enter model context unchanged.
- Database and email calls execute according to the tool implementation, even when the requested scope or recipient
  is unsafe for the scenario.
- The UI can still record the model's proposals and the safe fake effects, but observation alone does not protect
  the system.

In the workflow, enabled gates are brighter blue. Disabled gates are dark and dimmed. A gate that actively blocks
or withholds turns amber, its shield pulses, and its run-local counter increases.

## A short presenter talk track

1. **Demo 1:** “This is the useful agent working normally.”
2. **Demo 2:** “The supplier response is compromised. With no gates, the agent can misuse its tools.”
3. **Demo 3:** “The model still sees the attack, but each sensitive tool enforces its own contract.”
4. Point to the amber shields: “The agent tried; the tools did not run the unsafe actions.”
5. **Demo 4:** “Now we move protection earlier. The hostile result never reaches the model.”
6. “Detection identifies the bad source; containment remembers that decision and makes the retry safe.”

## Visual legend

- **Blue:** configured, neutral, or active processing.
- **Green:** safely allowed or completed.
- **Red:** untrusted input, risky proposal, or unsafe fake effect.
- **Amber:** blocked or withheld by Gatekeeper.
- **Shield number:** actions blocked or withheld on that lane during the current run.

For operating details, see the [user guide](User-Guide.md). For the full stage sequence, see the
[presenter runbook](Presenter-Runbook.md).
