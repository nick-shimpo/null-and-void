using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.AI.Behaviors;

/// <summary>
/// Wander randomly when nothing else to do.
/// Low priority fallback behavior.
/// </summary>
public class WanderBehavior : IBehavior
{
    public string Name => "Wander";
    public int Priority { get; set; } = BehaviorPriorities.Wander;

    /// <summary>
    /// Chance to move each turn (0.0 to 1.0).
    /// </summary>
    public float MoveChance { get; set; } = 0.5f;

    /// <summary>
    /// Whether to allow diagonal movement.
    /// </summary>
    public bool AllowDiagonal { get; set; } = true;

    private static readonly Vector2I[] _cardinalDirections = new[]
    {
        new Vector2I(0, -1),  // Up
        new Vector2I(0, 1),   // Down
        new Vector2I(-1, 0),  // Left
        new Vector2I(1, 0)    // Right
    };

    private static readonly Vector2I[] _allDirections = new[]
    {
        new Vector2I(0, -1),   // Up
        new Vector2I(0, 1),    // Down
        new Vector2I(-1, 0),   // Left
        new Vector2I(1, 0),    // Right
        new Vector2I(-1, -1),  // Up-Left
        new Vector2I(1, -1),   // Up-Right
        new Vector2I(-1, 1),   // Down-Left
        new Vector2I(1, 1)     // Down-Right
    };

    public WanderBehavior() { }

    public WanderBehavior(float moveChance, bool allowDiagonal = true)
    {
        MoveChance = moveChance;
        AllowDiagonal = allowDiagonal;
    }

    public bool CanExecute(BehaviorContext context)
    {
        // Always can execute - this is the fallback behavior
        return true;
    }

    public async Task<BehaviorResult> Execute(BehaviorContext context)
    {
        var self = context.Self;

        // Random chance to stay still
        if (GD.Randf() > MoveChance)
        {
            await Task.CompletedTask;
            return new BehaviorResult(true, ActionCosts.Wait, "Standing still");
        }

        // Pick random direction
        var directions = AllowDiagonal ? _allDirections : _cardinalDirections;

        // Shuffle directions to try them in random order
        var shuffled = ShuffleArray(directions);

        foreach (var direction in shuffled)
        {
            var newPos = self.GridPosition + direction;
            if (context.IsPositionFree(newPos))
            {
                self.Move(direction);
                // Note: Animation delay removed - handled by TurnAnimator batching
                return new BehaviorResult(true, ActionCosts.Move, "Wandering");
            }
        }

        // Couldn't find a valid direction - stay still
        await Task.CompletedTask;
        return new BehaviorResult(true, ActionCosts.Wait, "Blocked - standing still");
    }

    /// <summary>
    /// Shuffle an array using Fisher-Yates.
    /// </summary>
    private static Vector2I[] ShuffleArray(Vector2I[] array)
    {
        var result = new Vector2I[array.Length];
        array.CopyTo(result, 0);

        for (int i = result.Length - 1; i > 0; i--)
        {
            int j = GD.RandRange(0, i);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }
}
