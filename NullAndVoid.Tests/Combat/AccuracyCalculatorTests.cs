namespace NullAndVoid.Tests.Combat;

/// <summary>
/// Unit tests for AccuracyCalculator.
/// </summary>
public class AccuracyCalculatorTests
{
    #region Constants Verification

    [Fact]
    public void Constants_HaveExpectedValues()
    {
        AccuracyCalculator.DEFAULT_BASE_ACCURACY.Should().Be(60);
        AccuracyCalculator.MIN_ACCURACY.Should().Be(5);
        AccuracyCalculator.MAX_ACCURACY.Should().Be(95);
        AccuracyCalculator.CLOSE_RANGE_THRESHOLD.Should().Be(6);
        AccuracyCalculator.CLOSE_RANGE_BONUS_PER_TILE.Should().Be(3);
        AccuracyCalculator.PARTIAL_COVER_PENALTY.Should().Be(-20);
    }

    #endregion

    #region CalculateSimple Tests

    [Fact]
    public void CalculateSimple_BaseAccuracy_ReturnsBase()
    {
        // Arrange
        int baseAccuracy = 80;
        int distance = 7; // Beyond close range but not applying penalty
        var lofResult = LineOfFireResult.Clear;

        // Act
        int accuracy = AccuracyCalculator.CalculateSimple(baseAccuracy, false, distance, lofResult);

        // Assert
        accuracy.Should().Be(80);
    }

    [Fact]
    public void CalculateSimple_ZeroBaseAccuracy_UsesDefault()
    {
        // Arrange
        int baseAccuracy = 0;
        int distance = 7;
        var lofResult = LineOfFireResult.Clear;

        // Act
        int accuracy = AccuracyCalculator.CalculateSimple(baseAccuracy, false, distance, lofResult);

        // Assert
        accuracy.Should().Be(AccuracyCalculator.DEFAULT_BASE_ACCURACY);
    }

    [Fact]
    public void CalculateSimple_CloseRange_AppliesBonus()
    {
        // Arrange
        int baseAccuracy = 60;
        int distance = 3; // 3 tiles inside close range threshold (6)
        var lofResult = LineOfFireResult.Clear;

        // Act
        int accuracy = AccuracyCalculator.CalculateSimple(baseAccuracy, false, distance, lofResult);

        // Assert
        // Bonus = (6 - 3) * 3 = 9
        accuracy.Should().Be(69);
    }

    [Fact]
    public void CalculateSimple_PointBlank_AppliesMaxBonus()
    {
        // Arrange
        int baseAccuracy = 60;
        int distance = 1; // Very close
        var lofResult = LineOfFireResult.Clear;

        // Act
        int accuracy = AccuracyCalculator.CalculateSimple(baseAccuracy, false, distance, lofResult);

        // Assert
        // Bonus = (6 - 1) * 3 = 15
        accuracy.Should().Be(75);
    }

    [Fact]
    public void CalculateSimple_PartialCover_AppliesPenalty()
    {
        // Arrange
        int baseAccuracy = 80;
        int distance = 7;
        var lofResult = LineOfFireResult.PartialCover;

        // Act
        int accuracy = AccuracyCalculator.CalculateSimple(baseAccuracy, false, distance, lofResult);

        // Assert
        accuracy.Should().Be(60); // 80 - 20 = 60
    }

    [Fact]
    public void CalculateSimple_IgnoresCover_NoPenalty()
    {
        // Arrange
        int baseAccuracy = 80;
        int distance = 7;
        var lofResult = LineOfFireResult.PartialCover;
        bool ignoresCover = true;

        // Act
        int accuracy = AccuracyCalculator.CalculateSimple(baseAccuracy, ignoresCover, distance, lofResult);

        // Assert
        accuracy.Should().Be(80); // No penalty applied
    }

    [Fact]
    public void CalculateSimple_ClearLOS_NoCoverPenalty()
    {
        // Arrange
        int baseAccuracy = 80;
        int distance = 7;
        var lofResult = LineOfFireResult.Clear;

        // Act
        int accuracy = AccuracyCalculator.CalculateSimple(baseAccuracy, false, distance, lofResult);

        // Assert
        accuracy.Should().Be(80);
    }

