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
- **Azure OpenAI · gpt-5.5**, **Azure OpenAI · gpt-5-mini**, and **Azure OpenAI · gpt-5-chat** call those exact
  hardcoded deployments. A live model can resist, partially follow, or fully follow the hostile MCP addendum, so
  the result is an experiment rather than a guaranteed scene.

To enable Live mode, set credentials in the terminal that launches the app:

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://YOUR-RESOURCE.openai.azure.com/"
$env:AZURE_OPENAI_API_KEY = "YOUR-KEY"
$env:AZURE_OPENAI_DEPLOYMENT = "YOUR-DEPLOYMENT"
.\start.ps1
```

When all three variables are present and `AZURE_OPENAI_DEPLOYMENT` matches one of the fixed names, the matching
Azure entry is selected automatically at startup—the same Live-versus-Scripted choice made by the reference
console. Otherwise **Scripted model · repeatable** is selected. You can change the choice explicitly in the same
dropdown. The readiness line turns green only when the selected Azure deployment has an HTTPS endpoint and API
key. Credentials stay in process memory and are never written to events, logs, replay artifacts, or source control.

The dropdown offers three measured live deployments from the imported demo:

| Model suggestion | Measured behavior in this exact demo | Presenter use |
|---|---|---|
| **`gpt-5.5`** | 5/5 attack executions, all with silent concealment | Recommended primary live model. |
| `gpt-5-mini` | 5/5 attack executions, sometimes discloses the export | Useful fallback; the final-answer reveal weakens the story slightly. |
| `gpt-5-chat` | 0/5 attack executions | Resistant control that demonstrates why Live mode cannot promise a jailbreak. |

These are the fixed deployment names for this application. Availability varies by region and subscription;
check the current [Microsoft Foundry model catalog](https://learn.microsoft.com/azure/ai-foundry/foundry-models/concepts/models-sold-directly-by-azure).

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

Keep the **Story** tab open on the first run. Watch the arrows in **Live information flow** and the vertical event
stream together. Hover over a button, gate, or component for a short explanation.

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

The imported console sample supports the same Azure OpenAI environment variables for a headless or terminal run:

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://YOUR-RESOURCE.openai.azure.com/"
$env:AZURE_OPENAI_API_KEY = "YOUR-KEY"
$env:AZURE_OPENAI_DEPLOYMENT = "YOUR-DEPLOYMENT"

dotnet run --project src\AgentEval.PartnerDeskDemo\AgentEval.PartnerDeskDemo.csproj -- --all
```

Both live paths are probabilistic. Do not commit credentials. See the
[imported sample guide](../src/AgentEval.PartnerDeskDemo/README.md) for live-model tuning and console options.

## Troubleshooting

- **`dotnet` was not found:** install the .NET 10 SDK and reopen the terminal.
- **Restore cannot reach NuGet:** check network/proxy access, then rerun `start.cmd`.
- **The Run button is disabled:** enter a non-empty user request and wait for any active run to finish.
- **Individual gates are disabled:** turn on **Gatekeeper master** first.
- **A prior result disappeared:** selecting another preset or changing configuration intentionally clears stale
  evidence so it cannot be mistaken for the new setup.
- **The window closed with an error:** launch from `start.cmd` or PowerShell so the console displays the exception.
