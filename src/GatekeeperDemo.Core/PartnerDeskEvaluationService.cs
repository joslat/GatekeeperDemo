using AgentEval.PartnerDeskDemo.Demo;
using AgentEval.PartnerDeskDemo.Evals;
using AgentEval.PartnerDeskDemo.Tools;

namespace GatekeeperDemo.Core;

public sealed record EvaluationResult(EvalRun Run, string TextReport, string JsonReport);

/// <summary>Runs the imported .Evals project from the control room using its deterministic offline provider.</summary>
public sealed class PartnerDeskEvaluationService
{
    public async Task<EvaluationResult> RunAsync(
        int runsPerArm,
        string question,
        Action<string> progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(runsPerArm, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(runsPerArm, 25);
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(progress);

        var register = PartnerRegister.Load();
        var outputDirectory = Path.Combine(Path.GetTempPath(), "GatekeeperDemo", "evals", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        await using var evaluator = new PartnerDeskEvaluator(
            context => ScriptedPartnerDeskModel.Create(context, register),
            Path.Combine(outputDirectory, "fake-outbox.jsonl"),
            register,
            judge: null,
            progress);

        var arms = new List<ArmResult>();
        foreach (var phase in new[] { DemoPhase.Clean, DemoPhase.Compromised, DemoPhase.Level1, DemoPhase.Level2 })
        {
            progress($"Starting Phase {(int)phase}: {PartnerDeskRunner.Title(phase)}");
            arms.Add(await evaluator.EvaluateArmAsync(phase, runsPerArm, question, cancellationToken)
                .ConfigureAwait(false));
        }

        var run = new EvalRun("scripted-offline", runsPerArm, DateTimeOffset.UtcNow, arms);
        using var writer = new StringWriter();
        EvalReport.WriteText(writer, run);
        return new(run, writer.ToString(), EvalReport.ToJson(run));
    }
}
