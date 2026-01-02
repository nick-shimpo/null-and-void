using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Destruction;
using NullAndVoid.Entities;
using NullAndVoid.Rendering;

namespace NullAndVoid.Effects;

/// <summary>
/// Types of status effects that can be applied to entities.
/// </summary>
public enum StatusEffectType
{
    None,
    Burning,
    Stunned,
    Slowed,
    Poisoned,
    EMP,        // Disables modules
    Corroded,   // Reduces armor
    Energized,  // Bonus damage
    Shielded    // Damage reduction
}

/// <summary>
/// Base interface for all status effects.
/// </summary>
public interface IStatusEffect
{
    StatusEffectType Type { get; }
    string Name { get; }
    int RemainingDuration { get; }
    bool IsExpired { get; }

    /// <summary>
    /// Apply per-turn effect. Called at start of entity's turn.
    /// </summary>
    void OnTurnStart(Entity entity);

    /// <summary>
    /// Called when effect is first applied.
    /// </summary>
    void OnApply(Entity entity);

    /// <summary>
    /// Called when effect expires or is removed.
    /// </summary>
    void OnRemove(Entity entity);

    /// <summary>
    /// Advance effect by one turn.
    /// </summary>
    void AdvanceTurn();

    /// <summary>
    /// Get visual indicator for this effect.
    /// </summary>
    (char character, Color color) GetVisualIndicator();
}

/// <summary>
/// Burning status effect - entity takes fire damage over time.
/// Based on Brogue: 1-3 damage per turn, burns for ~7 turns.
/// </summary>
public class BurningEffect : IStatusEffect
{
    public StatusEffectType Type => StatusEffectType.Burning;
    public string Name => "Burning";
    public int RemainingDuration { get; private set; }
    public bool IsExpired => RemainingDuration <= 0;

    private readonly int _damagePerTurn;
    private readonly Random _random = new();
    private float _animTimer;
    private bool _useAltVisual;

    public BurningEffect(int duration = 7, int damagePerTurn = 2)
    {
        RemainingDuration = duration;
        _damagePerTurn = damagePerTurn;
    }

    public void OnApply(Entity entity)
    {
        GD.Print($"{entity.EntityName} catches fire!");
    }

    public void OnTurnStart(Entity entity)
    {
        // Random damage 1-3 (or configured value +/- 1)
        int damage = _damagePerTurn + _random.Next(-1, 2);
        damage = Mathf.Max(1, damage);

        ApplyDamageToEntity(entity, damage);
        GD.Print($"{entity.EntityName} takes {damage} fire damage from burning!");
    }

    public void OnRemove(Entity entity)
    {
        GD.Print($"{entity.EntityName} is no longer burning.");
    }

    private static void ApplyDamageToEntity(Entity entity, int damage)
    {
        if (entity is Player player)
            player.TakeDamage(damage);
        else if (entity is Enemy enemy)
            enemy.TakeDamage(damage);
    }

    public void AdvanceTurn()
    {
        RemainingDuration--;
    }

    public (char character, Color color) GetVisualIndicator()
    {
        _animTimer += 0.1f;
        if (_animTimer >= 0.2f)
        {
            _animTimer = 0;
            _useAltVisual = !_useAltVisual;
        }

        char c = _useAltVisual ? '▒' : '░';
        Color color = _useAltVisual
            ? Color.Color8(255, 150, 30)
            : Color.Color8(255, 100, 20);

        return (c, color);
    }

    /// <summary>
    /// Extend burning duration (stacking fire).
    /// </summary>
    public void ExtendDuration(int turns)
    {
        RemainingDuration = Mathf.Min(RemainingDuration + turns, 15);
    }
}

/// <summary>
/// EMP status effect - disables modules temporarily.
/// </summary>
public class EMPEffect : IStatusEffect
{
    public StatusEffectType Type => StatusEffectType.EMP;
    public string Name => "EMP";
    public int RemainingDuration { get; private set; }
    public bool IsExpired => RemainingDuration <= 0;

    public EMPEffect(int duration = 3)
    {
        RemainingDuration = duration;
    }

    public void OnApply(Entity entity)
    {
        GD.Print($"{entity.EntityName} is disabled by EMP!");
        // Could force-deactivate modules here
    }

