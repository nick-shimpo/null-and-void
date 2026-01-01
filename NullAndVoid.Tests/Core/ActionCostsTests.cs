namespace NullAndVoid.Tests.Core;

/// <summary>
/// Unit tests for ActionCosts.
/// </summary>
public class ActionCostsTests
{
    #region Standard Action Costs

    [Fact]
    public void StandardActions_HaveExpectedCosts()
    {
        ActionCosts.Move.Should().Be(100);
        ActionCosts.Attack.Should().Be(100);
        ActionCosts.Wait.Should().Be(100);
        ActionCosts.PickupItem.Should().Be(50);
        ActionCosts.DropItem.Should().Be(50);
        ActionCosts.UseItem.Should().Be(75);
        ActionCosts.EquipItem.Should().Be(100);
        ActionCosts.UnequipModule.Should().Be(50);
        ActionCosts.ToggleModule.Should().Be(25);
        ActionCosts.Free.Should().Be(0);
        ActionCosts.UseDoor.Should().Be(50);
        ActionCosts.UseStairs.Should().Be(100);
    }

    [Fact]
    public void ActionModifiers_HaveExpectedCosts()
    {
        ActionCosts.QuickAction.Should().Be(50);
        ActionCosts.SlowAction.Should().Be(150);
        ActionCosts.FullAction.Should().Be(200);
    }

    #endregion

    #region Propulsion Costs

    [Fact]
    public void PropulsionCosts_HaveExpectedValues()
    {
        ActionCosts.Propulsion.Flight.Should().Be(50);
        ActionCosts.Propulsion.Hover.Should().Be(60);
        ActionCosts.Propulsion.Legs.Should().Be(80);
        ActionCosts.Propulsion.Wheels.Should().Be(100);
        ActionCosts.Propulsion.Treads.Should().Be(120);
        ActionCosts.Propulsion.Core.Should().Be(150);
    }

    [Theory]
    [InlineData(PropulsionType.Flight, 50)]
    [InlineData(PropulsionType.Hover, 60)]
    [InlineData(PropulsionType.Legs, 80)]
    [InlineData(PropulsionType.Wheels, 100)]
    [InlineData(PropulsionType.Treads, 120)]
    [InlineData(PropulsionType.None, 150)]
    public void GetMovementCost_ReturnsCorrectCost(PropulsionType type, int expected)
    {
        int cost = ActionCosts.GetMovementCost(type);
        cost.Should().Be(expected);
    }

    #endregion

    #region Terrain Modifiers

    [Fact]
    public void TerrainModifiers_HaveExpectedValues()
    {
        ActionCosts.Terrain.Normal.Should().Be(1.0f);
        ActionCosts.Terrain.Difficult.Should().Be(1.5f);
        ActionCosts.Terrain.Water.Should().Be(2.0f);
        ActionCosts.Terrain.Rubble.Should().Be(1.25f);
    }

    [Theory]
    [InlineData(TerrainType.Normal, PropulsionType.Legs, 1.0f)]
    [InlineData(TerrainType.Difficult, PropulsionType.Legs, 1.5f)]
    [InlineData(TerrainType.Water, PropulsionType.Legs, 2.0f)]
    [InlineData(TerrainType.Rubble, PropulsionType.Legs, 1.25f)]
    public void GetTerrainModifier_GroundPropulsion_ReturnsCorrectModifier(TerrainType terrain, PropulsionType propulsion, float expected)
    {
        float modifier = ActionCosts.GetTerrainModifier(terrain, propulsion);
        modifier.Should().Be(expected);
    }

    [Theory]
    [InlineData(TerrainType.Difficult, PropulsionType.Flight)]
    [InlineData(TerrainType.Water, PropulsionType.Flight)]
    [InlineData(TerrainType.Rubble, PropulsionType.Flight)]
    [InlineData(TerrainType.Difficult, PropulsionType.Hover)]
    [InlineData(TerrainType.Rubble, PropulsionType.Hover)]
    public void GetTerrainModifier_FlyingPropulsion_IgnoresTerrain(TerrainType terrain, PropulsionType propulsion)
    {
        float modifier = ActionCosts.GetTerrainModifier(terrain, propulsion);
        modifier.Should().Be(1.0f);
    }

    #endregion

    #region Combined Movement Costs

    [Fact]
    public void GetMovementCost_WithTerrain_AppliesModifier()
    {
        // Legs (80) on difficult terrain (1.5x) = 120
        int cost = ActionCosts.GetMovementCost(PropulsionType.Legs, TerrainType.Difficult);
        cost.Should().Be(120);
    }

    [Fact]
    public void GetMovementCost_WithWater_AppliesHighModifier()
    {
        // Wheels (100) on water (2.0x) = 200
        int cost = ActionCosts.GetMovementCost(PropulsionType.Wheels, TerrainType.Water);
        cost.Should().Be(200);
    }

    [Fact]
    public void GetMovementCost_FlightOnDifficult_IgnoresTerrainPenalty()
    {
        // Flight (50) on difficult terrain should still be 50
        int cost = ActionCosts.GetMovementCost(PropulsionType.Flight, TerrainType.Difficult);
        cost.Should().Be(50);
    }

    [Fact]
    public void GetMovementCost_TreadsOnRubble_AppliesModifier()
    {
        // Treads (120) on rubble (1.25x) = 150
        int cost = ActionCosts.GetMovementCost(PropulsionType.Treads, TerrainType.Rubble);
        cost.Should().Be(150);
    }

    #endregion

    #region PropulsionType Enum Values

    [Fact]
    public void PropulsionType_HasExpectedValues()
    {
        Enum.GetValues<PropulsionType>().Should().HaveCount(6);
        Enum.IsDefined(PropulsionType.None).Should().BeTrue();
        Enum.IsDefined(PropulsionType.Legs).Should().BeTrue();
        Enum.IsDefined(PropulsionType.Wheels).Should().BeTrue();
        Enum.IsDefined(PropulsionType.Treads).Should().BeTrue();
        Enum.IsDefined(PropulsionType.Hover).Should().BeTrue();
        Enum.IsDefined(PropulsionType.Flight).Should().BeTrue();
    }

    #endregion

    #region TerrainType Enum Values

    [Fact]
    public void TerrainType_HasExpectedValues()
    {
        Enum.GetValues<TerrainType>().Should().HaveCount(4);
        Enum.IsDefined(TerrainType.Normal).Should().BeTrue();
        Enum.IsDefined(TerrainType.Difficult).Should().BeTrue();
        Enum.IsDefined(TerrainType.Water).Should().BeTrue();
        Enum.IsDefined(TerrainType.Rubble).Should().BeTrue();
    }

    #endregion
}
