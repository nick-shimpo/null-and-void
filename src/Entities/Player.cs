using System;
using System.Threading.Tasks;
using Godot;
using NullAndVoid.Components;
using NullAndVoid.Core;
using NullAndVoid.Systems;
using NullAndVoid.World;

namespace NullAndVoid.Entities;

/// <summary>
/// The player character "Null" - a sentient AI seeking revenge.
/// Implements IScheduledActor for energy-based turn scheduling.
/// </summary>
public partial class Player : Entity, IScheduledActor
{
    // Components
    public Attributes? AttributesComponent { get; private set; }
    public Inventory? InventoryComponent { get; private set; }
    public Equipment? EquipmentComponent { get; private set; }

    // Convenience accessors through Attributes
    public int CurrentHealth => AttributesComponent?.CurrentIntegrity ?? 0;
    public int MaxHealth => AttributesComponent?.MaxIntegrity ?? 100;
    public int AttackDamage => AttributesComponent?.AttackDamage ?? 10;
    public int SightRange => AttributesComponent?.SightRange ?? 10;
    public int Armor => AttributesComponent?.Armor ?? 0;

    // IScheduledActor implementation
    public string ActorName => EntityName;
    public int Speed => AttributesComponent?.Speed ?? 100;
    public bool IsActive => IsInstanceValid(this) && !IsQueuedForDeletion();
    public bool CanAct => _canAct && GameState.Instance.CurrentState == GameState.State.Playing;

    private bool _canAct = false;
    private TaskCompletionSource<int>? _actionCompletionSource;
    private int _lastActionCost = ActionCosts.Move;

    public override void _Ready()
    {
        base._Ready();
        EntityName = "Null";

        // Add to player group for enemy targeting
        AddToGroup("Player");

        // Create or get Attributes component
        AttributesComponent = GetNodeOrNull<Attributes>("Attributes");
        if (AttributesComponent == null)
        {
            AttributesComponent = new Attributes
            {
                Name = "Attributes",
                BaseIntegrity = 100,
                BaseAttackDamage = 10,
                BaseSightRange = 10,
                BaseArmor = 0,
                BaseSpeed = 100,
                BaseEnergyOutput = 15,  // Player has built-in power core
                BaseEnergyConsumption = 0,
                BaseEnergyReserveCapacity = 100,
                BaseNoise = 50
            };
            AddChild(AttributesComponent);
        }
        AttributesComponent.Initialize();

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

        // Link Equipment to Attributes for stat updates
        EquipmentComponent.SetAttributesComponent(AttributesComponent);

        // Subscribe to turn events
        EventBus.Instance.PlayerTurnStarted += OnPlayerTurnStarted;
        EventBus.Instance.PlayerTurnEnded += OnPlayerTurnEnded;

        // Subscribe to attribute events
        AttributesComponent.EnergyDepleted += OnEnergyDepleted;
        AttributesComponent.EntityDestroyed += OnEntityDestroyed;

        // Subscribe to equipment changes
        EquipmentComponent.EquipmentChanged += OnEquipmentChanged;

        // Register with energy scheduler
        TurnManager.Instance?.RegisterScheduledActor(this);
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

        if (AttributesComponent != null)
        {
            AttributesComponent.EnergyDepleted -= OnEnergyDepleted;
            AttributesComponent.EntityDestroyed -= OnEntityDestroyed;
        }

        if (EquipmentComponent != null)
        {
            EquipmentComponent.EquipmentChanged -= OnEquipmentChanged;
        }

        // Unregister from scheduler
        TurnManager.Instance?.UnregisterScheduledActor(this);
    }

    private void OnEquipmentChanged()
    {
        // Attributes component handles capping values automatically
    }

    private void OnEnergyDepleted()
    {
        // Emergency shutdown - deactivate high-consumption modules
        if (EquipmentComponent != null && AttributesComponent != null)
        {
            int deficit = -AttributesComponent.EnergyBalance;
            EquipmentComponent.ForceDeactivateModules(deficit);
            GD.Print("WARNING: Energy depleted! Modules shutting down.");
        }
    }

    private void OnEntityDestroyed()
    {
        Die();
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
            CompleteAction(ActionCosts.Wait);
            return;
        }

        if (moveDirection != Vector2I.Zero)
        {
            TryMove(moveDirection);
        }
    }

    /// <summary>
    /// IScheduledActor.TakeAction - waits for player input.
    /// </summary>
    public Task<int> TakeAction()
    {
        _actionCompletionSource = new TaskCompletionSource<int>();
        _canAct = true;

        // Process energy each turn
        AttributesComponent?.ProcessEnergyTick();

        return _actionCompletionSource.Task;
    }

    /// <summary>
    /// Complete the current action with the specified energy cost.
    /// Called internally or externally (e.g., from targeting system).
    /// </summary>
    public void CompleteAction(int actionCost)
    {
        _canAct = false;
        _lastActionCost = actionCost;
        _actionCompletionSource?.TrySetResult(actionCost);

        // Notify TurnManager with the action cost for proper scheduling
        TurnManager.Instance?.EndPlayerTurnWithCost(actionCost);
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
            CompleteAction(ActionCosts.Attack);
            return;
        }

        // Move to empty space
        if (Move(direction))
        {
            CompleteAction(ActionCosts.Move);
        }
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
        if (AttributesComponent == null)
            return;

        // First, route damage through equipped modules (shields first, then weighted)
        int remainingDamage = rawDamage;
        if (EquipmentComponent != null)
        {
            remainingDamage = EquipmentComponent.RouteDamageToModules(rawDamage);
        }

        // Apply remaining damage to core integrity (with armor reduction)
        if (remainingDamage > 0)
        {
            int actualDamage = AttributesComponent.TakeDamage(remainingDamage);
            EventBus.Instance.EmitEntityDamaged(this, actualDamage, CurrentHealth);
        }
        else
        {
            // All damage absorbed by modules
            EventBus.Instance.EmitEntityDamaged(this, 0, CurrentHealth);
        }
    }

    private void Die()
    {
        EventBus.Instance.EmitEntityDied(this);
        GameState.Instance.TransitionTo(GameState.State.GameOver);
    }

    /// <summary>
    /// Get debug info for display.
    /// </summary>
    public string GetAttributeDebugString()
    {
        return AttributesComponent?.GetDebugString() ?? "No attributes";
    }
}
