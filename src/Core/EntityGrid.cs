using System.Collections.Generic;
using Godot;

namespace NullAndVoid.Core;

/// <summary>
/// Spatial hash grid for O(1) entity position lookups.
/// Replaces O(N) group iteration for collision detection.
/// </summary>
public class EntityGrid
{
    private static EntityGrid? _instance;
    public static EntityGrid Instance => _instance ??= new EntityGrid();

    private readonly Dictionary<Vector2I, IGridEntity> _grid = new();

    /// <summary>
    /// Register an entity at a position.
    /// </summary>
    public void Register(IGridEntity entity, Vector2I position)
    {
        _grid[position] = entity;
    }

    /// <summary>
    /// Unregister an entity from a position.
    /// Only removes if the entity at this position matches (or no entity check needed).
    /// </summary>
    public void Unregister(Vector2I position, IGridEntity? entity = null)
    {
        // If entity specified, only remove if it's actually at this position
        if (entity != null)
        {
            if (_grid.TryGetValue(position, out var occupant) && occupant == entity)
            {
                _grid.Remove(position);
            }
        }
        else
        {
            _grid.Remove(position);
        }
    }

    /// <summary>
    /// Move an entity from one position to another.
    /// Only removes from 'from' if this entity is actually there.
    /// </summary>
    public void Move(IGridEntity entity, Vector2I from, Vector2I to)
    {
        // Only remove from old position if this entity is actually there
        if (_grid.TryGetValue(from, out var occupant) && occupant == entity)
        {
            _grid.Remove(from);
        }
        _grid[to] = entity;
    }

    /// <summary>
    /// Get the entity at a position, or null if empty.
    /// O(1) lookup.
    /// </summary>
    public IGridEntity? GetAt(Vector2I position)
    {
        return _grid.GetValueOrDefault(position);
    }

    /// <summary>
    /// Check if a position is occupied by any entity.
    /// O(1) lookup.
    /// </summary>
    public bool IsOccupied(Vector2I position)
    {
        return _grid.ContainsKey(position);
    }

    /// <summary>
    /// Check if a position is occupied by an entity other than the specified one.
    /// Useful for checking if movement would cause a collision.
    /// </summary>
    public bool IsOccupiedByOther(Vector2I position, IGridEntity? excludeEntity)
    {
        if (!_grid.TryGetValue(position, out var occupant))
            return false;

        return occupant != excludeEntity;
    }

    /// <summary>
    /// Get all entities within a radius of a position.
    /// </summary>
    public IEnumerable<IGridEntity> GetEntitiesInRadius(Vector2I center, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                var pos = new Vector2I(center.X + dx, center.Y + dy);
                if (_grid.TryGetValue(pos, out var entity))
                {
                    yield return entity;
                }
            }
        }
    }

    /// <summary>
    /// Clear all registrations (for level transitions).
    /// </summary>
    public void Clear()
    {
        _grid.Clear();
    }

    /// <summary>
    /// Get count of registered entities.
    /// </summary>
    public int Count => _grid.Count;

    /// <summary>
    /// Reset the singleton instance (for testing/restarting).
    /// </summary>
    public static void Reset()
    {
        _instance = null;
    }
}