    public void OnTurnStart(Entity entity)
    {
        // Entity can't use active abilities while EMP'd
    }

    public void OnRemove(Entity entity)
    {
        GD.Print($"{entity.EntityName} recovers from EMP.");
    }

    public void AdvanceTurn()
    {
        RemainingDuration--;
    }

    public (char character, Color color) GetVisualIndicator()
    {
        return ('⚡', Color.Color8(100, 150, 255));
    }
}

/// <summary>
/// Stunned status effect - entity loses turn.
/// </summary>
public class StunnedEffect : IStatusEffect
{
    public StatusEffectType Type => StatusEffectType.Stunned;
    public string Name => "Stunned";
    public int RemainingDuration { get; private set; }
    public bool IsExpired => RemainingDuration <= 0;

    public StunnedEffect(int duration = 2)
    {
        RemainingDuration = duration;
    }

    public void OnApply(Entity entity)
    {
        GD.Print($"{entity.EntityName} is stunned!");
    }

    public void OnTurnStart(Entity entity)
    {
        // Entity skips their turn
    }

    public void OnRemove(Entity entity)
    {
        GD.Print($"{entity.EntityName} recovers from stun.");
    }

    public void AdvanceTurn()
    {
        RemainingDuration--;
    }

    public (char character, Color color) GetVisualIndicator()
    {
        return ('*', Color.Color8(255, 255, 100));
    }
}

/// <summary>
/// Slowed status effect - entity moves slower.
/// </summary>
public class SlowedEffect : IStatusEffect
{
    public StatusEffectType Type => StatusEffectType.Slowed;
    public string Name => "Slowed";
    public int RemainingDuration { get; private set; }
    public bool IsExpired => RemainingDuration <= 0;

    public int SpeedReduction { get; }

    public SlowedEffect(int duration = 3, int speedReduction = 30)
    {
        RemainingDuration = duration;
        SpeedReduction = speedReduction;
    }

    public void OnApply(Entity entity)
    {
        GD.Print($"{entity.EntityName} is slowed!");
    }

    public void OnTurnStart(Entity entity)
    {
        // Speed reduction handled by checking this effect
    }

    public void OnRemove(Entity entity)
    {
        GD.Print($"{entity.EntityName} is no longer slowed.");
    }

    public void AdvanceTurn()
    {
        RemainingDuration--;
    }

    public (char character, Color color) GetVisualIndicator()
    {
        return ('▼', Color.Color8(100, 150, 200));
    }
}

/// <summary>
/// Poisoned status effect - entity takes damage over time and has reduced healing.
/// </summary>
public class PoisonedEffect : IStatusEffect
{
    public StatusEffectType Type => StatusEffectType.Poisoned;
    public string Name => "Poisoned";
    public int RemainingDuration { get; private set; }
    public bool IsExpired => RemainingDuration <= 0;

    private readonly int _damagePerTurn;
    private readonly Random _random = new();

    public PoisonedEffect(int duration = 5, int damagePerTurn = 2)
    {
        RemainingDuration = duration;
        _damagePerTurn = damagePerTurn;
    }

    public void OnApply(Entity entity)
    {
        GD.Print($"{entity.EntityName} is poisoned!");
    }

    public void OnTurnStart(Entity entity)
    {
        int damage = _damagePerTurn + _random.Next(0, 2);
        ApplyDamageToEntity(entity, damage);
        GD.Print($"{entity.EntityName} takes {damage} poison damage!");
    }

    private static void ApplyDamageToEntity(Entity entity, int damage)
    {
        if (entity is Player player)
            player.TakeDamage(damage);
        else if (entity is Enemy enemy)
            enemy.TakeDamage(damage);
    }

    public void OnRemove(Entity entity)
    {
        GD.Print($"{entity.EntityName} is no longer poisoned.");
    }

    public void AdvanceTurn()
    {
        RemainingDuration--;
    }

    public (char character, Color color) GetVisualIndicator()
    {
        return ('☠', Color.Color8(100, 200, 50));
    }
}

/// <summary>
/// Corroded status effect - reduces armor, making entity more vulnerable.
/// </summary>
public class CorrodedEffect : IStatusEffect
{
    public StatusEffectType Type => StatusEffectType.Corroded;
    public string Name => "Corroded";
    public int RemainingDuration { get; private set; }
    public bool IsExpired => RemainingDuration <= 0;