    [Fact]
    public void CalculateSimple_ClampedToMaximum()
    {
        // Arrange
        int baseAccuracy = 90;
        int distance = 1; // +15 bonus would exceed max

        // Act
        int accuracy = AccuracyCalculator.CalculateSimple(baseAccuracy, false, distance, LineOfFireResult.Clear);

        // Assert
        accuracy.Should().Be(AccuracyCalculator.MAX_ACCURACY);
    }

    [Fact]
    public void CalculateSimple_ClampedToMinimum()
    {
        // Arrange
        int baseAccuracy = 10;
        var lofResult = LineOfFireResult.PartialCover; // -20 would go below min

        // Act
        int accuracy = AccuracyCalculator.CalculateSimple(baseAccuracy, false, 10, lofResult);

        // Assert
        accuracy.Should().Be(AccuracyCalculator.MIN_ACCURACY);
    }

    [Fact]
    public void CalculateSimple_WithWeaponStats_Works()
    {
        // Arrange
        var weapon = new WeaponStats
        {
            BaseAccuracy = 75,
            IgnoresCover = false,
            Range = 10
        };
        int distance = 5;

        // Act
        int accuracy = AccuracyCalculator.CalculateSimple(weapon, distance, LineOfFireResult.Clear);

        // Assert
        // Base 75 + close range bonus (6-5)*3 = 78
        accuracy.Should().Be(78);
    }

    #endregion

    #region RollToHit Tests

    [Fact]
    public void RollToHit_LowRoll_Hits()
    {
        // Arrange
        var random = new TestRandom(new int[] { 50 }, new double[] { }); // Roll 50
        int accuracy = 80;

        // Act
        bool hit = AccuracyCalculator.RollToHit(accuracy, random);

        // Assert
        hit.Should().BeTrue(); // 50 < 80
    }

    [Fact]
    public void RollToHit_HighRoll_Misses()
    {
        // Arrange
        var random = new TestRandom(new int[] { 90 }, new double[] { }); // Roll 90
        int accuracy = 80;

        // Act
        bool hit = AccuracyCalculator.RollToHit(accuracy, random);

        // Assert
        hit.Should().BeFalse(); // 90 >= 80
    }

    [Fact]
    public void RollToHit_ExactAccuracy_Misses()
    {
        // Arrange
        var random = new TestRandom(new int[] { 80 }, new double[] { }); // Roll exactly 80
        int accuracy = 80;

        // Act
        bool hit = AccuracyCalculator.RollToHit(accuracy, random);

        // Assert
        hit.Should().BeFalse(); // 80 >= 80 (need to roll UNDER)
    }

    [Fact]
    public void RollToHit_ZeroRoll_AlwaysHits()
    {
        // Arrange
        var random = new TestRandom(new int[] { 0 }, new double[] { });
        int accuracy = 5; // Minimum accuracy

        // Act
        bool hit = AccuracyCalculator.RollToHit(accuracy, random);

        // Assert
        hit.Should().BeTrue(); // 0 < 5
    }

    #endregion

    #region GetSizeModifier Tests

    [Theory]
    [InlineData(TargetSize.Tiny, -30)]
    [InlineData(TargetSize.Small, -10)]
    [InlineData(TargetSize.Normal, 0)]
    [InlineData(TargetSize.Large, 10)]
    [InlineData(TargetSize.Huge, 30)]
    public void GetSizeModifier_ReturnsCorrectValue(TargetSize size, int expected)
    {
        int modifier = AccuracyCalculator.GetSizeModifier(size);
        modifier.Should().Be(expected);
    }

    #endregion

    #region AccuracyBreakdown Tests

    [Fact]
    public void AccuracyBreakdown_GetModifierStrings_IncludesBase()
    {
        // Arrange
        var breakdown = new AccuracyBreakdown
        {
            BaseAccuracy = 80,
            FinalAccuracy = 80
        };

        // Act
        var strings = breakdown.GetModifierStrings();

        // Assert
        strings.Should().Contain("Base: 80%");
        strings.Should().Contain("Final: 80%");
    }

    [Fact]
    public void AccuracyBreakdown_GetModifierStrings_IncludesNonZeroModifiers()
    {
        // Arrange
        var breakdown = new AccuracyBreakdown
        {
            BaseAccuracy = 60,
            DistanceModifier = 9,
            CoverModifier = -20,
            FinalAccuracy = 49
        };

        // Act
        var strings = breakdown.GetModifierStrings();

        // Assert
        strings.Should().Contain("Base: 60%");
        strings.Should().Contain("Distance: +9%");
        strings.Should().Contain("Cover: -20%");
        strings.Should().Contain("Final: 49%");
    }

