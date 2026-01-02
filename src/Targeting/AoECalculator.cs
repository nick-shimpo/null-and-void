using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NullAndVoid.World;

namespace NullAndVoid.Targeting;

/// <summary>
/// Shape types for area of effect attacks.
/// </summary>
public enum AoEShape
{
    /// <summary>Square/diamond area centered on target.</summary>
    Circle,

    /// <summary>Cone expanding from origin toward target.</summary>
    Cone,

    /// <summary>Straight line from origin through target.</summary>
    Line,

    /// <summary>Cross pattern (+) centered on target.</summary>
    Cross,

    /// <summary>Ring at specific distance from center.</summary>
    Ring,

    /// <summary>Chain that jumps between nearby targets.</summary>
    Chain
}

/// <summary>
/// Distance calculation method for area effects.
/// </summary>
public enum AoEDistanceType
{
    /// <summary>Chebyshev distance (diagonal = 1). Creates square areas.</summary>
    Chebyshev,

    /// <summary>Manhattan distance (diagonal = 2). Creates diamond areas.</summary>
    Manhattan,

    /// <summary>Euclidean distance (true circle).</summary>
    Euclidean
}

/// <summary>
/// Information about a tile affected by an AoE attack.
/// </summary>
public struct AoETileInfo
{
    /// <summary>Position of the tile.</summary>
    public Vector2I Position;

    /// <summary>Distance from center (for damage falloff).</summary>
    public int Distance;

    /// <summary>Damage multiplier based on distance (1.0 at center).</summary>
    public float DamageMultiplier;

    /// <summary>Whether line of sight from origin is blocked.</summary>
    public bool IsBlocked;

    /// <summary>Whether this tile contains an enemy.</summary>
    public bool HasEnemy;

    /// <summary>Whether this tile contains the player.</summary>
    public bool HasPlayer;

    /// <summary>Whether this tile is the center of the effect.</summary>
    public bool IsCenter;
}

/// <summary>
/// Result of an AoE calculation.
/// </summary>
public class AoEResult
{
    /// <summary>Center position of the effect.</summary>
    public Vector2I Center { get; set; }

    /// <summary>Origin position (attacker location).</summary>
    public Vector2I Origin { get; set; }

    /// <summary>Shape of the AoE.</summary>
    public AoEShape Shape { get; set; }

    /// <summary>Radius of the effect.</summary>
    public int Radius { get; set; }

    /// <summary>All affected tiles with their info.</summary>
    public List<AoETileInfo> AffectedTiles { get; set; } = new();

    /// <summary>Number of enemies that will be hit.</summary>
    public int EnemyCount => AffectedTiles.Count(t => t.HasEnemy && !t.IsBlocked);

    /// <summary>Whether player will be hit (friendly fire).</summary>
    public bool HitsPlayer => AffectedTiles.Any(t => t.HasPlayer && !t.IsBlocked);

    /// <summary>Total tiles affected.</summary>
    public int TileCount => AffectedTiles.Count(t => !t.IsBlocked);

    /// <summary>Get positions only (for simple iteration).</summary>
    public IEnumerable<Vector2I> GetPositions() => AffectedTiles.Select(t => t.Position);

    /// <summary>Get unblocked positions only.</summary>
    public IEnumerable<Vector2I> GetUnblockedPositions() =>
        AffectedTiles.Where(t => !t.IsBlocked).Select(t => t.Position);
}

