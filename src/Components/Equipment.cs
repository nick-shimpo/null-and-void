using System.Collections.Generic;
using System.Linq;
using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Items;

namespace NullAndVoid.Components;

/// <summary>
/// Equipment component managing equipped items in slots.
/// Slots: 2 Core, 2 Utility, 2 Base
/// Calculates total stat bonuses including energy production/consumption.
/// </summary>
public partial class Equipment : Node
{
    // Equipment slots
    private Item?[] _coreSlots = new Item?[2];
    private Item?[] _utilitySlots = new Item?[2];
    private Item?[] _baseSlots = new Item?[2];

    // Mount point damage tracking
    private bool[] _coreMountsDamaged = new bool[2];
    private bool[] _utilityMountsDamaged = new bool[2];
    private bool[] _baseMountsDamaged = new bool[2];

    // Reference to Attributes component for updates
    private Attributes? _attributes;

    // Random for mount failure rolls
    private static readonly System.Random _random = new();

    /// <summary>
    /// Chance of mount failure when equipping unidentified module.
    /// </summary>
    public const float MountFailureChance = 0.35f;

    /// <summary>
    /// Chance of mount point damage when a module is destroyed.
    /// </summary>
    public const float MountDamageOnDestroyChance = 0.15f;

    [Signal] public delegate void ItemEquippedEventHandler(Item item, EquipmentSlotType slotType, int slotIndex);
    [Signal] public delegate void ItemUnequippedEventHandler(Item item, EquipmentSlotType slotType, int slotIndex);
    [Signal] public delegate void EquipmentChangedEventHandler();
    [Signal] public delegate void ModuleToggledEventHandler(Item item, bool isActive);
    [Signal] public delegate void MountFailedEventHandler(Item item, EquipmentSlotType slotType, int slotIndex);
    [Signal] public delegate void MountPointDamagedEventHandler(EquipmentSlotType slotType, int slotIndex);
    [Signal] public delegate void MountPointRepairedEventHandler(EquipmentSlotType slotType, int slotIndex);
    [Signal] public delegate void ModuleIdentifiedEventHandler(Item item);
    [Signal] public delegate void EquipFailedEventHandler(Item item, string reason);
    [Signal] public delegate void ModulesDeactivatedEventHandler(int energySaved);

    // Computed stat bonuses from all equipped items
    public int TotalBonusDamage { get; private set; }
    public int TotalBonusArmor { get; private set; }
    public int TotalBonusHealth { get; private set; }
    public int TotalBonusSightRange { get; private set; }

    // Energy stats
    public int TotalEnergyOutput { get; private set; }
    public int TotalEnergyConsumption { get; private set; }
    public int TotalBonusEnergyCapacity { get; private set; }

    // Mobility/detection stats
    public int TotalBonusSpeed { get; private set; }
    public int TotalBonusNoise { get; private set; }

    // Cargo expansion stats
    public int TotalBonusMountPoints { get; private set; }

    // Derived energy values
    public int NetEnergyBalance => TotalEnergyOutput - TotalEnergyConsumption;
    public bool HasEnergyDeficit => NetEnergyBalance < 0;

    #region Shield System

    /// <summary>
    /// Current shield points (energy shielding pool).
    /// Absorbs all damage before module armor.
    /// </summary>
    public int CurrentShield { get; private set; } = 0;

    /// <summary>
    /// Maximum shield capacity from all equipped shield generators.
    /// </summary>
    public int MaxShield { get; private set; } = 0;

    /// <summary>
    /// Total shield points generated per turn from active shield modules.
    /// </summary>
    public int TotalShieldOutput { get; private set; } = 0;

    /// <summary>
    /// Total energy cost per turn for shield generation.
    /// </summary>
    public int TotalShieldEnergyCost { get; private set; } = 0;

    /// <summary>
    /// Whether shields are currently active (have points).
    /// </summary>
    public bool HasActiveShield => CurrentShield > 0;

    /// <summary>
    /// Shield percentage for display.
    /// </summary>
    public float ShieldPercent => MaxShield > 0 ? (float)CurrentShield / MaxShield : 0f;

