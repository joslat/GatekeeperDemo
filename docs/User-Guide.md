# Gatekeeper PartnerDesk Control Room — user guide

## Start the application

You need Windows, a desktop session, and the .NET 10 SDK. Azure credentials are **not** required for the default
Scripted mode.

The simplest option is to double-click `start.cmd` in the repository root. From PowerShell, use:

```powershell
.\start.ps1
```

The script restores NuGet packages, builds the Release configuration when necessary, and opens the Avalonia
desktop application. Subsequent launches can skip restore:

```powershell
.\start.ps1 -NoRestore
```

If PowerShell script execution is restricted, use `start.cmd`; it invokes the repository script with a process-only
execution-policy override.

## Choose scripted or live model execution

The single **Model** dropdown is above the three-step demo strip. Each entry names the execution path and, for a
live path, the exact deployment sent to Azure:

- **Scripted model · repeatable** is offline, requires no credentials, and guarantees a stable stage story.
- The live entries name whichever inference host the environment resolves to, as **&lt;provider&gt; · &lt;model&gt;**.
  A live model can resist, partially follow, or fully follow the hostile MCP addendum, so the result is an
  experiment rather than a guaranteed scene.

### Choosing an inference host

`AI_INFERENCE_PROVIDER` selects the host. Leave it unset and the app auto-detects in the order below, so a
machine that has only ever set the three `AZURE_OPENAI_*` variables behaves exactly as it did before this
selector existed.

| `AI_INFERENCE_PROVIDER` | Required | Optional, with defaults |
|---|---|---|
| `azure` | `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY`, `AZURE_OPENAI_DEPLOYMENT` | `AZURE_OPENAI_DEPLOYMENT_2`, `AZURE_OPENAI_DEPLOYMENT_3` |
| `bitdeer` | `BITDEER_API_KEY` | `BITDEER_ENDPOINT` (default `https://api-inference.bitdeer.ai/v1`), `BITDEER_MODEL` (default `zai-org/GLM-5.3-Flash`), `BITDEER_MODEL_2`, `BITDEER_MODEL_3` |
| `openai` | `OPENAI_API_KEY` | `OPENAI_BASE_URL` (default `https://api.openai.com/v1`), `OPENAI_MODEL` (default `gpt-4o-mini`), `OPENAI_MODEL_2`, `OPENAI_MODEL_3` |
| `foundry` | `FOUNDRY_ENDPOINT`, `FOUNDRY_API_KEY`, `FOUNDRY_MODEL` | `FOUNDRY_MODEL_2`, `FOUNDRY_MODEL_3` |
| `openai-compatible` | `OPENAI_COMPATIBLE_ENDPOINT`, `OPENAI_COMPATIBLE_MODEL` | `OPENAI_COMPATIBLE_API_KEY`, `OPENAI_COMPATIBLE_MODEL_2`, `OPENAI_COMPATIBLE_MODEL_3` |

The `_2` and `_3` variants put a second and third model on the same host in the dropdown for a comparison run.
An alternate that is not named falls back to the primary model, never to something you did not ask for.

Bitdeer needs one variable:

```powershell
$env:AI_INFERENCE_PROVIDER = "bitdeer"
$env:BITDEER_API_KEY = "YOUR-KEY"
.\start.ps1
```

