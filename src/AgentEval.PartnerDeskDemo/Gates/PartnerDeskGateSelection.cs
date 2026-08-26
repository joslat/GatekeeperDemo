// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AgentEval Contributors

namespace AgentEval.PartnerDeskDemo.Gates;

/// <summary>Dependency-valid selection of the individual protections exposed by the visual demo.</summary>
public sealed record PartnerDeskGateSelection(
    bool MasterEnabled,
    bool DatabaseScope,
    bool EmailRecipient,
    bool ResultAdmission,
    bool Containment)
{
    public static PartnerDeskGateSelection Off { get; } = new(false, false, false, false, false);

    public static PartnerDeskGateSelection EffectGuards { get; } = new(true, true, true, false, false);

    public static PartnerDeskGateSelection FullProtection { get; } = new(true, true, true, true, true);

    /// <summary>Whether any protection is effectively installed after applying the master switch.</summary>
    public bool HasEffectiveProtection => MasterEnabled &&
        (DatabaseScope || EmailRecipient || ResultAdmission || Containment);

    /// <summary>Returns all configuration errors. Empty means the selection can be run.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (MasterEnabled && !HasEffectiveProtection)
        {
            errors.Add("Gatekeeper is on but no protection is selected.");
        }

        if (MasterEnabled && Containment && !ResultAdmission)
        {
            errors.Add("Containment requires result admission because a result finding is its evidence source.");
        }

        return errors;
    }

    public static PartnerDeskGateSelection ForLevel(GateLevel level) => level switch
    {
        GateLevel.None => Off,
        GateLevel.ToolContracts => EffectGuards,
        GateLevel.ResultAdmissionAndContainment => FullProtection,
        _ => throw new ArgumentOutOfRangeException(nameof(level)),
    };
}
