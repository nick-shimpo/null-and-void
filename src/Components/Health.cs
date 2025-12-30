using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.Components;

/// <summary>
/// Health component that can be attached to any entity.
/// Handles damage, healing, and death.
/// </summary>
public partial class Health : Node
{
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int CurrentHealth { get; set; } = 100;
    [Export] public int Armor { get; set; } = 0;

    [Signal] public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
    [Signal] public delegate void DamagedEventHandler(int damage);
    [Signal] public delegate void HealedEventHandler(int amount);
    [Signal] public delegate void DiedEventHandler();

    public bool IsDead => CurrentHealth <= 0;
    public float HealthPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// Apply damage to this entity. Returns actual damage dealt after armor.
    /// </summary>
    public int TakeDamage(int rawDamage, Node? attacker = null)
    {
        if (IsDead) return 0;

        // Apply armor reduction
        int actualDamage = Mathf.Max(1, rawDamage - Armor);
        CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);

        EmitSignal(SignalName.Damaged, actualDamage);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        // Notify the event bus
        if (GetParent() is Node parent)
        {
            EventBus.Instance.EmitEntityDamaged(parent, actualDamage, CurrentHealth);
        }

        if (CurrentHealth <= 0)
        {
            Die();
        }

        return actualDamage;
    }

    /// <summary>
    /// Heal this entity by the specified amount.
    /// </summary>
    public int Heal(int amount)
    {
        if (IsDead) return 0;

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        int actualHealing = CurrentHealth - previousHealth;

        if (actualHealing > 0)
        {
            EmitSignal(SignalName.Healed, actualHealing);
            EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        }

        return actualHealing;
    }

    /// <summary>
    /// Set health to a specific value.
    /// </summary>
    public void SetHealth(int value)
    {
        CurrentHealth = Mathf.Clamp(value, 0, MaxHealth);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Set max health and optionally heal to full.
    /// </summary>
    public void SetMaxHealth(int value, bool healToFull = false)
    {
        MaxHealth = Mathf.Max(1, value);
        if (healToFull)
        {
            CurrentHealth = MaxHealth;
        }
        else
        {
            CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        }
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    private void Die()
    {
        EmitSignal(SignalName.Died);

        if (GetParent() is Node parent)
        {
            EventBus.Instance.EmitEntityDied(parent);
        }
    }
}
