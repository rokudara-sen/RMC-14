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

    /// <summary>
    /// How far the NPC will wander while idle or patrolling.
    /// </summary>
    [DataField]
    public float PatrolRadius = 5f;

    /// <summary>
    /// Delay between picking new wander targets while idle.
    /// </summary>
    [DataField]
    public float WanderCooldown = 3f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float WanderAccumulator = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2? WanderTarget;

    /// <summary>
    /// Preferred distance to the target when attacking.
    /// </summary>
    [DataField]
    public float AttackRange = 1.5f;
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
