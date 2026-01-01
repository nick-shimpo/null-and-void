namespace NullAndVoid.Tests.AI;

using NullAndVoid.AI;

/// <summary>
/// Unit tests for A* Pathfinding.
/// </summary>
public class PathfindingTests
{
    #region FindPath Tests

    [Fact]
    public void FindPath_StraightLine_FindsDirectPath()
    {
        // Arrange
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(5, 0);
        Func<Vector2I, bool> isWalkable = _ => true;

        // Act
        var path = Pathfinding.FindPath(start, goal, isWalkable);

        // Assert
        path.Should().NotBeNull();
        path!.First().Should().Be(start);
        path.Last().Should().Be(goal);
        path.Count.Should().Be(6); // 0,1,2,3,4,5
    }

    [Fact]
    public void FindPath_DiagonalPath_FindsShortestPath()
    {
        // Arrange
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(3, 3);
        Func<Vector2I, bool> isWalkable = _ => true;

        // Act
        var path = Pathfinding.FindPath(start, goal, isWalkable, allowDiagonal: true);

        // Assert
        path.Should().NotBeNull();
        path!.First().Should().Be(start);
        path.Last().Should().Be(goal);
        path.Count.Should().Be(4); // Diagonal is shortest
    }

    [Fact]
    public void FindPath_NoDiagonal_FindsCardinalPath()
    {
        // Arrange
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(2, 2);
        Func<Vector2I, bool> isWalkable = _ => true;

        // Act
        var path = Pathfinding.FindPath(start, goal, isWalkable, allowDiagonal: false);

        // Assert
        path.Should().NotBeNull();
        path!.First().Should().Be(start);
        path.Last().Should().Be(goal);
        // Without diagonal, need 4 moves (2 horizontal + 2 vertical)
        path.Count.Should().Be(5);
    }

    [Fact]
    public void FindPath_AroundObstacle_FindsPath()
    {
        // Arrange - wall at x=2, y=0-2
        var start = new Vector2I(0, 1);
        var goal = new Vector2I(4, 1);
        Func<Vector2I, bool> isWalkable = pos =>
        {
            if (pos.X == 2 && pos.Y >= 0 && pos.Y <= 2)
                return false; // Wall
            return true;
        };

        // Act
        var path = Pathfinding.FindPath(start, goal, isWalkable);

        // Assert
        path.Should().NotBeNull();
        path!.First().Should().Be(start);
        path.Last().Should().Be(goal);
        // Path should not go through the wall
        path.Should().NotContain(new Vector2I(2, 0));
        path.Should().NotContain(new Vector2I(2, 1));
        path.Should().NotContain(new Vector2I(2, 2));
    }

    [Fact]
    public void FindPath_UnreachableGoal_ReturnsNull()
    {
        // Arrange - goal is completely blocked
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(5, 5);
        Func<Vector2I, bool> isWalkable = pos => pos != goal;

        // Act
        var path = Pathfinding.FindPath(start, goal, isWalkable);

        // Assert
        path.Should().BeNull();
    }

    [Fact]
    public void FindPath_SurroundedStart_ReturnsNull()
    {
        // Arrange - start position is surrounded by walls
        var start = new Vector2I(5, 5);
        var goal = new Vector2I(10, 10);
        Func<Vector2I, bool> isWalkable = pos =>
        {
            // Block all adjacent tiles
            var dx = Math.Abs(pos.X - start.X);
            var dy = Math.Abs(pos.Y - start.Y);
            if (dx <= 1 && dy <= 1 && pos != start)
                return false;
            return true;
        };

        // Act
        var path = Pathfinding.FindPath(start, goal, isWalkable);

        // Assert
        path.Should().BeNull();
    }

    [Fact]
    public void FindPath_SameStartAndGoal_ReturnsSingleElement()
    {
        // Arrange
        var pos = new Vector2I(5, 5);
        Func<Vector2I, bool> isWalkable = _ => true;

        // Act
        var path = Pathfinding.FindPath(pos, pos, isWalkable);

        // Assert
        path.Should().NotBeNull();
        path!.Count.Should().Be(1);
        path[0].Should().Be(pos);
    }

    [Fact]
    public void FindPath_PrefersDiagonalWhenAllowed()
    {
        // Arrange
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(5, 5);
        Func<Vector2I, bool> isWalkable = _ => true;

        // Act
        var diagonalPath = Pathfinding.FindPath(start, goal, isWalkable, allowDiagonal: true);
        var cardinalPath = Pathfinding.FindPath(start, goal, isWalkable, allowDiagonal: false);

        // Assert
        diagonalPath.Should().NotBeNull();
        cardinalPath.Should().NotBeNull();
        diagonalPath!.Count.Should().BeLessThan(cardinalPath!.Count);
    }

    #endregion

    #region GetNextStep Tests

    [Fact]
    public void GetNextStep_ReturnsSecondPosition()
    {
        // Arrange
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(3, 0);
        Func<Vector2I, bool> isWalkable = _ => true;

        // Act
        var nextStep = Pathfinding.GetNextStep(start, goal, isWalkable);

        // Assert
        nextStep.Should().NotBeNull();
        nextStep.Should().Be(new Vector2I(1, 0));
    }

