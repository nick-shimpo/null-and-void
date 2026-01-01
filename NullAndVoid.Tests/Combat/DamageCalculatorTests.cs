namespace NullAndVoid.Tests.Combat;

/// <summary>
/// Unit tests for DamageCalculator.
/// </summary>
public class DamageCalculatorTests
{
    #region CalculateAoEDamage Tests

    [Fact]
    public void CalculateAoEDamage_AtCenter_ReturnsFullDamage()
    {
        // Arrange
        int baseDamage = 20;
        int distanceFromCenter = 0;
        int maxRadius = 3;

        // Act
        int result = DamageCalculator.CalculateAoEDamage(baseDamage, distanceFromCenter, maxRadius);

        // Assert
        result.Should().Be(20);
    }

    [Fact]
    public void CalculateAoEDamage_AtEdge_ReturnsReducedDamage()
    {
        // Arrange
        int baseDamage = 20;
        int distanceFromCenter = 3;
        int maxRadius = 3;

        // Act
        int result = DamageCalculator.CalculateAoEDamage(baseDamage, distanceFromCenter, maxRadius);

        // Assert
        // At distance 3 with radius 3: falloff = 1 - (3/4) = 0.25
        // 20 * 0.25 = 5
        result.Should().Be(5);
    }

    [Fact]
    public void CalculateAoEDamage_MidRange_ReturnsProportionalDamage()
    {
        // Arrange
        int baseDamage = 40;
        int distanceFromCenter = 2;
        int maxRadius = 4;

        // Act
        int result = DamageCalculator.CalculateAoEDamage(baseDamage, distanceFromCenter, maxRadius);

        // Assert
        // falloff = 1 - (2/5) = 0.6
        // 40 * 0.6 = 24
        result.Should().Be(24);
    }

    [Fact]
    public void CalculateAoEDamage_AlwaysReturnsMinimumOne()
    {
        // Arrange
        int baseDamage = 2;
        int distanceFromCenter = 5;
        int maxRadius = 5;

        // Act
        int result = DamageCalculator.CalculateAoEDamage(baseDamage, distanceFromCenter, maxRadius);

        // Assert
        result.Should().BeGreaterOrEqualTo(1);
    }

    [Theory]
    [InlineData(100, 0, 5, 100)]  // Center = full damage
    [InlineData(100, 1, 5, 83)]   // 1 tile out of 5: 1 - 1/6 = 0.833
    [InlineData(100, 2, 5, 66)]   // 2 tiles out of 5: 1 - 2/6 = 0.666
    [InlineData(100, 5, 5, 16)]   // Edge: 1 - 5/6 = 0.166
    public void CalculateAoEDamage_VariousDistances_CorrectFalloff(
        int baseDamage, int distance, int radius, int expectedMin)
    {
        // Act
        int result = DamageCalculator.CalculateAoEDamage(baseDamage, distance, radius);

        // Assert - allow for rounding differences
        result.Should().BeInRange(expectedMin - 1, expectedMin + 1);
    }

    #endregion

    #region CalculateKnockback Tests

