using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Destruction;
using NullAndVoid.World;

namespace NullAndVoid.AI;

/// <summary>
/// A* pathfinding implementation for grid-based movement.
/// </summary>
public static class Pathfinding
{
    /// <summary>
    /// Maximum iterations to prevent infinite loops on complex maps.
    /// </summary>
    private const int MaxIterations = 1000;

    /// <summary>
    /// All 8 directions (including diagonals).
    /// </summary>
    private static readonly Vector2I[] _directions = new[]
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

    /// <summary>
    /// Cardinal directions only (no diagonals).
    /// </summary>
    private static readonly Vector2I[] _cardinalDirections = new[]
    {
        new Vector2I(0, -1),  // Up
        new Vector2I(0, 1),   // Down
        new Vector2I(-1, 0),  // Left
        new Vector2I(1, 0)    // Right
    };

    /// <summary>
    /// Find a path from start to goal using A*.
    /// </summary>
    /// <param name="start">Starting position.</param>
    /// <param name="goal">Goal position.</param>
    /// <param name="tileMap">Tile map for walkability checks.</param>
    /// <param name="allowDiagonal">Whether to allow diagonal movement.</param>
    /// <returns>List of positions from start to goal, or null if no path found.</returns>
    public static List<Vector2I>? FindPath(Vector2I start, Vector2I goal, TileMapManager tileMap, bool allowDiagonal = true)
    {
        return FindPath(start, goal, pos => tileMap.IsWalkable(pos), allowDiagonal);
    }

