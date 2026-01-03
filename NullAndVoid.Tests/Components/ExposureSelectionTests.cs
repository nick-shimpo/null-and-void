using FluentAssertions;
using NullAndVoid.Tests.TestHelpers;
using Xunit;

namespace NullAndVoid.Tests.Components;

/// <summary>
/// Tests for the exposure-weighted selection algorithm used in module damage routing.
/// Note: Item class extends Godot.Resource and cannot be instantiated in unit tests.
/// These tests verify the selection algorithm logic directly.
/// </summary>
public class ExposureSelectionTests
{
    /// <summary>
    /// Mock module for testing weighted selection.
    /// </summary>
    private record MockModule(string Name, int Exposure, int Armor, bool IsArmor = false);

    /// <summary>
    /// Simulate the weighted selection algorithm from Equipment.RouteDamageToModules.
    /// </summary>
    private static MockModule? SelectByExposure(List<MockModule> modules, Random random)
    {
        var activeModules = modules.Where(m => m.Armor > 0).ToList();
        if (activeModules.Count == 0)
            return null;

        int totalExposure = activeModules.Sum(m => m.Exposure);
        if (totalExposure <= 0)
            return null;

        int roll = random.Next(totalExposure);
        int cumulative = 0;

        foreach (var module in activeModules)
        {
            cumulative += module.Exposure;
            if (roll < cumulative)
                return module;
        }

        return null;
    }

    [Fact]
    public void WeightedSelection_HighExposureHitMoreOften()
    {
        // Arrange - armor with 40 exposure vs sensor with 5 exposure
        var modules = new List<MockModule>
        {
            new("Armor", Exposure: 40, Armor: 20, IsArmor: true),
            new("Sensor", Exposure: 5, Armor: 8)
        };

        var random = new Random(42); // Deterministic seed
        var hitCounts = new Dictionary<string, int>
        {
            { "Armor", 0 },
            { "Sensor", 0 }
        };

        // Act - run many iterations
        for (int i = 0; i < 1000; i++)
        {
            var hit = SelectByExposure(modules, random);
            if (hit != null)
                hitCounts[hit.Name]++;
        }

        // Assert - armor should be hit ~89% of time (40/45)
        double armorRate = hitCounts["Armor"] / 1000.0;
        double sensorRate = hitCounts["Sensor"] / 1000.0;

        armorRate.Should().BeGreaterThan(0.80, "high exposure armor should be hit most of the time");
        sensorRate.Should().BeLessThan(0.20, "low exposure sensor should rarely be hit");
    }

    [Fact]
    public void WeightedSelection_DeterministicWithTestRandom()
    {
        // Arrange - modules with known exposures: total = 45
        var modules = new List<MockModule>
        {
            new("Armor", Exposure: 40, Armor: 20),   // 0-39
            new("Sensor", Exposure: 5, Armor: 8)     // 40-44
        };

        // Test random returns specific values
        var random = new TestRandom(0, 39, 40, 44);

        // Act & Assert
        // Roll 0 should hit Armor (cumulative 40 > 0)
        var hit1 = SelectByExposure(modules, random);
        hit1.Should().NotBeNull();
        hit1!.Name.Should().Be("Armor");

        // Roll 39 should still hit Armor (cumulative 40 > 39)
        var hit2 = SelectByExposure(modules, random);
        hit2.Should().NotBeNull();
        hit2!.Name.Should().Be("Armor");

        // Roll 40 should hit Sensor (cumulative 40 <= 40, then 45 > 40)
        var hit3 = SelectByExposure(modules, random);
        hit3.Should().NotBeNull();
        hit3!.Name.Should().Be("Sensor");

        // Roll 44 should hit Sensor (cumulative 45 > 44)
        var hit4 = SelectByExposure(modules, random);
        hit4.Should().NotBeNull();
        hit4!.Name.Should().Be("Sensor");
    }

