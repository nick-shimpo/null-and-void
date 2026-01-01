namespace NullAndVoid.Tests.Items;

/// <summary>
/// Unit tests for Item system enums and extensions.
/// Note: Item and Ammunition classes extend Godot.Resource and cannot be
/// instantiated in unit tests. This file tests the pure logic components.
/// </summary>
public class ItemTests
{
    #region EquipmentSlotType Enum

    [Fact]
    public void EquipmentSlotType_HasAllExpectedValues()
    {
        Enum.GetValues<EquipmentSlotType>().Should().HaveCount(4);
        Enum.IsDefined(EquipmentSlotType.Any).Should().BeTrue();
        Enum.IsDefined(EquipmentSlotType.Core).Should().BeTrue();
        Enum.IsDefined(EquipmentSlotType.Utility).Should().BeTrue();
        Enum.IsDefined(EquipmentSlotType.Base).Should().BeTrue();
    }

    #endregion

    #region ItemRarity Enum

    [Fact]
    public void ItemRarity_HasAllExpectedValues()
    {
        Enum.GetValues<ItemRarity>().Should().HaveCount(5);
        Enum.IsDefined(ItemRarity.Common).Should().BeTrue();
        Enum.IsDefined(ItemRarity.Uncommon).Should().BeTrue();
        Enum.IsDefined(ItemRarity.Rare).Should().BeTrue();
        Enum.IsDefined(ItemRarity.Epic).Should().BeTrue();
        Enum.IsDefined(ItemRarity.Legendary).Should().BeTrue();
    }

    #endregion

    #region ItemType Enum

    [Fact]
    public void ItemType_HasAllExpectedValues()
    {
        Enum.GetValues<ItemType>().Should().HaveCount(3);
        Enum.IsDefined(ItemType.Module).Should().BeTrue();
        Enum.IsDefined(ItemType.Consumable).Should().BeTrue();
        Enum.IsDefined(ItemType.KeyItem).Should().BeTrue();
    }

    #endregion

    #region ModuleType Enum

    [Fact]
    public void ModuleType_HasAllExpectedValues()
    {
        Enum.GetValues<ModuleType>().Should().HaveCount(11);
        // None
        Enum.IsDefined(ModuleType.None).Should().BeTrue();
        // Core mount types
        Enum.IsDefined(ModuleType.Logic).Should().BeTrue();
        Enum.IsDefined(ModuleType.Battery).Should().BeTrue();
        Enum.IsDefined(ModuleType.Generator).Should().BeTrue();
        // Utility mount types
        Enum.IsDefined(ModuleType.Weapon).Should().BeTrue();
        Enum.IsDefined(ModuleType.Sensor).Should().BeTrue();
        Enum.IsDefined(ModuleType.Shield).Should().BeTrue();
        // Base mount types
        Enum.IsDefined(ModuleType.Treads).Should().BeTrue();
        Enum.IsDefined(ModuleType.Legs).Should().BeTrue();
        Enum.IsDefined(ModuleType.Flight).Should().BeTrue();
        Enum.IsDefined(ModuleType.Cargo).Should().BeTrue();
    }

    #endregion

    #region ModuleTypeExtensions - GetRequiredSlotType

    [Theory]
    [InlineData(ModuleType.Logic, EquipmentSlotType.Core)]
    [InlineData(ModuleType.Battery, EquipmentSlotType.Core)]
    [InlineData(ModuleType.Generator, EquipmentSlotType.Core)]
    public void GetRequiredSlotType_CoreModules_ReturnsCore(ModuleType moduleType, EquipmentSlotType expected)
    {
        moduleType.GetRequiredSlotType().Should().Be(expected);
    }

    [Theory]
    [InlineData(ModuleType.Weapon, EquipmentSlotType.Utility)]
    [InlineData(ModuleType.Sensor, EquipmentSlotType.Utility)]
    [InlineData(ModuleType.Shield, EquipmentSlotType.Utility)]
    public void GetRequiredSlotType_UtilityModules_ReturnsUtility(ModuleType moduleType, EquipmentSlotType expected)
    {
        moduleType.GetRequiredSlotType().Should().Be(expected);
    }

