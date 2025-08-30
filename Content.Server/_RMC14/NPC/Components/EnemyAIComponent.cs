using System.Numerics;

namespace Content.Server._RMC14.NPC.Components;

/// <summary>
/// Basic finite state machine for enemy NPC behaviour.
/// Tracks perception data, alert levels and movement parameters.
/// </summary>
[RegisterComponent]
public sealed partial class EnemyAIComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EnemyAIState State = EnemyAIState.Idle;

    [ViewVariables(VVAccess.ReadWrite)]
    public float AlertLevel = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;

    [ViewVariables]
    public Vector2? LastKnownTargetPos;

    [DataField]
    public float HearingRadius = 8f;

    [DataField]
    public float SightRange = 12f;

    [DataField]
    public float AggressiveThreshold = 3f;

    [DataField]
    public float CalmDecay = 1f;
}

public enum EnemyAIState
{
    Idle,
    Patrol,
    Investigate,
    Attack,
    Retreat,
    Search
}
