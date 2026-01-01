namespace NullAndVoid.Core;

/// <summary>
/// Interface for entities that can be tracked by the spatial grid.
/// Primarily used for decoupling EntityGrid from Godot's Node2D.
/// </summary>
public interface IGridEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    string Id { get; }
}
