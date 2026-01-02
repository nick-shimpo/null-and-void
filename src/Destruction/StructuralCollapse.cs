using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Destruction;

/// <summary>
/// Represents structural support for a tile.
/// Based on Dwarf Fortress cave-in mechanics.
/// </summary>
public struct StructuralSupport
{
    public bool IsStructural;           // Is this a load-bearing element?
    public bool HasFoundation;          // Connected to ground/wall?
    public int SupportStrength;         // How much weight it can hold
    public int CurrentLoad;             // Weight it's currently bearing
    public List<Vector2I>? Connections; // Adjacent structural elements

    public readonly bool IsOverloaded => CurrentLoad > SupportStrength;
    public readonly bool WillCollapse => IsStructural && !HasFoundation && (Connections == null || Connections.Count == 0);
}

/// <summary>
/// Result of a collapse event.
/// </summary>
public struct CollapseResult
{
    public Vector2I Position;
    public int DebrisDamage;
    public bool CreatedDebris;
    public string MaterialName;
}

/// <summary>
/// Manages structural collapse simulation.
/// When supporting structures are destroyed, dependent structures may collapse.
/// </summary>
public class StructuralCollapseSystem
{
    private readonly DestructibleTile[,] _tiles;
    private readonly StructuralSupport[,] _supports;
    private readonly int _width;
    private readonly int _height;
    private readonly Random _random = new();

    // Collapse visual effects
    public List<CollapseVisual> ActiveVisuals { get; } = new();

    // Pending collapses (chain reactions)
    private readonly Queue<Vector2I> _pendingCollapses = new();

    // Collapse damage
    private const int BaseDebrisDamage = 15;
    private const int DebrisDamageVariance = 10;

    public StructuralCollapseSystem(DestructibleTile[,] tiles, int width, int height)
    {
        _tiles = tiles;
        _width = width;
        _height = height;
        _supports = new StructuralSupport[width, height];

        // Initialize support data
        InitializeSupports();
    }

    /// <summary>
    /// Initialize structural support data based on tile properties.
    /// </summary>
    private void InitializeSupports()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                ref var tile = ref _tiles[x, y];
                ref var support = ref _supports[x, y];

                // Walls and pillars are structural
                support.IsStructural = tile.BlocksMovement && tile.Material.Hardness >= 50;

                // Ground level or adjacent to ground = has foundation
                support.HasFoundation = y == _height - 1 || IsAdjacentToFoundation(x, y);