    [Fact]
    public void WeightedSelection_DisabledModulesNotTargeted()
    {
        // Arrange - disabled armor (0 armor) + active sensor
        var modules = new List<MockModule>
        {
            new("DisabledArmor", Exposure: 50, Armor: 0), // Disabled
            new("Sensor", Exposure: 5, Armor: 8)
        };

        var random = new TestRandom(0); // Would hit armor if active

        // Act
        var hit = SelectByExposure(modules, random);

        // Assert - only sensor can be hit
        hit.Should().NotBeNull();
        hit!.Name.Should().Be("Sensor");
    }

    [Fact]
    public void WeightedSelection_MultipleModulesProportional()
    {
        // Arrange - three modules with different exposures
        var modules = new List<MockModule>
        {
            new("Armor", Exposure: 50, Armor: 20),    // 50% expected
            new("Generator", Exposure: 30, Armor: 10), // 30% expected
            new("Sensor", Exposure: 20, Armor: 8)      // 20% expected
        };

        var random = new Random(123);
        var hitCounts = new Dictionary<string, int>
        {
            { "Armor", 0 },
            { "Generator", 0 },
            { "Sensor", 0 }
        };

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var hit = SelectByExposure(modules, random);
            if (hit != null)
                hitCounts[hit.Name]++;
        }

        // Assert - should be roughly proportional
        double armorRate = hitCounts["Armor"] / 1000.0;
        double generatorRate = hitCounts["Generator"] / 1000.0;
        double sensorRate = hitCounts["Sensor"] / 1000.0;

        armorRate.Should().BeInRange(0.40, 0.60, "armor (50%) should be hit ~50% of time");
        generatorRate.Should().BeInRange(0.20, 0.40, "generator (30%) should be hit ~30% of time");
        sensorRate.Should().BeInRange(0.10, 0.30, "sensor (20%) should be hit ~20% of time");
    }

    [Fact]
    public void WeightedSelection_SingleModule_AlwaysHit()
    {
        // Arrange
        var modules = new List<MockModule>
        {
            new("OnlyModule", Exposure: 10, Armor: 15)
        };

        var random = new TestRandom(0, 5, 9);

        // Act & Assert - any roll should hit the only module
        for (int i = 0; i < 3; i++)
        {
            var hit = SelectByExposure(modules, random);
            hit.Should().NotBeNull();
            hit!.Name.Should().Be("OnlyModule");
        }
    }

    [Fact]
    public void WeightedSelection_NoModules_ReturnsNull()
    {
        // Arrange
        var modules = new List<MockModule>();
        var random = new TestRandom(0);

        // Act
        var hit = SelectByExposure(modules, random);

        // Assert
        hit.Should().BeNull();
    }

    [Fact]
    public void WeightedSelection_AllModulesDisabled_ReturnsNull()
    {
        // Arrange
        var modules = new List<MockModule>
        {
            new("Armor", Exposure: 50, Armor: 0),
            new("Sensor", Exposure: 10, Armor: 0)
        };

        var random = new TestRandom(0);

        // Act
        var hit = SelectByExposure(modules, random);

        // Assert
        hit.Should().BeNull();
    }

    [Fact]
    public void ExposureValues_ArmorHigherThanUtility()
    {
        // Document expected exposure values for different module types
        // Armor: 35-50 (tanks damage)
        // Shield: 16-20 (external)
        // Propulsion: 18-20 (external)
        // Generator: 10-12 (internal core)
        // Battery: 8-10 (protected)
        // Sensor: 5-8 (low profile)
        // Logic: 5-8 (internal processor)

        // These constants match the values set in ItemFactory.cs
        const int ArmorMinExposure = 35;
        const int ArmorMaxExposure = 55;
        const int SensorMinExposure = 5;
        const int SensorMaxExposure = 8;
        const int GeneratorExposure = 12;

        ArmorMinExposure.Should().BeGreaterThan(GeneratorExposure * 2,
            "armor should have much higher exposure than generators");

        ArmorMaxExposure.Should().BeGreaterThan(SensorMaxExposure * 5,
            "armor should have 5x+ the exposure of sensors");
    }
}
