namespace NullAndVoid.Tests.Targeting;

using NullAndVoid.Targeting;

/// <summary>
/// Unit tests for LineOfFire calculations.
/// </summary>
public class LineOfFireTests
{
    #region GetLine Tests (Bresenham's Algorithm)

    [Fact]
    public void GetLine_Horizontal_ReturnsCorrectTiles()
    {
        // Arrange
        var origin = new Vector2I(0, 0);
        var target = new Vector2I(5, 0);

        // Act
        var line = LineOfFire.GetLine(origin, target);

        // Assert
        line.Should().HaveCount(6);
        line[0].Should().Be(new Vector2I(0, 0));
        line[1].Should().Be(new Vector2I(1, 0));
        line[2].Should().Be(new Vector2I(2, 0));
        line[3].Should().Be(new Vector2I(3, 0));
        line[4].Should().Be(new Vector2I(4, 0));
        line[5].Should().Be(new Vector2I(5, 0));
    }

    [Fact]
    public void GetLine_Vertical_ReturnsCorrectTiles()
    {
        // Arrange
        var origin = new Vector2I(0, 0);
        var target = new Vector2I(0, 4);

        // Act
        var line = LineOfFire.GetLine(origin, target);

        // Assert
        line.Should().HaveCount(5);
        line[0].Should().Be(new Vector2I(0, 0));
        line[4].Should().Be(new Vector2I(0, 4));
    }

    [Fact]
    public void GetLine_Diagonal_ReturnsCorrectTiles()
    {
        // Arrange
        var origin = new Vector2I(0, 0);
        var target = new Vector2I(3, 3);

        // Act
        var line = LineOfFire.GetLine(origin, target);

        // Assert
        line.Should().HaveCount(4);
        line[0].Should().Be(new Vector2I(0, 0));
        line[1].Should().Be(new Vector2I(1, 1));
        line[2].Should().Be(new Vector2I(2, 2));
        line[3].Should().Be(new Vector2I(3, 3));
    }

    [Fact]
    public void GetLine_NegativeDirection_ReturnsCorrectTiles()
    {
        // Arrange
        var origin = new Vector2I(5, 5);
        var target = new Vector2I(2, 2);

        // Act
        var line = LineOfFire.GetLine(origin, target);

        // Assert
        line.Should().HaveCount(4);
        line[0].Should().Be(new Vector2I(5, 5));
        line[3].Should().Be(new Vector2I(2, 2));
    }

    [Fact]
    public void GetLine_SamePosition_ReturnsSingleTile()
    {
        // Arrange
        var position = new Vector2I(3, 3);

        // Act
        var line = LineOfFire.GetLine(position, position);

        // Assert
        line.Should().HaveCount(1);
        line[0].Should().Be(position);
    }

    [Fact]
    public void GetLine_SteepSlope_ReturnsCorrectTiles()
    {
        // Arrange - steep line (more vertical than horizontal)
        var origin = new Vector2I(0, 0);
        var target = new Vector2I(2, 5);

        // Act
        var line = LineOfFire.GetLine(origin, target);

        // Assert
        line.Should().HaveCount(6);
        line[0].Should().Be(new Vector2I(0, 0));
        line[5].Should().Be(new Vector2I(2, 5));
        // All Y values should be incrementing
        for (int i = 1; i < line.Count; i++)
        {
            line[i].Y.Should().BeGreaterThanOrEqualTo(line[i - 1].Y);
        }
    }

    #endregion

    #region Distance Calculations

    [Fact]
    public void GetDistance_Horizontal_ReturnsChebyshevDistance()
    {
        // Chebyshev distance = max(|dx|, |dy|)
        var a = new Vector2I(0, 0);
        var b = new Vector2I(5, 0);

        int distance = LineOfFire.GetDistance(a, b);

        distance.Should().Be(5);
    }

    [Fact]
    public void GetDistance_Vertical_ReturnsChebyshevDistance()
    {
        var a = new Vector2I(0, 0);
        var b = new Vector2I(0, 7);

        int distance = LineOfFire.GetDistance(a, b);

        distance.Should().Be(7);
    }

    [Fact]
    public void GetDistance_Diagonal_ReturnsChebyshevDistance()
    {
        // Diagonal: max(3, 3) = 3
        var a = new Vector2I(0, 0);
        var b = new Vector2I(3, 3);

        int distance = LineOfFire.GetDistance(a, b);

        distance.Should().Be(3);
    }

    [Fact]
    public void GetDistance_Mixed_ReturnsChebyshevDistance()
    {
        // max(|4|, |2|) = 4
        var a = new Vector2I(1, 1);
        var b = new Vector2I(5, 3);

        int distance = LineOfFire.GetDistance(a, b);

        distance.Should().Be(4);
    }

    [Fact]
    public void GetManhattanDistance_ReturnsSum()
    {
        var a = new Vector2I(0, 0);
        var b = new Vector2I(3, 4);

        int distance = LineOfFire.GetManhattanDistance(a, b);

        distance.Should().Be(7); // 3 + 4
    }

    [Fact]
    public void GetManhattanDistance_NegativeCoords_ReturnsAbsoluteSum()
    {
        var a = new Vector2I(-2, -3);
        var b = new Vector2I(2, 1);

        int distance = LineOfFire.GetManhattanDistance(a, b);

        distance.Should().Be(8); // |4| + |4|
    }

    #endregion

    #region Range Checks