    [Signal] public delegate void ShieldChangedEventHandler(int current, int max);
    [Signal] public delegate void ShieldDepletedEventHandler();

    #endregion

    /// <summary>
    /// Set the Attributes component to update when equipment changes.
    /// Also connects energy depletion handling.
    /// </summary>
    public void SetAttributesComponent(Attributes attributes)
    {
        // Disconnect from previous attributes if any
        if (_attributes != null && IsInstanceValid(_attributes))
        {
            if (_attributes.IsConnected(Attributes.SignalName.EnergyDepleted, Callable.From(OnEnergyDepleted)))
            {
                _attributes.Disconnect(Attributes.SignalName.EnergyDepleted, Callable.From(OnEnergyDepleted));
            }
        }

        _attributes = attributes;

        // Connect energy depletion handling
        if (_attributes != null)
        {
            _attributes.Connect(Attributes.SignalName.EnergyDepleted, Callable.From(OnEnergyDepleted));
        }

        RecalculateStats();
    }

    /// <summary>
    /// Handle energy depletion by force-deactivating modules.
    /// </summary>
    private void OnEnergyDepleted()
    {
        int deficit = -_attributes!.EnergyBalance;
        if (deficit <= 0)
            return;

        int saved = ForceDeactivateModules(deficit);
        if (saved > 0)
        {
            EmitSignal(SignalName.ModulesDeactivated, saved);
            GD.Print($"Energy depleted! Emergency shutdown saved {saved} energy.");
        }
    }

    /// <summary>
    /// Get all equipped items.
    /// </summary>
    public IEnumerable<Item> GetAllEquippedItems()
    {
        foreach (var item in _coreSlots)
            if (item != null)
                yield return item;
        foreach (var item in _utilitySlots)
            if (item != null)
                yield return item;
        foreach (var item in _baseSlots)
            if (item != null)
                yield return item;
    }

    /// <summary>
    /// Get item in a specific slot.
    /// </summary>
    public Item? GetItemInSlot(EquipmentSlotType slotType, int index)
    {
        var slots = GetSlotArray(slotType);
        if (slots == null || index < 0 || index >= slots.Length)
            return null;
        return slots[index];
    }

    /// <summary>
    /// Equip an item to a slot. Returns the previously equipped item if any.
    /// Returns null if equip failed (mount damaged, mount failure, or insufficient energy).
    /// </summary>
    public Item? Equip(Item item, EquipmentSlotType slotType, int index)
    {
        // Validate slot type compatibility (legacy check)
        if (item.SlotType != EquipmentSlotType.Any && item.SlotType != slotType)
        {
            GD.Print($"Cannot equip {item.Name} to {slotType} slot - requires {item.SlotType}");
            return null;
        }

        // Validate ModuleCategory compatibility (new system)
        if (item.ModuleCategory != ModuleType.None && !item.ModuleCategory.CanMountIn(slotType))
        {
            var requiredSlot = item.ModuleCategory.GetRequiredSlotType();
            GD.Print($"Cannot mount {item.ModuleCategory} in {slotType} slot - requires {requiredSlot}");
            return null;
        }

        var slots = GetSlotArray(slotType);
        if (slots == null || index < 0 || index >= slots.Length)
            return null;

        // Check if mount point is damaged
        if (IsMountPointDamaged(slotType, index))
        {
            GD.Print($"Cannot equip {item.Name} - mount point is damaged. Use a Repair Kit to fix it.");
            return null;
        }

        // Check and consume boot cost energy
        if (item.BootCost > 0 && _attributes != null)
        {
            if (!_attributes.TryConsumeEnergy(item.BootCost))
            {
                GD.Print($"Cannot equip {item.GetDisplayName()} - insufficient energy. Need {item.BootCost}, have {_attributes.CurrentEnergyReserve}.");
                EmitSignal(SignalName.EquipFailed, item, "Insufficient energy for boot cost");
                return null;
            }
            GD.Print($"Consumed {item.BootCost} energy to mount {item.GetDisplayName()}");
        }

        // Initialize module armor BEFORE mount failure check
        item.InitializeArmor();

        // Check for mount failure with unidentified modules
        if (!item.IsIdentified && item.Type == ItemType.Module)
        {
            // Identify the module first (player sees what they're dealing with)
            item.Identify();
            EmitSignal(SignalName.ModuleIdentified, item);

            if (_random.NextDouble() < MountFailureChance)
            {
                // Mount failure! Both mount point AND item are damaged and jammed together
                DamageMountPoint(slotType, index);

                // Damage the item - deplete its module armor completely (disabled state)
                item.CurrentModuleArmor = 0;

                EmitSignal(SignalName.MountFailed, item, (int)slotType, index);
                GD.Print($"Mount failure! {item.Name} jammed into mount point. Both are damaged!");
                // Note: Boot cost energy is still consumed (risk of mounting unidentified)
                // Item continues to be equipped below, but in damaged/jammed state
            }
            else
            {
                GD.Print($"Module identified and mounted: {item.Name}");
            }
        }

        // Get previous item
        var previousItem = slots[index];

        // Equip new item
        slots[index] = item;

        // Emit signals
        if (previousItem != null)
            EmitSignal(SignalName.ItemUnequipped, previousItem, (int)slotType, index);

        EmitSignal(SignalName.ItemEquipped, item, (int)slotType, index);
        EmitSignal(SignalName.EquipmentChanged);

        RecalculateStats();

        return previousItem;
    }

