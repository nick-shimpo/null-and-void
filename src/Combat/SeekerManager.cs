using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NullAndVoid.Core;
using NullAndVoid.Entities;
using NullAndVoid.World;

namespace NullAndVoid.Combat;

/// <summary>
/// Result of a seeker hit for combat resolution.
/// </summary>
public class SeekerHitResult
{
    public SeekerProjectile Seeker { get; set; } = null!;
    public Entity Target { get; set; } = null!;
    public int Damage { get; set; }
    public bool TargetKilled { get; set; }
}

/// <summary>
/// Manages all active seeker projectiles in the game.
/// Processes seeker movement each turn and handles collisions.
/// </summary>
public class SeekerManager
{
    private static SeekerManager? _instance;
    public static SeekerManager Instance => _instance ??= new SeekerManager();

    private readonly List<SeekerProjectile> _activeSeekers = new();

    /// <summary>
    /// Event fired when a seeker hits its target.
    /// </summary>
    public event Action<SeekerHitResult>? SeekerHit;

    /// <summary>
    /// Event fired when a seeker expires or is destroyed.
    /// </summary>
    public event Action<SeekerProjectile, SeekerResult>? SeekerLost;

    /// <summary>
    /// Get all active seekers.
    /// </summary>
    public IReadOnlyList<SeekerProjectile> ActiveSeekers => _activeSeekers;

    /// <summary>
    /// Number of active seekers.
    /// </summary>
    public int ActiveCount => _activeSeekers.Count;

    /// <summary>
    /// Launch a new seeker projectile.
    /// </summary>
    public SeekerProjectile LaunchSeeker(
        WeaponData weapon,
        Vector2I origin,
        Entity target,
        Node owner,
        int? fuel = null,
        int? speed = null)
    {
        var seeker = new SeekerProjectile(
            origin,
            target,
            weapon,
            owner,
            fuel ?? weapon.SeekerFuel,
            speed ?? weapon.SeekerSpeed
        );

        _activeSeekers.Add(seeker);

        GD.Print($"[Seeker] Launched seeker #{seeker.Id} targeting {target.EntityName}");

        return seeker;
    }

    /// <summary>
    /// Process all active seekers for one turn.
    /// Returns list of hits that occurred.
    /// </summary>
    public List<SeekerHitResult> ProcessAllSeekers(TileMapManager tileMap)
    {
        var hits = new List<SeekerHitResult>();
        var toRemove = new List<SeekerProjectile>();

        foreach (var seeker in _activeSeekers)
        {
            var result = seeker.ProcessTurn(tileMap);

            switch (result)
            {
                case SeekerResult.HitTarget:
                    var hitResult = ResolveSeekerHit(seeker);
                    if (hitResult != null)
                    {
                        hits.Add(hitResult);
                        SeekerHit?.Invoke(hitResult);
                    }
                    toRemove.Add(seeker);
                    break;

                case SeekerResult.Expired:
                case SeekerResult.LostTarget:
                case SeekerResult.Destroyed:
                    GD.Print($"[Seeker] Seeker #{seeker.Id} {result}");
                    SeekerLost?.Invoke(seeker, result);
                    toRemove.Add(seeker);
                    break;

                case SeekerResult.Moving:
                    // Continue tracking
                    break;
            }
        }

        // Remove inactive seekers
        foreach (var seeker in toRemove)
        {
            _activeSeekers.Remove(seeker);
        }

        return hits;
    }

    /// <summary>
    /// Resolve a seeker hitting its target.
    /// </summary>
    private SeekerHitResult? ResolveSeekerHit(SeekerProjectile seeker)
    {
        if (seeker.Target == null)
            return null;

        int damage = seeker.CalculateDamage();
        bool killed = false;

        // Apply damage - pass origin so target can locate attacker
        if (seeker.Target is Enemy enemy)
        {
            enemy.TakeDamage(damage, seeker.Origin);
            killed = enemy.CurrentHealth <= 0;
        }
        else if (seeker.Target is Player player)
        {
            player.TakeDamage(damage);
            killed = player.CurrentHealth <= 0;
        }

        GD.Print($"[Seeker] Seeker #{seeker.Id} hit {seeker.Target.EntityName} for {damage} damage" +
                 (killed ? " (destroyed)" : ""));

        // Emit event
        try
        {
            EventBus.Instance.EmitAttackPerformed(seeker.Owner, seeker.Target, damage);
        }
        catch (InvalidOperationException)
        {
            // EventBus not initialized
        }

        return new SeekerHitResult
        {
            Seeker = seeker,
            Target = seeker.Target,
            Damage = damage,
            TargetKilled = killed
        };
    }

    /// <summary>
    /// Get seekers targeting a specific entity.
    /// </summary>
    public IEnumerable<SeekerProjectile> GetSeekersTargeting(Entity target)
    {
        return _activeSeekers.Where(s => s.Target == target);
    }

    /// <summary>
    /// Get seekers owned by a specific entity.
    /// </summary>
    public IEnumerable<SeekerProjectile> GetSeekersOwnedBy(Node owner)
    {
        return _activeSeekers.Where(s => s.Owner == owner);
    }

    /// <summary>
    /// Cancel all seekers targeting a specific entity.
    /// </summary>
    public void CancelSeekersTargeting(Entity target)
    {
        var toRemove = _activeSeekers.Where(s => s.Target == target).ToList();
        foreach (var seeker in toRemove)
        {
            _activeSeekers.Remove(seeker);
            SeekerLost?.Invoke(seeker, SeekerResult.LostTarget);
        }
    }

    /// <summary>
    /// Clear all active seekers (e.g., on level transition).
    /// </summary>
    public void ClearAll()
    {
        _activeSeekers.Clear();
    }

    /// <summary>
    /// Reset singleton instance (for testing/restarting).
    /// </summary>
    public static void Reset()
    {
        _instance = null;
    }
}
