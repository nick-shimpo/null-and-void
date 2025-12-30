using Godot;
using System;
using NullAndVoid.Core;

namespace NullAndVoid.Entities;

/// <summary>
/// Base class for all game entities (player, enemies, items, etc.)
/// </summary>
public partial class Entity : Node2D
{
    [Export] public string EntityName { get; set; } = "Entity";
    [Export] public Vector2I GridPosition { get; set; } = Vector2I.Zero;

    [Export] public int TileSize { get; set; } = 32;

    public override void _Ready()
    {
        UpdateVisualPosition();
    }

    /// <summary>
    /// Moves the entity to a new grid position.
    /// </summary>
    public virtual bool MoveTo(Vector2I newPosition)
    {
        var oldPosition = GridPosition;
        GridPosition = newPosition;
        UpdateVisualPosition();

        EventBus.Instance.EmitEntityMoved(this, oldPosition, newPosition);
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
    protected void UpdateVisualPosition()
    {
        Position = new Vector2(
            GridPosition.X * TileSize + TileSize / 2,
            GridPosition.Y * TileSize + TileSize / 2
        );
    }

    /// <summary>
    /// Called when the entity is spawned into the world.
    /// </summary>
    public virtual void OnSpawn()
    {
        EventBus.Instance.EmitEntitySpawned(this);
    }

    /// <summary>
    /// Called when the entity is destroyed/removed from the world.
    /// </summary>
    public virtual void OnDestroy()
    {
        EventBus.Instance.EmitEntityDestroyed(this);
    }

    public override void _ExitTree()
    {
        OnDestroy();
    }
}