    [Fact]
    public void AccuracyBreakdown_GetModifierStrings_OmitsZeroModifiers()
    {
        // Arrange
        var breakdown = new AccuracyBreakdown
        {
            BaseAccuracy = 80,
            DistanceModifier = 0,
            TargetSizeModifier = 0,
            FinalAccuracy = 80
        };

        // Act
        var strings = breakdown.GetModifierStrings();

        // Assert
        strings.Should().HaveCount(2); // Only Base and Final
    }

    #endregion

    #region GetAccuracyColor Tests

    [Fact]
    public void GetAccuracyColor_HighAccuracy_ReturnsGreen()
    {
        // Arrange
        int accuracy = 85;

        // Act
        var color = AccuracyCalculator.GetAccuracyColor(accuracy);

        // Assert
        color.R.Should().BeApproximately(0.3f, 0.01f);
        color.G.Should().BeApproximately(1.0f, 0.01f);
        color.B.Should().BeApproximately(0.3f, 0.01f);
    }

    [Fact]
    public void GetAccuracyColor_MediumHighAccuracy_ReturnsYellow()
    {
        // Arrange
        int accuracy = 65;

        // Act
        var color = AccuracyCalculator.GetAccuracyColor(accuracy);

        // Assert
        color.R.Should().BeApproximately(0.8f, 0.01f);
        color.G.Should().BeApproximately(0.8f, 0.01f);
    }

    [Fact]
    public void GetAccuracyColor_MediumLowAccuracy_ReturnsOrange()
    {
        // Arrange
        int accuracy = 45;

        // Act
        var color = AccuracyCalculator.GetAccuracyColor(accuracy);

        // Assert
        color.R.Should().BeApproximately(1.0f, 0.01f);
        color.G.Should().BeApproximately(0.6f, 0.01f);
    }

    [Fact]
    public void GetAccuracyColor_LowAccuracy_ReturnsRed()
    {
        // Arrange
        int accuracy = 30;

        // Act
        var color = AccuracyCalculator.GetAccuracyColor(accuracy);

        // Assert
        color.R.Should().BeApproximately(1.0f, 0.01f);
        color.G.Should().BeApproximately(0.3f, 0.01f);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(95)]
    [InlineData(100)]
    public void GetAccuracyColor_Threshold80Plus_ReturnsGreen(int accuracy)
    {
        var color = AccuracyCalculator.GetAccuracyColor(accuracy);
        color.G.Should().BeApproximately(1.0f, 0.01f);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(70)]
    [InlineData(79)]
    public void GetAccuracyColor_Between60And79_ReturnsYellow(int accuracy)
    {
        var color = AccuracyCalculator.GetAccuracyColor(accuracy);
        color.R.Should().BeApproximately(0.8f, 0.01f);
    }

    [Theory]
    [InlineData(40)]
    [InlineData(50)]
    [InlineData(59)]
    public void GetAccuracyColor_Between40And59_ReturnsOrange(int accuracy)
    {
        var color = AccuracyCalculator.GetAccuracyColor(accuracy);
        color.G.Should().BeApproximately(0.6f, 0.01f);
    }

    #endregion

    #region AccuracyBreakdown Additional Tests

    [Fact]
    public void AccuracyBreakdown_AllModifiers_IncludesAll()
    {
        // Arrange
        var breakdown = new AccuracyBreakdown
        {
            BaseAccuracy = 60,
            DistanceModifier = 5,
            TargetSizeModifier = -10,
            TargetStateModifier = 15,
            AttackerMovementModifier = -10,
            CoverModifier = -20,
            WeaponModifier = 10,
            EquipmentModifier = 5,
            FinalAccuracy = 55
        };

        // Act
        var strings = breakdown.GetModifierStrings();

        // Assert
        strings.Should().HaveCount(9); // Base + 7 modifiers + Final
        strings.Should().Contain("Target Size: -10%");
        strings.Should().Contain("Target State: +15%");
        strings.Should().Contain("Movement: -10%");
        strings.Should().Contain("Weapon: +10%");
        strings.Should().Contain("Equipment: +5%");
    }

    #endregion
}
