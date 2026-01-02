using FluentAssertions;
using NullAndVoid.Combat;

namespace NullAndVoid.Tests.Combat;

/// <summary>
/// Unit tests for WeaponStats struct.
/// </summary>
public class WeaponStatsTests
{
    [Fact]
    public void Default_ReturnsExpectedValues()
    {
        // Act
        var stats = WeaponStats.Default;

        // Assert
        stats.MinDamage.Should().Be(5);
        stats.MaxDamage.Should().Be(10);
        stats.CriticalChance.Should().Be(5);
        stats.CriticalMultiplier.Should().Be(1.5f);
        stats.DamageType.Should().Be(DamageType.Kinetic);
        stats.PrimaryEffect.Should().Be(WeaponEffect.None);
        stats.EffectChance.Should().Be(0);
        stats.EffectDuration.Should().Be(0);
        stats.BaseAccuracy.Should().Be(80);
        stats.Range.Should().Be(10);
        stats.IgnoresCover.Should().BeFalse();
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        // Arrange
        var stats = new WeaponStats
        {
            MinDamage = 10,
            MaxDamage = 20,
            CriticalChance = 15,
            CriticalMultiplier = 2.0f,
            DamageType = DamageType.Thermal,
            PrimaryEffect = WeaponEffect.Burning,
            EffectChance = 25,
            EffectDuration = 3,
            BaseAccuracy = 75,
            Range = 15,
            IgnoresCover = true
        };

        // Assert
        stats.MinDamage.Should().Be(10);
        stats.MaxDamage.Should().Be(20);
        stats.CriticalChance.Should().Be(15);
        stats.CriticalMultiplier.Should().Be(2.0f);
        stats.DamageType.Should().Be(DamageType.Thermal);
        stats.PrimaryEffect.Should().Be(WeaponEffect.Burning);
        stats.EffectChance.Should().Be(25);
        stats.EffectDuration.Should().Be(3);
        stats.BaseAccuracy.Should().Be(75);
        stats.Range.Should().Be(15);
        stats.IgnoresCover.Should().BeTrue();
    }

    [Fact]
    public void ImplementsIWeaponStats()
    {
        // Arrange
        IWeaponStats stats = WeaponStats.Default;

        // Assert
        stats.Should().NotBeNull();
        stats.MinDamage.Should().Be(5);
        stats.MaxDamage.Should().Be(10);
    }

    [Theory]
    [InlineData(DamageType.Kinetic)]
    [InlineData(DamageType.Thermal)]
    [InlineData(DamageType.Electromagnetic)]
    [InlineData(DamageType.Explosive)]
    [InlineData(DamageType.Impact)]
    public void DamageType_AllTypesSupported(DamageType damageType)
    {
        // Arrange
        var stats = new WeaponStats { DamageType = damageType };

        // Assert
        stats.DamageType.Should().Be(damageType);
    }

    [Theory]
    [InlineData(WeaponEffect.None)]
    [InlineData(WeaponEffect.Burning)]
    [InlineData(WeaponEffect.Corrupted)]
    [InlineData(WeaponEffect.Stunned)]
    [InlineData(WeaponEffect.Knockback)]
    public void PrimaryEffect_AllEffectsSupported(WeaponEffect effect)
    {
        // Arrange
        var stats = new WeaponStats { PrimaryEffect = effect };

        // Assert
        stats.PrimaryEffect.Should().Be(effect);
    }
}
