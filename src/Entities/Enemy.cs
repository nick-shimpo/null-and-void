using System.Threading.Tasks;
using Godot;
using NullAndVoid.AI;
using NullAndVoid.AI.Behaviors;
using NullAndVoid.Components;
using NullAndVoid.Core;
using NullAndVoid.World;

namespace NullAndVoid.Entities;

/// <summary>
/// Base class for all enemy entities.
/// Implements IScheduledActor for energy-based turn scheduling.
/// Also implements ITurnActor for backward compatibility.
/// </summary>
public partial class Enemy : Entity, IScheduledActor, ITurnActor
{
    // Legacy exports for editor configuration (used to initialize Attributes)
    [Export] public int BaseMaxHealth { get; set; } = 20;
    [Export] public int BaseAttackDamage { get; set; } = 5;
    [Export] public int BaseSpeed { get; set; } = 100;
    [Export] public int BaseSightRange { get; set; } = 8;
    [Export] public int BaseEnergyOutput { get; set; } = 10;
    [Export] public int BaseNoise { get; set; } = 50;

    // Components
    public Attributes? AttributesComponent { get; private set; }
    public Health? HealthComponent { get; private set; }

    // AI System
    public BehaviorSelector Behaviors { get; private set; } = new();
    public EnemyMemory Memory { get; private set; } = new();
    private BehaviorContext? _behaviorContext;

    /// <summary>
    /// If true, skip default behavior initialization in _Ready().
    /// Used when behaviors are configured by EnemyFactory.
    /// </summary>
    public bool SkipDefaultBehaviors { get; set; } = false;

    // IScheduledActor implementation
    public string ActorName => EntityName;
    public int Speed => AttributesComponent?.Speed ?? BaseSpeed;
    public bool IsActive => !_isDead && IsInstanceValid(this) && !IsQueuedForDeletion();
    public bool CanAct => IsActive;

    // Convenience accessors through Attributes
    public int MaxHealth => AttributesComponent?.MaxIntegrity ?? BaseMaxHealth;
    public int CurrentHealth => AttributesComponent?.CurrentIntegrity ?? BaseMaxHealth;
    public int AttackDamage => AttributesComponent?.AttackDamage ?? BaseAttackDamage;
    public int SightRange => AttributesComponent?.SightRange ?? BaseSightRange;

    private bool _isDead = false;

    // AI State
    public enum AIState
    {
        Idle,
        Wandering,
        Chasing,
        Attacking,
        Fleeing
    }

    [Export] public AIState CurrentState { get; set; } = AIState.Idle;

    private Player? _targetPlayer;

    public override void _Ready()
    {
        base._Ready();

        // Create or get Attributes component
        AttributesComponent = GetNodeOrNull<Attributes>("Attributes");
        if (AttributesComponent == null)
        {
            AttributesComponent = new Attributes
            {
                Name = "Attributes",
                BaseIntegrity = BaseMaxHealth,
                BaseAttackDamage = BaseAttackDamage,
                BaseSightRange = BaseSightRange,
                BaseArmor = 0,
                BaseSpeed = BaseSpeed,
                BaseEnergyOutput = BaseEnergyOutput,
                BaseEnergyConsumption = 0,
                BaseEnergyReserveCapacity = 50,
                BaseNoise = BaseNoise
            };
            AddChild(AttributesComponent);
        }
        AttributesComponent.Initialize();
        AttributesComponent.EntityDestroyed += OnDied;

        // Get or create health component (for compatibility)
        HealthComponent = GetNodeOrNull<Health>("Health");
        if (HealthComponent == null)
        {
            HealthComponent = new Health();
            HealthComponent.Name = "Health";
            AddChild(HealthComponent);
        }

        HealthComponent.MaxHealth = MaxHealth;
        HealthComponent.CurrentHealth = MaxHealth;
        HealthComponent.Died += OnDied;

        // Register with turn manager (legacy support)
        TurnManager.Instance?.RegisterActor(this);

        // Find player reference
        _targetPlayer = GetTree().GetFirstNodeInGroup("Player") as Player;

        // Initialize AI system
        if (!SkipDefaultBehaviors)
        {
            InitializeBehaviors();
        }
        Memory.Initialize(GridPosition);

        // Create behavior context
        _behaviorContext = new BehaviorContext(this, Memory, TileMapManager.Instance, GetTree());
    }