                // Support strength based on material
                support.SupportStrength = tile.Material.Hardness;
                support.CurrentLoad = 0;
            }
        }

        // Build connection graph for structural elements
        BuildConnections();
    }

    /// <summary>
    /// Build connections between structural elements.
    /// </summary>
    private void BuildConnections()
    {
        var directions = new Vector2I[]
        {
            new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
        };

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                ref var support = ref _supports[x, y];
                if (!support.IsStructural)
                    continue;

                support.Connections = new List<Vector2I>();

                foreach (var dir in directions)
                {
                    int nx = x + dir.X;
                    int ny = y + dir.Y;

                    if (IsInBounds(nx, ny) && _supports[nx, ny].IsStructural)
                    {
                        support.Connections.Add(new Vector2I(nx, ny));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check if a position is adjacent to a foundation (ground or strong wall).
    /// </summary>
    private bool IsAdjacentToFoundation(int x, int y)
    {
        var directions = new Vector2I[]
        {
            new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
        };

        foreach (var dir in directions)
        {
            int nx = x + dir.X;
            int ny = y + dir.Y;

            if (!IsInBounds(nx, ny))
                continue;

            // Adjacent to ground
            if (ny == _height - 1)
                return true;

            // Adjacent to intact structural element with foundation
            if (_tiles[nx, ny].State == DestructionState.Intact &&
                _supports[nx, ny].IsStructural &&
                _supports[nx, ny].HasFoundation)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Called when a tile is destroyed. Checks for structural collapse.
    /// </summary>
    public List<CollapseResult> OnTileDestroyed(int x, int y)
    {
        var results = new List<CollapseResult>();

        if (!IsInBounds(x, y))
            return results;

        ref var destroyedSupport = ref _supports[x, y];

        // If this was a structural element, check for dependent collapses
        if (destroyedSupport.IsStructural && destroyedSupport.Connections != null)
        {
            foreach (var connectedPos in destroyedSupport.Connections)
            {
                CheckCollapseChain(connectedPos.X, connectedPos.Y);
            }
        }

        // Clear the destroyed support
        destroyedSupport.IsStructural = false;
        destroyedSupport.HasFoundation = false;
        destroyedSupport.Connections = null;

        // Process pending collapses
        while (_pendingCollapses.Count > 0)
        {
            var pos = _pendingCollapses.Dequeue();
            var result = TriggerCollapse(pos.X, pos.Y);
            if (result.HasValue)
            {
                results.Add(result.Value);
            }
        }

        return results;
    }

    /// <summary>
    /// Check if a tile should collapse (lost all support).
    /// </summary>
    private void CheckCollapseChain(int x, int y)
    {
        if (!IsInBounds(x, y))
            return;

        ref var tile = ref _tiles[x, y];
        ref var support = ref _supports[x, y];

        if (!support.IsStructural)
            return;
        if (tile.State == DestructionState.Destroyed)
            return;

        // Recalculate foundation status
        support.HasFoundation = IsAdjacentToFoundation(x, y);

        // Recalculate connections (remove destroyed connections)
        if (support.Connections != null)
        {
            support.Connections.RemoveAll(pos =>
                !IsInBounds(pos.X, pos.Y) ||
                _tiles[pos.X, pos.Y].State == DestructionState.Destroyed ||
                !_supports[pos.X, pos.Y].IsStructural);
        }

        // Check if should collapse
        if (support.WillCollapse)
        {
            _pendingCollapses.Enqueue(new Vector2I(x, y));
        }
    }

    /// <summary>
    /// Trigger a collapse at the specified position.
    /// </summary>
    private CollapseResult? TriggerCollapse(int x, int y)
    {
        if (!IsInBounds(x, y))
            return null;

        ref var tile = ref _tiles[x, y];
        if (tile.State == DestructionState.Destroyed)
            return null;

        // Destroy the tile
        tile.CurrentHP = 0;
        tile.State = DestructionState.Destroyed;
        tile.BlocksMovement = false;
        tile.BlocksSight = false;

        // Add visual effect
        ActiveVisuals.Add(new CollapseVisual
        {
            Position = new Vector2I(x, y),
            Duration = 0.5f,
            Timer = 0,
            Character = '▼',
            Color = tile.GetCurrentForeground()
        });

        // Check for chain collapses on connected tiles
        var support = _supports[x, y];
        if (support.Connections != null)
        {
            foreach (var connectedPos in support.Connections)
            {
                CheckCollapseChain(connectedPos.X, connectedPos.Y);
            }
        }

        // Calculate debris damage
        int debrisDamage = BaseDebrisDamage + _random.Next(DebrisDamageVariance);

        return new CollapseResult
        {
            Position = new Vector2I(x, y),
            DebrisDamage = debrisDamage,
            CreatedDebris = true,
            MaterialName = tile.Material.Name
        };
    }

    /// <summary>
    /// Force a collapse at a position (e.g., from massive damage).
    /// </summary>
    public CollapseResult? ForceCollapse(int x, int y)
    {
        if (!IsInBounds(x, y))
            return null;

        var result = TriggerCollapse(x, y);

        // Process chain collapses
        var chainResults = new List<CollapseResult>();
        while (_pendingCollapses.Count > 0)
        {
            var pos = _pendingCollapses.Dequeue();
            var chainResult = TriggerCollapse(pos.X, pos.Y);
            if (chainResult.HasValue)
            {
                chainResults.Add(chainResult.Value);
            }
        }

        return result;
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

            if (visual.IsComplete)
            {
                ActiveVisuals.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Rebuild all structural supports (call after major map changes).
    /// </summary>
    public void RebuildSupports()
    {
        InitializeSupports();
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }
}

/// <summary>
/// Visual data for collapse animation.
/// </summary>
public struct CollapseVisual
{
    public Vector2I Position;
    public float Duration;
    public float Timer;
    public char Character;
    public Color Color;

    public readonly float Progress => Timer / Duration;
    public readonly bool IsComplete => Timer >= Duration;

    /// <summary>
    /// Get the falling character for animation.
    /// </summary>
    public readonly char GetAnimatedChar()
    {
        // Show falling debris
        if (Progress < 0.3f)
            return '▼';
        if (Progress < 0.6f)
            return '▽';
        return '░';
    }
}