    /// <summary>
    /// Check if a mount point is damaged.
    /// </summary>
    public bool IsMountPointDamaged(EquipmentSlotType slotType, int index)
    {
        var damagedArray = GetDamagedArray(slotType);
        if (damagedArray == null || index < 0 || index >= damagedArray.Length)
            return false;
        return damagedArray[index];
    }

    /// <summary>
    /// Damage a mount point, preventing equipment until repaired.
    /// </summary>
    public void DamageMountPoint(EquipmentSlotType slotType, int index)
    {
        var damagedArray = GetDamagedArray(slotType);
        if (damagedArray == null || index < 0 || index >= damagedArray.Length)
            return;

        damagedArray[index] = true;
        EmitSignal(SignalName.MountPointDamaged, (int)slotType, index);
        GD.Print($"Mount point {slotType} {index + 1} has been damaged!");
    }

    /// <summary>
    /// Repair a damaged mount point.
    /// </summary>
    public bool RepairMountPoint(EquipmentSlotType slotType, int index)
    {
        var damagedArray = GetDamagedArray(slotType);
        if (damagedArray == null || index < 0 || index >= damagedArray.Length)
            return false;

        if (!damagedArray[index])
            return false; // Not damaged

        damagedArray[index] = false;
        EmitSignal(SignalName.MountPointRepaired, (int)slotType, index);
        GD.Print($"Mount point {slotType} {index + 1} has been repaired.");
        return true;
    }

    /// <summary>
    /// Get the first damaged mount point, if any.
    /// </summary>
    public (EquipmentSlotType slotType, int index)? GetFirstDamagedMountPoint()
    {
        for (int i = 0; i < 2; i++)
            if (_coreMountsDamaged[i])
                return (EquipmentSlotType.Core, i);
        for (int i = 0; i < 2; i++)
            if (_utilityMountsDamaged[i])
                return (EquipmentSlotType.Utility, i);
        for (int i = 0; i < 2; i++)
            if (_baseMountsDamaged[i])
                return (EquipmentSlotType.Base, i);
        return null;
    }

    /// <summary>
    /// Check if any mount points are damaged.
    /// </summary>
    public bool HasDamagedMountPoints()
    {
        return GetFirstDamagedMountPoint().HasValue;
    }

    private bool[]? GetDamagedArray(EquipmentSlotType slotType)
    {
        return slotType switch
        {
            EquipmentSlotType.Core => _coreMountsDamaged,
            EquipmentSlotType.Utility => _utilityMountsDamaged,
            EquipmentSlotType.Base => _baseMountsDamaged,
            _ => null
        };
    }

