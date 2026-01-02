using System.Collections.Generic;
using Godot;
using NullAndVoid.AI;
using NullAndVoid.Entities;
using NullAndVoid.World;

namespace NullAndVoid.Combat;

/// <summary>
/// Result of seeker projectile processing.
/// </summary>
public enum SeekerResult
{
    /// <summary>
    /// Seeker is still moving toward target.
    /// </summary>
    Moving,

    /// <summary>
    /// Seeker hit the target.
    /// </summary>
    HitTarget,

    /// <summary>
    /// Seeker ran out of fuel.
    /// </summary>
    Expired,

    /// <summary>
    /// Seeker lost tracking on target.
    /// </summary>
    LostTarget,

    /// <summary>
    /// Seeker was destroyed (hit a wall, etc.)
    /// </summary>
    Destroyed
}

/// <summary>
/// A guided projectile that pathfinds to its target.
/// Processes movement each turn until it hits or expires.
/// </summary>
public class SeekerProjectile
{
    private static int _nextId = 1;

    /// <summary>
    /// Unique ID for this seeker.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Current position of the seeker.
    /// </summary>
    public Vector2I Position { get; private set; }

    /// <summary>
    /// Previous positions for trail visualization.
    /// </summary>
    public List<Vector2I> Trail { get; } = new();

    /// <summary>
    /// Target entity being tracked.
    /// </summary>
    public Entity? Target { get; private set; }

    /// <summary>
    /// Last known target position (used if target is lost).
    /// </summary>
    public Vector2I LastKnownTargetPos { get; private set; }

    /// <summary>
    /// Weapon that fired this seeker.
    /// </summary>
    public WeaponData Weapon { get; }

    /// <summary>
    /// Entity that fired this seeker.
    /// </summary>
    public Node Owner { get; }

    /// <summary>
    /// Remaining fuel in turns.
    /// </summary>
    public int RemainingFuel { get; private set; }

    /// <summary>
    /// Tiles moved per turn.
    /// </summary>
    public int Speed { get; }

    /// <summary>
    /// Cached path to target.
    /// </summary>
    private List<Vector2I>? _cachedPath;
    private Vector2I _cachedPathTarget;

    /// <summary>
    /// Whether the seeker is still active.
    /// </summary>
    public bool IsActive => RemainingFuel > 0;

    public SeekerProjectile(
        Vector2I startPosition,
        Entity target,
        WeaponData weapon,
        Node owner,
        int fuel = 10,
        int speed = 2)
    {
        Id = _nextId++;
        Position = startPosition;
        Target = target;
        LastKnownTargetPos = target.GridPosition;
        Weapon = weapon;
        Owner = owner;
        RemainingFuel = fuel;
        Speed = speed;
    }

    /// <summary>
    /// Process seeker movement for one turn.
    /// </summary>
    public SeekerResult ProcessTurn(TileMapManager tileMap)
    {
        // Check if target is still valid
        if (Target == null || !IsEntityAlive(Target))
        {
            // Try to continue to last known position
            if (Position == LastKnownTargetPos)
            {
                return SeekerResult.LostTarget;
            }
        }
        else
        {
            // Update last known position
            LastKnownTargetPos = Target.GridPosition;
        }

        // Check fuel
        if (RemainingFuel <= 0)
            return SeekerResult.Expired;

        // Recalculate path if needed
        if (_cachedPath == null || !IsPathValid() || _cachedPathTarget != LastKnownTargetPos)
        {
            _cachedPath = Pathfinding.FindPath(Position, LastKnownTargetPos, tileMap);
            _cachedPathTarget = LastKnownTargetPos;
        }

        if (_cachedPath == null || _cachedPath.Count <= 1)
        {
            RemainingFuel--;
            return SeekerResult.LostTarget;
        }

        // Move along path
        int movesMade = 0;
        while (movesMade < Speed && _cachedPath.Count > 1)
        {
            // Add current position to trail
            Trail.Add(Position);

            // Move to next position
            Position = _cachedPath[1];
            _cachedPath.RemoveAt(0);
            movesMade++;

            // Check if we hit the target
            if (Target != null && Position == Target.GridPosition)
            {
                return SeekerResult.HitTarget;
            }

            // Check if we hit last known position (target lost)
            if (Target == null && Position == LastKnownTargetPos)
            {
                return SeekerResult.LostTarget;
            }

            // Check if we hit a wall (shouldn't happen with pathfinding, but safety check)
            if (!tileMap.IsWalkable(Position))
            {
                return SeekerResult.Destroyed;
            }
        }

        RemainingFuel--;
        return SeekerResult.Moving;
    }

    /// <summary>
    /// Check if cached path is still valid.
    /// </summary>
    private bool IsPathValid()
    {
        if (_cachedPath == null || _cachedPath.Count == 0)
            return false;

        // Path is valid if first element matches current position
        return _cachedPath[0] == Position;
    }

    /// <summary>
    /// Check if an entity is still alive.
    /// </summary>
    private static bool IsEntityAlive(Entity entity)
    {
        if (entity is Enemy enemy)
            return enemy.CurrentHealth > 0;
        if (entity is Player player)
            return player.CurrentHealth > 0;
        return true;
    }

    /// <summary>
    /// Calculate damage when hitting target.
    /// </summary>
    public int CalculateDamage()
    {
        // Full damage if fuel remaining, reduced if running low
        float fuelFactor = Mathf.Min(1.0f, RemainingFuel / 3.0f);
        int baseDamage = Weapon.RollDamage();
        return Mathf.Max(1, (int)(baseDamage * fuelFactor));
    }

    /// <summary>
    /// Get visual character for this seeker.
    /// </summary>
    public string GetVisualChar()
    {
        return RemainingFuel > 5 ? "@" : (RemainingFuel > 2 ? "o" : ".");
    }

    /// <summary>
    /// Get color based on fuel remaining.
    /// </summary>
    public Color GetVisualColor()
    {
        if (RemainingFuel > 5)
            return new Color(1.0f, 0.3f, 0.3f);  // Bright red
        if (RemainingFuel > 2)
            return new Color(1.0f, 0.6f, 0.3f);  // Orange
        return new Color(0.8f, 0.8f, 0.3f);       // Yellow (low fuel)
    }
}