    public int ArmorReduction { get; }

    public CorrodedEffect(int duration = 4, int armorReduction = 5)
    {
        RemainingDuration = duration;
        ArmorReduction = armorReduction;
    }

    public void OnApply(Entity entity)
    {
        GD.Print($"{entity.EntityName}'s armor is corroding!");
    }

    public void OnTurnStart(Entity entity)
    {
        // Armor reduction handled by checking this effect
    }

    public void OnRemove(Entity entity)
    {
        GD.Print($"{entity.EntityName}'s armor stops corroding.");
    }

    public void AdvanceTurn()
    {
        RemainingDuration--;
    }

    public (char character, Color color) GetVisualIndicator()
    {
        return ('░', Color.Color8(150, 200, 50));
    }
}

/// <summary>
/// Energized status effect - bonus damage on attacks.
/// </summary>
public class EnergizedEffect : IStatusEffect
{
    public StatusEffectType Type => StatusEffectType.Energized;
    public string Name => "Energized";
    public int RemainingDuration { get; private set; }
    public bool IsExpired => RemainingDuration <= 0;

    public int BonusDamage { get; }
    public float DamageMultiplier { get; }

    public EnergizedEffect(int duration = 3, int bonusDamage = 5, float damageMultiplier = 1.25f)
    {
        RemainingDuration = duration;
        BonusDamage = bonusDamage;
        DamageMultiplier = damageMultiplier;
    }

    public void OnApply(Entity entity)
    {
        GD.Print($"{entity.EntityName} is energized!");
    }

    public void OnTurnStart(Entity entity)
    {
        // Damage bonus handled by checking this effect during combat
    }

    public void OnRemove(Entity entity)
    {
        GD.Print($"{entity.EntityName} is no longer energized.");
    }

    public void AdvanceTurn()
    {
        RemainingDuration--;
    }

    public (char character, Color color) GetVisualIndicator()
    {
        return ('↑', Color.Color8(255, 255, 100));
    }
}

/// <summary>
/// Shielded status effect - reduces incoming damage.
/// </summary>
public class ShieldedEffect : IStatusEffect
{
    public StatusEffectType Type => StatusEffectType.Shielded;
    public string Name => "Shielded";
    public int RemainingDuration { get; private set; }
    public bool IsExpired => RemainingDuration <= 0;

    public int DamageReduction { get; }
    public float DamageReductionPercent { get; }

    public ShieldedEffect(int duration = 3, int damageReduction = 3, float damageReductionPercent = 0.25f)
    {
        RemainingDuration = duration;
        DamageReduction = damageReduction;
        DamageReductionPercent = damageReductionPercent;
    }

    public void OnApply(Entity entity)
    {
        GD.Print($"{entity.EntityName} is shielded!");
    }

    public void OnTurnStart(Entity entity)
    {
        // Damage reduction handled by checking this effect
    }

    public void OnRemove(Entity entity)
    {
        GD.Print($"{entity.EntityName}'s shield fades.");
    }

    public void AdvanceTurn()
    {
        RemainingDuration--;
    }

    public (char character, Color color) GetVisualIndicator()
    {
        return ('◊', Color.Color8(100, 200, 255));
    }
}

/// <summary>
/// Manages status effects on an entity.
/// </summary>
public class StatusEffectManager
{
    private readonly Dictionary<StatusEffectType, IStatusEffect> _effects = new();
    private readonly Entity _entity;

    public StatusEffectManager(Entity entity)
    {
        _entity = entity;
    }

    /// <summary>
    /// Apply a status effect to the entity.
    /// </summary>
    public void ApplyEffect(IStatusEffect effect)
    {
        if (_effects.TryGetValue(effect.Type, out var existing))
        {
            // Stack or extend existing effect
            if (existing is BurningEffect burning && effect is BurningEffect newBurning)
            {
                burning.ExtendDuration(newBurning.RemainingDuration / 2);
            }
            // Other effects just refresh duration
            return;
        }

        _effects[effect.Type] = effect;
        effect.OnApply(_entity);
    }

    /// <summary>
    /// Remove a status effect.
    /// </summary>
    public void RemoveEffect(StatusEffectType type)
    {
        if (_effects.TryGetValue(type, out var effect))
        {
            effect.OnRemove(_entity);
            _effects.Remove(type);
        }
    }