    [Fact]
    public void GetNextStep_NoPath_ReturnsNull()
    {
        // Arrange
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(5, 5);
        Func<Vector2I, bool> isWalkable = pos => pos == start; // Only start is walkable

        // Act
        var nextStep = Pathfinding.GetNextStep(start, goal, isWalkable);

        // Assert
        nextStep.Should().BeNull();
    }

    #endregion

    #region GetFleeDirection Tests

    [Fact]
    public void GetFleeDirection_MovesAwayFromThreat()
    {
        // Arrange
        var position = new Vector2I(5, 5);
        var threat = new Vector2I(6, 5); // Threat to the right
        Func<Vector2I, bool> isWalkable = _ => true;

        // Act
        var fleeDir = Pathfinding.GetFleeDirection(position, threat, isWalkable);
        var newPos = position + fleeDir;

        // Assert - should move left (away from threat)
        fleeDir.X.Should().BeLessThan(0);
    }

    [Fact]
    public void GetFleeDirection_ConsidersWalkability()
    {
        // Arrange
        var position = new Vector2I(5, 5);
        var threat = new Vector2I(5, 6); // Threat below
        // Block the ideal flee direction (up)
        Func<Vector2I, bool> isWalkable = pos => pos.Y != 4;

        // Act
        var fleeDir = Pathfinding.GetFleeDirection(position, threat, isWalkable);
        var newPos = position + fleeDir;

        // Assert - should not try to go up (blocked)
        fleeDir.Y.Should().NotBe(-1);
        isWalkable(newPos).Should().BeTrue();
    }

    [Fact]
    public void GetFleeDirection_AllBlocked_ReturnsZero()
    {
        // Arrange
        var position = new Vector2I(5, 5);
        var threat = new Vector2I(6, 5);
        Func<Vector2I, bool> isWalkable = _ => false;

        // Act
        var fleeDir = Pathfinding.GetFleeDirection(position, threat, isWalkable);

        // Assert
        fleeDir.Should().Be(Vector2I.Zero);
    }

    #endregion

    #region FindPathWithCosts Tests

    [Fact]
    public void FindPathWithCosts_AvoidsHighCostTiles()
    {
        // Arrange
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(4, 0);
        Func<Vector2I, bool> isWalkable = _ => true;
        // High cost at x=2, y=0 (direct path)
        Func<Vector2I, float> getCost = pos =>
            (pos.X == 2 && pos.Y == 0) ? 100f : 0f;

        // Act
        var path = Pathfinding.FindPathWithCosts(start, goal, isWalkable, getCost);

        // Assert
        path.Should().NotBeNull();
        // Path should avoid the high cost tile if possible
        path!.First().Should().Be(start);
        path.Last().Should().Be(goal);
    }

    [Fact]
    public void FindPathWithCosts_ZeroCost_SameAsNormalPath()
    {
        // Arrange
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(3, 3);
        Func<Vector2I, bool> isWalkable = _ => true;
        Func<Vector2I, float> getCost = _ => 0f;

        // Act
        var costPath = Pathfinding.FindPathWithCosts(start, goal, isWalkable, getCost);
        var normalPath = Pathfinding.FindPath(start, goal, isWalkable);

        // Assert
        costPath.Should().NotBeNull();
        normalPath.Should().NotBeNull();
        costPath!.Count.Should().Be(normalPath!.Count);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FindPath_NegativeCoordinates_Works()
    {
        // Arrange
        var start = new Vector2I(-5, -5);
        var goal = new Vector2I(0, 0);
        Func<Vector2I, bool> isWalkable = _ => true;

        // Act
        var path = Pathfinding.FindPath(start, goal, isWalkable);

        // Assert
        path.Should().NotBeNull();
        path!.First().Should().Be(start);
        path.Last().Should().Be(goal);
    }

    [Fact]
    public void FindPath_LargeDistance_CompletesInReasonableTime()
    {
        // Arrange
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(50, 50);
        Func<Vector2I, bool> isWalkable = _ => true;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var path = Pathfinding.FindPath(start, goal, isWalkable);
        stopwatch.Stop();

        // Assert
        path.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete in < 1 second
    }

    [Fact]
    public void FindPath_PathConsecutiveTiles()
    {
        // Arrange
        var start = new Vector2I(0, 0);
        var goal = new Vector2I(4, 4);
        Func<Vector2I, bool> isWalkable = _ => true;

        // Act
        var path = Pathfinding.FindPath(start, goal, isWalkable);

        // Assert - each step should be adjacent to the previous
        path.Should().NotBeNull();
        for (int i = 1; i < path!.Count; i++)
        {
            var prev = path[i - 1];
            var curr = path[i];
            var dx = Math.Abs(curr.X - prev.X);
            var dy = Math.Abs(curr.Y - prev.Y);
            dx.Should().BeLessThanOrEqualTo(1);
            dy.Should().BeLessThanOrEqualTo(1);
            (dx + dy).Should().BeGreaterThan(0); // Not same tile
        }
    }

    #endregion
}
