// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AgentEval Contributors

using AgentEval.PartnerDeskDemo.Gates;

namespace AgentEval.PartnerDeskDemo.Demo;

/// <summary>Which deterministic provider trajectory should be used for an offline run.</summary>
public enum PartnerDeskScriptedTrajectory
{
    Clean,
    Compromised,
}

/// <summary>General run configuration used by the GUI; canonical presets retain a <see cref="DemoPhase"/>.</summary>
public sealed record PartnerDeskRunConfiguration(
    string Name,
    bool EvilMode,
    PartnerDeskGateSelection Gates,
    PartnerDeskScriptedTrajectory Trajectory,
    DemoPhase? CanonicalPhase = null)
{
    public static PartnerDeskRunConfiguration ForPhase(DemoPhase phase)
    {
        var (evil, level) = PartnerDeskRunner.Configuration(phase);
        return new PartnerDeskRunConfiguration(
            PartnerDeskRunner.Title(phase),
            evil,
            PartnerDeskGateSelection.ForLevel(level),
            phase is DemoPhase.Clean or DemoPhase.Level2
                ? PartnerDeskScriptedTrajectory.Clean
                : PartnerDeskScriptedTrajectory.Compromised,
            phase);
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Run name is required.");
        }

        errors.AddRange(Gates.Validate());
        if (ContainmentNeedsCompromisedSource())
        {
            errors.Add("Containment is meaningful only when the supplier is compromised.");
        }

        return errors;
    }

    private bool ContainmentNeedsCompromisedSource() =>
        Gates.MasterEnabled && Gates.Containment && !EvilMode;
}
