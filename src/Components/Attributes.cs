using System;
using Godot;

namespace NullAndVoid.Components;

/// <summary>
/// Core attributes for all entities participating in game systems.
/// Manages base values, equipment modifiers, and computed totals.
/// Handles energy budget (production vs consumption) and integrity (damage).
/// </summary>
public partial class Attributes : Node
{
    // === BASE ATTRIBUTES (intrinsic to the entity) ===

    // Energy System
    [Export] public int BaseEnergyOutput { get; set; } = 10;
    [Export] public int BaseEnergyConsumption { get; set; } = 0;
    [Export] public int BaseEnergyReserveCapacity { get; set; } = 100;

    // Combat/Survival
    [Export] public int BaseIntegrity { get; set; } = 100;
    [Export] public int BaseArmor { get; set; } = 0;
    [Export] public int BaseAttackDamage { get; set; } = 10;

    // Mobility
    [Export] public int BaseSpeed { get; set; } = 100;

    // Detection
    [Export] public int BaseSightRange { get; set; } = 10;
    [Export] public int BaseNoise { get; set; } = 50;

    // === CURRENT STATE ===
    public int CurrentIntegrity { get; set; }
    public int CurrentEnergyReserve { get; set; }

    // === EQUIPMENT MODIFIERS (set by Equipment component) ===
    public int EquipmentEnergyOutput { get; set; } = 0;
    public int EquipmentEnergyConsumption { get; set; } = 0;
    public int EquipmentEnergyReserveCapacity { get; set; } = 0;
    public int EquipmentIntegrity { get; set; } = 0;
    public int EquipmentArmor { get; set; } = 0;
    public int EquipmentAttackDamage { get; set; } = 0;
    public int EquipmentSpeed { get; set; } = 0;
    public int EquipmentSightRange { get; set; } = 0;
    public int EquipmentNoise { get; set; } = 0;

    // === COMPUTED PROPERTIES ===
    public int EnergyOutput => BaseEnergyOutput + EquipmentEnergyOutput;
    public int EnergyConsumption => BaseEnergyConsumption + EquipmentEnergyConsumption;
    public int EnergyReserveCapacity => Math.Max(0, BaseEnergyReserveCapacity + EquipmentEnergyReserveCapacity);
    public int MaxIntegrity => Math.Max(1, BaseIntegrity + EquipmentIntegrity);
    public int Armor => Math.Max(0, BaseArmor + EquipmentArmor);
    public int AttackDamage => Math.Max(0, BaseAttackDamage + EquipmentAttackDamage);
    public int Speed => Math.Max(1, BaseSpeed + EquipmentSpeed);
    public int SightRange => Math.Max(1, BaseSightRange + EquipmentSightRange);
    public int Noise => Math.Clamp(BaseNoise + EquipmentNoise, 0, 200);

    // === DERIVED ENERGY VALUES ===
    public int EnergyBalance => EnergyOutput - EnergyConsumption;
    public bool HasEnergyDeficit => EnergyBalance < 0;
    public bool HasEnergySurplus => EnergyBalance > 0;
    public float EnergyReservePercent => EnergyReserveCapacity > 0
        ? (float)CurrentEnergyReserve / EnergyReserveCapacity : 0;
    public float IntegrityPercent => MaxIntegrity > 0
        ? (float)CurrentIntegrity / MaxIntegrity : 0;

    // === SIGNALS ===
    [Signal] public delegate void IntegrityChangedEventHandler(int current, int max);
    [Signal] public delegate void EnergyReserveChangedEventHandler(int current, int max, int balance);
    [Signal] public delegate void AttributesRecalculatedEventHandler();
    [Signal] public delegate void EnergyDepletedEventHandler();
    [Signal] public delegate void EntityDestroyedEventHandler();

    public override void _Ready()
    {
        CurrentIntegrity = MaxIntegrity;
        CurrentEnergyReserve = EnergyReserveCapacity;
    }

    /// <summary>
    /// Initialize attributes to full values. Call after setting base values.
    /// </summary>
    public void Initialize()
    {
        CurrentIntegrity = MaxIntegrity;
        CurrentEnergyReserve = EnergyReserveCapacity;
    }

    /// <summary>
    /// Process energy each game tick. Called by TurnManager.
    /// </summary>
    public void ProcessEnergyTick()
    {
        int balance = EnergyBalance;

        if (balance > 0)
        {
            // Surplus: charge reserve
            CurrentEnergyReserve = Math.Min(
                EnergyReserveCapacity,
                CurrentEnergyReserve + balance
            );
        }
        else if (balance < 0)
        {
            // Deficit: drain reserve
            CurrentEnergyReserve += balance; // balance is negative

            if (CurrentEnergyReserve <= 0)
            {
                CurrentEnergyReserve = 0;
                EmitSignal(SignalName.EnergyDepleted);
            }
        }

        EmitSignal(SignalName.EnergyReserveChanged, CurrentEnergyReserve, EnergyReserveCapacity, balance);
    }

