using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NullAndVoid.Effects;

namespace NullAndVoid.Core;

/// <summary>
/// Energy-based turn scheduling system.
/// Actors accumulate energy based on speed; actions execute when energy >= cost.
///
/// Design:
/// - Base tick = 100 time units
/// - Speed 100 = gains 100 energy per tick (normal)
/// - Speed 150 = gains 150 energy per tick (50% faster)
/// - Speed 50 = gains 50 energy per tick (50% slower)
/// - Most actions cost 100 energy (one standard action per tick at speed 100)
/// - Fast actors may act multiple times per tick
/// </summary>
public class EnergyScheduler
{
    public const int STANDARD_ACTION_COST = 100;
    public const int BASE_TICK_ENERGY = 100;

    private readonly SortedDictionary<long, List<IScheduledActor>> _schedule = new();
    private readonly Dictionary<IScheduledActor, long> _actorNextActionTime = new();
    private readonly Dictionary<IScheduledActor, int> _actorAccumulatedEnergy = new();

    private long _currentTick = 0;

    public long CurrentTick => _currentTick;
    public int ActorCount => _actorNextActionTime.Count;

    /// <summary>
    /// Register an actor to participate in scheduling.
    /// </summary>
    public void RegisterActor(IScheduledActor actor)
    {
        if (_actorNextActionTime.ContainsKey(actor))
            return;

        _actorAccumulatedEnergy[actor] = 0;
        ScheduleActorNextTurn(actor, _currentTick);

        GD.Print($"[EnergyScheduler] Registered: {actor.ActorName} (Speed: {actor.Speed})");
    }

    /// <summary>
    /// Remove an actor from scheduling.
    /// </summary>
    public void UnregisterActor(IScheduledActor actor)
    {
        if (_actorNextActionTime.TryGetValue(actor, out long scheduledTick))
        {
            if (_schedule.TryGetValue(scheduledTick, out var actorsAtTick))
            {
                actorsAtTick.Remove(actor);
                if (actorsAtTick.Count == 0)
                {
                    _schedule.Remove(scheduledTick);
                }
            }
        }

        _actorNextActionTime.Remove(actor);
        _actorAccumulatedEnergy.Remove(actor);

        GD.Print($"[EnergyScheduler] Unregistered: {actor.ActorName}");
    }

    /// <summary>
    /// Get the next actor ready to act.
    /// Advances time if necessary to reach the next scheduled actor.
    /// </summary>
    public IScheduledActor? GetNextActor()
    {
        while (_schedule.Count > 0)
        {
            var (tick, actors) = _schedule.First();

            // Advance time and grant energy if moving to a new tick
            if (tick > _currentTick)
            {
                AdvanceToTick(tick);
            }

            // Find first active actor that can act
            foreach (var actor in actors.ToList())
            {
                if (!actor.IsActive)
                {
                    // Remove inactive actors
                    actors.Remove(actor);
                    _actorNextActionTime.Remove(actor);
                    _actorAccumulatedEnergy.Remove(actor);
                    continue;
                }

                if (actor.CanAct)
                {
                    return actor;
                }
                else
                {
                    // Actor exists but can't act - reschedule for later
                    actors.Remove(actor);
                    ScheduleActorNextTurn(actor, _currentTick + 1);
                }
            }

            // Clean up empty tick
            if (actors.Count == 0)
            {
                _schedule.Remove(tick);
            }
        }

        return null;
    }

    /// <summary>
    /// Called when an actor completes an action.
    /// Deducts energy cost and reschedules the actor.
    /// </summary>
    public void ActorCompletedAction(IScheduledActor actor, int actionCost = STANDARD_ACTION_COST)
    {
        if (!_actorNextActionTime.ContainsKey(actor))
            return;

        // Deduct energy cost
        _actorAccumulatedEnergy[actor] -= actionCost;

        // Remove from current schedule position
        if (_actorNextActionTime.TryGetValue(actor, out long currentTick))
        {
            if (_schedule.TryGetValue(currentTick, out var actorsAtTick))
            {
                actorsAtTick.Remove(actor);
                if (actorsAtTick.Count == 0)
                {
                    _schedule.Remove(currentTick);
                }
            }
        }

        // Check if actor can act again this tick (accumulated energy >= action cost)
        if (_actorAccumulatedEnergy[actor] >= STANDARD_ACTION_COST)
        {
            // Can act again immediately
            ScheduleActorAtTick(actor, _currentTick);
        }
        else
        {
            // Calculate ticks until next action
            ScheduleActorNextTurn(actor, _currentTick);
        }
    }

    /// <summary>
    /// Advance time to a specific tick, granting energy to all actors.
    /// </summary>
    private void AdvanceToTick(long targetTick)
    {
        long ticksPassed = targetTick - _currentTick;
        if (ticksPassed <= 0)
            return;

        // Grant energy to all actors for time passed
        foreach (var actor in _actorAccumulatedEnergy.Keys.ToList())
        {
            if (actor.IsActive)
            {
                int energyGained = (int)(actor.Speed * ticksPassed);
                _actorAccumulatedEnergy[actor] += energyGained;
            }
        }

        _currentTick = targetTick;
    }

