using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Components;
using NullAndVoid.Items;
using NullAndVoid.Rendering;

namespace NullAndVoid.UI;

/// <summary>
/// Static helper class for formatting inventory and equipment data for display.
/// </summary>
public static class InventoryDataFormatter
{
    /// <summary>
    /// Get abbreviated module type for compact display.
    /// </summary>
    public static string GetModuleTypeAbbrev(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Logic => "LOGIC",
            ModuleType.Battery => "BATT",
            ModuleType.Generator => "GEN",
            ModuleType.Weapon => "WEAPON",
            ModuleType.Sensor => "SENSOR",
            ModuleType.Shield => "SHIELD",
            ModuleType.Treads => "TREADS",
            ModuleType.Legs => "LEGS",
            ModuleType.Flight => "FLIGHT",
            ModuleType.Cargo => "CARGO",
            _ => "MOD"
        };
    }

    /// <summary>
    /// Format weapon stats for multi-line display.
    /// </summary>
    public static List<string> FormatWeaponStats(WeaponData weapon, int maxWidth)
    {
        var lines = new List<string>();

        // Damage line: DAMAGE: 8-15 x2  CRIT: 15% (x2.5)
        string dmgStr = $"DAMAGE: {weapon.DamageString}";
        string critStr = weapon.CriticalChance > 5
            ? $"CRIT: {weapon.CriticalChance}% (x{weapon.CriticalMultiplier:F1})"
            : "";
        lines.Add(FormatTwoColumn(dmgStr, critStr, maxWidth));

        // Range line: RANGE: 6 tiles  ACCURACY: 85%
        string rangeStr = weapon.Range > 1 ? $"RANGE: {weapon.Range} tiles" : "RANGE: Melee";
        string accStr = weapon.BaseAccuracy > 0 ? $"ACCURACY: {weapon.BaseAccuracy}%" : "";
        lines.Add(FormatTwoColumn(rangeStr, accStr, maxWidth));

        // Energy/Cooldown: ENERGY: 8/shot  COOLDOWN: 2 turns
        string energyStr = weapon.EnergyCost > 0 ? $"ENERGY: {weapon.EnergyCost}/shot" : "";
        string cooldownStr = weapon.Cooldown > 0 ? $"COOLDOWN: {weapon.Cooldown} turns" : "";
        if (!string.IsNullOrEmpty(energyStr) || !string.IsNullOrEmpty(cooldownStr))
            lines.Add(FormatTwoColumn(energyStr, cooldownStr, maxWidth));

        // Effect line
        if (weapon.PrimaryEffect != WeaponEffect.None && weapon.EffectChance > 0)
        {
            string effectStr = $"EFFECT: {weapon.PrimaryEffect} {weapon.EffectChance}%";
            if (weapon.EffectDuration > 0)
                effectStr += $" ({weapon.EffectDuration} turns)";
            lines.Add(effectStr);
        }

        // Special properties
        var specials = new List<string>();
        if (weapon.AreaRadius > 0)
            specials.Add($"Area: {weapon.AreaRadius}");
        if (weapon.IndirectFire)
            specials.Add("Indirect Fire");
        if (weapon.IgnoresCover)
            specials.Add("Ignores Cover");
        if (weapon.KnockbackDistance > 0)
            specials.Add($"Knockback: {weapon.KnockbackDistance}");
        if (weapon.ChainTargets > 0)
            specials.Add($"Chain: {weapon.ChainTargets}");

        if (specials.Count > 0)
            lines.Add("SPECIAL: " + string.Join(", ", specials));

        return lines;
    }

    /// <summary>
    /// Format module stats for multi-line display (non-weapons).
    /// </summary>
    public static List<string> FormatModuleStats(Item item, int maxWidth)
    {
        var lines = new List<string>();

        // Energy stats first
        var energyParts = new List<string>();
        if (item.EnergyOutput > 0)
            energyParts.Add($"PWR +{item.EnergyOutput}");
        if (item.EnergyConsumption > 0)
            energyParts.Add($"DRAIN -{item.EnergyConsumption}");
        if (item.BonusEnergyCapacity != 0)
            energyParts.Add($"CAP {FormatBonus(item.BonusEnergyCapacity)}");
        if (energyParts.Count > 0)
            lines.Add(string.Join("  ", energyParts));

        // Combat stats
        var combatParts = new List<string>();
        if (item.BonusDamage != 0)
            combatParts.Add($"DMG {FormatBonus(item.BonusDamage)}");
        if (item.BonusArmor != 0)
            combatParts.Add($"ARM {FormatBonus(item.BonusArmor)}");
        if (item.BonusHealth != 0)
            combatParts.Add($"HP {FormatBonus(item.BonusHealth)}");
        if (combatParts.Count > 0)
            lines.Add(string.Join("  ", combatParts));

        // Detection/mobility stats
        var mobilityParts = new List<string>();
        if (item.BonusSightRange != 0)
            mobilityParts.Add($"VIS {FormatBonus(item.BonusSightRange)}");
        if (item.BonusSpeed != 0)
            mobilityParts.Add($"SPD {FormatBonus(item.BonusSpeed)}");
        if (item.BonusNoise != 0)
            mobilityParts.Add($"NSE {FormatBonus(item.BonusNoise)}");
        if (mobilityParts.Count > 0)
            lines.Add(string.Join("  ", mobilityParts));

        return lines;
    }

    /// <summary>
    /// Format energy cost info line.
    /// </summary>
    public static string FormatEnergyCost(Item item)
    {
        var parts = new List<string>();

        parts.Add($"Boot: {item.BootCost} NRG");

        if (item.EnergyConsumption > 0)
            parts.Add($"Drain: {item.EnergyConsumption}/turn");
        else if (item.EnergyOutput > 0)
            parts.Add($"Output: +{item.EnergyOutput}/turn");

        return string.Join("  ", parts);
    }

    /// <summary>
    /// Format module health bar.
    /// </summary>
    public static string FormatModuleHealthBar(int current, int max, int width)
    {
        if (max <= 0)
            return "";

        int barWidth = width - 10; // Leave room for "XX/XX" label
        float percent = (float)current / max;
        int filledChars = (int)(barWidth * percent);

        string bar = new string('█', filledChars) + new string('░', barWidth - filledChars);
        return $"{bar} {current}/{max}";
    }

    /// <summary>
    /// Get color for module health bar based on percentage.
    /// </summary>
    public static Color GetHealthBarColor(int current, int max)
    {
        if (max <= 0)
            return ASCIIColors.TextDisabled;

        float percent = (float)current / max;
        if (percent <= 0)
            return ASCIIColors.AlertDanger;
        if (percent <= 0.25f)
            return ASCIIColors.AlertDanger;
        if (percent <= 0.5f)
            return ASCIIColors.AlertWarning;
        if (percent <= 0.75f)
            return ASCIIColors.AlertInfo;
        return ASCIIColors.AlertSuccess;
    }

    /// <summary>
    /// Format comparison delta with indicator.
    /// </summary>
    public static (string indicator, Color color) FormatStatDelta(int newVal, int oldVal, bool higherIsBetter = true)
    {
        int diff = newVal - oldVal;

        if (diff == 0)
            return ("=", ASCIIColors.TextMuted);

        bool isBetter = higherIsBetter ? diff > 0 : diff < 0;
        int magnitude = Math.Abs(diff);

        string indicator;
        if (magnitude >= 5)
            indicator = isBetter ? "+++" : "---";
        else if (magnitude >= 2)
            indicator = isBetter ? "+" : "-";
        else
            indicator = isBetter ? "+" : "-";

        Color color = isBetter ? ASCIIColors.AlertSuccess : ASCIIColors.AlertDanger;
        return (indicator, color);
    }

    /// <summary>
    /// Calculate equip preview data.
    /// </summary>
    public static EquipPreview CalculateEquipPreview(Item item, Equipment equipment, Attributes attributes)
    {
        var preview = new EquipPreview
        {
            BootCost = item.BootCost,
            CurrentEnergy = attributes.CurrentEnergyReserve,
            CurrentBalance = equipment.NetEnergyBalance,
            DisplacedItem = null
        };

        // Calculate energy after boot cost
        preview.EnergyAfterBoot = preview.CurrentEnergy - item.BootCost;
        preview.CanAffordBoot = preview.EnergyAfterBoot >= 0;

        // Find what would be displaced
        var slot = equipment.FindEmptySlot(item.SlotType);
        if (slot == null)
        {
            // Would need to displace something
            var targetType = item.SlotType == EquipmentSlotType.Any
                ? EquipmentSlotType.Core : item.SlotType;
            preview.DisplacedItem = equipment.GetItemInSlot(targetType, 0);
        }

        // Calculate new balance
        int newOutput = equipment.TotalEnergyOutput + item.EnergyOutput;
        int newConsumption = equipment.TotalEnergyConsumption + item.EnergyConsumption;

        // Account for displaced item
        if (preview.DisplacedItem != null)
        {
            newOutput -= preview.DisplacedItem.EnergyOutput;
            newConsumption -= preview.DisplacedItem.EnergyConsumption;
        }

        preview.NewBalance = newOutput - newConsumption;
        preview.WillCauseDeficit = preview.NewBalance < 0 && equipment.NetEnergyBalance >= 0;

        return preview;
    }

    /// <summary>
    /// Format energy balance display.
    /// </summary>
    public static (string text, Color color) FormatEnergyBalance(int balance)
    {
        if (balance > 0)
            return ($"+{balance}/turn [SURPLUS]", ASCIIColors.AlertSuccess);
        if (balance < 0)
            return ($"{balance}/turn [DEFICIT!]", ASCIIColors.AlertDanger);
        return ("0/turn [BALANCED]", ASCIIColors.TextNormal);
    }

    /// <summary>
    /// Format energy bar.
    /// </summary>
    public static string FormatEnergyBar(int current, int max, int width)
    {
        if (max <= 0)
            return new string('░', width);

        float percent = Mathf.Clamp((float)current / max, 0f, 1f);
        int filledChars = (int)(width * percent);

        return new string('█', filledChars) + new string('░', width - filledChars);
    }

    /// <summary>
    /// Get item state indicator prefix.
    /// </summary>
    public static string GetItemStateIndicator(Item item)
    {
        if (item.IsDisabled)
            return "*"; // Disabled
        if (item.IsToggleable && !item.IsActive)
            return "~"; // Inactive toggleable
        if (!item.IsIdentified && item.Type != ItemType.Consumable)
            return "?"; // Unidentified (appended to name, not prefix)
        return "";
    }

    /// <summary>
    /// Get item state suffix.
    /// </summary>
    public static string GetItemStateSuffix(Item item)
    {
        if (!item.IsIdentified && item.Type != ItemType.Consumable)
            return "?";
        return "";
    }

    /// <summary>
    /// Format two columns of text within a max width.
    /// </summary>
    private static string FormatTwoColumn(string left, string right, int maxWidth)
    {
        if (string.IsNullOrEmpty(right))
            return left;

        int padding = maxWidth - left.Length - right.Length;
        if (padding < 2)
            return left + "  " + right; // Minimum 2 spaces

        return left + new string(' ', padding) + right;
    }

    /// <summary>
    /// Format bonus value with sign.
    /// </summary>
    private static string FormatBonus(int value) => value > 0 ? $"+{value}" : value.ToString();
}

/// <summary>
/// Preview data for equipping an item.
/// </summary>
public struct EquipPreview
{
    public int BootCost;
    public int CurrentEnergy;
    public int EnergyAfterBoot;
    public int CurrentBalance;
    public int NewBalance;
    public bool WillCauseDeficit;
    public bool CanAffordBoot;
    public Item? DisplacedItem;
}

/// <summary>
/// Data for comparing two items.
/// </summary>
public struct ItemComparison
{
    public Item NewItem;
    public Item? OldItem;
    public EquipmentSlotType SlotType;
    public int SlotIndex;
}
