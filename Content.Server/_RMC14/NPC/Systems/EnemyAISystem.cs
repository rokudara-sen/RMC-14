using System;
using System.Numerics;
using Content.Server._RMC14.NPC.Components;
using Content.Server.Interaction;
using Content.Server.NPC.Systems;
using Content.Shared.NPC;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
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
    [Dependency] private readonly NPCSteeringSystem _steering = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnemyAIComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EnemyAIComponent, ComponentShutdown>(OnShutdown);
    }

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

            HandleMovement(uid, comp, xform, frameTime);
        }
    }

    private void OnStartup(EntityUid uid, EnemyAIComponent comp, ComponentStartup args)
    {
        EnsureComp<ActiveNPCComponent>(uid);
    }

    private void OnShutdown(EntityUid uid, EnemyAIComponent comp, ComponentShutdown args)
    {
        _steering.Unregister(uid);
        RemComp<ActiveNPCComponent>(uid);
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
                if (comp.Target == null)
                    AlertAllies(uid, comp, xform, targetPos);

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

    private void HandleMovement(EntityUid uid, EnemyAIComponent comp, TransformComponent xform, float frameTime)
    {
        var worldPos = _transform.GetWorldPosition(xform);

        if (xform.MapUid == null)
            return;
        var mapUid = xform.MapUid.Value;

        switch (comp.State)
        {
            case EnemyAIState.Idle:
            case EnemyAIState.Patrol:
                comp.WanderAccumulator -= frameTime;

                if (comp.WanderTarget != null)
                {
                    var dist = (comp.WanderTarget.Value - worldPos).Length();
                    if (dist <= 0.5f)
                    {
                        comp.WanderTarget = null;
                        _steering.Unregister(uid);
                    }
                }

                if (comp.WanderAccumulator <= 0f && comp.WanderTarget == null)
                {
                    var offset = _random.NextVector2(comp.PatrolRadius);
                    var target = worldPos + offset;
                    comp.WanderTarget = target;
                    comp.WanderAccumulator = comp.WanderCooldown;
                    _steering.TryRegister(uid, new EntityCoordinates(mapUid, target));
                }
                break;

            case EnemyAIState.Investigate:
            case EnemyAIState.Search:
                if (comp.LastKnownTargetPos != null)
                {
                    _steering.TryRegister(uid, new EntityCoordinates(mapUid, comp.LastKnownTargetPos.Value));

                    var dist = (comp.LastKnownTargetPos.Value - worldPos).Length();
                    if (dist <= 0.75f)
                        comp.LastKnownTargetPos = null;
                }
                break;

            case EnemyAIState.Attack:
                if (comp.Target != null && TryComp<TransformComponent>(comp.Target.Value, out var targetXform))
                {
                    var targetPos = _transform.GetWorldPosition(targetXform);
                    _steering.TryRegister(uid, targetXform.Coordinates);

                    var dist = (targetPos - worldPos).Length();
                    if (dist <= comp.AttackRange)
                        _steering.Unregister(uid);
                }
                break;

            case EnemyAIState.Retreat:
                if (comp.Target != null && TryComp<TransformComponent>(comp.Target.Value, out var retreatXform))
                {
                    var targetPos = _transform.GetWorldPosition(retreatXform);
                    var dir = (worldPos - targetPos).Normalized();
                    var retreatPos = worldPos + dir * comp.PatrolRadius;
                    _steering.TryRegister(uid, new EntityCoordinates(mapUid, retreatPos));
                }
                break;
        }
    }

    private void AlertAllies(EntityUid uid, EnemyAIComponent comp, TransformComponent xform, Vector2 targetPos)
    {
        var ourPos = _transform.GetWorldPosition(xform);
        var allies = EntityQueryEnumerator<EnemyAIComponent, TransformComponent>();

        while (allies.MoveNext(out var otherUid, out var otherComp, out var otherXform))
        {
            if (otherUid == uid || otherXform.MapID != xform.MapID)
                continue;

            var dist = (_transform.GetWorldPosition(otherXform) - ourPos).Length();
            if (dist > comp.HearingRadius * 2f)
                continue;

            otherComp.AlertLevel = MathF.Max(otherComp.AlertLevel, comp.AlertLevel);
            otherComp.LastKnownTargetPos = targetPos;
            if (otherComp.State == EnemyAIState.Idle || otherComp.State == EnemyAIState.Patrol)
                otherComp.State = EnemyAIState.Investigate;
        }
    }
}