    /// <summary>
    /// Unequip an item from a slot.
    /// Returns null if slot is empty or mount point is damaged (item jammed).
    /// </summary>
    public Item? Unequip(EquipmentSlotType slotType, int index)
    {
        var slots = GetSlotArray(slotType);
        if (slots == null || index < 0 || index >= slots.Length)
            return null;

        var item = slots[index];
        if (item == null)
            return null;

        // Cannot unequip from damaged mount point - item is jammed
        if (IsMountPointDamaged(slotType, index))
        {
            GD.Print($"Cannot unequip {item.Name} - mount point is damaged. Item is jammed! Repair mount first.");
            return null;
        }

        slots[index] = null;

        EmitSignal(SignalName.ItemUnequipped, item, (int)slotType, index);
        EmitSignal(SignalName.EquipmentChanged);

        RecalculateStats();

        return item;
    }

    /// <summary>
    /// Check if an item is jammed in a damaged mount point.
    /// </summary>
    public bool IsItemJammed(EquipmentSlotType slotType, int index)
    {
        var item = GetItemInSlot(slotType, index);
        return item != null && IsMountPointDamaged(slotType, index);
    }

    /// <summary>
    /// Find the first empty slot for an item type.
    /// </summary>
    public (EquipmentSlotType slotType, int index)? FindEmptySlot(EquipmentSlotType preferredType)
    {
        // If item can go in any slot, check all in order
        if (preferredType == EquipmentSlotType.Any)
        {
            var coreSlot = FindEmptyInArray(_coreSlots);
            if (coreSlot >= 0)
                return (EquipmentSlotType.Core, coreSlot);

            var utilitySlot = FindEmptyInArray(_utilitySlots);
            if (utilitySlot >= 0)
                return (EquipmentSlotType.Utility, utilitySlot);

            var baseSlot = FindEmptyInArray(_baseSlots);
            if (baseSlot >= 0)
                return (EquipmentSlotType.Base, baseSlot);

            return null;
        }

        // Check preferred slot type
        var slots = GetSlotArray(preferredType);
        if (slots != null)
        {
            var emptyIndex = FindEmptyInArray(slots);
            if (emptyIndex >= 0)
                return (preferredType, emptyIndex);
        }

        return null;
    }

    private int FindEmptyInArray(Item?[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                return i;
        }
        return -1;
    }

    private Item?[]? GetSlotArray(EquipmentSlotType slotType)
    {
        return slotType switch
        {
            EquipmentSlotType.Core => _coreSlots,
            EquipmentSlotType.Utility => _utilitySlots,
            EquipmentSlotType.Base => _baseSlots,
            _ => null
        };
    }

    private void RecalculateStats()
    {
        // Reset all totals
        TotalBonusDamage = 0;
        TotalBonusArmor = 0;
        TotalBonusHealth = 0;
        TotalBonusSightRange = 0;
        TotalEnergyOutput = 0;
        TotalEnergyConsumption = 0;
        TotalBonusEnergyCapacity = 0;
        TotalBonusSpeed = 0;
        TotalBonusNoise = 0;
        TotalBonusMountPoints = 0;

        // Reset shield stats
        MaxShield = 0;
        TotalShieldOutput = 0;
        TotalShieldEnergyCost = 0;

        foreach (var item in GetAllEquippedItems())
        {
            // Disabled modules (armor destroyed) provide NO benefits
            if (item.IsDisabled)
            {
                continue;
            }

            // Skip inactive toggleable modules for energy/effect calculations
            bool contributesEffects = !item.IsToggleable || item.IsActive;

            // Combat stats always apply (structural bonuses) for non-disabled modules
            TotalBonusArmor += item.BonusArmor;
            TotalBonusHealth += item.BonusHealth;

            // Cargo expansion always applies (passive structural)
            TotalBonusMountPoints += item.BonusMountPoints;

            // Energy capacity always applies (it's passive storage) for non-disabled modules
            TotalBonusEnergyCapacity += item.BonusEnergyCapacity;

            // These only apply when module is active and not disabled
            if (contributesEffects)
            {
                TotalBonusDamage += item.BonusDamage;
                TotalBonusSightRange += item.BonusSightRange;
                TotalEnergyOutput += item.EnergyOutput;
                TotalEnergyConsumption += item.EnergyConsumption;
                TotalBonusSpeed += item.BonusSpeed;
                TotalBonusNoise += item.BonusNoise;

                // Shield generator stats (only when active)
                MaxShield += item.ShieldCapacity;
                TotalShieldOutput += item.ShieldOutput;
                TotalShieldEnergyCost += item.ShieldEnergyCost;
            }
        }

        // Cap current shield if max decreased
        if (CurrentShield > MaxShield)
        {
            CurrentShield = MaxShield;
            EmitSignal(SignalName.ShieldChanged, CurrentShield, MaxShield);
        }

        // Update Attributes component if linked
        // Note: Passing 0 for integrity - modules should NOT provide integrity, only armor
        _attributes?.RecalculateFromEquipment(
            TotalEnergyOutput, TotalEnergyConsumption, TotalBonusEnergyCapacity,
            0, TotalBonusArmor, TotalBonusDamage,
            TotalBonusSpeed, TotalBonusSightRange, TotalBonusNoise);
    }

