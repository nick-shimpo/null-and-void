using Godot;
using NullAndVoid.Combat;

namespace NullAndVoid.Items;

/// <summary>
/// Types of ammunition that weapons can consume.
/// </summary>
public enum AmmoType
{
    /// <summary>
    /// Standard ballistic rounds (bullets, nails, rivets).
    /// Common, used by projectile weapons.
    /// </summary>
    Basic,

    /// <summary>
    /// Guided missile fuel/propellant.
    /// Used by seeker weapons that pathfind to targets.
    /// </summary>
    Seeker,

    /// <summary>
    /// Orbital strike rocket propellant.
    /// Rare, used for devastating low-orbit strikes.
    /// </summary>
    Orbital,

    /// <summary>
    /// Energy cells/power packs.
    /// Used by energy weapons with limited charges.
    /// </summary>
    Energy
}

/// <summary>
/// Represents a stack of ammunition that can be stored in inventory
/// and consumed by weapons when firing.
/// </summary>
public partial class Ammunition : Resource
{
    #region Identity

    /// <summary>
    /// Unique identifier for this ammo type.
    /// </summary>
    [Export] public string AmmoId { get; set; } = "";

    /// <summary>
    /// Display name of this ammunition.
    /// </summary>
    [Export] public string Name { get; set; } = "Unknown Ammo";

    /// <summary>
    /// Description of the ammunition.
    /// </summary>
    [Export] public string Description { get; set; } = "";

    /// <summary>
    /// The type category of this ammunition.
    /// </summary>
    [Export] public AmmoType Type { get; set; } = AmmoType.Basic;

    #endregion

    #region Quantity

    /// <summary>
    /// Current quantity in this stack.
    /// </summary>
    [Export] public int Quantity { get; set; } = 1;

    /// <summary>
    /// Maximum stack size for this ammo type.
    /// </summary>
    [Export] public int MaxStack { get; set; } = 50;

    /// <summary>
    /// Whether this stack is full.
    /// </summary>
    public bool IsFull => Quantity >= MaxStack;

    /// <summary>
    /// Whether this stack is empty.
    /// </summary>
    public bool IsEmpty => Quantity <= 0;

    #endregion

    #region Damage Modifiers

    /// <summary>
    /// Damage multiplier when this ammo is used.
    /// 1.0 = normal, 1.5 = +50% damage.
    /// </summary>
    [Export] public float DamageMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// If set, overrides the weapon's damage type.
    /// Useful for specialty ammo like incendiary rounds.
    /// </summary>
    public DamageType? OverrideDamageType { get; set; } = null;

    /// <summary>
    /// Status effect applied when hitting with this ammo.
    /// </summary>
    [Export] public WeaponEffect AppliedEffect { get; set; } = WeaponEffect.None;

    /// <summary>
    /// Chance to apply the effect (0-100).
    /// </summary>
    [Export] public int EffectChance { get; set; } = 0;

    /// <summary>
    /// Duration of applied effect in turns.
    /// </summary>
    [Export] public int EffectDuration { get; set; } = 0;

    /// <summary>
    /// Bonus accuracy when using this ammo (can be negative).
    /// </summary>
    [Export] public int AccuracyModifier { get; set; } = 0;

    /// <summary>
    /// Armor penetration bonus.
    /// </summary>
    [Export] public int PenetrationBonus { get; set; } = 0;

    #endregion

    #region Visuals

    /// <summary>
    /// Display color for this ammunition type.
    /// </summary>
    [Export] public Color DisplayColor { get; set; } = new Color(0.7f, 0.7f, 0.5f);

    /// <summary>
    /// Rarity of this ammunition (affects drop rates).
    /// </summary>
    [Export] public ItemRarity Rarity { get; set; } = ItemRarity.Common;

    #endregion

    #region Methods

    /// <summary>
    /// Consume a specified amount of ammo.
    /// Returns the amount actually consumed.
    /// </summary>
    public int Consume(int amount)
    {
        int consumed = Mathf.Min(amount, Quantity);
        Quantity -= consumed;
        return consumed;
    }

    /// <summary>
    /// Add ammo to this stack.
    /// Returns the amount that couldn't be added (overflow).
    /// </summary>
    public int Add(int amount)
    {
        int canAdd = MaxStack - Quantity;
        int added = Mathf.Min(amount, canAdd);
        Quantity += added;
        return amount - added;  // Return overflow
    }

    /// <summary>
    /// Check if this ammo can stack with another.
    /// Ammo stacks if they have the same AmmoId.
    /// </summary>
    public bool CanStackWith(Ammunition other)
    {
        return AmmoId == other.AmmoId && !IsFull;
    }

    /// <summary>
    /// Create a copy of this ammunition with specified quantity.
    /// </summary>
    public Ammunition Clone(int? quantity = null)
    {
        return new Ammunition
        {
            AmmoId = AmmoId,
            Name = Name,
            Description = Description,
            Type = Type,
            Quantity = quantity ?? Quantity,
            MaxStack = MaxStack,
            DamageMultiplier = DamageMultiplier,
            OverrideDamageType = OverrideDamageType,
            AppliedEffect = AppliedEffect,
            EffectChance = EffectChance,
            EffectDuration = EffectDuration,
            AccuracyModifier = AccuracyModifier,
            PenetrationBonus = PenetrationBonus,
            DisplayColor = DisplayColor,
            Rarity = Rarity
        };
    }

    /// <summary>
    /// Get a display string for the ammo stack.
    /// </summary>
    public string GetDisplayString()
    {
        return $"{Name} x{Quantity}";
    }

    /// <summary>
    /// Get stats string for UI display.
    /// </summary>
    public string GetStatsString()
    {
        var stats = new System.Collections.Generic.List<string>();

        if (DamageMultiplier != 1.0f)
            stats.Add($"DMG x{DamageMultiplier:F1}");
        if (AccuracyModifier != 0)
            stats.Add($"ACC {(AccuracyModifier > 0 ? "+" : "")}{AccuracyModifier}");
        if (PenetrationBonus > 0)
            stats.Add($"PEN +{PenetrationBonus}");
        if (AppliedEffect != WeaponEffect.None && EffectChance > 0)
            stats.Add($"{AppliedEffect} {EffectChance}%");

        return stats.Count > 0 ? string.Join(" ", stats) : "Standard";
    }

    #endregion
}