    /// <summary>
    /// Find a path from start to goal using A*.
    /// </summary>
    /// <param name="start">Starting position.</param>
    /// <param name="goal">Goal position.</param>
    /// <param name="isWalkable">Function to check if a position is walkable.</param>
    /// <param name="allowDiagonal">Whether to allow diagonal movement.</param>
    /// <returns>List of positions from start to goal, or null if no path found.</returns>
    public static List<Vector2I>? FindPath(Vector2I start, Vector2I goal, Func<Vector2I, bool> isWalkable, bool allowDiagonal = true)
    {
        // Quick check - if goal is not walkable, no path possible
        if (!isWalkable(goal))
            return null;

        var openSet = new PriorityQueue<Vector2I, float>();
        var cameFrom = new Dictionary<Vector2I, Vector2I>();
        var gScore = new Dictionary<Vector2I, float> { [start] = 0 };
        var fScore = new Dictionary<Vector2I, float> { [start] = Heuristic(start, goal) };

        openSet.Enqueue(start, fScore[start]);
        var inOpenSet = new HashSet<Vector2I> { start };

        int iterations = 0;
        var directions = allowDiagonal ? _directions : _cardinalDirections;

        while (openSet.Count > 0 && iterations < MaxIterations)
        {
            iterations++;

            var current = openSet.Dequeue();
            inOpenSet.Remove(current);

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var dir in directions)
            {
                var neighbor = current + dir;

                // Skip if not walkable
                if (!isWalkable(neighbor))
                    continue;

                // Calculate movement cost (diagonal costs more)
                float moveCost = (dir.X != 0 && dir.Y != 0) ? 1.414f : 1.0f;
                float tentativeG = gScore[current] + moveCost;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!inOpenSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                        inOpenSet.Add(neighbor);
                    }
                }
            }
        }

        // No path found
        return null;
    }

    /// <summary>
    /// Get just the next step toward the goal.
    /// More efficient than full pathfinding when you only need one step.
    /// </summary>
    /// <param name="start">Starting position.</param>
    /// <param name="goal">Goal position.</param>
    /// <param name="tileMap">Tile map for walkability checks.</param>
    /// <returns>The next position to move to, or null if no path.</returns>
    public static Vector2I? GetNextStep(Vector2I start, Vector2I goal, TileMapManager tileMap)
    {
        return GetNextStep(start, goal, pos => tileMap.IsWalkable(pos));
    }

    /// <summary>
    /// Get just the next step toward the goal.
    /// </summary>
    public static Vector2I? GetNextStep(Vector2I start, Vector2I goal, Func<Vector2I, bool> isWalkable)
    {
        var path = FindPath(start, goal, isWalkable);
        if (path == null || path.Count < 2)
            return null;

        return path[1]; // Return second element (first is start)
    }

    /// <summary>
    /// Get the best direction to flee from a threat.
    /// </summary>
    /// <param name="position">Current position.</param>
    /// <param name="threat">Position to flee from.</param>
    /// <param name="tileMap">Tile map for walkability checks.</param>
    /// <returns>Best direction to move away from threat.</returns>
    public static Vector2I GetFleeDirection(Vector2I position, Vector2I threat, TileMapManager tileMap)
    {
        return GetFleeDirection(position, threat, pos => tileMap.IsWalkable(pos));
    }

    /// <summary>
    /// Get the best direction to flee from a threat.
    /// </summary>
    public static Vector2I GetFleeDirection(Vector2I position, Vector2I threat, Func<Vector2I, bool> isWalkable)
    {
        Vector2I bestDir = Vector2I.Zero;
        float bestScore = float.MinValue;

        foreach (var dir in _directions)
        {
            var newPos = position + dir;

            if (!isWalkable(newPos))
                continue;

            // Score is distance from threat (higher is better)
            float distance = LineOfSight.EuclideanDistance(newPos, threat);
            if (distance > bestScore)
            {
                bestScore = distance;
                bestDir = dir;
            }
        }

        return bestDir;
    }

    /// <summary>
    /// Find a position at approximately the given range from target.
    /// Useful for ranged attackers maintaining distance.
    /// </summary>
    /// <param name="position">Current position.</param>
    /// <param name="target">Target position to maintain distance from.</param>
    /// <param name="desiredRange">Desired range from target.</param>
    /// <param name="tileMap">Tile map for walkability checks.</param>
    /// <returns>Best adjacent position to move to, or null.</returns>
    public static Vector2I? GetPositionAtRange(Vector2I position, Vector2I target, int desiredRange, TileMapManager tileMap)
    {
        float currentDist = LineOfSight.EuclideanDistance(position, target);

        Vector2I? bestPos = null;
        float bestDiff = float.MaxValue;

        foreach (var dir in _directions)
        {
            var newPos = position + dir;

            if (!tileMap.IsWalkable(newPos))
                continue;

            float newDist = LineOfSight.EuclideanDistance(newPos, target);
            float diff = Mathf.Abs(newDist - desiredRange);

            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestPos = newPos;
            }
        }

        return bestPos;
    }

    /// <summary>
    /// Find a flanking position (to the side or behind the target).
    /// </summary>
    /// <param name="position">Current position.</param>
    /// <param name="target">Target position.</param>
    /// <param name="targetFacing">Direction the target is facing (optional).</param>
    /// <param name="tileMap">Tile map for walkability checks.</param>
    /// <returns>Best flanking position, or null if none found.</returns>
    public static Vector2I? GetFlankingPosition(Vector2I position, Vector2I target, Vector2I? targetFacing, TileMapManager tileMap)
    {
        // If we don't know where target is facing, just pick a random adjacent position
        if (!targetFacing.HasValue)
        {
            foreach (var dir in _directions)
            {
                var pos = target + dir;
                if (tileMap.IsWalkable(pos) && pos != position)
                {
                    return pos;
                }
            }
            return null;
        }

        // Prefer positions perpendicular to or behind the target's facing
        var facing = targetFacing.Value;
        var perpendicular1 = new Vector2I(-facing.Y, facing.X);
        var perpendicular2 = new Vector2I(facing.Y, -facing.X);
        var behind = new Vector2I(-facing.X, -facing.Y);

        // Priority: behind > perpendicular
        var preferred = new[] { behind, perpendicular1, perpendicular2 };

        foreach (var dir in preferred)
        {
            var pos = target + dir;
            if (tileMap.IsWalkable(pos))
            {
                return pos;
            }
        }

        return null;
    }

    #region Fire-Aware Pathfinding

    /// <summary>
    /// Cost added to tiles that are on fire.
    /// </summary>
    public const float FireCost = 10.0f;

    /// <summary>
    /// Cost added to tiles that have smoke.
    /// </summary>
    public const float SmokeCost = 2.0f;

    /// <summary>
    /// Find a path that avoids fire when possible.
    /// </summary>
    public static List<Vector2I>? FindPathAvoidingFire(
        Vector2I start,
        Vector2I goal,
        TileMapManager tileMap,
        GameMap? gameMap,
        bool allowDiagonal = true)
    {
        if (gameMap == null)
            return FindPath(start, goal, tileMap, allowDiagonal);

        return FindPathWithCosts(
            start,
            goal,
            pos => tileMap.IsWalkable(pos),
            pos => GetFireCost(pos, gameMap),
            allowDiagonal
        );
    }

    /// <summary>
    /// Find a path using custom walkability and cost functions.
    /// </summary>
    public static List<Vector2I>? FindPathWithCosts(
        Vector2I start,
        Vector2I goal,
        Func<Vector2I, bool> isWalkable,
        Func<Vector2I, float> getExtraCost,
        bool allowDiagonal = true)
    {
        if (!isWalkable(goal))
            return null;

        var openSet = new PriorityQueue<Vector2I, float>();
        var cameFrom = new Dictionary<Vector2I, Vector2I>();
        var gScore = new Dictionary<Vector2I, float> { [start] = 0 };
        var fScore = new Dictionary<Vector2I, float> { [start] = Heuristic(start, goal) };

        openSet.Enqueue(start, fScore[start]);
        var inOpenSet = new HashSet<Vector2I> { start };

        int iterations = 0;
        var directions = allowDiagonal ? _directions : _cardinalDirections;

        while (openSet.Count > 0 && iterations < MaxIterations)
        {
            iterations++;

            var current = openSet.Dequeue();
            inOpenSet.Remove(current);

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var dir in directions)
            {
                var neighbor = current + dir;

                if (!isWalkable(neighbor))
                    continue;

                // Base movement cost + extra cost from hazards
                float moveCost = (dir.X != 0 && dir.Y != 0) ? 1.414f : 1.0f;
                float extraCost = getExtraCost(neighbor);
                float tentativeG = gScore[current] + moveCost + extraCost;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!inOpenSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                        inOpenSet.Add(neighbor);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get the next step toward goal while avoiding fire.
    /// </summary>
    public static Vector2I? GetNextStepAvoidingFire(
        Vector2I start,
        Vector2I goal,
        TileMapManager tileMap,
        GameMap? gameMap)
    {
        var path = FindPathAvoidingFire(start, goal, tileMap, gameMap);
        if (path == null || path.Count < 2)
            return null;

        return path[1];
    }

    /// <summary>
    /// Get the extra pathfinding cost for a tile based on fire/smoke.
    /// </summary>
    public static float GetFireCost(Vector2I pos, GameMap gameMap)
    {
        float cost = 0f;

        // Check for fire
        if (gameMap.HasFireAt(pos))
        {
            int fireDamage = gameMap.GetFireDamageAt(pos);
            // Higher damage fire = higher cost
            cost += FireCost * (1 + fireDamage / 5f);
        }

        return cost;
    }

    /// <summary>
    /// Get flee direction that also avoids fire.
    /// </summary>
    public static Vector2I GetFleeDirectionAvoidingFire(
        Vector2I position,
        Vector2I threat,
        TileMapManager tileMap,
        GameMap? gameMap)
    {
        Vector2I bestDir = Vector2I.Zero;
        float bestScore = float.MinValue;

        foreach (var dir in _directions)
        {
            var newPos = position + dir;

            if (!tileMap.IsWalkable(newPos))
                continue;

            // Score is distance from threat minus fire penalty
            float distance = LineOfSight.EuclideanDistance(newPos, threat);
            float firePenalty = gameMap != null ? GetFireCost(newPos, gameMap) : 0f;
            float score = distance - firePenalty;

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }

        return bestDir;
    }

    #endregion

    /// <summary>
    /// Heuristic function for A* (Euclidean distance).
    /// </summary>
    private static float Heuristic(Vector2I a, Vector2I b)
    {
        // Euclidean distance works well for 8-directional movement
        int dx = b.X - a.X;
        int dy = b.Y - a.Y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Reconstruct path from A* search result.
    /// </summary>
    private static List<Vector2I> ReconstructPath(Dictionary<Vector2I, Vector2I> cameFrom, Vector2I current)
    {
        var path = new List<Vector2I> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
