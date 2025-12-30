using Godot;
using System;
using System.Collections.Generic;

namespace NullAndVoid.Core;

/// <summary>
/// Manages the turn-based game loop.
/// Coordinates actions between the player and all other entities.
/// </summary>
public partial class TurnManager : Node
{
    private static TurnManager? _instance;
    public static TurnManager Instance => _instance ?? throw new InvalidOperationException("TurnManager not initialized");

    [Export] public int CurrentTurn { get; private set; } = 0;

    private bool _isPlayerTurn = true;
    private readonly List<ITurnActor> _actors = new();
    private bool _processingTurn = false;

    public bool IsPlayerTurn => _isPlayerTurn;

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

    public void RegisterActor(ITurnActor actor)
    {
        if (!_actors.Contains(actor))
        {
            _actors.Add(actor);
        }
    }

    public void UnregisterActor(ITurnActor actor)
    {
        _actors.Remove(actor);
    }

    /// <summary>
    /// Called when the player completes their action.
    /// Advances the turn and processes all other actors.
    /// </summary>
    public async void EndPlayerTurn()
    {
        if (!_isPlayerTurn || _processingTurn)
            return;

        _processingTurn = true;
        _isPlayerTurn = false;

        EventBus.Instance.EmitPlayerTurnEnded();

        // Process all other actors
        foreach (var actor in _actors)
        {
            if (actor.IsActive)
            {
                await actor.ProcessTurn();
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
    /// Starts a new game by resetting the turn counter.
    /// </summary>
    public void StartNewGame()
    {
        CurrentTurn = 0;
        _isPlayerTurn = true;
        _processingTurn = false;
        _actors.Clear();

        EventBus.Instance.EmitTurnStarted(CurrentTurn);
        EventBus.Instance.EmitPlayerTurnStarted();
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