    /// <summary>
    /// Toggle a module on/off. Only works for toggleable modules.
    /// </summary>
    public bool ToggleModule(EquipmentSlotType slotType, int index)
    {
        var item = GetItemInSlot(slotType, index);
        if (item == null || !item.IsToggleable)
            return false;

        item.IsActive = !item.IsActive;
        EmitSignal(SignalName.ModuleToggled, item, item.IsActive);
        EmitSignal(SignalName.EquipmentChanged);
        RecalculateStats();

        GD.Print($"Module {item.Name} toggled {(item.IsActive ? "ON" : "OFF")}");
        return true;
    }

    /// <summary>
    /// Force deactivate modules to reduce energy consumption.
    /// Called when energy reserve is depleted.
    /// Deactivates highest-consumption toggleable modules first.
    /// </summary>
    public int ForceDeactivateModules(int energyDeficit)
    {
        var toggleableItems = GetAllEquippedItems()
            .Where(i => i.IsToggleable && i.IsActive && i.EnergyConsumption > 0)
            .OrderByDescending(i => i.EnergyConsumption)
            .ToList();

        int energySaved = 0;

        foreach (var item in toggleableItems)
        {
            if (energySaved >= energyDeficit)
                break;

            item.IsActive = false;
            energySaved += item.EnergyConsumption;
            EmitSignal(SignalName.ModuleToggled, item, false);
            GD.Print($"Emergency shutdown: {item.Name} (saved {item.EnergyConsumption} energy)");
        }

        if (energySaved > 0)
        {
            EmitSignal(SignalName.EquipmentChanged);
            RecalculateStats();
        }

        return energySaved;
    }

    /// <summary>
    /// Get a summary of all slots for display.
    /// </summary>
    public List<EquipmentSlotInfo> GetAllSlots()
    {
        var slots = new List<EquipmentSlotInfo>();

        for (int i = 0; i < 2; i++)
            slots.Add(new EquipmentSlotInfo(EquipmentSlotType.Core, i, _coreSlots[i], _coreMountsDamaged[i]));
        for (int i = 0; i < 2; i++)
            slots.Add(new EquipmentSlotInfo(EquipmentSlotType.Utility, i, _utilitySlots[i], _utilityMountsDamaged[i]));
        for (int i = 0; i < 2; i++)
            slots.Add(new EquipmentSlotInfo(EquipmentSlotType.Base, i, _baseSlots[i], _baseMountsDamaged[i]));

        return slots;
    }

    #region Weapon Management

    /// <summary>
    /// Get all equipped weapons.
    /// </summary>
    public IEnumerable<Item> GetEquippedWeapons()
    {
        return GetAllEquippedItems().Where(item => item.IsWeapon);
    }

    /// <summary>
    /// Get all equipped melee weapons.
    /// </summary>
    public IEnumerable<Item> GetMeleeWeapons()
    {
        return GetEquippedWeapons().Where(item => item.IsMeleeWeapon);
    }

    /// <summary>
    /// Get all equipped ranged weapons.
    /// </summary>
    public IEnumerable<Item> GetRangedWeapons()
    {
        return GetEquippedWeapons().Where(item => item.IsRangedWeapon);
    }

    /// <summary>
    /// Get all weapons that are ready to fire (not on cooldown).
    /// </summary>
    public IEnumerable<Item> GetReadyWeapons()
    {
        return GetEquippedWeapons().Where(item => item.IsWeaponReady);
    }

