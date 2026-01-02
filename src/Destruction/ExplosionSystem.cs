using System;
using System.Collections.Generic;
using Godot;

namespace NullAndVoid.Destruction;

/// <summary>
/// Handles explosion effects including damage, knockback, and fire.
/// </summary>
public class ExplosionSystem
{
    private readonly DestructibleTile[,] _tiles;
    private readonly int _width;
    private readonly int _height;
    private readonly FireSimulation _fireSimulation;
    private readonly Random _random = new();

    // Visual effect tracking
    public List<ExplosionVisual> ActiveVisuals { get; } = new();

    public ExplosionSystem(DestructibleTile[,] tiles, int width, int height, FireSimulation fireSimulation)
    {
        _tiles = tiles;
        _width = width;
        _height = height;
        _fireSimulation = fireSimulation;
    }

    /// <summary>
    /// Trigger an explosion at the specified position.
    /// Returns list of affected positions with damage dealt.
    /// </summary>
    public List<ExplosionResult> Explode(int centerX, int centerY, ExplosionData explosion)
    {
        var results = new List<ExplosionResult>();

        // Create visual effect
        ActiveVisuals.Add(new ExplosionVisual
        {
            Center = new Vector2I(centerX, centerY),
            Radius = explosion.Radius,
            Color = explosion.FlashColor,
            Duration = 0.5f,
            Timer = 0
        });

        // Process all tiles in radius
        for (int dy = -explosion.Radius; dy <= explosion.Radius; dy++)
        {
            for (int dx = -explosion.Radius; dx <= explosion.Radius; dx++)
            {
                int x = centerX + dx;
                int y = centerY + dy;

                if (!IsInBounds(x, y))
                    continue;

                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance > explosion.Radius)
                    continue;

                // Check line of sight (simple - blocked by destroyed walls)
                if (!HasLineOfSight(centerX, centerY, x, y))
                {
                    // Partial damage through cover
                    distance *= 1.5f;
                    if (distance > explosion.Radius)
                        continue;
                }

                // Calculate and apply damage
                int damage = explosion.CalculateDamageAtDistance(distance);
                int terrainDamage = (int)(damage * explosion.TerrainDamage);

                ref var tile = ref _tiles[x, y];
                bool destroyed = tile.TakeDamage(terrainDamage, DamageType.Explosive);

                // Try to ignite
                if (explosion.CausesFire && tile.Material.Flammability > 0)
                {
                    float igniteChance = 0.5f * (1.0f - distance / explosion.Radius);
                    if (_random.NextDouble() < igniteChance)
                    {
                        _fireSimulation.Ignite(x, y, FireIntensity.Flame);
                    }
                }

                // Check for chain reaction (fuel tanks, etc.)
                if (destroyed && tile.Material.Flammability >= 1.0f)
                {
                    // Schedule secondary explosion
                    QueueSecondaryExplosion(x, y);
                }

                results.Add(new ExplosionResult
                {
                    Position = new Vector2I(x, y),
                    Damage = damage,
                    TerrainDamage = terrainDamage,
                    WasDestroyed = destroyed,
                    CaughtFire = tile.Fire.IsActive
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Simple line of sight check for explosion blocking.
    /// </summary>
    private bool HasLineOfSight(int x0, int y0, int x1, int y1)
    {
        // Bresenham's line algorithm
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        int x = x0;
        int y = y0;

        while (true)
        {
            // Skip start and end points
            if ((x != x0 || y != y0) && (x != x1 || y != y1))
            {
                if (IsInBounds(x, y))
                {
                    var tile = _tiles[x, y];
                    // Intact walls block explosions
                    if (tile.BlocksSight && tile.State == DestructionState.Intact)
                        return false;
                }
            }

            if (x == x1 && y == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            { err -= dy; x += sx; }
            if (e2 < dx)
            { err += dx; y += sy; }
        }

        return true;
    }

    /// <summary>
    /// Queue a secondary explosion (for chain reactions).
    /// </summary>
    private readonly Queue<(Vector2I pos, ExplosionData explosion)> _pendingExplosions = new();

    private void QueueSecondaryExplosion(int x, int y)
    {
        _pendingExplosions.Enqueue((new Vector2I(x, y), ExplosionData.FuelExplosion));
    }

    /// <summary>
    /// Process any pending chain reaction explosions.
    /// Returns true if there are more to process.
    /// </summary>
    public bool ProcessChainReactions()
    {
        if (_pendingExplosions.Count == 0)
            return false;

        var (pos, explosion) = _pendingExplosions.Dequeue();
        Explode(pos.X, pos.Y, explosion);

        return _pendingExplosions.Count > 0;
    }

    /// <summary>
    /// Update visual effects.
    /// </summary>
    public void UpdateVisuals(float delta)
    {
        for (int i = ActiveVisuals.Count - 1; i >= 0; i--)
        {
            var visual = ActiveVisuals[i];
            visual.Timer += delta;
            ActiveVisuals[i] = visual;

            if (visual.Timer >= visual.Duration)
            {
                ActiveVisuals.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Clear all pending effects.
    /// </summary>
    public void Clear()
    {
        ActiveVisuals.Clear();
        _pendingExplosions.Clear();
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }
}

/// <summary>
/// Result of explosion damage at a single tile.
/// </summary>
public struct ExplosionResult
{
    public Vector2I Position;
    public int Damage;
    public int TerrainDamage;
    public bool WasDestroyed;
    public bool CaughtFire;
}

/// <summary>
/// Visual data for rendering explosion effect.
/// </summary>
public struct ExplosionVisual
{
    public Vector2I Center;
    public int Radius;
    public Color Color;
    public float Duration;
    public float Timer;

    public readonly float Progress => Timer / Duration;
    public readonly bool IsComplete => Timer >= Duration;
}
