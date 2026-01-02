using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Destruction;

/// <summary>
/// Density levels for smoke.
/// </summary>
public enum SmokeDensity
{
    None = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3
}

/// <summary>
/// A single smoke cell in the simulation.
/// </summary>
public struct SmokeCell
{
    public SmokeDensity Density;
    public int RemainingDuration;
    public float AnimationTimer;
    public bool UseAltChar;

    public readonly bool IsActive => Density > SmokeDensity.None;

    public static SmokeCell None => new() { Density = SmokeDensity.None };

    public static SmokeCell Create(SmokeDensity density, int duration = 8)
    {
        return new SmokeCell
        {
            Density = density,
            RemainingDuration = duration,
            AnimationTimer = 0,
            UseAltChar = false
        };
    }

    public readonly char GetCharacter()
    {
        return Density switch
        {
            SmokeDensity.Light => UseAltChar ? '·' : '░',
            SmokeDensity.Medium => UseAltChar ? '░' : '▒',
            SmokeDensity.Heavy => UseAltChar ? '▒' : '▓',
            _ => ' '
        };
    }

    public readonly Color GetColor()
    {
        return Density switch
        {
            SmokeDensity.Light => Color.Color8(120, 120, 120, 180),
            SmokeDensity.Medium => Color.Color8(90, 90, 90, 200),
            SmokeDensity.Heavy => Color.Color8(60, 60, 60, 220),
            _ => Colors.Transparent
        };
    }

    /// <summary>
    /// Smoke reduces visibility.
    /// </summary>
    public readonly int GetVisibilityReduction()
    {
        return Density switch
        {
            SmokeDensity.Light => 1,
            SmokeDensity.Medium => 2,
            SmokeDensity.Heavy => 3,
            _ => 0
        };
    }
}

/// <summary>
/// Simulates smoke rising and dispersing from fire sources.
/// Smoke rises, disperses, and eventually dissipates.
/// </summary>
public class SmokeSimulation
{
    private readonly SmokeCell[,] _cells;
    private readonly int _width;
    private readonly int _height;
    private readonly Random _random = new();

    // Wind affects smoke movement
    public Vector2 WindDirection { get; set; } = new Vector2(0, -1); // Default: rises up
    public float WindStrength { get; set; } = 0.3f;

    // Track active smoke for efficient processing
    private readonly HashSet<Vector2I> _activeSmoke = new();

    public SmokeSimulation(int width, int height)
    {
        _width = width;
        _height = height;
        _cells = new SmokeCell[width, height];

        // Initialize all cells to none
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _cells[x, y] = SmokeCell.None;
    }

    /// <summary>
    /// Add smoke at a position (usually from fire).
    /// </summary>
    public void AddSmoke(int x, int y, SmokeDensity density, int duration = 8)
    {
        if (!IsInBounds(x, y))
            return;

        ref var cell = ref _cells[x, y];

        if (cell.IsActive)
        {
            // Increase density if adding more smoke
            if (density > cell.Density)
            {
                cell.Density = density;
            }
            cell.RemainingDuration = Mathf.Max(cell.RemainingDuration, duration);
        }
        else
        {
            cell = SmokeCell.Create(density, duration);
            _activeSmoke.Add(new Vector2I(x, y));
        }
    }