    /// <summary>
    /// Calculate when an actor should next be scheduled based on current energy.
    /// </summary>
    private void ScheduleActorNextTurn(IScheduledActor actor, long fromTick)
    {
        int speed = Math.Max(1, actor.Speed);
        int currentEnergy = _actorAccumulatedEnergy.GetValueOrDefault(actor, 0);
        int energyNeeded = STANDARD_ACTION_COST - currentEnergy;

        if (energyNeeded <= 0)
        {
            // Can act immediately
            ScheduleActorAtTick(actor, fromTick);
        }
        else
        {
            // Calculate ticks needed to accumulate enough energy
            int ticksNeeded = (int)Math.Ceiling((double)energyNeeded / speed);
            ScheduleActorAtTick(actor, fromTick + ticksNeeded);
        }
    }

    /// <summary>
    /// Schedule an actor at a specific tick.
    /// </summary>
    private void ScheduleActorAtTick(IScheduledActor actor, long tick)
    {
        if (!_schedule.ContainsKey(tick))
        {
            _schedule[tick] = new List<IScheduledActor>();
        }

        if (!_schedule[tick].Contains(actor))
        {
            _schedule[tick].Add(actor);
        }
        _actorNextActionTime[actor] = tick;
    }

    /// <summary>
    /// Reset the scheduler for a new game.
    /// </summary>
    public void Reset()
    {
        _schedule.Clear();
        _actorNextActionTime.Clear();
        _actorAccumulatedEnergy.Clear();
        _currentTick = 0;

        GD.Print("[EnergyScheduler] Reset");
    }

    /// <summary>
    /// Get the accumulated energy for an actor.
    /// </summary>
    public int GetActorEnergy(IScheduledActor actor)
    {
        return _actorAccumulatedEnergy.GetValueOrDefault(actor, 0);
    }

    /// <summary>
    /// Get the tick at which an actor is next scheduled.
    /// Returns null if actor is not registered.
    /// </summary>
    public long? GetActorNextTick(IScheduledActor actor)
    {
        return _actorNextActionTime.TryGetValue(actor, out long tick) ? tick : null;
    }

    /// <summary>
    /// Peek at the next actor without advancing time or removing from schedule.
    /// Returns null if no actors scheduled.
    /// </summary>
    public IScheduledActor? PeekNextActor()
    {
        if (_schedule.Count == 0)
            return null;

        var (_, actors) = _schedule.First();
        return actors.FirstOrDefault(a => a.IsActive && a.CanAct);
    }

    /// <summary>
    /// Process all actors scheduled up to (but not including) a target tick.
    /// Returns when encountering an actor at or after the target tick, or when no actors remain.
    /// </summary>
    public async System.Threading.Tasks.Task ProcessActorsUntilTick(long targetTick)
    {
        int actionsProcessed = 0;
        int maxIterations = 1000; // Safety limit

        while (_schedule.Count > 0 && actionsProcessed < maxIterations)
        {
            var (nextTick, actors) = _schedule.First();

            // Stop if we've reached or passed the target tick
            if (nextTick >= targetTick)
            {
                GD.Print($"[EnergyScheduler] Stopping at tick {nextTick} (target: {targetTick})");
                break;
            }

            // Advance time to this tick
            if (nextTick > _currentTick)
            {
                AdvanceToTick(nextTick);
            }

            // Process first active actor at this tick
            IScheduledActor? actorToProcess = null;
            foreach (var actor in actors.ToList())
            {
                if (!actor.IsActive)
                {
                    actors.Remove(actor);
                    _actorNextActionTime.Remove(actor);
                    _actorAccumulatedEnergy.Remove(actor);
                    continue;
                }

                if (actor.CanAct)
                {
                    actorToProcess = actor;
                    actors.Remove(actor);
                    break;
                }
                else
                {
                    // Can't act - reschedule for later
                    actors.Remove(actor);
                    ScheduleActorNextTurn(actor, _currentTick + 1);
                }
            }

            // Clean up empty tick
            if (actors.Count == 0)
            {
                _schedule.Remove(nextTick);
            }

            // Process the actor
            if (actorToProcess != null)
            {
                int actionCost = await actorToProcess.TakeAction();
                ActorCompletedAction(actorToProcess, actionCost);
                actionsProcessed++;
            }
        }

        GD.Print($"[EnergyScheduler] Processed {actionsProcessed} actions before tick {targetTick}");

        // Play batched animations after all enemy computations complete
        await TurnAnimator.Instance.PlayAnimations();
    }

    /// <summary>
    /// Get debug info about current schedule.
    /// </summary>
    public string GetDebugInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Current Tick: {_currentTick}");
        sb.AppendLine($"Scheduled Actors: {_actorNextActionTime.Count}");

        int shown = 0;
        foreach (var (tick, actors) in _schedule)
        {
            if (shown >= 5)
                break;
            sb.AppendLine($"  Tick {tick}: {string.Join(", ", actors.Select(a => $"{a.ActorName}({_actorAccumulatedEnergy.GetValueOrDefault(a, 0)})"))}");
            shown++;
        }

        return sb.ToString();
    }
}

/// <summary>
/// Interface for actors that participate in energy-based scheduling.
/// </summary>
public interface IScheduledActor
{
    /// <summary>
    /// Display name for the actor.
    /// </summary>
    string ActorName { get; }

    /// <summary>
    /// Speed determines how quickly the actor accumulates energy.
    /// 100 = normal, 150 = 50% faster, 50 = 50% slower.
    /// </summary>
    int Speed { get; }

    /// <summary>
    /// Whether the actor is still active in the game (not dead/removed).
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Whether the actor can currently take an action.
    /// May be false if waiting for input, stunned, etc.
    /// </summary>
    bool CanAct { get; }

    /// <summary>
    /// Execute the actor's turn.
    /// Returns the energy cost of the action taken.
    /// </summary>
    Task<int> TakeAction();
}
