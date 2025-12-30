using Godot;
using System;
using NullAndVoid.Core;
using NullAndVoid.World;
using NullAndVoid.Systems;
using NullAndVoid.Components;

namespace NullAndVoid.Entities;

/// <summary>
/// The player character "Null" - a sentient AI seeking revenge.
/// </summary>
public partial class Player : Entity
{
    [Export] public int BaseMaxHealth { get; set; } = 100;
    [Export] public int BaseAttackDamage { get; set; } = 10;
    [Export] public int BaseSightRange { get; set; } = 10;
    [Export] public int BaseArmor { get; set; } = 0;

    // Current health tracks actual HP
    public int CurrentHealth { get; set; } = 100;

    // Computed stats including equipment bonuses
    public int MaxHealth => BaseMaxHealth + (EquipmentComponent?.TotalBonusHealth ?? 0);
    public int AttackDamage => BaseAttackDamage + (EquipmentComponent?.TotalBonusDamage ?? 0);
    public int SightRange => BaseSightRange + (EquipmentComponent?.TotalBonusSightRange ?? 0);
    public int Armor => BaseArmor + (EquipmentComponent?.TotalBonusArmor ?? 0);

    // Components
    public Inventory? InventoryComponent { get; private set; }
    public Equipment? EquipmentComponent { get; private set; }

    private bool _canAct = true;

    public override void _Ready()
    {
        base._Ready();
        EntityName = "Null";

        // Add to player group for enemy targeting
        AddToGroup("Player");

        // Create or get inventory component
        InventoryComponent = GetNodeOrNull<Inventory>("Inventory");
        if (InventoryComponent == null)
        {
            InventoryComponent = new Inventory { Name = "Inventory" };
            AddChild(InventoryComponent);
        }

        // Create or get equipment component
        EquipmentComponent = GetNodeOrNull<Equipment>("Equipment");
        if (EquipmentComponent == null)
        {
            EquipmentComponent = new Equipment { Name = "Equipment" };
            AddChild(EquipmentComponent);
        }

        // Initialize health
        CurrentHealth = MaxHealth;

        // Subscribe to turn events
        EventBus.Instance.PlayerTurnStarted += OnPlayerTurnStarted;
        EventBus.Instance.PlayerTurnEnded += OnPlayerTurnEnded;

        // Subscribe to equipment changes to update stats
        EquipmentComponent.EquipmentChanged += OnEquipmentChanged;
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

        if (EquipmentComponent != null)
        {
            EquipmentComponent.EquipmentChanged -= OnEquipmentChanged;
        }
    }

    private void OnEquipmentChanged()
    {
        // Cap current health to new max health if it decreased
        if (CurrentHealth > MaxHealth)
        {
            CurrentHealth = MaxHealth;
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

    public void TakeDamage(int rawDamage)
    {
        // Apply armor reduction (minimum 1 damage)
        int actualDamage = Mathf.Max(1, rawDamage - Armor);
        CurrentHealth -= actualDamage;
        EventBus.Instance.EmitEntityDamaged(this, actualDamage, CurrentHealth);

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