    /// <summary>
    /// Initialize the behavior selector with default behaviors.
    /// Override in subclasses to customize AI behavior.
    /// </summary>
    protected virtual void InitializeBehaviors()
    {
        // Default behavior set: Attack > Chase > Wander
        Behaviors.AddBehaviors(
            new MeleeAttackBehavior(),
            new ChaseBehavior(persistence: 5),
            new WanderBehavior(moveChance: 0.5f)
        );
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (AttributesComponent != null)
        {
            AttributesComponent.EntityDestroyed -= OnDied;
        }

        if (HealthComponent != null)
        {
            HealthComponent.Died -= OnDied;
        }

        TurnManager.Instance?.UnregisterActor(this);
    }

    /// <summary>
    /// IScheduledActor.TakeAction - executes AI turn and returns action cost.
    /// </summary>
    public virtual async Task<int> TakeAction()
    {
        if (!IsActive)
        {
            GD.Print($"[{EntityName}] TakeAction: Not active, skipping");
            return ActionCosts.Wait;
        }

        GD.Print($"[{EntityName}] TakeAction: Starting turn, {Behaviors.GetBehaviors().Count} behaviors registered");

        // Process energy each turn
        AttributesComponent?.ProcessEnergyTick();

        // Find player if not cached
        _targetPlayer ??= GetTree().GetFirstNodeInGroup("Player") as Player;

        // Update memory timers
        Memory.OnTurnStart();

        // Create or update behavior context
        if (_behaviorContext == null)
        {
            _behaviorContext = new BehaviorContext(this, Memory, TileMapManager.Instance, GetTree());
        }

        // Update context with current target info
        _behaviorContext.UpdateTargetInfo(_targetPlayer);

        GD.Print($"[{EntityName}] Context: CanSeeTarget={_behaviorContext.CanSeeTarget}, Distance={_behaviorContext.DistanceToTarget}, Target={_targetPlayer?.EntityName ?? "null"}");

        // Evaluate and execute behaviors
        var result = await Behaviors.Evaluate(_behaviorContext);

        GD.Print($"[{EntityName}] Result: {result.ActionTaken} (Success={result.Success}, Cost={result.ActionCost})");

        // Update state for debugging/display
        UpdateStateFromBehavior(Behaviors.LastExecutedBehavior);

        return result.ActionCost;
    }

    /// <summary>
    /// Update the AIState enum based on which behavior executed.
    /// Used for debugging and status display.
    /// </summary>
    private void UpdateStateFromBehavior(IBehavior? behavior)
    {
        if (behavior == null)
        {
            CurrentState = AIState.Idle;
            return;
        }

        CurrentState = behavior.Name switch
        {
            "MeleeAttack" or "RangedAttack" => AIState.Attacking,
            "Chase" or "Investigate" => AIState.Chasing,
            "Flee" => AIState.Fleeing,
            "Wander" or "Patrol" or "Guard" => AIState.Wandering,
            _ => AIState.Idle
        };
    }

    /// <summary>
    /// Legacy ProcessTurn for compatibility with old TurnManager.
    /// </summary>
    public virtual async Task ProcessTurn()
    {
        await TakeAction();
    }

    /// <summary>
    /// Calculate Manhattan distance to player.
    /// </summary>
    protected int GetDistanceToPlayer()
    {
        if (_targetPlayer == null)
            return int.MaxValue;

        return Mathf.Abs(GridPosition.X - _targetPlayer.GridPosition.X) +
               Mathf.Abs(GridPosition.Y - _targetPlayer.GridPosition.Y);
    }

    /// <summary>
    /// Get direction vector towards player.
    /// </summary>
    protected Vector2I GetDirectionToPlayer()
    {
        if (_targetPlayer == null)
            return Vector2I.Zero;

        int dx = _targetPlayer.GridPosition.X - GridPosition.X;
        int dy = _targetPlayer.GridPosition.Y - GridPosition.Y;

        return new Vector2I(
            dx != 0 ? (dx > 0 ? 1 : -1) : 0,
            dy != 0 ? (dy > 0 ? 1 : -1) : 0
        );
    }

