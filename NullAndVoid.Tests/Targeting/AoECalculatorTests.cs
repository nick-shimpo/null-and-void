namespace NullAndVoid.Tests.Targeting;

/// <summary>
/// Unit tests for AoECalculator.
/// </summary>
public class AoECalculatorTests
{
    #region CalculateCircle Tests

    [Fact]
    public void CalculateCircle_RadiusZero_ReturnsCenterOnly()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);

        // Act
        var result = AoECalculator.CalculateCircle(center, 0, origin);

        // Assert
        result.AffectedTiles.Should().HaveCount(1);
        result.AffectedTiles[0].Position.Should().Be(center);
        result.AffectedTiles[0].IsCenter.Should().BeTrue();
    }

    [Fact]
    public void CalculateCircle_Radius1_Chebyshev_Returns9Tiles()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);

        // Act
        var result = AoECalculator.CalculateCircle(center, 1, origin, AoEDistanceType.Chebyshev);

        // Assert
        // 3x3 square = 9 tiles
        result.AffectedTiles.Should().HaveCount(9);
        result.Shape.Should().Be(AoEShape.Circle);
        result.Radius.Should().Be(1);
    }

    [Fact]
    public void CalculateCircle_Radius1_Manhattan_Returns5Tiles()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);

        // Act
        var result = AoECalculator.CalculateCircle(center, 1, origin, AoEDistanceType.Manhattan);

        // Assert
        // Diamond shape: center + 4 cardinals = 5 tiles
        result.AffectedTiles.Should().HaveCount(5);
    }

    [Fact]
    public void CalculateCircle_Radius2_Chebyshev_Returns25Tiles()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);

        // Act
        var result = AoECalculator.CalculateCircle(center, 2, origin, AoEDistanceType.Chebyshev);

        // Assert
        // 5x5 square = 25 tiles
        result.AffectedTiles.Should().HaveCount(25);
    }

    [Fact]
    public void CalculateCircle_WithFalloff_CenterHasFullDamage()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);

        // Act
        var result = AoECalculator.CalculateCircle(center, 3, origin, hasFalloff: true);

        // Assert
        var centerTile = result.AffectedTiles.First(t => t.Position == center);
        centerTile.DamageMultiplier.Should().Be(1.0f);
        centerTile.IsCenter.Should().BeTrue();
    }

    [Fact]
    public void CalculateCircle_WithFalloff_EdgeHasReducedDamage()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);
        int radius = 3;

        // Act
        var result = AoECalculator.CalculateCircle(center, radius, origin, hasFalloff: true);

        // Assert
        var edgeTile = result.AffectedTiles.First(t => t.Distance == radius);
        edgeTile.DamageMultiplier.Should().BeLessThan(1.0f);
    }

    [Fact]
    public void CalculateCircle_NoFalloff_AllTilesHaveFullDamage()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);

        // Act
        var result = AoECalculator.CalculateCircle(center, 2, origin, hasFalloff: false);

        // Assert
        result.AffectedTiles.Should().AllSatisfy(t => t.DamageMultiplier.Should().Be(1.0f));
    }

    [Fact]
    public void CalculateCircle_ContainsExpectedPositions()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);

        // Act
        var result = AoECalculator.CalculateCircle(center, 1, origin, AoEDistanceType.Chebyshev);
        var positions = result.GetPositions().ToList();

        // Assert
        positions.Should().Contain(center);
        positions.Should().Contain(new Vector2I(4, 4)); // Top-left
        positions.Should().Contain(new Vector2I(5, 4)); // Top
        positions.Should().Contain(new Vector2I(6, 4)); // Top-right
        positions.Should().Contain(new Vector2I(4, 5)); // Left
        positions.Should().Contain(new Vector2I(6, 5)); // Right
        positions.Should().Contain(new Vector2I(4, 6)); // Bottom-left
        positions.Should().Contain(new Vector2I(5, 6)); // Bottom
        positions.Should().Contain(new Vector2I(6, 6)); // Bottom-right
    }

    #endregion

    #region CalculateLine Tests

    [Fact]
    public void CalculateLine_Horizontal_ReturnsCorrectTiles()
    {
        // Arrange
        var origin = new Vector2I(0, 5);
        var target = new Vector2I(5, 5);
        int length = 5;

        // Act
        var result = AoECalculator.CalculateLine(origin, target, length, width: 1, pierce: false);

        // Assert
        result.Shape.Should().Be(AoEShape.Line);
        result.Origin.Should().Be(origin);
        // Should have tiles along the line (excluding origin)
        result.AffectedTiles.Should().NotBeEmpty();
        result.AffectedTiles.Should().AllSatisfy(t => t.Position.Y.Should().Be(5));
    }

    [Fact]
    public void CalculateLine_ExcludesOrigin()
    {
        // Arrange
        var origin = new Vector2I(0, 0);
        var target = new Vector2I(5, 0);
        int length = 5;

        // Act
        var result = AoECalculator.CalculateLine(origin, target, length);
        var positions = result.GetPositions().ToList();

        // Assert
        positions.Should().NotContain(origin);
    }

    [Fact]
    public void CalculateLine_WithWidth_IncludesAdjacentTiles()
    {
        // Arrange
        var origin = new Vector2I(0, 5);
        var target = new Vector2I(5, 5);
        int length = 5;
        int width = 3; // Should include tiles above and below the line

        // Act
        var result = AoECalculator.CalculateLine(origin, target, length, width);

        // Assert
        // Width 3 means tiles at y=4, y=5, y=6 along the line
        var positions = result.GetPositions().ToList();
        positions.Should().Contain(p => p.Y == 4);
        positions.Should().Contain(p => p.Y == 5);
        positions.Should().Contain(p => p.Y == 6);
    }

    [Fact]
    public void CalculateLine_NoFalloff()
    {
        // Arrange
        var origin = new Vector2I(0, 0);
        var target = new Vector2I(5, 0);

        // Act
        var result = AoECalculator.CalculateLine(origin, target, 5);

        // Assert - Line attacks don't have falloff by default
        result.AffectedTiles.Should().AllSatisfy(t => t.DamageMultiplier.Should().Be(1.0f));
    }

    #endregion

    #region CalculateCross Tests

    [Fact]
    public void CalculateCross_ReturnsCorrectShape()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);
        int armLength = 2;

        // Act
        var result = AoECalculator.CalculateCross(center, armLength, origin);

        // Assert
        result.Shape.Should().Be(AoEShape.Cross);
        result.Center.Should().Be(center);
    }

    [Fact]
    public void CalculateCross_IncludesCenter()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);

        // Act
        var result = AoECalculator.CalculateCross(center, 2, origin);
        var positions = result.GetPositions().ToList();

        // Assert
        positions.Should().Contain(center);
        var centerTile = result.AffectedTiles.First(t => t.Position == center);
        centerTile.IsCenter.Should().BeTrue();
    }

    [Fact]
    public void CalculateCross_CardinalDirections_Only()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);
        int armLength = 2;

        // Act
        var result = AoECalculator.CalculateCross(center, armLength, origin, includeDiagonals: false);

        // Assert - Should be center + 4 directions * 2 tiles = 9 tiles
        result.AffectedTiles.Should().HaveCount(9);

        var positions = result.GetPositions().ToList();
        // Should have cardinals
        positions.Should().Contain(new Vector2I(5, 3)); // Up 2
        positions.Should().Contain(new Vector2I(5, 4)); // Up 1
        positions.Should().Contain(new Vector2I(5, 6)); // Down 1
        positions.Should().Contain(new Vector2I(5, 7)); // Down 2
        positions.Should().Contain(new Vector2I(3, 5)); // Left 2
        positions.Should().Contain(new Vector2I(4, 5)); // Left 1
        positions.Should().Contain(new Vector2I(6, 5)); // Right 1
        positions.Should().Contain(new Vector2I(7, 5)); // Right 2
        // Should NOT have diagonals
        positions.Should().NotContain(new Vector2I(6, 6));
        positions.Should().NotContain(new Vector2I(4, 4));
    }

    [Fact]
    public void CalculateCross_WithDiagonals_IncludesAllDirections()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);
        int armLength = 2;

        // Act
        var result = AoECalculator.CalculateCross(center, armLength, origin, includeDiagonals: true);

        // Assert - Should be center + 8 directions * 2 tiles = 17 tiles
        result.AffectedTiles.Should().HaveCount(17);

        var positions = result.GetPositions().ToList();
        // Should have diagonals
        positions.Should().Contain(new Vector2I(6, 6)); // Down-Right 1
        positions.Should().Contain(new Vector2I(7, 7)); // Down-Right 2
        positions.Should().Contain(new Vector2I(4, 4)); // Up-Left 1
        positions.Should().Contain(new Vector2I(3, 3)); // Up-Left 2
    }

    #endregion

    #region CalculateRing Tests

    [Fact]
    public void CalculateRing_ReturnsCorrectShape()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);

        // Act
        var result = AoECalculator.CalculateRing(center, 2, 3, origin);

        // Assert
        result.Shape.Should().Be(AoEShape.Ring);
        result.Center.Should().Be(center);
        result.Radius.Should().Be(3); // Outer radius
    }

    [Fact]
    public void CalculateRing_ExcludesInnerRadius()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);
        int innerRadius = 2;
        int outerRadius = 3;

        // Act
        var result = AoECalculator.CalculateRing(center, innerRadius, outerRadius, origin);
        var positions = result.GetPositions().ToList();

        // Assert - center should not be included
        positions.Should().NotContain(center);
        // Tiles at distance 1 should not be included
        positions.Should().NotContain(new Vector2I(4, 5)); // Distance 1
        positions.Should().NotContain(new Vector2I(6, 5)); // Distance 1
    }

    [Fact]
    public void CalculateRing_IncludesOuterEdge()
    {
        // Arrange
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);
        int innerRadius = 2;
        int outerRadius = 3;

        // Act
        var result = AoECalculator.CalculateRing(center, innerRadius, outerRadius, origin);
        var positions = result.GetPositions().ToList();

        // Assert - outer edge tiles should be included
        positions.Should().Contain(new Vector2I(2, 5)); // Distance 3 left
        positions.Should().Contain(new Vector2I(8, 5)); // Distance 3 right
        positions.Should().Contain(new Vector2I(5, 2)); // Distance 3 up
        positions.Should().Contain(new Vector2I(5, 8)); // Distance 3 down
    }

    [Fact]
    public void CalculateRing_InnerEqualsOuter_Returns4Tiles()
    {
        // Arrange - A ring where inner = outer = 2 should form just the perimeter at distance 2
        var center = new Vector2I(5, 5);
        var origin = new Vector2I(0, 0);

        // Act
        var result = AoECalculator.CalculateRing(center, 2, 2, origin);

        // Assert
        result.AffectedTiles.Should().NotBeEmpty();
        // All tiles should be at distance 2
        result.AffectedTiles.Should().AllSatisfy(t =>
        {
            var dx = Math.Abs(t.Position.X - center.X);
            var dy = Math.Abs(t.Position.Y - center.Y);
            var dist = Math.Max(dx, dy);
            dist.Should().Be(2);
        });
    }

    #endregion

    #region CalculateCone Tests

    [Fact]
    public void CalculateCone_ReturnsCorrectShape()
    {
        // Arrange
        var origin = new Vector2I(5, 5);
        var target = new Vector2I(10, 5); // East

        // Act
        var result = AoECalculator.CalculateCone(origin, target, length: 5, spreadAngle: 45);

        // Assert
        result.Shape.Should().Be(AoEShape.Cone);
        result.Origin.Should().Be(origin);
    }

    [Fact]
    public void CalculateCone_ExcludesOrigin()
    {
        // Arrange
        var origin = new Vector2I(5, 5);
        var target = new Vector2I(10, 5);

        // Act
        var result = AoECalculator.CalculateCone(origin, target, length: 5, spreadAngle: 45);
        var positions = result.GetPositions().ToList();

        // Assert
        positions.Should().NotContain(origin);
    }

    [Fact]
    public void CalculateCone_SpreadAngles_AffectsCoverage()
    {
        // Arrange
        var origin = new Vector2I(5, 5);
        var target = new Vector2I(10, 5);

        // Act
        var narrowCone = AoECalculator.CalculateCone(origin, target, length: 5, spreadAngle: 30);
        var wideCone = AoECalculator.CalculateCone(origin, target, length: 5, spreadAngle: 90);

        // Assert
        wideCone.AffectedTiles.Count.Should().BeGreaterThan(narrowCone.AffectedTiles.Count);
    }

    [Fact]
    public void CalculateCone_PointsTowardTarget()
    {
        // Arrange
        var origin = new Vector2I(5, 5);
        var target = new Vector2I(10, 5); // East

        // Act
        var result = AoECalculator.CalculateCone(origin, target, length: 3, spreadAngle: 45);
        var positions = result.GetPositions().ToList();

        // Assert - Most tiles should be east of origin
        positions.Should().OnlyContain(p => p.X > origin.X || p == target);
    }

    #endregion

    #region CalculateDamageWithFalloff Tests

    [Fact]
    public void CalculateDamageWithFalloff_FullMultiplier_ReturnsBaseDamage()
    {
        // Arrange
        int baseDamage = 100;
        float multiplier = 1.0f;

        // Act
        int damage = AoECalculator.CalculateDamageWithFalloff(baseDamage, multiplier);

        // Assert
        damage.Should().Be(100);
    }

    [Fact]
    public void CalculateDamageWithFalloff_HalfMultiplier_ReturnsHalfDamage()
    {
        // Arrange
        int baseDamage = 100;
        float multiplier = 0.5f;

        // Act
        int damage = AoECalculator.CalculateDamageWithFalloff(baseDamage, multiplier);

        // Assert
        damage.Should().Be(50);
    }

    [Fact]
    public void CalculateDamageWithFalloff_ZeroMultiplier_ReturnsMinDamage()
    {
        // Arrange
        int baseDamage = 100;
        float multiplier = 0.0f;
        int minDamage = 1;

        // Act
        int damage = AoECalculator.CalculateDamageWithFalloff(baseDamage, multiplier, minDamage);

        // Assert
        damage.Should().Be(minDamage);
    }

    [Fact]
    public void CalculateDamageWithFalloff_VeryLowMultiplier_RespectsMinDamage()
    {
        // Arrange
        int baseDamage = 10;
        float multiplier = 0.05f; // Would be 0.5, rounds to 0
        int minDamage = 5;

        // Act
        int damage = AoECalculator.CalculateDamageWithFalloff(baseDamage, multiplier, minDamage);

        // Assert
        damage.Should().Be(minDamage);
    }

    [Theory]
    [InlineData(100, 0.75f, 1, 75)]
    [InlineData(50, 0.5f, 1, 25)]
    [InlineData(20, 0.25f, 1, 5)]
    [InlineData(100, 0.33f, 1, 33)]
    public void CalculateDamageWithFalloff_VariousMultipliers(int baseDamage, float multiplier, int minDamage, int expected)
    {
        // Act
        int damage = AoECalculator.CalculateDamageWithFalloff(baseDamage, multiplier, minDamage);

        // Assert
        damage.Should().Be(expected);
    }

    #endregion

    #region AoEResult Tests

    [Fact]
    public void AoEResult_GetPositions_ReturnsAllPositions()
    {
        // Arrange
        var result = new AoEResult
        {
            AffectedTiles = new List<AoETileInfo>
            {
                new() { Position = new Vector2I(0, 0) },
                new() { Position = new Vector2I(1, 0) },
                new() { Position = new Vector2I(0, 1) }
            }
        };

        // Act
        var positions = result.GetPositions().ToList();

        // Assert
        positions.Should().HaveCount(3);
    }

    [Fact]
    public void AoEResult_GetUnblockedPositions_ExcludesBlocked()
    {
        // Arrange
        var result = new AoEResult
        {
            AffectedTiles = new List<AoETileInfo>
            {
                new() { Position = new Vector2I(0, 0), IsBlocked = false },
                new() { Position = new Vector2I(1, 0), IsBlocked = true },
                new() { Position = new Vector2I(0, 1), IsBlocked = false }
            }
        };

        // Act
        var positions = result.GetUnblockedPositions().ToList();

        // Assert
        positions.Should().HaveCount(2);
        positions.Should().NotContain(new Vector2I(1, 0));
    }

    [Fact]
    public void AoEResult_TileCount_ExcludesBlocked()
    {
        // Arrange
        var result = new AoEResult
        {
            AffectedTiles = new List<AoETileInfo>
            {
                new() { IsBlocked = false },
                new() { IsBlocked = true },
                new() { IsBlocked = false }
            }
        };

        // Act & Assert
        result.TileCount.Should().Be(2);
    }

    [Fact]
    public void AoEResult_EnemyCount_CountsUnblockedEnemies()
    {
        // Arrange
        var result = new AoEResult
        {
            AffectedTiles = new List<AoETileInfo>
            {
                new() { HasEnemy = true, IsBlocked = false },
                new() { HasEnemy = true, IsBlocked = true },  // Blocked, shouldn't count
                new() { HasEnemy = false, IsBlocked = false },
                new() { HasEnemy = true, IsBlocked = false }
            }
        };

        // Act & Assert
        result.EnemyCount.Should().Be(2);
    }

    [Fact]
    public void AoEResult_HitsPlayer_TrueIfPlayerUnblocked()
    {
        // Arrange
        var result = new AoEResult
        {
            AffectedTiles = new List<AoETileInfo>
            {
                new() { HasPlayer = true, IsBlocked = false }
            }
        };

        // Act & Assert
        result.HitsPlayer.Should().BeTrue();
    }

    [Fact]
    public void AoEResult_HitsPlayer_FalseIfPlayerBlocked()
    {
        // Arrange
        var result = new AoEResult
        {
            AffectedTiles = new List<AoETileInfo>
            {
                new() { HasPlayer = true, IsBlocked = true }
            }
        };

        // Act & Assert
        result.HitsPlayer.Should().BeFalse();
    }

    #endregion

    #region AoEShape Enum Tests

    [Fact]
    public void AoEShape_HasAllExpectedValues()
    {
        // Assert
        Enum.GetValues<AoEShape>().Should().HaveCount(6);
        Enum.IsDefined(AoEShape.Circle).Should().BeTrue();
        Enum.IsDefined(AoEShape.Cone).Should().BeTrue();
        Enum.IsDefined(AoEShape.Line).Should().BeTrue();
        Enum.IsDefined(AoEShape.Cross).Should().BeTrue();
        Enum.IsDefined(AoEShape.Ring).Should().BeTrue();
        Enum.IsDefined(AoEShape.Chain).Should().BeTrue();
    }

    #endregion

    #region AoEDistanceType Enum Tests

    [Fact]
    public void AoEDistanceType_HasAllExpectedValues()
    {
        // Assert
        Enum.GetValues<AoEDistanceType>().Should().HaveCount(3);
        Enum.IsDefined(AoEDistanceType.Chebyshev).Should().BeTrue();
        Enum.IsDefined(AoEDistanceType.Manhattan).Should().BeTrue();
        Enum.IsDefined(AoEDistanceType.Euclidean).Should().BeTrue();
    }

    #endregion

    #region AoETileInfo Tests

    [Fact]
    public void AoETileInfo_DefaultValues()
    {
        // Arrange
        var tileInfo = new AoETileInfo();

        // Assert
        tileInfo.Position.Should().Be(Vector2I.Zero);
        tileInfo.Distance.Should().Be(0);
        tileInfo.DamageMultiplier.Should().Be(0);
        tileInfo.IsBlocked.Should().BeFalse();
        tileInfo.HasEnemy.Should().BeFalse();
        tileInfo.HasPlayer.Should().BeFalse();
        tileInfo.IsCenter.Should().BeFalse();
    }

    #endregion
}