    /// <summary>
    /// Consume energy for a special action (abilities, etc.)
    /// Returns true if successful, false if insufficient energy.
    /// </summary>
    public bool TryConsumeEnergy(int amount)
    {
        if (CurrentEnergyReserve >= amount)
        {
            CurrentEnergyReserve -= amount;
            EmitSignal(SignalName.EnergyReserveChanged, CurrentEnergyReserve, EnergyReserveCapacity, EnergyBalance);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Add energy to reserve (e.g., from pickup or ability).
    /// </summary>
    public int AddEnergy(int amount)
    {
        int previousReserve = CurrentEnergyReserve;
        CurrentEnergyReserve = Math.Min(EnergyReserveCapacity, CurrentEnergyReserve + amount);
        int actualAdded = CurrentEnergyReserve - previousReserve;

        if (actualAdded > 0)
        {
            EmitSignal(SignalName.EnergyReserveChanged, CurrentEnergyReserve, EnergyReserveCapacity, EnergyBalance);
        }

        return actualAdded;
    }

    /// <summary>
    /// Apply damage to integrity.
    /// Returns the actual damage dealt after armor reduction.
    /// </summary>
    public int TakeDamage(int rawDamage)
    {
        int actualDamage = Math.Max(1, rawDamage - Armor);
        CurrentIntegrity = Math.Max(0, CurrentIntegrity - actualDamage);

        EmitSignal(SignalName.IntegrityChanged, CurrentIntegrity, MaxIntegrity);

        if (CurrentIntegrity <= 0)
        {
            EmitSignal(SignalName.EntityDestroyed);
        }

        return actualDamage;
    }

    /// <summary>
    /// Repair integrity.
    /// Returns actual amount repaired.
    /// </summary>
    public int Repair(int amount)
    {
        int previousIntegrity = CurrentIntegrity;
        CurrentIntegrity = Math.Min(MaxIntegrity, CurrentIntegrity + amount);
        int actualRepair = CurrentIntegrity - previousIntegrity;

        if (actualRepair > 0)
        {
            EmitSignal(SignalName.IntegrityChanged, CurrentIntegrity, MaxIntegrity);
        }

        return actualRepair;
    }

    /// <summary>
    /// Set integrity to a specific value.
    /// </summary>
    public void SetIntegrity(int value)
    {
        CurrentIntegrity = Math.Clamp(value, 0, MaxIntegrity);
        EmitSignal(SignalName.IntegrityChanged, CurrentIntegrity, MaxIntegrity);

        if (CurrentIntegrity <= 0)
        {
            EmitSignal(SignalName.EntityDestroyed);
        }
    }

    /// <summary>
    /// Recalculate equipment modifiers. Called by Equipment component.
    /// </summary>
    public void RecalculateFromEquipment(
        int energyOutput, int energyConsumption, int energyCapacity,
        int integrity, int armor, int damage, int speed, int sight, int noise)
    {
        EquipmentEnergyOutput = energyOutput;
        EquipmentEnergyConsumption = energyConsumption;
        EquipmentEnergyReserveCapacity = energyCapacity;
        EquipmentIntegrity = integrity;
        EquipmentArmor = armor;
        EquipmentAttackDamage = damage;
        EquipmentSpeed = speed;
        EquipmentSightRange = sight;
        EquipmentNoise = noise;

        // Cap current values if max decreased
        CurrentIntegrity = Math.Min(CurrentIntegrity, MaxIntegrity);
        CurrentEnergyReserve = Math.Min(CurrentEnergyReserve, EnergyReserveCapacity);

        EmitSignal(SignalName.AttributesRecalculated);
        EmitSignal(SignalName.IntegrityChanged, CurrentIntegrity, MaxIntegrity);
        EmitSignal(SignalName.EnergyReserveChanged, CurrentEnergyReserve, EnergyReserveCapacity, EnergyBalance);
    }

    /// <summary>
    /// Get a debug string of current attributes.
    /// </summary>
    public string GetDebugString()
    {
        return $"INT:{CurrentIntegrity}/{MaxIntegrity} PWR:{CurrentEnergyReserve}/{EnergyReserveCapacity} " +
               $"BAL:{(EnergyBalance >= 0 ? "+" : "")}{EnergyBalance} SPD:{Speed} ARM:{Armor} DMG:{AttackDamage} NSE:{Noise}";
    }
}
