using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Rendering;

namespace NullAndVoid.Effects;

/// <summary>
/// Orchestrates combat visual effects, connecting combat results to particle system.
/// </summary>
public class CombatAnimator
{
    private readonly ParticleSystem _particleSystem;

    /// <summary>
    /// Queue of pending animations to play.
    /// </summary>
    private readonly Queue<CombatAnimation> _animationQueue = new();

    /// <summary>
    /// Currently playing animation (if blocking).
    /// </summary>
    private CombatAnimation? _currentAnimation;

    /// <summary>
    /// Whether we're currently playing an animation sequence.
    /// </summary>
    public bool IsAnimating => _currentAnimation != null || _animationQueue.Count > 0;

    /// <summary>
    /// Event fired when all animations complete.
    /// </summary>
    public event Action? AnimationsComplete;

    public CombatAnimator(ParticleSystem particleSystem)
    {
        _particleSystem = particleSystem;
    }

    /// <summary>
    /// Play attack animation for a combat result.
    /// </summary>
    public void PlayAttackAnimation(CombatResult result)
    {
        var attackerPos = GetEntityPosition(result.Attacker);
        var weapon = result.Weapon;
        var weaponData = weapon.WeaponData;

        if (weaponData == null)
            return;

        // Create projectile based on weapon type
        if (weaponData.IsRanged)
        {
            // Ranged attack - spawn projectile
            var projectile = ProjectileVisual.FromWeaponData(
                attackerPos,
                result.PrimaryTarget,
                weaponData
            );
            _particleSystem.SpawnProjectile(projectile);

            // Handle AoE weapons specially
            if (weaponData.AreaRadius > 0)
            {
                // Queue AoE effect at impact point
                QueueAoEEffect(result.PrimaryTarget, weaponData, projectile.Duration);
            }

            // Queue impact effects for each target after projectile completes
            foreach (var attackResult in result.Results)
            {
                QueueImpactEffect(attackResult, projectile.Duration + 0.05f);
            }
        }
        else
        {
            // Melee attack - immediate impact effects
            foreach (var attackResult in result.Results)
            {
                PlayImpactEffect(attackResult, 0);
            }
        }
    }

    /// <summary>
    /// Queue AoE effect to play at impact.
    /// </summary>
    private void QueueAoEEffect(Vector2I center, WeaponData weaponData, float delay)
    {
        var animation = new CombatAnimation
        {
            Type = CombatAnimationType.AoE,
            Position = center,
            Delay = delay,
            Radius = weaponData.AreaRadius,
            Color = weaponData.ProjectileColor,
            DamageType = weaponData.DamageType
        };
        _animationQueue.Enqueue(animation);
    }

    /// <summary>
    /// Play AoE effect based on damage type.
    /// </summary>
    private void PlayAoEEffect(Vector2I center, int radius, DamageType damageType, Color color)
    {
        switch (damageType)
        {
            case DamageType.Explosive:
                PlayExplosion(center, radius, color);
                break;

            case DamageType.Thermal:
                _particleSystem.SpawnBatch(ParticleEffects.GroundFire(center, radius));
                break;

            case DamageType.Electromagnetic:
                PlayEMPBurst(center, radius);
                break;

            case DamageType.Impact:
                _particleSystem.SpawnBatch(ParticleEffects.Shockwave(center, radius, color));
                _particleSystem.SpawnBatch(ParticleEffects.Debris(center, radius * 4, ASCIIColors.RuinStone));
                break;

            default:
                // Default to explosion for other types
                PlayExplosion(center, radius, color);
                break;
        }
    }

    /// <summary>
    /// Play attack animation for a single attack result.
    /// </summary>
    public void PlayAttackAnimation(AttackResult result)
    {
        var attackerPos = GetEntityPosition(result.Attacker);
        var targetPos = result.TargetPosition;
        var weapon = result.Weapon;
        var weaponData = weapon.WeaponData;

        if (weaponData == null)
            return;

        if (weaponData.IsRanged)
        {
            // Spawn projectile
            var projectile = ProjectileVisual.FromWeaponData(attackerPos, targetPos, weaponData);
            _particleSystem.SpawnProjectile(projectile);

            // Queue impact effect
            QueueImpactEffect(result, projectile.Duration);
        }
        else
        {
            // Melee - immediate
            PlayImpactEffect(result, 0);
        }
    }

    /// <summary>
    /// Queue an impact effect to play after a delay.
    /// </summary>
    private void QueueImpactEffect(AttackResult result, float delay)
    {
        var animation = new CombatAnimation
        {
            Type = CombatAnimationType.Impact,
            Position = result.TargetPosition,
            Delay = delay,
            AttackResult = result
        };
        _animationQueue.Enqueue(animation);
    }

    /// <summary>
    /// Play impact effect immediately or with delay.
    /// </summary>
    private void PlayImpactEffect(AttackResult result, float delay)
    {
        if (delay > 0)
        {
            QueueImpactEffect(result, delay);
            return;
        }

        var pos = result.TargetPosition;
        var damageType = result.Weapon.WeaponData?.DamageType ?? DamageType.Kinetic;

        if (result.Hit)
        {
            // Hit effects
            var impactParticles = ParticleEffects.GetImpactEffect(pos, damageType, result.Damage.FinalDamage);
            _particleSystem.SpawnBatch(impactParticles);

            // Damage number
            if (result.Damage.IsCritical)
            {
                _particleSystem.Spawn(ParticleEffects.Critical(pos, result.Damage.FinalDamage));
            }
            else
            {
                var damageColor = DamageCalculator.GetDamageColor(result.Damage.FinalDamage, false);
                _particleSystem.Spawn(ParticleEffects.DamageNumber(pos, result.Damage.FinalDamage, damageColor));
            }

            // Status effect particles
            if (result.Damage.AppliedEffect != WeaponEffect.None)
            {
                PlayStatusEffect(pos, result.Damage.AppliedEffect);
            }

            // Death effect
            if (result.TargetKilled)
            {
                PlayDeathEffect(pos);
            }
        }
        else
        {
            // Miss effect
            _particleSystem.Spawn(ParticleEffects.Miss(pos));
        }
    }