    /// <summary>
    /// Attack the player.
    /// </summary>
    protected virtual async Task AttackPlayer()
    {
        if (_targetPlayer == null)
            return;

        // Deal damage to player
        _targetPlayer.TakeDamage(AttackDamage);
        EventBus.Instance.EmitAttackPerformed(this, _targetPlayer, AttackDamage);

        GD.Print($"{EntityName} attacks {_targetPlayer.EntityName} for {AttackDamage} damage!");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Move towards the player.
    /// Returns true if movement occurred.
    /// </summary>
    protected virtual async Task<bool> ChasePlayer()
    {
        var direction = GetDirectionToPlayer();
        bool moved = false;

        // Try to move towards player, checking collision
        if (TryMoveInDirection(direction))
        {
            moved = true;
        }
        else if (direction.X != 0 && direction.Y != 0)
        {
            // If direct path blocked, try cardinal directions only
            // Try horizontal first
            if (TryMoveInDirection(new Vector2I(direction.X, 0)))
            {
                moved = true;
            }
            else if (TryMoveInDirection(new Vector2I(0, direction.Y)))
            {
                // Try vertical
                moved = true;
            }
        }

        await Task.CompletedTask;
        return moved;
    }

    /// <summary>
    /// Wander randomly.
    /// Returns true if movement occurred.
    /// </summary>
    protected virtual async Task<bool> Wander()
    {
        // 50% chance to stay still
        if (GD.Randf() < 0.5f)
        {
            await Task.CompletedTask;
            return false;
        }

        // Pick a random direction
        var directions = new Vector2I[]
        {
            new(0, -1), new(0, 1), new(-1, 0), new(1, 0),
            new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)
        };

        var randomDir = directions[GD.RandRange(0, directions.Length - 1)];
        bool moved = TryMoveInDirection(randomDir);

        await Task.CompletedTask;
        return moved;
    }

    /// <summary>
    /// Try to move in a direction, checking for collisions.
    /// Uses O(1) EntityGrid lookup instead of O(N) group iteration.
    /// </summary>
    protected bool TryMoveInDirection(Vector2I direction)
    {
        var newPosition = GridPosition + direction;

        // Check wall collision
        if (!TileMapManager.Instance.IsWalkable(newPosition))
            return false;

        // O(1) check if position is occupied by any entity other than self
        if (EntityGrid.Instance.IsOccupiedByOther(newPosition, this))
            return false;

        // Move!
        Move(direction);
        return true;
    }

    /// <summary>
    /// Called when this enemy dies.
    /// </summary>
    protected virtual void OnDied()
    {
        _isDead = true;
        GD.Print($"{EntityName} has been destroyed!");

        // Remove from scene after a short delay
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    /// <summary>
    /// Take damage from an attack.
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, null);
    }

    /// <summary>
    /// Take damage from an attack, with attacker position for AI alerting.
    /// </summary>
    public void TakeDamage(int damage, Vector2I? attackerPosition)
    {
        if (AttributesComponent != null)
        {
            AttributesComponent.TakeDamage(damage);
        }
        else
        {
            // Fallback to legacy health component
            HealthComponent?.TakeDamage(damage);
        }

        // Alert the enemy AI when taking damage
        if (attackerPosition.HasValue)
        {
            // Fully alert - we know exactly where the attack came from
            Memory.AlertLevel = 100;
            Memory.LastKnownTargetPos = attackerPosition.Value;
            Memory.TurnsSinceTargetSeen = 0;
            Memory.InvalidatePath(); // Recalculate path to attacker
            GD.Print($"[Enemy] {EntityName} alerted by damage from {attackerPosition.Value}!");
        }
        else
        {
            // Partially alert - we took damage but don't know from where
            Memory.AlertLevel = Mathf.Max(Memory.AlertLevel, 75);
            GD.Print($"[Enemy] {EntityName} alerted by damage (source unknown)!");
        }
    }
}