    /// <summary>
    /// Generate smoke from active fires.
    /// Call this each turn.
    /// </summary>
    public void GenerateFromFires(IEnumerable<Vector2I> firePositions, Func<Vector2I, FireIntensity> getFireIntensity)
    {
        foreach (var pos in firePositions)
        {
            var intensity = getFireIntensity(pos);

            // More intense fires produce more smoke
            SmokeDensity density = intensity switch
            {
                FireIntensity.Inferno => SmokeDensity.Heavy,
                FireIntensity.Blaze => SmokeDensity.Heavy,
                FireIntensity.Flame => SmokeDensity.Medium,
                FireIntensity.Smolder => SmokeDensity.Light,
                _ => SmokeDensity.None
            };

            if (density != SmokeDensity.None)
            {
                // Smoke appears above fire (y-1)
                int smokeY = pos.Y - 1;
                if (_random.NextDouble() < 0.6f) // 60% chance each turn
                {
                    AddSmoke(pos.X, smokeY, density);

                    // Spread slightly horizontally too
                    if (_random.NextDouble() < 0.3f)
                    {
                        int dx = _random.Next(-1, 2);
                        AddSmoke(pos.X + dx, smokeY, (SmokeDensity)Mathf.Max(1, (int)density - 1));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Process one turn of smoke simulation.
    /// </summary>
    public void ProcessTurn()
    {
        var toRemove = new List<Vector2I>();
        var newSmoke = new List<(Vector2I pos, SmokeDensity density, int duration)>();

        foreach (var pos in _activeSmoke)
        {
            ref var cell = ref _cells[pos.X, pos.Y];

            // Reduce duration
            cell.RemainingDuration--;

            if (cell.RemainingDuration <= 0)
            {
                // Smoke dissipates
                cell = SmokeCell.None;
                toRemove.Add(pos);
                continue;
            }

            // Smoke rises and disperses
            if (_random.NextDouble() < 0.5f)
            {
                // Calculate rise direction (mostly up, affected by wind)
                int riseX = pos.X;
                int riseY = pos.Y - 1;

                // Wind effect
                if (WindStrength > 0 && _random.NextDouble() < WindStrength)
                {
                    riseX += (int)Mathf.Round(WindDirection.X);
                    riseY += (int)Mathf.Round(WindDirection.Y);
                }

                // Random drift
                if (_random.NextDouble() < 0.3f)
                {
                    riseX += _random.Next(-1, 2);
                }

                if (IsInBounds(riseX, riseY))
                {
                    // Smoke loses density as it rises
                    SmokeDensity newDensity = (SmokeDensity)Mathf.Max(1, (int)cell.Density - 1);
                    newSmoke.Add((new Vector2I(riseX, riseY), newDensity, cell.RemainingDuration - 1));
                }
            }

            // Natural dispersion (density decreases)
            if (_random.NextDouble() < 0.2f && cell.Density > SmokeDensity.Light)
            {
                cell.Density--;
            }
        }

        // Remove dissipated smoke
        foreach (var pos in toRemove)
        {
            _activeSmoke.Remove(pos);
        }

        // Add risen smoke
        foreach (var (pos, density, duration) in newSmoke)
        {
            if (duration > 0)
            {
                AddSmoke(pos.X, pos.Y, density, duration);
            }
        }
    }

    /// <summary>
    /// Update animation timers.
    /// </summary>
    public void UpdateAnimations(float delta)
    {
        foreach (var pos in _activeSmoke)
        {
            ref var cell = ref _cells[pos.X, pos.Y];
            cell.AnimationTimer += delta;

            if (cell.AnimationTimer >= 0.3f)
            {
                cell.AnimationTimer = 0;
                cell.UseAltChar = !cell.UseAltChar;
            }
        }
    }

    /// <summary>
    /// Get smoke cell at position.
    /// </summary>
    public SmokeCell GetSmoke(int x, int y)
    {
        if (!IsInBounds(x, y))
            return SmokeCell.None;
        return _cells[x, y];
    }

    /// <summary>
    /// Get all active smoke positions.
    /// </summary>
    public IEnumerable<Vector2I> GetActiveSmokePositions()
    {
        return _activeSmoke;
    }

    /// <summary>
    /// Clear smoke in a radius (e.g., from wind gust or explosion).
    /// </summary>
    public void ClearRadius(int centerX, int centerY, int radius)
    {
        var toClear = new List<Vector2I>();

        foreach (var pos in _activeSmoke)
        {
            float dist = pos.DistanceTo(new Vector2I(centerX, centerY));
            if (dist <= radius)
            {
                toClear.Add(pos);
            }
        }

        foreach (var pos in toClear)
        {
            _cells[pos.X, pos.Y] = SmokeCell.None;
            _activeSmoke.Remove(pos);
        }
    }

    /// <summary>
    /// Get total visibility reduction at a position (for FOV calculations).
    /// </summary>
    public int GetVisibilityReduction(int x, int y)
    {
        if (!IsInBounds(x, y))
            return 0;
        return _cells[x, y].GetVisibilityReduction();
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }
}
