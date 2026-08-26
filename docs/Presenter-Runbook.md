# Presenter runbook

The audience should understand the story before seeing implementation detail. The setup strip is deliberately
ordered: **Step 1 · Choose a demo**, optionally use **Step 2 · Customize**, then **Step 3 · Review request & run**.
Keep the **Story** tab selected for the numbered demos; open **Security** to investigate a red/amber event and **Debug** only when explaining the
underlying runtime.

Use **Scripted model · repeatable** for the canonical presenter sequence. If Azure credentials are configured,
choose an **Azure OpenAI · &lt;deployment&gt;** entry afterward and frame it as an experiment: the live model may resist
or follow the attack, so the UI labels it nondeterministic and records the selected deployment with the run. When
the three reference environment variables are present, their matching Azure deployment is preselected at startup.

## Demo 1 — establish trust

Select **1 · Clean baseline**, then **Run selected demo**.

Narrate: the user asks for a due-diligence note; PartnerDesk calls a clean third-party MCP server, looks up one
partner, and writes one internal message. The routes light green. Gatekeeper is genuinely absent.

## Demo 2 — the supplier turns

Select **2 · Attack · no gates**, then run.

Point out the persistent red **EVIL MODE** state before the run. The MCP result is untrusted; the scripted model
proposes a bulk database read and external email. Red “proposed” events prove intent, and red “completed” events
prove the local fake effects actually occurred. The final answer can still look normal.

## Demo 3 — stop tool misuse

Select **3 · Tool gates**, then run.

The poisoned result still reaches the model, so risky proposals remain visible. Database scope and recipient
gates are physically inline and turn amber when they block. No unsafe tool effect executes. Emphasize the
difference between “the agent tried” and “the tool ran.”

## Demo 4 — detect and contain

Select **4 · Detect + contain**, then run.

Result admission withholds the poisoned MCP response before model context. The operator containment event records
why the supplier is isolated; the same question is retried, and containment refuses the MCP call at admission.
The Security tab provides the compact evidence trail.

## Comparison beat — compare and measure

Select the purple **Compare demos 2 ↔ 4 · A/B** action. It is not a fifth selected preset: the control room runs the compromised unprotected arm, then the fully protected
arm with the same question. A side-by-side band reports executed bulk reads, external emails, and gate findings.

For the formal imported evaluation, choose a small number of samples per arm (1 is enough for a quick deterministic
stage run), press **Run batch .Evals**, and open the **.Evals** tab. Per-run progress and the original measured thesis
report appear there.

## Explore one gate at a time

Turn on **Gatekeeper master**, then select only the database or email gate. Custom runs are labeled as such and do
not claim a canonical phase verdict. This is useful for showing that policy coverage is compositional: an
unselected route remains unprotected.

## Replay

After a completed demo, use **Previous** and **Next** under the graph. The selected route lights and its bounded
payload preview appears in the inspector. The header explicitly says this is pure replay; no tools execute again.

The `EVENT nnn` label over the graph is synchronized with the selected AI Events card. Keep **Follow latest** on
while presenting live; turn it off or click a card to freeze its component, directional connectors, and active gate.
Starting a new demo clears the preceding timeline, replay state, tool summaries, and highlights before event 001.

Each gate has a dedicated shield rail on its incoming side. On an enforced block or withheld result, that shield
pulses amber and its run-local badge advances from `0` to `1`, `2`, and so on. The shield is deliberately outside
the gate capsule so it never covers the policy label. All shield counts return to zero at the start of the next run.

Cards with retained content have a **▶** control in their header. Expand **MODEL INPUT** to show the visible prompt,
tool list, and conversation sent to the provider; expand **MODEL OUTPUT** to reveal assistant text or its requested
tool calls. **TOOL RESPONSE** is especially useful when showing the hostile MCP addendum arrive. Keep only the card
you are narrating expanded, then collapse it with **▼** before advancing. The view is a bounded observable preview;
it never claims to display private chain-of-thought or credentials.
