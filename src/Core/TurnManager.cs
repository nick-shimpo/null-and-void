using System;
using System.Collections.Generic;
using Godot;

namespace NullAndVoid.Core;

/// <summary>
/// Manages the turn-based game loop.
/// Coordinates actions between the player and all other entities.
/// Supports both legacy ITurnActor and new IScheduledActor interfaces.
/// </summary>
public partial class TurnManager : Node
{
    private static TurnManager? _instance;
    public static TurnManager Instance => _instance ?? throw new InvalidOperationException("TurnManager not initialized");

    [Export] public int CurrentTurn { get; private set; } = 0;

    /// <summary>
    /// If true, use the new EnergyScheduler system. Otherwise use legacy turn order.
    /// </summary>
    [Export] public bool UseEnergyScheduler { get; set; } = true;

    private bool _isPlayerTurn = true;
    private readonly List<ITurnActor> _actors = new();
    private bool _processingTurn = false;

    // New energy-based scheduler
    private readonly EnergyScheduler _scheduler = new();

    public bool IsPlayerTurn => _isPlayerTurn;
    public EnergyScheduler Scheduler => _scheduler;
    public long SchedulerTick => _scheduler.CurrentTick;

    public override void _EnterTree()
    {
        if (_instance != null && _instance != this)
        {
            QueueFree();
            return;
        }
        _instance = this;
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// Register an actor (legacy ITurnActor interface).
    /// </summary>
    public void RegisterActor(ITurnActor actor)
    {
        if (!_actors.Contains(actor))
        {
            _actors.Add(actor);
        }

        // Also register with scheduler if it implements IScheduledActor
        if (actor is IScheduledActor scheduledActor)
        {
            _scheduler.RegisterActor(scheduledActor);
        }
    }

    /// <summary>
    /// Register an actor (new IScheduledActor interface).
    /// </summary>
    public void RegisterScheduledActor(IScheduledActor actor)
    {
        _scheduler.RegisterActor(actor);
    }

    public void UnregisterActor(ITurnActor actor)
    {
        _actors.Remove(actor);

        // Also unregister from scheduler
        if (actor is IScheduledActor scheduledActor)
        {
            _scheduler.UnregisterActor(scheduledActor);
        }
    }

    public void UnregisterScheduledActor(IScheduledActor actor)
    {
        _scheduler.UnregisterActor(actor);
    }

    /// <summary>
    /// Called when the player completes their action.
    /// Pass the action cost so the scheduler can properly reschedule.
    /// </summary>
    public void EndPlayerTurnWithCost(int actionCost)
    {
        _pendingPlayerActionCost = actionCost;
        EndPlayerTurn();
    }

    private int _pendingPlayerActionCost = ActionCosts.Move;

    public async void EndPlayerTurn()
    {
        if (!_isPlayerTurn || _processingTurn)
            return;

        _processingTurn = true;
        _isPlayerTurn = false;

        EventBus.Instance.EmitPlayerTurnEnded();

        // Invalidate enemy path caches when player moves (target position changed)
        InvalidateEnemyPaths();

        if (UseEnergyScheduler)
        {
            // Variable speed processing using energy scheduler
            // 1. Find the player and tell scheduler they acted
            // 2. Process all actors scheduled before player's next turn
            // 3. Fast enemies (Speed > 100) may get multiple actions per player turn
            // 4. Slow enemies (Speed < 100) may skip some player turns

            var player = GetPlayerActor();
            if (player != null)
            {
                // Inform scheduler the player completed their action
                _scheduler.ActorCompletedAction(player, _pendingPlayerActionCost);

                // Get when player is next scheduled
                long? playerNextTick = _scheduler.GetActorNextTick(player);

                if (playerNextTick.HasValue)
                {
                    GD.Print($"[TurnManager] Player acted (cost {_pendingPlayerActionCost}), next turn at tick {playerNextTick.Value}");
                    GD.Print($"[TurnManager] Current tick: {_scheduler.CurrentTick}");

                    // Process all actors scheduled before player's next turn
                    await _scheduler.ProcessActorsUntilTick(playerNextTick.Value);
                }
                else
                {
                    GD.PrintErr("[TurnManager] Player not found in scheduler!");
                }
            }
            else
            {
                GD.PrintErr("[TurnManager] No player actor found!");
            }
        }
        else
        {
            // Legacy system: process all actors once
            foreach (var actor in _actors)
            {
                if (actor.IsActive)
                {
                    await actor.ProcessTurn();
                }
            }
        }

        // Advance to next turn
        CurrentTurn++;
        EventBus.Instance.EmitTurnStarted(CurrentTurn);

        _isPlayerTurn = true;
        _processingTurn = false;

        EventBus.Instance.EmitPlayerTurnStarted();
    }

    /// <summary>
    /// Find the player actor in the scene tree.
    /// </summary>
    private IScheduledActor? GetPlayerActor()
    {
        var players = GetTree().GetNodesInGroup("Player");
        foreach (var node in players)
        {
            if (node is IScheduledActor actor)
                return actor;
        }
        return null;
    }

    /// <summary>
    /// Process the next scheduled actor (energy-based system).
    /// Call this in a loop to process the game.
    /// Returns true if an actor was processed, false if no actors available.
    /// </summary>
    public async System.Threading.Tasks.Task<bool> ProcessNextScheduledActor()
    {
        var actor = _scheduler.GetNextActor();
        if (actor == null)
            return false;

        // Process the actor's turn
        int actionCost = await actor.TakeAction();
        _scheduler.ActorCompletedAction(actor, actionCost);

        return true;
    }

    /// <summary>
    /// Starts a new game by resetting the turn counter.
    /// </summary>
    public void StartNewGame()
    {
        CurrentTurn = 0;
        _isPlayerTurn = true;
        _processingTurn = false;
        _actors.Clear();
        _scheduler.Reset();

        EventBus.Instance.EmitTurnStarted(CurrentTurn);
        EventBus.Instance.EmitPlayerTurnStarted();
    }

    /// <summary>
    /// Get debug info about the current scheduling state.
    /// </summary>
    public string GetSchedulerDebugInfo()
    {
        return _scheduler.GetDebugInfo();
    }

    /// <summary>
    /// Invalidate cached paths for all enemies.
    /// Called when player moves since their target position changed.
    /// </summary>
    private void InvalidateEnemyPaths()
    {
        var enemies = GetTree().GetNodesInGroup("Enemies");
        foreach (var node in enemies)
        {
            if (node is Entities.Enemy enemy)
            {
                enemy.Memory.InvalidatePath();
            }
        }
    }
}

/// <summary>
/// Interface for entities that participate in the turn system.
/// </summary>
public interface ITurnActor
{
    bool IsActive { get; }
    int Speed { get; }
    System.Threading.Tasks.Task ProcessTurn();
}