    /// <summary>
    /// Get the primary melee weapon (first equipped melee weapon).
    /// Used for bump-to-attack.
    /// </summary>
    public Item? GetPrimaryMeleeWeapon()
    {
        return GetMeleeWeapons().FirstOrDefault();
    }

    /// <summary>
    /// Get a weapon by its slot index (1-6 for hotkey mapping).
    /// Weapons are numbered: Utility1=1, Utility2=2, Core1=3, Core2=4, Base1=5, Base2=6
    /// </summary>
    public Item? GetWeaponByHotkey(int hotkey)
    {
        if (hotkey < 1 || hotkey > 6)
            return null;

        // Map hotkeys to slots (prioritize utility slots for weapons)
        var item = hotkey switch
        {
            1 => _utilitySlots[0],
            2 => _utilitySlots[1],
            3 => _coreSlots[0],
            4 => _coreSlots[1],
            5 => _baseSlots[0],
            6 => _baseSlots[1],
            _ => null
        };

        return item?.IsWeapon == true ? item : null;
    }

    /// <summary>
    /// Get list of all equipped weapons with their hotkey assignments.
    /// </summary>
    public List<(int hotkey, Item weapon)> GetWeaponsWithHotkeys()
    {
        var result = new List<(int, Item)>();

        for (int i = 1; i <= 6; i++)
        {
            var weapon = GetWeaponByHotkey(i);
            if (weapon != null)
            {
                result.Add((i, weapon));
            }
        }

        return result;
    }

    /// <summary>
    /// Advance cooldowns for all equipped weapons.
    /// Call this at the start of each turn.
    /// </summary>
    public void TickWeaponCooldowns()
    {
        foreach (var item in GetEquippedWeapons())
        {
            item.WeaponData?.TickCooldown();
        }
    }

    /// <summary>
    /// Check if entity has any usable weapons.
    /// </summary>
    public bool HasWeapons => GetEquippedWeapons().Any();

    /// <summary>
    /// Check if entity has any melee weapons.
    /// </summary>
    public bool HasMeleeWeapon => GetMeleeWeapons().Any();

    /// <summary>
    /// Check if entity has any ranged weapons.
    /// </summary>
    public bool HasRangedWeapon => GetRangedWeapons().Any();

    #endregion

    #region Module Damage System

    /// <summary>
    /// Route incoming damage to equipped modules based on exposure ratings.
    /// Higher exposure = more likely to be hit. Armor modules tank for others.
    /// Returns damage that passes through to core integrity.
    /// </summary>
    public int RouteDamageToModules(int damage)
    {
        if (damage <= 0)
            return 0;

        // Shield modules (ModuleType.Shield) absorb damage first
        var shieldModules = GetAllEquippedItems()
            .Where(i => i.ModuleCategory == ModuleType.Shield && !i.IsDisabled)
            .ToList();

        foreach (var shield in shieldModules)
        {
            damage = shield.TakeModuleDamage(damage);
            if (damage <= 0)
                return 0;
        }

        // Get all non-disabled, non-shield modules with exposure
        var modules = GetAllEquippedItems()
            .Where(m => !m.IsDisabled && m.ModuleCategory != ModuleType.Shield)
            .ToList();

        if (modules.Count == 0)
            return damage;

        // Weighted selection by exposure rating
        int totalExposure = modules.Sum(m => m.Exposure);
        if (totalExposure <= 0)
            return damage;

        int roll = _random.Next(totalExposure);
        Item? targetModule = null;
        int cumulative = 0;

        foreach (var module in modules)
        {
            cumulative += module.Exposure;
            if (roll < cumulative)
            {
                targetModule = module;
                break;
            }
        }

        if (targetModule == null)
            return damage;

        // Apply damage to target module
        int previousArmor = targetModule.CurrentModuleArmor;
        damage = targetModule.TakeModuleDamage(damage);

        GD.Print($"Module {targetModule.Name} took {previousArmor - targetModule.CurrentModuleArmor} damage (Exposure: {targetModule.Exposure})");

        // Check if module was destroyed
        if (targetModule.IsDisabled)
        {
            // Find which slot this module is in for mount damage check
            CheckMountDamageOnDestroy(targetModule);
            RecalculateStats();
        }

        // Armor module tank behavior: if still functional after hit, redirect excess damage
        if (targetModule.ModuleCategory == ModuleType.Armor
            && !targetModule.IsDisabled
            && damage > 0)
        {
            // Armor module tanks for others - recursively route remaining damage
            return RouteDamageToModules(damage);
        }

        return damage;
    }