    [Theory]
    [InlineData(ModuleType.Treads, EquipmentSlotType.Base)]
    [InlineData(ModuleType.Legs, EquipmentSlotType.Base)]
    [InlineData(ModuleType.Flight, EquipmentSlotType.Base)]
    [InlineData(ModuleType.Cargo, EquipmentSlotType.Base)]
    public void GetRequiredSlotType_BaseModules_ReturnsBase(ModuleType moduleType, EquipmentSlotType expected)
    {
        moduleType.GetRequiredSlotType().Should().Be(expected);
    }

    [Fact]
    public void GetRequiredSlotType_None_ReturnsAny()
    {
        ModuleType.None.GetRequiredSlotType().Should().Be(EquipmentSlotType.Any);
    }

    #endregion

    #region ModuleTypeExtensions - CanMountIn

    [Theory]
    [InlineData(ModuleType.Logic, EquipmentSlotType.Core, true)]
    [InlineData(ModuleType.Logic, EquipmentSlotType.Utility, false)]
    [InlineData(ModuleType.Logic, EquipmentSlotType.Base, false)]
    public void CanMountIn_CoreModule_OnlyMountsInCore(ModuleType moduleType, EquipmentSlotType slotType, bool expected)
    {
        moduleType.CanMountIn(slotType).Should().Be(expected);
    }

    [Theory]
    [InlineData(ModuleType.Weapon, EquipmentSlotType.Core, false)]
    [InlineData(ModuleType.Weapon, EquipmentSlotType.Utility, true)]
    [InlineData(ModuleType.Weapon, EquipmentSlotType.Base, false)]
    public void CanMountIn_UtilityModule_OnlyMountsInUtility(ModuleType moduleType, EquipmentSlotType slotType, bool expected)
    {
        moduleType.CanMountIn(slotType).Should().Be(expected);
    }

    [Theory]
    [InlineData(ModuleType.Treads, EquipmentSlotType.Core, false)]
    [InlineData(ModuleType.Treads, EquipmentSlotType.Utility, false)]
    [InlineData(ModuleType.Treads, EquipmentSlotType.Base, true)]
    public void CanMountIn_BaseModule_OnlyMountsInBase(ModuleType moduleType, EquipmentSlotType slotType, bool expected)
    {
        moduleType.CanMountIn(slotType).Should().Be(expected);
    }

    [Theory]
    [InlineData(EquipmentSlotType.Core)]
    [InlineData(EquipmentSlotType.Utility)]
    [InlineData(EquipmentSlotType.Base)]
    [InlineData(EquipmentSlotType.Any)]
    public void CanMountIn_NoneType_MountsAnywhere(EquipmentSlotType slotType)
    {
        ModuleType.None.CanMountIn(slotType).Should().BeTrue();
    }

    #endregion

    #region ModuleTypeExtensions - GetDisplayName

    [Theory]
    [InlineData(ModuleType.Logic, "Logic Module")]
    [InlineData(ModuleType.Battery, "Battery")]
    [InlineData(ModuleType.Generator, "Generator")]
    [InlineData(ModuleType.Weapon, "Weapon")]
    [InlineData(ModuleType.Sensor, "Sensor")]
    [InlineData(ModuleType.Shield, "Shield")]
    [InlineData(ModuleType.Treads, "Treads")]
    [InlineData(ModuleType.Legs, "Legs")]
    [InlineData(ModuleType.Flight, "Flight System")]
    [InlineData(ModuleType.Cargo, "Cargo Module")]
    [InlineData(ModuleType.None, "Unknown Module")]
    public void GetDisplayName_ReturnsCorrectName(ModuleType moduleType, string expected)
    {
        moduleType.GetDisplayName().Should().Be(expected);
    }

    #endregion

    #region AmmoType Enum

    [Fact]
    public void AmmoType_HasAllExpectedValues()
    {
        Enum.GetValues<AmmoType>().Should().HaveCount(4);
        Enum.IsDefined(AmmoType.Basic).Should().BeTrue();
        Enum.IsDefined(AmmoType.Seeker).Should().BeTrue();
        Enum.IsDefined(AmmoType.Orbital).Should().BeTrue();
        Enum.IsDefined(AmmoType.Energy).Should().BeTrue();
    }

    #endregion
}
