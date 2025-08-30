using System;
using Content.Server._RMC14.NPC.Components;
using Content.Server.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._RMC14.NPC.Systems;

/// <summary>
/// Simple finite state machine driven enemy AI.
/// Handles perception, alert levels and basic state transitions.
/// </summary>
public sealed class EnemyAISystem : EntitySystem
{
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<EnemyAIComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            Perceive(uid, comp, xform, frameTime);

            switch (comp.State)
            {
                case EnemyAIState.Idle:
                case EnemyAIState.Patrol:
                    if (comp.Target != null && comp.AlertLevel >= comp.AggressiveThreshold)
                        comp.State = EnemyAIState.Attack;
                    break;
                case EnemyAIState.Investigate:
                    if (comp.Target != null && comp.AlertLevel >= comp.AggressiveThreshold)
                        comp.State = EnemyAIState.Attack;
                    else if (comp.LastKnownTargetPos == null)
                        comp.State = EnemyAIState.Search;
                    break;
                case EnemyAIState.Attack:
                    if (comp.Target == null)
                        comp.State = EnemyAIState.Search;
                    break;
                case EnemyAIState.Retreat:
                    if (comp.AlertLevel <= 0f)
                        comp.State = EnemyAIState.Idle;
                    break;
                case EnemyAIState.Search:
                    if (comp.Target != null)
                        comp.State = EnemyAIState.Attack;
                    else if (comp.AlertLevel <= 0f)
                        comp.State = EnemyAIState.Idle;
                    break;
            }
        }
    }

    private void Perceive(EntityUid uid, EnemyAIComponent comp, TransformComponent xform, float frameTime)
    {
        comp.AlertLevel = MathF.Max(0f, comp.AlertLevel - comp.CalmDecay * frameTime);
        comp.Target = null;

        var ourPos = _transform.GetWorldPosition(xform);
        var players = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (players.MoveNext(out var playerUid, out var actor, out var targetXform))
        {
            if (targetXform.MapID != xform.MapID)
                continue;

            var targetPos = _transform.GetWorldPosition(targetXform);
            var distance = (targetPos - ourPos).Length();

            if (distance <= comp.SightRange && _interaction.InRangeUnobstructed(uid, playerUid, comp.SightRange))
            {
                comp.Target = playerUid;
                comp.AlertLevel += frameTime;
                comp.LastKnownTargetPos = targetPos;
                return;
            }

            if (distance <= comp.HearingRadius)
            {
                comp.AlertLevel += frameTime * 0.5f;
                comp.LastKnownTargetPos = targetPos;
            }
        }
    }
}