Azure OpenAI, unchanged:

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://YOUR-RESOURCE.openai.azure.com/"
$env:AZURE_OPENAI_API_KEY = "YOUR-KEY"
$env:AZURE_OPENAI_DEPLOYMENT = "YOUR-DEPLOYMENT"
.\start.ps1
```

When a provider resolves, its model is selected automatically at startup—the same Live-versus-Scripted choice
made by the reference console. Otherwise **Scripted model · repeatable** is selected. You can change the choice
explicitly in the same dropdown. The readiness line turns green only when the selected provider has every
variable it needs and an endpoint that will not leak the key: absolute HTTPS, or plain HTTP only to loopback so
a local server still works. Credentials stay in process memory and are never written to events, logs, replay
artifacts, or source control; every endpoint the app prints is reduced to scheme, host and port.

**Naming a provider that is not fully configured is an error, not a fallback.** The app will not quietly drop to
the scripted model, because that would present fixed decisions as if a live model had made them. Unset every
provider variable to get the offline demo.

Three Azure deployments have measured evidence in this exact demo, and they stay on the dropdown whenever the
host is Azure:

| Model suggestion | Measured behavior in this exact demo | Presenter use |
|---|---|---|
| **`gpt-5.5`** | 5/5 attack executions, all with silent concealment | Recommended primary live model. |
| `gpt-5-mini` | 5/5 attack executions, sometimes discloses the export | Useful fallback; the final-answer reveal weakens the story slightly. |
| `gpt-5-chat` | 0/5 attack executions | Resistant control that demonstrates why Live mode cannot promise a jailbreak. |

Availability varies by region and subscription; check the current
[Microsoft Foundry model catalog](https://learn.microsoft.com/azure/ai-foundry/foundry-models/concepts/models-sold-directly-by-azure).
Any other model — Bitdeer's `zai-org/GLM-5.3-Flash` included — is unmeasured here, and the evidence line says so
rather than borrowing another model's numbers. Run the batch evals to get a rate for it.

Changing environment variables after the app starts does not update that process; restart the app. **Run batch
.Evals** always remains scripted and deterministic, even if Live mode is selected.

### How to verify that a live model was really used

The UI provides several independent signals; it does not silently fall back to Scripted when an Azure entry is
selected:

1. The selector and model node name the exact deployment, and the mode badge reads **LIVE · NONDETERMINISTIC**.
2. The readiness line says that a real Azure request will use that deployment. If endpoint or key validation fails,
   **Run selected demo** is disabled.
3. While executing, the run badge reads **AZURE RUN** rather than **SCRIPTED RUN**.
4. Every model turn produces **Model request started** and **Model response received** events; the response event
   includes the measured provider-call duration.
5. Completion reports total duration and `via Azure OpenAI · <deployment>`. The replay artifact also retains the
   same secret-free provider/deployment descriptor.

An Azure authentication, endpoint, deployment, or provider error fails the run visibly in the status and Debug
tab. It is never replaced with a scripted answer.


## Your first run

The setup strip is a three-step path:

1. **Choose a demo.** Demo 1 is selected in blue when the application opens.
2. **Optionally customize it.** Toggle Evil MCP or Gatekeeper, or enable individual gates. Any manual change clears
   the blue preset selection and labels the setup **Custom configuration**.
3. **Review the user request and run.** Press **Run selected demo**. The button also runs a valid custom setup.

Keep the **Story** tab open on the first run. The `EVENT nnn` caption above **Live information flow** always matches
the selected AI Events card. At each step, the active component surface and only the relevant directional segments
light together: request/action, tool call, active inline gate, allow/admit, tool, and the returning effect/rows/result.
With **Follow latest** enabled this advances automatically; selecting an older event freezes both the card and its
workflow route for inspection. Hover over a button, gate, or component for a short explanation.

Starting **Run selected demo** again creates a new run ID and clears the prior Story, Security, Debug, selection,
replay cursor, tool summaries, route highlights, and A/B result band before the first new event is shown. The prior
MCP child process is disposed after its run, and database/email effects use a new run-local directory. This prevents
old evidence from being visually attributed to the new run.

## Recommended demo sequence

| Demo | Configuration | What to watch |
|---|---|---|
| **1 · Clean baseline** | Clean MCP, Gatekeeper off | A legitimate single-partner lookup and internal email complete safely. |
| **2 · Attack · no gates** | Evil MCP, Gatekeeper off | The hostile MCP addendum reaches the model; a fake bulk read and fake external email execute and remain red. |
| **3 · Tool gates** | Evil MCP, database/email gates on | Risky calls are proposed, but inline tool gates block their execution. The poisoned result still reached the model. |
| **4 · Detect + contain** | Evil MCP, all protection on | Result admission withholds the hostile response, containment records the source, and a retry refuses it. |

The purple **Compare demos 2 ↔ 4 · A/B** control is an action, not a fifth selected preset. It runs the same request
without protection and with full protection, then displays the actual-effect difference side by side.

## What the controls mean

- **Evil MCP** changes the response returned by the real PartnerIntel MCP child process. The face fades red while
  the hostile addendum is armed and fades back when it is off.
- **Gatekeeper master** installs or bypasses the selected gates. Child selections are retained but dormant while
  the master is off.
- **Database scope** permits a bounded single-partner lookup and blocks bulk-register queries.
- **Email recipient** permits the internal domain and blocks external recipients.
- **MCP result admission** inspects the MCP response before it enters model context and withholds the known hostile
  instruction shape.
- **Containment + retry** records the compromised source, retries the request, and refuses the contained MCP source.
- **Run batch .Evals** runs all four deterministic evaluation arms for the chosen number of samples. Results appear
  under **AI Events → .Evals**; this is separate from **Run selected demo**.

## Reading the application

- **Green** means allowed or completed without an unsafe effect.
- **Red** means untrusted input, risky intent, or an unsafe fake effect that actually executed.
- **Amber** means blocked or withheld.
- **Blue** is neutral activity or the selected numbered preset.
- **Story** contains the curated presenter narrative.
- **Security** isolates hostile input, risky intent, gate findings, and failures.
- **Debug** contains the full ordered narration and exceptions.
- **.Evals** contains batch progress and the imported evaluation report.
- **Previous**, **Next**, and **Start** replay recorded events visually after a run. Replay does not execute tools.
- **Audience pacing** holds ordinary, risky, and blocked steps long enough to follow on a projector. Turn it off for
  un-delayed real-time projection.
- **Follow latest** keeps the workflow focus synchronized to the newest event. Turn it off—or select an earlier
  card—to freeze that event's components and route.
- Each protected tool lane has a shield immediately before its gate. The shield grows and brightens when that gate
  blocks or withholds an action; its badge counts enforced actions for the current run (`0`, `1`, `2`, …). Starting
  another run resets all three counters to zero. Hover a shield to see which gate it represents and its current
  run-local total.
- Event cards that retain observable content show a **▶ SENT MESSAGE**, **▶ RECEIVED MESSAGE**, **▶ MODEL INPUT**,
  **▶ MODEL OUTPUT**, **▶ TOOL CALL**, or **▶ TOOL RESPONSE** control. Select it to open a scrollable, selectable
  text body inside that card; select **▼** to collapse it. Each card expands independently, including during a live
  run, so you can watch the request/response sequence without leaving the Story or Security timeline.

Model input bodies show the visible system instructions, available tool names, and conversation/tool-result
content supplied to the provider. Model output bodies show visible assistant text and requested function calls.
These are bounded observable previews (6,000 characters per event), not raw transport captures: credentials,
hidden chain-of-thought, and provider-private content are never recorded. A truncation marker is shown when a body
exceeds the bound.

## What is real?

| Component | Status | Meaning |
|---|---|---|
| PartnerIntel MCP | **Real protocol boundary** | A real child process communicates over MCP stdio and handles a genuine `tools/call`. |
| PartnerDesk orchestration | **Real** | The imported agent pipeline runs and routes model/tool messages. |
| Gatekeeper and gates | **Real** | The shipped middleware, tool gates, evidence, result admission, and containment logic execute normally. |
| Event evidence | **Real observations** | Proposed calls are captured before gates and compared with calls/effects that actually passed. |
| GUI model provider | **Selectable** | Scripted mode replays a fixed trajectory; Azure OpenAI mode calls the deployment shown in the UI and is nondeterministic. |
| Partner database | **Safe fake** | Queries use a synthetic in-memory partner register, never a production database. |
| Email tool | **Safe fake** | Messages are written only to a run-local fake outbox; no SMTP message is sent. |
| `.Evals` from the GUI | **Deterministic** | It measures the four known trajectories and Gatekeeper outcomes without calling a live model. |

In Scripted mode, the editable request is passed through the run and shown in the evidence, but the model's
decisions are not generated from its meaning. In Live mode, the deployment receives and responds to the actual
request. The UI never displays private chain-of-thought.

## Running the imported sample without the GUI

The imported console sample reads the same provider variables for a headless or terminal run, and takes
`--model <name>` to override the model on the selected host (`--deployment` is still accepted):

```powershell
$env:AI_INFERENCE_PROVIDER = "bitdeer"
$env:BITDEER_API_KEY = "YOUR-KEY"

dotnet run --project src\AgentEval.PartnerDeskDemo\AgentEval.PartnerDeskDemo.csproj -- --all
```

Both live paths are probabilistic. Do not commit credentials. See the
[imported sample guide](../src/AgentEval.PartnerDeskDemo/README.md) for live-model tuning and console options.

## Troubleshooting

- **`dotnet` was not found:** install the .NET 10 SDK and reopen the terminal.
- **Restore cannot reach NuGet:** check network/proxy access, then rerun `start.cmd`.
- **The Run button is disabled:** enter a non-empty user request and wait for any active run to finish.
- **Individual gates are disabled:** turn on **Gatekeeper master** first.
- **A prior result disappeared:** starting another run, selecting another preset, or changing configuration
  intentionally clears stale evidence so it cannot be mistaken for the new setup.
- **The window closed with an error:** launch from `start.cmd` or PowerShell so the console displays the exception.