    [Fact]
    public void IsInRange_WithinRange_ReturnsTrue()
    {
        var origin = new Vector2I(5, 5);
        var target = new Vector2I(8, 8);
        int range = 5;

        bool inRange = LineOfFire.IsInRange(origin, target, range);

        inRange.Should().BeTrue();
    }

    [Fact]
    public void IsInRange_AtExactRange_ReturnsTrue()
    {
        var origin = new Vector2I(0, 0);
        var target = new Vector2I(5, 0);
        int range = 5;

        bool inRange = LineOfFire.IsInRange(origin, target, range);

        inRange.Should().BeTrue();
    }

    [Fact]
    public void IsInRange_BeyondRange_ReturnsFalse()
    {
        var origin = new Vector2I(0, 0);
        var target = new Vector2I(6, 0);
        int range = 5;

        bool inRange = LineOfFire.IsInRange(origin, target, range);

        inRange.Should().BeFalse();
    }

    #endregion

    #region Positions In Radius

    [Fact]
    public void GetPositionsInRadius_RadiusOne_Returns9Positions()
    {
        var center = new Vector2I(5, 5);
        int radius = 1;

        var positions = LineOfFire.GetPositionsInRadius(center, radius);

        // 3x3 grid = 9 positions
        positions.Should().HaveCount(9);
        positions.Should().Contain(center);
        positions.Should().Contain(new Vector2I(4, 4));
        positions.Should().Contain(new Vector2I(6, 6));
    }

    [Fact]
    public void GetPositionsInRadius_RadiusTwo_Returns25Positions()
    {
        var center = new Vector2I(5, 5);
        int radius = 2;

        var positions = LineOfFire.GetPositionsInRadius(center, radius);

        // 5x5 grid = 25 positions (Chebyshev distance, square shape)
        positions.Should().HaveCount(25);
    }

    [Fact]
    public void GetPositionsInRadius_RadiusZero_ReturnsOnlyCenter()
    {
        var center = new Vector2I(3, 3);
        int radius = 0;

        var positions = LineOfFire.GetPositionsInRadius(center, radius);

        positions.Should().HaveCount(1);
        positions[0].Should().Be(center);
    }

    [Fact]
    public void GetPositionsInRadius_ContainsAllExpectedCorners()
    {
        var center = new Vector2I(10, 10);
        int radius = 3;

        var positions = LineOfFire.GetPositionsInRadius(center, radius);

        // Check corners are included
        positions.Should().Contain(new Vector2I(7, 7));   // Top-left
        positions.Should().Contain(new Vector2I(13, 7));  // Top-right
        positions.Should().Contain(new Vector2I(7, 13));  // Bottom-left
        positions.Should().Contain(new Vector2I(13, 13)); // Bottom-right
    }

    #endregion

    #region LineOfFireInfo Static Constructors

    [Fact]
    public void LineOfFireInfo_Clear_SetsCorrectValues()
    {
        var path = new List<Vector2I> { new(0, 0), new(1, 0), new(2, 0) };

        var info = LineOfFireInfo.Clear(2, path);

        info.Result.Should().Be(LineOfFireResult.Clear);
        info.Distance.Should().Be(2);
        info.CoverPenalty.Should().Be(0);
        info.BlockingPosition.Should().BeNull();
        info.Path.Should().BeSameAs(path);
    }

    [Fact]
    public void LineOfFireInfo_PartialCover_SetsCorrectValues()
    {
        var path = new List<Vector2I> { new(0, 0), new(1, 0), new(2, 0) };
        var coverPos = new Vector2I(1, 0);

        var info = LineOfFireInfo.PartialCover(2, coverPos, path);

        info.Result.Should().Be(LineOfFireResult.PartialCover);
        info.Distance.Should().Be(2);
        info.CoverPenalty.Should().Be(-20);
        info.BlockingPosition.Should().Be(coverPos);
    }

    [Fact]
    public void LineOfFireInfo_Blocked_SetsCorrectValues()
    {
        var path = new List<Vector2I> { new(0, 0), new(1, 0) };
        var blockPos = new Vector2I(1, 0);

        var info = LineOfFireInfo.Blocked(blockPos, path);

        info.Result.Should().Be(LineOfFireResult.Blocked);
        info.Distance.Should().Be(0);
        info.CoverPenalty.Should().Be(-100);
        info.BlockingPosition.Should().Be(blockPos);
    }

    [Fact]
    public void LineOfFireInfo_OutOfRange_SetsCorrectValues()
    {
        var info = LineOfFireInfo.OutOfRange(15);

        info.Result.Should().Be(LineOfFireResult.OutOfRange);
        info.Distance.Should().Be(15);
        info.CoverPenalty.Should().Be(0);
        info.BlockingPosition.Should().BeNull();
        info.Path.Should().BeEmpty();
    }

    #endregion

    #region LineOfFire Check Tests

    [Fact]
    public void Check_SamePosition_ReturnsClear()
    {
        // Arrange
        var position = new Vector2I(5, 5);

        // Act
        var result = LineOfFire.Check(position, position, 10);

        // Assert
        result.Result.Should().Be(LineOfFireResult.Clear);
        result.Distance.Should().Be(0);
    }

    [Fact]
    public void Check_BeyondMaxRange_ReturnsOutOfRange()
    {
        // Arrange
        var origin = new Vector2I(0, 0);
        var target = new Vector2I(15, 0);

        // Act
        var result = LineOfFire.Check(origin, target, 10);

        // Assert
        result.Result.Should().Be(LineOfFireResult.OutOfRange);
        result.Distance.Should().Be(15);
    }

    #endregion
}
