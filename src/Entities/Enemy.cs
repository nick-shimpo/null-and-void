using Godot;
using System.Threading.Tasks;
using NullAndVoid.Core;
using NullAndVoid.Components;
using NullAndVoid.World;

namespace NullAndVoid.Entities;

/// <summary>
/// Base class for all enemy entities.
/// Implements turn-based AI behavior.
/// </summary>
public partial class Enemy : Entity, ITurnActor
{
    [Export] public int MaxHealth { get; set; } = 20;
    [Export] public int AttackDamage { get; set; } = 5;
    [Export] public int Speed { get; set; } = 1;
    [Export] public int SightRange { get; set; } = 8;

    public bool IsActive { get; set; } = true;
    public Health? HealthComponent { get; private set; }

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

        // Get or create health component
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

        // Register with turn manager
        TurnManager.Instance.RegisterActor(this);

        // Find player reference
        _targetPlayer = GetTree().GetFirstNodeInGroup("Player") as Player;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (HealthComponent != null)
        {
            HealthComponent.Died -= OnDied;
        }

        TurnManager.Instance?.UnregisterActor(this);
    }

    /// <summary>
    /// Process this enemy's turn. Called by TurnManager.
    /// </summary>
    public virtual async Task ProcessTurn()
    {
        if (!IsActive || HealthComponent?.IsDead == true)
            return;

        // Find player if not cached
        _targetPlayer ??= GetTree().GetFirstNodeInGroup("Player") as Player;

        if (_targetPlayer == null)
            return;

        // Determine AI behavior based on distance to player
        int distanceToPlayer = GetDistanceToPlayer();

        if (distanceToPlayer <= 1)
        {
            // Adjacent to player - attack!
            CurrentState = AIState.Attacking;
            await AttackPlayer();
        }
        else if (distanceToPlayer <= SightRange)
        {
            // Can see player - chase!
            CurrentState = AIState.Chasing;
            await ChasePlayer();
        }
        else
        {
            // Can't see player - wander or idle
            CurrentState = AIState.Wandering;
            await Wander();
        }

        // Small delay for visual feedback
        await Task.Delay(50);
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
    /// </summary>
    protected virtual async Task ChasePlayer()
    {
        var direction = GetDirectionToPlayer();

        // Try to move towards player, checking collision
        if (!TryMoveInDirection(direction))
        {
            // If direct path blocked, try cardinal directions only
            if (direction.X != 0 && direction.Y != 0)
            {
                // Try horizontal first
                if (!TryMoveInDirection(new Vector2I(direction.X, 0)))
                {
                    // Try vertical
                    TryMoveInDirection(new Vector2I(0, direction.Y));
                }
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Wander randomly.
    /// </summary>
    protected virtual async Task Wander()
    {
        // 50% chance to stay still
        if (GD.Randf() < 0.5f)
            return;

        // Pick a random direction
        var directions = new Vector2I[]
        {
            new(0, -1), new(0, 1), new(-1, 0), new(1, 0),
            new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)
        };

        var randomDir = directions[GD.RandRange(0, directions.Length - 1)];
        TryMoveInDirection(randomDir);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Try to move in a direction, checking for collisions.
    /// </summary>
    protected bool TryMoveInDirection(Vector2I direction)
    {
        var newPosition = GridPosition + direction;

        // Check wall collision
        if (!TileMapManager.Instance.IsWalkable(newPosition))
            return false;

        // Check collision with player
        if (_targetPlayer != null && _targetPlayer.GridPosition == newPosition)
            return false;

        // Check collision with other enemies
        var enemies = GetTree().GetNodesInGroup("Enemies");
        foreach (var node in enemies)
        {
            if (node is Enemy other && other != this && other.GridPosition == newPosition)
                return false;
        }

        // Move!
        Move(direction);
        return true;
    }

    /// <summary>
    /// Called when this enemy dies.
    /// </summary>
    protected virtual void OnDied()
    {
        IsActive = false;
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
        HealthComponent?.TakeDamage(damage);
    }
}
