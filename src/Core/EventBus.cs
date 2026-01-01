using System;
using System.Collections.Generic;
using Godot;

namespace NullAndVoid.Core;

/// <summary>
/// Central event bus for decoupled communication between game systems.
/// Implements a publish-subscribe pattern for game events.
/// </summary>
public partial class EventBus : Node
{
    private static EventBus? _instance;
    public static EventBus Instance => _instance ?? throw new InvalidOperationException("EventBus not initialized");

    // Turn events
    [Signal] public delegate void TurnStartedEventHandler(int turnNumber);
    [Signal] public delegate void TurnEndedEventHandler(int turnNumber);
    [Signal] public delegate void PlayerTurnStartedEventHandler();
    [Signal] public delegate void PlayerTurnEndedEventHandler();

    // Entity events
    [Signal] public delegate void EntitySpawnedEventHandler(Node entity);
    [Signal] public delegate void EntityDestroyedEventHandler(Node entity);
    [Signal] public delegate void EntityMovedEventHandler(Node entity, Vector2I from, Vector2I to);

    // Combat events
    [Signal] public delegate void AttackPerformedEventHandler(Node attacker, Node target, int damage);
    [Signal] public delegate void EntityDamagedEventHandler(Node entity, int damage, int remainingHealth);
    [Signal] public delegate void EntityDiedEventHandler(Node entity);

    // Game state events
    [Signal] public delegate void GameStateChangedEventHandler(string newState);

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

    // Convenience methods for emitting events
    public void EmitTurnStarted(int turnNumber) => EmitSignal(SignalName.TurnStarted, turnNumber);
    public void EmitTurnEnded(int turnNumber) => EmitSignal(SignalName.TurnEnded, turnNumber);
    public void EmitPlayerTurnStarted() => EmitSignal(SignalName.PlayerTurnStarted);
    public void EmitPlayerTurnEnded() => EmitSignal(SignalName.PlayerTurnEnded);

    public void EmitEntitySpawned(Node entity) => EmitSignal(SignalName.EntitySpawned, entity);
    public void EmitEntityDestroyed(Node entity) => EmitSignal(SignalName.EntityDestroyed, entity);
    public void EmitEntityMoved(Node entity, Vector2I from, Vector2I to) =>
        EmitSignal(SignalName.EntityMoved, entity, from, to);

    public void EmitAttackPerformed(Node attacker, Node target, int damage) =>
        EmitSignal(SignalName.AttackPerformed, attacker, target, damage);
    public void EmitEntityDamaged(Node entity, int damage, int remainingHealth) =>
        EmitSignal(SignalName.EntityDamaged, entity, damage, remainingHealth);
    public void EmitEntityDied(Node entity) => EmitSignal(SignalName.EntityDied, entity);

    public void EmitGameStateChanged(string newState) => EmitSignal(SignalName.GameStateChanged, newState);
}