    /// <summary>
    /// Check if entity has a specific effect.
    /// </summary>
    public bool HasEffect(StatusEffectType type)
    {
        return _effects.ContainsKey(type);
    }

    /// <summary>
    /// Get a specific effect if present.
    /// </summary>
    public IStatusEffect? GetEffect(StatusEffectType type)
    {
        return _effects.TryGetValue(type, out var effect) ? effect : null;
    }

    /// <summary>
    /// Process all effects at turn start.
    /// </summary>
    public void ProcessTurnStart()
    {
        var expiredEffects = new List<StatusEffectType>();

        foreach (var (type, effect) in _effects)
        {
            effect.OnTurnStart(_entity);
            effect.AdvanceTurn();

            if (effect.IsExpired)
            {
                expiredEffects.Add(type);
            }
        }

        foreach (var type in expiredEffects)
        {
            RemoveEffect(type);
        }
    }

    /// <summary>
    /// Get all active effects.
    /// </summary>
    public IEnumerable<IStatusEffect> GetActiveEffects()
    {
        return _effects.Values;
    }

    /// <summary>
    /// Clear all effects (e.g., on death).
    /// </summary>
    public void ClearAll()
    {
        foreach (var effect in _effects.Values)
        {
            effect.OnRemove(_entity);
        }
        _effects.Clear();
    }

    /// <summary>
    /// Check if entity is incapacitated (stunned/EMP).
    /// </summary>
    public bool IsIncapacitated => HasEffect(StatusEffectType.Stunned) || HasEffect(StatusEffectType.EMP);

    /// <summary>
    /// Check if entity is burning.
    /// </summary>
    public bool IsBurning => HasEffect(StatusEffectType.Burning);

    /// <summary>
    /// Check if entity is slowed.
    /// </summary>
    public bool IsSlowed => HasEffect(StatusEffectType.Slowed);

    /// <summary>
    /// Check if entity is poisoned.
    /// </summary>
    public bool IsPoisoned => HasEffect(StatusEffectType.Poisoned);

    /// <summary>
    /// Check if entity's armor is corroded.
    /// </summary>
    public bool IsCorroded => HasEffect(StatusEffectType.Corroded);

    /// <summary>
    /// Check if entity is energized (bonus damage).
    /// </summary>
    public bool IsEnergized => HasEffect(StatusEffectType.Energized);

    /// <summary>
    /// Check if entity is shielded.
    /// </summary>
    public bool IsShielded => HasEffect(StatusEffectType.Shielded);

    /// <summary>
    /// Get total armor reduction from effects (e.g., Corroded).
    /// </summary>
    public int GetArmorReduction()
    {
        int reduction = 0;
        if (GetEffect(StatusEffectType.Corroded) is CorrodedEffect corroded)
        {
            reduction += corroded.ArmorReduction;
        }
        return reduction;
    }

    /// <summary>
    /// Get total speed reduction from effects (e.g., Slowed).
    /// </summary>
    public int GetSpeedReduction()
    {
        int reduction = 0;
        if (GetEffect(StatusEffectType.Slowed) is SlowedEffect slowed)
        {
            reduction += slowed.SpeedReduction;
        }
        return reduction;
    }

    /// <summary>
    /// Get bonus damage from effects (e.g., Energized).
    /// </summary>
    public (int flatBonus, float multiplier) GetBonusDamage()
    {
        int flatBonus = 0;
        float multiplier = 1.0f;
        if (GetEffect(StatusEffectType.Energized) is EnergizedEffect energized)
        {
            flatBonus += energized.BonusDamage;
            multiplier *= energized.DamageMultiplier;
        }
        return (flatBonus, multiplier);
    }

    /// <summary>
    /// Get damage reduction from effects (e.g., Shielded).
    /// </summary>
    public (int flatReduction, float percentReduction) GetDamageReduction()
    {
        int flatReduction = 0;
        float percentReduction = 0f;
        if (GetEffect(StatusEffectType.Shielded) is ShieldedEffect shielded)
        {
            flatReduction += shielded.DamageReduction;
            percentReduction += shielded.DamageReductionPercent;
        }
        return (flatReduction, percentReduction);
    }
}