    /// <summary>
    /// Play status effect particles.
    /// </summary>
    private void PlayStatusEffect(Vector2I position, WeaponEffect effect)
    {
        switch (effect)
        {
            case WeaponEffect.Burning:
                _particleSystem.SpawnBatch(ParticleEffects.Burning(position));
                break;

            case WeaponEffect.Corrupted:
            case WeaponEffect.Stunned:
            case WeaponEffect.Chain:
                _particleSystem.SpawnBatch(ParticleEffects.Electric(position));
                break;

            case WeaponEffect.Knockback:
                _particleSystem.SpawnBatch(ParticleEffects.Debris(position, 3, ASCIIColors.TechWall));
                break;
        }
    }

    /// <summary>
    /// Play death/destruction effect.
    /// </summary>
    private void PlayDeathEffect(Vector2I position)
    {
        // Explosion of debris
        _particleSystem.SpawnBatch(ParticleEffects.Explosion(position, 1, ASCIIColors.AlertDanger));
        _particleSystem.SpawnBatch(ParticleEffects.Debris(position, 6, ASCIIColors.TechWall));
        _particleSystem.SpawnBatch(ParticleEffects.Smoke(position, 4));
    }

    /// <summary>
    /// Play explosion effect.
    /// </summary>
    public void PlayExplosion(Vector2I center, int radius, Color color)
    {
        _particleSystem.SpawnBatch(ParticleEffects.Explosion(center, radius, color));
        _particleSystem.SpawnBatch(ParticleEffects.Debris(center, radius * 3, ASCIIColors.RuinStone));

        // Smoke after explosion
        for (int i = 0; i < radius; i++)
        {
            int dx = GD.RandRange(-radius, radius);
            int dy = GD.RandRange(-radius, radius);
            _particleSystem.SpawnBatch(ParticleEffects.Smoke(
                new Vector2I(center.X + dx, center.Y + dy), 2));
        }
    }

    /// <summary>
    /// Play EMP burst effect.
    /// </summary>
    public void PlayEMPBurst(Vector2I center, int radius)
    {
        _particleSystem.SpawnBatch(ParticleEffects.EMPBurst(center, radius));
    }

    /// <summary>
    /// Play fire effect at position.
    /// </summary>
    public void PlayFireEffect(Vector2I position)
    {
        _particleSystem.SpawnBatch(ParticleEffects.FireBurst(position));
    }

    /// <summary>
    /// Update animation system.
    /// </summary>
    public void Update(float delta)
    {
        // Process animation queue
        ProcessAnimationQueue(delta);
    }

    /// <summary>
    /// Process queued animations.
    /// </summary>
    private void ProcessAnimationQueue(float delta)
    {
        // Update current animation delay
        if (_currentAnimation != null)
        {
            _currentAnimation.Delay -= delta;
            if (_currentAnimation.Delay <= 0)
            {
                ExecuteAnimation(_currentAnimation);
                _currentAnimation = null;
            }
        }

        // Start next animation if no current
        if (_currentAnimation == null && _animationQueue.Count > 0)
        {
            _currentAnimation = _animationQueue.Dequeue();

            // If no delay, execute immediately
            if (_currentAnimation.Delay <= 0)
            {
                ExecuteAnimation(_currentAnimation);
                _currentAnimation = null;
            }
        }

        // Check if all animations complete
        if (!IsAnimating && !_particleSystem.HasBlockingAnimation)
        {
            AnimationsComplete?.Invoke();
        }
    }

    /// <summary>
    /// Execute a queued animation.
    /// </summary>
    private void ExecuteAnimation(CombatAnimation animation)
    {
        switch (animation.Type)
        {
            case CombatAnimationType.Impact:
                if (animation.AttackResult != null)
                {
                    PlayImpactEffect(animation.AttackResult, 0);
                }
                break;

            case CombatAnimationType.Explosion:
                PlayExplosion(animation.Position, animation.Radius, animation.Color);
                break;

            case CombatAnimationType.StatusEffect:
                if (animation.Effect != WeaponEffect.None)
                {
                    PlayStatusEffect(animation.Position, animation.Effect);
                }
                break;

            case CombatAnimationType.AoE:
                PlayAoEEffect(animation.Position, animation.Radius, animation.DamageType, animation.Color);
                break;
        }
    }

    /// <summary>
    /// Get entity grid position.
    /// </summary>
    private Vector2I GetEntityPosition(Node entity)
    {
        if (entity is Entities.Entity e)
            return e.GridPosition;
        return Vector2I.Zero;
    }

    /// <summary>
    /// Clear all animations.
    /// </summary>
    public void Clear()
    {
        _animationQueue.Clear();
        _currentAnimation = null;
        _particleSystem.Clear();
    }
}

/// <summary>
/// Type of combat animation.
/// </summary>
public enum CombatAnimationType
{
    Projectile,
    Impact,
    Explosion,
    StatusEffect,
    AoE
}

/// <summary>
/// Queued combat animation data.
/// </summary>
public class CombatAnimation
{
    public CombatAnimationType Type { get; set; }
    public Vector2I Position { get; set; }
    public float Delay { get; set; }
    public int Radius { get; set; }
    public Color Color { get; set; }
    public WeaponEffect Effect { get; set; }
    public AttackResult? AttackResult { get; set; }
    public DamageType DamageType { get; set; }
}