    [Fact]
    public void CalculateKnockback_BaseDamage_ReturnsBaseKnockback()
    {
        // Arrange
        int knockbackBase = 2;
        int damage = 10;

        // Act
        int result = DamageCalculator.CalculateKnockback(knockbackBase, damage);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void CalculateKnockback_HighDamage_IncreasesKnockback()
    {
        // Arrange
        int knockbackBase = 2;
        int damage = 25; // > 20, should add +1

        // Act
        int result = DamageCalculator.CalculateKnockback(knockbackBase, damage);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public void CalculateKnockback_VeryHighDamage_IncreasesKnockbackMore()
    {
        // Arrange
        int knockbackBase = 2;
        int damage = 45; // > 40, should add +2 total

        // Act
        int result = DamageCalculator.CalculateKnockback(knockbackBase, damage);

        // Assert
        result.Should().Be(4);
    }

    [Fact]
    public void CalculateKnockback_HeavyTarget_ReducesKnockback()
    {
        // Arrange
        int knockbackBase = 4;
        int damage = 10;

        // Act
        int result = DamageCalculator.CalculateKnockback(knockbackBase, damage, targetIsHeavy: true);

        // Assert
        result.Should().Be(2); // 4 / 2 = 2
    }

    [Fact]
    public void CalculateKnockback_HeavyTarget_MinimumOne()
    {
        // Arrange
        int knockbackBase = 1;
        int damage = 5;

        // Act
        int result = DamageCalculator.CalculateKnockback(knockbackBase, damage, targetIsHeavy: true);

        // Assert
        result.Should().Be(1); // Max(1, 1/2) = 1
    }

    #endregion

    #region RollDamage Tests

    [Fact]
    public void RollDamage_WithDeterministicRandom_ReturnsExpectedValue()
    {
        // Arrange
        var random = new TestRandom(15);

        // Act
        int result = DamageCalculator.RollDamage(10, 20, random);

        // Assert
        result.Should().Be(15);
    }

    [Fact]
    public void RollDamage_ClampsToRange()
    {
        // Arrange - TestRandom clamps to valid range
        var random = new TestRandom(5);

        // Act
        int result = DamageCalculator.RollDamage(10, 20, random);

        // Assert - should be clamped to min of range
        result.Should().BeInRange(10, 20);
    }

    #endregion

    #region RollCritical Tests

    [Fact]
    public void RollCritical_ZeroChance_NeverCrits()
    {
        // Arrange
        var random = new TestRandom(new int[] { }, new double[] { 0.0 });

        // Act
        bool result = DamageCalculator.RollCritical(0, random);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RollCritical_HundredPercent_AlwaysCrits()
    {
        // Arrange - any roll below 100 should crit
        var random = new TestRandom(new int[] { }, new double[] { 0.5 });

        // Act
        bool result = DamageCalculator.RollCritical(100, random);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RollCritical_LowRoll_Crits()
    {
        // Arrange - roll 0.05 (5%) with 10% crit chance should crit
        var random = new TestRandom(new int[] { }, new double[] { 0.05 });

        // Act
        bool result = DamageCalculator.RollCritical(10, random);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RollCritical_HighRoll_NoCrit()
    {
        // Arrange - roll 0.15 (15%) with 10% crit chance should not crit
        var random = new TestRandom(new int[] { }, new double[] { 0.15 });

        // Act
        bool result = DamageCalculator.RollCritical(10, random);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ApplyCritical Tests

    [Fact]
    public void ApplyCritical_StandardMultiplier_Returns150Percent()
    {
        // Act
        int result = DamageCalculator.ApplyCritical(10, 1.5f);

        // Assert
        result.Should().Be(15);
    }

    [Fact]
    public void ApplyCritical_DoubleMultiplier_Returns200Percent()
    {
        // Act
        int result = DamageCalculator.ApplyCritical(10, 2.0f);

        // Assert
        result.Should().Be(20);
    }

    [Fact]
    public void ApplyCritical_NoMultiplier_ReturnsSameDamage()
    {
        // Act
        int result = DamageCalculator.ApplyCritical(10, 1.0f);

        // Assert
        result.Should().Be(10);
    }

    #endregion

    #region Calculate Tests (with WeaponStats)

    [Fact]
    public void Calculate_NoArmor_ReturnsFullDamage()
    {
        // Arrange
        var weapon = CreateTestWeapon(minDamage: 10, maxDamage: 10);
        var random = new TestRandom(10);

        // Act
        var result = DamageCalculator.Calculate(weapon, targetArmor: 0, random: random);

        // Assert
        result.RawDamage.Should().Be(10);
        result.FinalDamage.Should().Be(10);
        result.ArmorReduction.Should().Be(0);
    }

    [Fact]
    public void Calculate_WithArmor_ReducesDamage()
    {
        // Arrange - use Impact type which has no modifiers
        var weapon = CreateTestWeapon(minDamage: 15, maxDamage: 15, damageType: DamageType.Impact);
        var random = new TestRandom(15);

        // Act
        var result = DamageCalculator.Calculate(weapon, targetArmor: 5, random: random);

        // Assert
        result.RawDamage.Should().Be(15);
        result.ArmorReduction.Should().Be(5);
        result.FinalDamage.Should().Be(10); // 15 - 5 = 10
    }

    [Fact]
    public void Calculate_ArmorGreaterThanDamage_MinimumDamageApplied()
    {
        // Arrange
        var weapon = CreateTestWeapon(minDamage: 5, maxDamage: 5);
        var random = new TestRandom(5);

        // Act
        var result = DamageCalculator.Calculate(weapon, targetArmor: 20, random: random);

        // Assert
        result.RawDamage.Should().Be(5);
        result.FinalDamage.Should().Be(DamageCalculator.MIN_DAMAGE);
    }

    [Fact]
    public void Calculate_CriticalHit_IncreasesRawDamage()
    {
        // Arrange
        var weapon = CreateTestWeapon(minDamage: 10, maxDamage: 10, critChance: 100, critMultiplier: 2.0f);
        var random = new TestRandom(new int[] { 10 }, new double[] { 0.0 }); // Damage roll, then crit roll (always hits)

        // Act
        var result = DamageCalculator.Calculate(weapon, targetArmor: 0, random: random);

        // Assert
        result.IsCritical.Should().BeTrue();
        result.RawDamage.Should().Be(20); // 10 * 2.0 = 20
        result.CriticalBonus.Should().Be(10);
    }

    [Fact]
    public void Calculate_NoCritical_WhenCritChanceZero()
    {
        // Arrange
        var weapon = CreateTestWeapon(minDamage: 10, maxDamage: 10, critChance: 0);
        var random = new TestRandom(10);

        // Act
        var result = DamageCalculator.Calculate(weapon, targetArmor: 0, random: random);

        // Assert
        result.IsCritical.Should().BeFalse();
        result.CriticalBonus.Should().Be(0);
    }

    [Fact]
    public void Calculate_KineticVsArmor_AppliesDamageBonus()
    {
        // Arrange
        var weapon = CreateTestWeapon(minDamage: 10, maxDamage: 10, damageType: DamageType.Kinetic);
        var random = new TestRandom(10);

        // Act
        var result = DamageCalculator.Calculate(weapon, targetArmor: 5, random: random);

        // Assert
        // Kinetic vs armor: 10 * 1.5 = 15, then 15 - 5 armor = 10
        result.DamageTypeModifier.Should().Be(5); // 15 - 10 = 5 bonus
        result.FinalDamage.Should().Be(10);
    }

    [Fact]
    public void Calculate_KineticVsShield_AppliesDamagePenalty()
    {
        // Arrange
        var weapon = CreateTestWeapon(minDamage: 20, maxDamage: 20, damageType: DamageType.Kinetic);
        var random = new TestRandom(20);

        // Act
        var result = DamageCalculator.Calculate(weapon, targetArmor: 0, targetHasShield: true, random: random);

        // Assert
        // Kinetic vs shield: 20 * 0.75 = 15
        result.DamageTypeModifier.Should().Be(-5); // 15 - 20 = -5
        result.FinalDamage.Should().Be(15);
    }

    [Fact]
    public void Calculate_ThermalVsShield_AppliesDamageBonus()
    {
        // Arrange
        var weapon = CreateTestWeapon(minDamage: 20, maxDamage: 20, damageType: DamageType.Thermal);
        var random = new TestRandom(20);

        // Act
        var result = DamageCalculator.Calculate(weapon, targetArmor: 0, targetHasShield: true, random: random);

        // Assert
        // Thermal vs shield: 20 * 1.25 = 25
        result.DamageTypeModifier.Should().Be(5);
        result.FinalDamage.Should().Be(25);
    }

    [Fact]
    public void Calculate_EMDamage_AppliesHalfDamage()
    {
        // Arrange
        var weapon = CreateTestWeapon(minDamage: 20, maxDamage: 20, damageType: DamageType.Electromagnetic);
        var random = new TestRandom(20);

        // Act
        var result = DamageCalculator.Calculate(weapon, targetArmor: 0, random: random);

        // Assert
        // EM: 20 * 0.5 = 10
        result.DamageTypeModifier.Should().Be(-10);
        result.FinalDamage.Should().Be(10);
    }

    [Fact]
    public void Calculate_ExplosiveDamage_NoModifier()
    {
        // Arrange
        var weapon = CreateTestWeapon(minDamage: 20, maxDamage: 20, damageType: DamageType.Explosive);
        var random = new TestRandom(20);

        // Act
        var result = DamageCalculator.Calculate(weapon, targetArmor: 0, random: random);

        // Assert
        result.DamageTypeModifier.Should().Be(0);
        result.FinalDamage.Should().Be(20);
    }

    #endregion

    #region DamageResult Tests

    [Fact]
    public void DamageResult_Miss_ReturnsZeroDamage()
    {
        // Act
        var result = DamageResult.Miss();

        // Assert
        result.RawDamage.Should().Be(0);
        result.FinalDamage.Should().Be(0);
        result.IsCritical.Should().BeFalse();
        result.AppliedEffect.Should().Be(WeaponEffect.None);
    }

    [Fact]
    public void DamageResult_GetDescription_ForMiss_ReturnsMISS()
    {
        // Arrange
        var result = DamageResult.Miss();

        // Act
        string desc = result.GetDescription();

        // Assert
        desc.Should().Be("MISS");
    }

    [Fact]
    public void DamageResult_GetDescription_ForCritical_ShowsCriticalBonus()
    {
        // Arrange
        var result = new DamageResult
        {
            RawDamage = 20,
            IsCritical = true,
            CriticalBonus = 10,
            FinalDamage = 20,
            DamageType = DamageType.Kinetic
        };

        // Act
        string desc = result.GetDescription();

        // Assert
        desc.Should().Contain("CRITICAL!");
        desc.Should().Contain("20");
        desc.Should().Contain("10");
    }

    #endregion

    #region Helper Methods

    private static WeaponStats CreateTestWeapon(
        int minDamage = 5,
        int maxDamage = 10,
        int critChance = 0,
        float critMultiplier = 1.5f,
        DamageType damageType = DamageType.Kinetic)
    {
        return new WeaponStats
        {
            MinDamage = minDamage,
            MaxDamage = maxDamage,
            CriticalChance = critChance,
            CriticalMultiplier = critMultiplier,
            DamageType = damageType,
            PrimaryEffect = WeaponEffect.None,
            EffectChance = 0,
            EffectDuration = 0
        };
    }

    #endregion
}
