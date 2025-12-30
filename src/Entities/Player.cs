using Godot;
using System;
using NullAndVoid.Core;
using NullAndVoid.World;
using NullAndVoid.Systems;

namespace NullAndVoid.Entities;

/// <summary>
/// The player character "Null" - a sentient AI seeking revenge.
/// </summary>
public partial class Player : Entity
{
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int CurrentHealth { get; set; } = 100;
    [Export] public int AttackDamage { get; set; } = 10;
    [Export] public int SightRange { get; set; } = 10;

    private bool _canAct = true;

    public override void _Ready()
    {
        base._Ready();
        EntityName = "Null";

        // Add to player group for enemy targeting
        AddToGroup("Player");

        // Subscribe to turn events
        EventBus.Instance.PlayerTurnStarted += OnPlayerTurnStarted;
        EventBus.Instance.PlayerTurnEnded += OnPlayerTurnEnded;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Unsubscribe from events
        if (EventBus.Instance != null)
        {
            EventBus.Instance.PlayerTurnStarted -= OnPlayerTurnStarted;
            EventBus.Instance.PlayerTurnEnded -= OnPlayerTurnEnded;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_canAct || GameState.Instance.CurrentState != GameState.State.Playing)
            return;

        Vector2I moveDirection = Vector2I.Zero;

        // Cardinal directions
        if (@event.IsActionPressed("move_up"))
            moveDirection = new Vector2I(0, -1);
        else if (@event.IsActionPressed("move_down"))
            moveDirection = new Vector2I(0, 1);
        else if (@event.IsActionPressed("move_left"))
            moveDirection = new Vector2I(-1, 0);
        else if (@event.IsActionPressed("move_right"))
            moveDirection = new Vector2I(1, 0);
        // Diagonal directions
        else if (@event.IsActionPressed("move_up_left"))
            moveDirection = new Vector2I(-1, -1);
        else if (@event.IsActionPressed("move_up_right"))
            moveDirection = new Vector2I(1, -1);
        else if (@event.IsActionPressed("move_down_left"))
            moveDirection = new Vector2I(-1, 1);
        else if (@event.IsActionPressed("move_down_right"))
            moveDirection = new Vector2I(1, 1);
        // Wait
        else if (@event.IsActionPressed("wait_turn"))
        {
            // Wait in place (skip turn)
            EndTurn();
            return;
        }

        if (moveDirection != Vector2I.Zero)
        {
            TryMove(moveDirection);
        }
    }

    private void TryMove(Vector2I direction)
    {
        var newPosition = GridPosition + direction;

        // Check collision with walls
        if (!TileMapManager.Instance.IsWalkable(newPosition))
        {
            // Can't move - wall or out of bounds
            return;
        }

        // Check for enemy at target position - attack instead of move
        var enemy = CombatSystem.GetEnemyAtPosition(GetTree(), newPosition);
        if (enemy != null)
        {
            // Attack the enemy
            CombatSystem.PerformMeleeAttack(this, enemy, AttackDamage);
            EndTurn();
            return;
        }

        // Move to empty space
        if (Move(direction))
        {
            EndTurn();
        }
    }

    private void EndTurn()
    {
        _canAct = false;
        TurnManager.Instance.EndPlayerTurn();
    }

    private void OnPlayerTurnStarted()
    {
        _canAct = true;
    }

    private void OnPlayerTurnEnded()
    {
        _canAct = false;
    }

    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        EventBus.Instance.EmitEntityDamaged(this, damage, CurrentHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        EventBus.Instance.EmitEntityDied(this);
        GameState.Instance.TransitionTo(GameState.State.GameOver);
    }
}