/// <summary>
/// Calculates area of effect for weapons and abilities.
/// Supports multiple shapes, damage falloff, and line of sight blocking.
/// </summary>
public static class AoECalculator
{
    /// <summary>
    /// Calculate a circular/square AoE centered on a position.
    /// </summary>
    public static AoEResult CalculateCircle(
        Vector2I center,
        int radius,
        Vector2I origin,
        AoEDistanceType distanceType = AoEDistanceType.Chebyshev,
        bool checkLineOfSight = false,
        bool hasFalloff = true)
    {
        var result = new AoEResult
        {
            Center = center,
            Origin = origin,
            Shape = AoEShape.Circle,
            Radius = radius
        };

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                var pos = center + new Vector2I(dx, dy);
                int distance = GetDistance(dx, dy, distanceType);

                if (distance > radius)
                    continue;

                var tileInfo = CreateTileInfo(pos, center, distance, radius, hasFalloff, checkLineOfSight, origin);
                result.AffectedTiles.Add(tileInfo);
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate a cone AoE from origin toward target direction.
    /// </summary>
    public static AoEResult CalculateCone(
        Vector2I origin,
        Vector2I target,
        int length,
        int spreadAngle = 45,
        bool checkLineOfSight = false)
    {
        var result = new AoEResult
        {
            Center = target,
            Origin = origin,
            Shape = AoEShape.Cone,
            Radius = length
        };

        // Calculate direction angle
        var direction = target - origin;
        float baseAngle = Mathf.Atan2(direction.Y, direction.X);
        float halfSpread = Mathf.DegToRad(spreadAngle / 2f);

        // Check all tiles in a bounding box
        for (int dx = -length; dx <= length; dx++)
        {
            for (int dy = -length; dy <= length; dy++)
            {
                var pos = origin + new Vector2I(dx, dy);
                if (pos == origin)
                    continue;

                // Check if within cone
                float angle = Mathf.Atan2(dy, dx);
                float angleDiff = Mathf.Abs(NormalizeAngle(angle - baseAngle));

                if (angleDiff > halfSpread)
                    continue;

                int distance = (int)Mathf.Sqrt(dx * dx + dy * dy);
                if (distance > length || distance == 0)
                    continue;

                var tileInfo = CreateTileInfo(pos, origin, distance, length, true, checkLineOfSight, origin);
                result.AffectedTiles.Add(tileInfo);
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate a line AoE from origin through target.
    /// </summary>
    public static AoEResult CalculateLine(
        Vector2I origin,
        Vector2I target,
        int length,
        int width = 1,
        bool pierce = true)
    {
        var result = new AoEResult
        {
            Center = target,
            Origin = origin,
            Shape = AoEShape.Line,
            Radius = length
        };

        // Get line points using Bresenham
        var linePoints = LineOfFire.GetLine(origin, target);

        // Extend line beyond target if needed
        if (pierce && linePoints.Count > 1)
        {
            var direction = target - origin;
            float angle = Mathf.Atan2(direction.Y, direction.X);

            for (int i = linePoints.Count; i <= length; i++)
            {
                int dx = (int)Mathf.Round(Mathf.Cos(angle) * i);
                int dy = (int)Mathf.Round(Mathf.Sin(angle) * i);
                var pos = origin + new Vector2I(dx, dy);

                if (!linePoints.Contains(pos))
                    linePoints.Add(pos);
            }
        }

        // Add tiles along the line (and width if > 1)
        var addedPositions = new HashSet<Vector2I>();

        foreach (var linePos in linePoints.Take(length + 1))
        {
            if (linePos == origin)
                continue;

            // Add perpendicular tiles for width
            for (int w = -(width / 2); w <= width / 2; w++)
            {
                Vector2I pos;
                if (w == 0)
                {
                    pos = linePos;
                }
                else
                {
                    // Calculate perpendicular offset
                    var direction = target - origin;
                    float angle = Mathf.Atan2(direction.Y, direction.X) + Mathf.Pi / 2;
                    int dx = (int)Mathf.Round(Mathf.Cos(angle) * w);
                    int dy = (int)Mathf.Round(Mathf.Sin(angle) * w);
                    pos = linePos + new Vector2I(dx, dy);
                }

                if (addedPositions.Contains(pos))
                    continue;

                addedPositions.Add(pos);
                int distance = LineOfFire.GetDistance(origin, pos);
                var tileInfo = CreateTileInfo(pos, origin, distance, length, false, false, origin);
                result.AffectedTiles.Add(tileInfo);
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate a cross (+) AoE centered on target.
    /// </summary>
    public static AoEResult CalculateCross(
        Vector2I center,
        int armLength,
        Vector2I origin,
        bool includeDiagonals = false)
    {
        var result = new AoEResult
        {
            Center = center,
            Origin = origin,
            Shape = AoEShape.Cross,
            Radius = armLength
        };

        // Center tile
        var centerInfo = CreateTileInfo(center, center, 0, armLength, true, false, origin);
        centerInfo.IsCenter = true;
        result.AffectedTiles.Add(centerInfo);

        // Cardinal directions
        Vector2I[] cardinals = { new(0, -1), new(0, 1), new(-1, 0), new(1, 0) };
        Vector2I[] diagonals = { new(-1, -1), new(1, -1), new(-1, 1), new(1, 1) };

        var directions = includeDiagonals
            ? cardinals.Concat(diagonals).ToArray()
            : cardinals;

        foreach (var dir in directions)
        {
            for (int i = 1; i <= armLength; i++)
            {
                var pos = center + dir * i;
                var tileInfo = CreateTileInfo(pos, center, i, armLength, true, false, origin);
                result.AffectedTiles.Add(tileInfo);
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate a ring AoE at specific distance from center.
    /// </summary>
    public static AoEResult CalculateRing(
        Vector2I center,
        int innerRadius,
        int outerRadius,
        Vector2I origin)
    {
        var result = new AoEResult
        {
            Center = center,
            Origin = origin,
            Shape = AoEShape.Ring,
            Radius = outerRadius
        };

        for (int dx = -outerRadius; dx <= outerRadius; dx++)
        {
            for (int dy = -outerRadius; dy <= outerRadius; dy++)
            {
                int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));

                if (distance < innerRadius || distance > outerRadius)
                    continue;

                var pos = center + new Vector2I(dx, dy);
                var tileInfo = CreateTileInfo(pos, center, distance - innerRadius, outerRadius - innerRadius, true, false, origin);
                result.AffectedTiles.Add(tileInfo);
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate chain lightning targets.
    /// Finds nearby enemies to chain to.
    /// </summary>
    public static AoEResult CalculateChain(
        Vector2I origin,
        Vector2I firstTarget,
        int maxJumps,
        int jumpRange,
        SceneTree sceneTree)
    {
        var result = new AoEResult
        {
            Center = firstTarget,
            Origin = origin,
            Shape = AoEShape.Chain,
            Radius = maxJumps
        };

        var hitTargets = new HashSet<Vector2I> { firstTarget };
        var currentPos = firstTarget;

        // First target
        var firstInfo = CreateTileInfo(firstTarget, origin, 1, maxJumps + 1, true, false, origin);
        firstInfo.HasEnemy = true;
        result.AffectedTiles.Add(firstInfo);

        // Find chain targets
        for (int jump = 1; jump < maxJumps; jump++)
        {
            var nextTarget = FindNearestEnemy(currentPos, jumpRange, hitTargets, sceneTree);
            if (nextTarget == null)
                break;

            hitTargets.Add(nextTarget.Value);
            var tileInfo = CreateTileInfo(nextTarget.Value, origin, jump + 1, maxJumps + 1, true, false, origin);
            tileInfo.HasEnemy = true;
            result.AffectedTiles.Add(tileInfo);

            currentPos = nextTarget.Value;
        }

        return result;
    }

    /// <summary>
    /// Create tile info with entity detection.
    /// </summary>
    private static AoETileInfo CreateTileInfo(
        Vector2I position,
        Vector2I center,
        int distance,
        int maxDistance,
        bool hasFalloff,
        bool checkLoS,
        Vector2I origin)
    {
        float damageMultiplier = 1.0f;
        if (hasFalloff && maxDistance > 0)
        {
            // Linear falloff from center
            damageMultiplier = 1.0f - ((float)distance / (maxDistance + 1));
        }

        bool isBlocked = false;
        if (checkLoS)
        {
            var lof = LineOfFire.Check(origin, position, maxDistance + 10);
            isBlocked = lof.Result == LineOfFireResult.Blocked;
        }

        return new AoETileInfo
        {
            Position = position,
            Distance = distance,
            DamageMultiplier = damageMultiplier,
            IsBlocked = isBlocked,
            IsCenter = position == center,
            HasEnemy = false,  // Set externally
            HasPlayer = false  // Set externally
        };
    }

    /// <summary>
    /// Update AoE result with entity positions.
    /// </summary>
    public static void UpdateEntityInfo(AoEResult result, Vector2I playerPos, SceneTree sceneTree)
    {
        var enemies = sceneTree.GetNodesInGroup("Enemies");
        var enemyPositions = new HashSet<Vector2I>();

        foreach (var node in enemies)
        {
            if (node is Entities.Entity entity)
            {
                enemyPositions.Add(entity.GridPosition);
            }
        }

        for (int i = 0; i < result.AffectedTiles.Count; i++)
        {
            var tile = result.AffectedTiles[i];
            tile.HasEnemy = enemyPositions.Contains(tile.Position);
            tile.HasPlayer = tile.Position == playerPos;
            result.AffectedTiles[i] = tile;
        }
    }

    /// <summary>
    /// Calculate damage with falloff.
    /// </summary>
    public static int CalculateDamageWithFalloff(int baseDamage, float multiplier, int minDamage = 1)
    {
        return Math.Max(minDamage, (int)(baseDamage * multiplier));
    }

    /// <summary>
    /// Get distance based on distance type.
    /// </summary>
    private static int GetDistance(int dx, int dy, AoEDistanceType type)
    {
        return type switch
        {
            AoEDistanceType.Chebyshev => Math.Max(Math.Abs(dx), Math.Abs(dy)),
            AoEDistanceType.Manhattan => Math.Abs(dx) + Math.Abs(dy),
            AoEDistanceType.Euclidean => (int)Mathf.Sqrt(dx * dx + dy * dy),
            _ => Math.Max(Math.Abs(dx), Math.Abs(dy))
        };
    }

    /// <summary>
    /// Normalize angle to [-PI, PI].
    /// </summary>
    private static float NormalizeAngle(float angle)
    {
        while (angle > Mathf.Pi)
            angle -= Mathf.Tau;
        while (angle < -Mathf.Pi)
            angle += Mathf.Tau;
        return angle;
    }

    /// <summary>
    /// Find nearest enemy within range, excluding already hit targets.
    /// </summary>
    private static Vector2I? FindNearestEnemy(
        Vector2I from,
        int range,
        HashSet<Vector2I> excluded,
        SceneTree sceneTree)
    {
        var enemies = sceneTree.GetNodesInGroup("Enemies");
        Vector2I? nearest = null;
        int nearestDist = int.MaxValue;

        foreach (var node in enemies)
        {
            if (node is not Entities.Entity entity)
                continue;

            var pos = entity.GridPosition;
            if (excluded.Contains(pos))
                continue;

            int dist = LineOfFire.GetDistance(from, pos);
            if (dist <= range && dist < nearestDist)
            {
                nearest = pos;
                nearestDist = dist;
            }
        }

        return nearest;
    }
}
