using System;
using Godot;

namespace NullAndVoid.Core;

/// <summary>
/// Manages the overall game state using a state machine pattern.
/// </summary>
public partial class GameState : Node
{
    private static GameState? _instance;
    public static GameState Instance => _instance ?? throw new InvalidOperationException("GameState not initialized");

    public enum State
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        Inventory,
        GameOver,
        Victory
    }

    private State _currentState = State.MainMenu;

    public State CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                var previousState = _currentState;
                _currentState = value;
                OnStateChanged(previousState, value);
            }
        }
    }

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

    public void TransitionTo(State newState)
    {
        // Validate state transitions
        if (!IsValidTransition(_currentState, newState))
        {
            GD.PrintErr($"Invalid state transition: {_currentState} -> {newState}");
            return;
        }

        CurrentState = newState;
    }

    private bool IsValidTransition(State from, State to)
    {
        // Define valid state transitions
        return (from, to) switch
        {
            (State.MainMenu, State.Loading) => true,
            (State.MainMenu, State.Playing) => true,
            (State.Loading, State.Playing) => true,
            (State.Playing, State.Paused) => true,
            (State.Playing, State.Inventory) => true,
            (State.Playing, State.GameOver) => true,
            (State.Playing, State.Victory) => true,
            (State.Paused, State.Playing) => true,
            (State.Paused, State.MainMenu) => true,
            (State.Inventory, State.Playing) => true,
            (State.GameOver, State.MainMenu) => true,
            (State.Victory, State.MainMenu) => true,
            _ => false
        };
    }

    private void OnStateChanged(State previousState, State newState)
    {
        GD.Print($"Game state changed: {previousState} -> {newState}");
        EventBus.Instance.EmitGameStateChanged(newState.ToString());

        // Handle state-specific logic
        switch (newState)
        {
            case State.Playing:
                GetTree().Paused = false;
                break;
            case State.Paused:
            case State.Inventory:
                GetTree().Paused = true;
                break;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            switch (_currentState)
            {
                case State.Playing:
                    TransitionTo(State.Paused);
                    break;
                case State.Paused:
                    TransitionTo(State.Playing);
                    break;
                case State.Inventory:
                    TransitionTo(State.Playing);
                    break;
            }
        }
    }
}
