using System;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.Entities;

/// <summary>
/// Base class for all game entities (player, enemies, items, etc.)
/// </summary>
public partial class Entity : Node2D, IGridEntity
{
    [Export] public string EntityName { get; set; } = "Entity";
    [Export] public Vector2I GridPosition { get; set; } = Vector2I.Zero;

    [Export] public int TileSize { get; set; } = 32;

    private readonly string _id = Guid.NewGuid().ToString();

    /// <summary>
    /// Unique identifier for spatial grid tracking.
    /// </summary>
    public string Id => _id;

    public override void _Ready()
    {
        UpdateVisualPosition();

        // Register with spatial grid for O(1) collision lookups
        EntityGrid.Instance.Register(this, GridPosition);
    }

    /// <summary>
    /// Moves the entity to a new grid position.
    /// </summary>
    public virtual bool MoveTo(Vector2I newPosition)
    {
        var oldPosition = GridPosition;
        GridPosition = newPosition;
        UpdateVisualPosition();

        // Update spatial grid for O(1) collision lookups
        EntityGrid.Instance.Move(this, oldPosition, newPosition);

        try
        {
            EventBus.Instance.EmitEntityMoved(this, oldPosition, newPosition);
        }
        catch (InvalidOperationException)
        {
            // EventBus not initialized - skip event emission
        }
        return true;
    }

    /// <summary>
    /// Attempts to move the entity by the given offset.
    /// </summary>
    public virtual bool Move(Vector2I offset)
    {
        return MoveTo(GridPosition + offset);
    }

    /// <summary>
    /// Updates the visual (pixel) position based on the grid position.
    /// </summary>
    public void UpdateVisualPosition()
    {
        Position = new Vector2(
            GridPosition.X * TileSize + TileSize / 2,
            GridPosition.Y * TileSize + TileSize / 2
        );
    }

    /// <summary>
    /// Called when the entity is spawned into the world.
    /// Note: EntityGrid registration now happens in _Ready() automatically.
    /// </summary>
    public virtual void OnSpawn()
    {
        try
        {
            EventBus.Instance.EmitEntitySpawned(this);
        }
        catch (InvalidOperationException)
        {
            // EventBus not initialized - skip event emission
        }
    }

    /// <summary>
    /// Called when the entity is destroyed/removed from the world.
    /// </summary>
    public virtual void OnDestroy()
    {
        // Unregister from spatial grid (only if we're actually at this position)
        EntityGrid.Instance.Unregister(GridPosition, this);

        try
        {
            EventBus.Instance.EmitEntityDestroyed(this);
        }
        catch (InvalidOperationException)
        {
            // EventBus not initialized or already freed - skip event emission
        }
    }

    public override void _ExitTree()
    {
        OnDestroy();
    }
}