    /// <summary>
    /// Check if mount point should be damaged when a module is destroyed.
    /// </summary>
    private void CheckMountDamageOnDestroy(Item destroyedModule)
    {
        // Find the slot containing this module
        for (int i = 0; i < _coreSlots.Length; i++)
        {
            if (_coreSlots[i] == destroyedModule)
            {
                if (_random.NextDouble() < MountDamageOnDestroyChance)
                    DamageMountPoint(EquipmentSlotType.Core, i);
                return;
            }
        }
        for (int i = 0; i < _utilitySlots.Length; i++)
        {
            if (_utilitySlots[i] == destroyedModule)
            {
                if (_random.NextDouble() < MountDamageOnDestroyChance)
                    DamageMountPoint(EquipmentSlotType.Utility, i);
                return;
            }
        }
        for (int i = 0; i < _baseSlots.Length; i++)
        {
            if (_baseSlots[i] == destroyedModule)
            {
                if (_random.NextDouble() < MountDamageOnDestroyChance)
                    DamageMountPoint(EquipmentSlotType.Base, i);
                return;
            }
        }
    }

    /// <summary>
    /// Identify a module using an Analyzer item.
    /// </summary>
    public bool IdentifyModule(Item module)
    {
        if (module.IsIdentified)
            return false;

        module.Identify();
        EmitSignal(SignalName.ModuleIdentified, module);
        GD.Print($"Analyzer identified: {module.Name}");
        return true;
    }

    #endregion

    #region Shield System Methods

    /// <summary>
    /// Process shield regeneration each turn.
    /// Consumes energy from reserves and regenerates shield points.
    /// Called by player during turn start.
    /// </summary>
    public void ProcessShieldTick()
    {
        if (TotalShieldOutput <= 0 || _attributes == null)
            return;

        // Check if we can afford the energy cost
        if (TotalShieldEnergyCost > 0)
        {
            if (!_attributes.TryConsumeEnergy(TotalShieldEnergyCost))
            {
                // Can't afford shield generation this turn
                GD.Print("Insufficient energy for shield generation");
                return;
            }
        }

        // Regenerate shields up to max capacity
        int previousShield = CurrentShield;
        CurrentShield = System.Math.Min(MaxShield, CurrentShield + TotalShieldOutput);

        if (CurrentShield != previousShield)
        {
            EmitSignal(SignalName.ShieldChanged, CurrentShield, MaxShield);
            GD.Print($"Shield regenerated: {previousShield} -> {CurrentShield}/{MaxShield}");
        }
    }

    /// <summary>
    /// Absorb damage with energy shields first.
    /// Returns damage that passes through to modules/integrity.
    /// </summary>
    public int AbsorbDamageWithShield(int damage)
    {
        if (CurrentShield <= 0 || damage <= 0)
            return damage;

        int absorbed = System.Math.Min(damage, CurrentShield);
        CurrentShield -= absorbed;
        int remaining = damage - absorbed;

        EmitSignal(SignalName.ShieldChanged, CurrentShield, MaxShield);

        if (CurrentShield <= 0)
        {
            EmitSignal(SignalName.ShieldDepleted);
            GD.Print("Shields depleted!");
        }
        else
        {
            GD.Print($"Shield absorbed {absorbed} damage ({CurrentShield}/{MaxShield} remaining)");
        }

        return remaining;
    }

    #endregion
}

/// <summary>
/// Info about a single equipment slot.
/// </summary>
public record EquipmentSlotInfo(EquipmentSlotType SlotType, int Index, Item? Item, bool IsMountDamaged = false)
{
    public string SlotName => $"{SlotType} {Index + 1}";
    public bool IsEmpty => Item == null;
    public bool IsUsable => !IsMountDamaged && (Item == null || !Item.IsDisabled);
}
