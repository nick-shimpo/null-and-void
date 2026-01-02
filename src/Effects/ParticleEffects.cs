using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Effects;

/// <summary>
/// Factory for creating preset particle effects.
/// Inspired by Cogmind's ASCII particle system.
/// </summary>
public static class ParticleEffects
{
    private static readonly Random _random = new();

    #region Impact Effects

    /// <summary>
    /// Create spark particles at impact point.
    /// </summary>
    public static List<Particle> Sparks(Vector2I position, int count, Color color)
    {
        var particles = new List<Particle>();
        char[] sparkChars = { '.', '\'', '*', '+' };

        for (int i = 0; i < count; i++)
        {
            float angle = (float)(_random.NextDouble() * Mathf.Tau);
            float speed = 2f + (float)_random.NextDouble() * 4f;

            var velocity = new Vector2(
                Mathf.Cos(angle) * speed,
                Mathf.Sin(angle) * speed
            );

            var particle = Particle.CreateMoving(
                position,
                velocity,
                sparkChars[_random.Next(sparkChars.Length)],
                color,
                0.2f + (float)_random.NextDouble() * 0.3f
            );
            particle.Type = ParticleType.Sparks;
            particle.FadeEnabled = true;
            particle.FadeColor = new Color(color.R, color.G, color.B, 0);
            particle.Gravity = 8f;  // Fall down

            particles.Add(particle);
        }

        return particles;
    }

    /// <summary>
    /// Create an expanding explosion effect.
    /// </summary>
    public static List<Particle> Explosion(Vector2I center, int radius, Color color)
    {
        var particles = new List<Particle>();
        char[] explosionChars = { '*', '+', 'x', 'o', '.' };
        float duration = 0.4f;

        // Core flash
        var coreParticle = Particle.CreateFading(
            center,
            '#',
            ASCIIColors.ExplosionCore,
            color,
            duration * 0.5f
        );
        coreParticle.Type = ParticleType.Explosion;
        particles.Add(coreParticle);

        // Expanding ring
        for (int r = 1; r <= radius; r++)
        {
            float ringDelay = r * 0.05f;
            float ringDuration = duration - ringDelay;

            // Points around the ring
            for (int angle = 0; angle < 360; angle += 45)
            {
                float rad = angle * Mathf.Pi / 180f;
                int dx = (int)Mathf.Round(Mathf.Cos(rad) * r);
                int dy = (int)Mathf.Round(Mathf.Sin(rad) * r);

                var pos = new Vector2I(center.X + dx, center.Y + dy);

                var particle = Particle.CreateFading(
                    pos,
                    explosionChars[_random.Next(explosionChars.Length)],
                    color,
                    new Color(color.R * 0.3f, color.G * 0.2f, color.B * 0.1f, 0),
                    ringDuration
                );
                particle.Type = ParticleType.Explosion;
                // Simulate delay by adjusting age
                particle.Age = -ringDelay;

                particles.Add(particle);
            }
        }

        return particles;
    }

    /// <summary>
    /// Create EMP burst effect.
    /// </summary>
    public static List<Particle> EMPBurst(Vector2I center, int radius)
    {
        var particles = new List<Particle>();
        Color empColor = ASCIIColors.TechTerminal;
        char[] empChars = { '~', '=', '*', '+' };

        // Radiating waves
        for (int r = 0; r <= radius; r++)
        {
            float delay = r * 0.08f;

            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    // Only on the ring edge
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                        continue;

                    if (_random.NextDouble() > 0.4)
                        continue;

                    var pos = new Vector2I(center.X + dx, center.Y + dy);

                    var particle = Particle.CreateFading(
                        pos,
                        empChars[_random.Next(empChars.Length)],
                        empColor,
                        new Color(empColor.R, empColor.G, empColor.B, 0),
                        0.3f
                    );
                    particle.Type = ParticleType.Generic;
                    particle.Age = -delay;

                    particles.Add(particle);
                }
            }
        }

        return particles;
    }

    /// <summary>
    /// Create fire burst effect.
    /// </summary>
    public static List<Particle> FireBurst(Vector2I position)
    {
        var particles = new List<Particle>();
        char[] fireChars = { '*', '^', '~' };
        Color[] fireColors = { ASCIIColors.FireFlame, ASCIIColors.FireBlaze, ASCIIColors.FireSpark };

        for (int i = 0; i < 8; i++)
        {
            float angle = (float)(_random.NextDouble() * Mathf.Tau);
            float speed = 1f + (float)_random.NextDouble() * 2f;

            var velocity = new Vector2(
                Mathf.Cos(angle) * speed,
                Mathf.Sin(angle) * speed - 2f  // Rise upward
            );

            var particle = Particle.CreateMoving(
                position,
                velocity,
                fireChars[_random.Next(fireChars.Length)],
                fireColors[_random.Next(fireColors.Length)],
                0.3f + (float)_random.NextDouble() * 0.2f
            );
            particle.Type = ParticleType.Sparks;
            particle.FadeEnabled = true;
            particle.FadeColor = ASCIIColors.FireDying;
            particle.Gravity = -2f;  // Rise

            particles.Add(particle);
        }

        return particles;
    }

    #endregion

    #region Hit Feedback

    /// <summary>
    /// Create damage number that floats up.
    /// </summary>
    public static Particle DamageNumber(Vector2I position, int damage, Color color)
    {
        var particle = Particle.Create(
            position,
            ' ',  // Text is rendered separately
            color,
            0.8f
        );
        particle.Type = ParticleType.DamageNumber;
        particle.Text = damage.ToString();
        particle.FadeEnabled = true;
        particle.FadeColor = new Color(color.R, color.G, color.B, 0);

        return particle;
    }

    /// <summary>
    /// Create miss indicator.
    /// </summary>
    public static Particle Miss(Vector2I position)
    {
        var particle = Particle.Create(
            position,
            ' ',
            ASCIIColors.TextDim,
            0.6f
        );
        particle.Type = ParticleType.DamageNumber;
        particle.Text = "MISS";
        particle.FadeEnabled = true;
        particle.FadeColor = new Color(0.5f, 0.5f, 0.5f, 0);

        return particle;
    }

    /// <summary>
    /// Create critical hit indicator.
    /// </summary>
    public static Particle Critical(Vector2I position, int damage)
    {
        var particle = Particle.Create(
            position,
            ' ',
            ASCIIColors.AlertWarning,
            1.0f
        );
        particle.Type = ParticleType.DamageNumber;
        particle.Text = $"CRIT! {damage}";
        particle.FadeEnabled = true;
        particle.FadeColor = new Color(1f, 0.8f, 0f, 0);

        return particle;
    }

    #endregion

    #region Status Effects

    /// <summary>
    /// Create burning effect particles.
    /// </summary>
    public static List<Particle> Burning(Vector2I position)
    {
        var particles = new List<Particle>();
        char[] fireChars = { '^', '*', '~' };

        for (int i = 0; i < 3; i++)
        {
            float xOffset = -0.5f + (float)_random.NextDouble();
            var particle = Particle.CreateMoving(
                new Vector2(position.X + xOffset, position.Y),
                new Vector2(0, -1.5f),
                fireChars[_random.Next(fireChars.Length)],
                ASCIIColors.FireFlame,
                0.4f + (float)_random.NextDouble() * 0.2f
            );
            particle.Type = ParticleType.Smoke;
            particle.FadeEnabled = true;
            particle.FadeColor = ASCIIColors.FireDying;

            particles.Add(particle);
        }

        return particles;
    }

    /// <summary>
    /// Create electric/corrupted effect.
    /// </summary>
    public static List<Particle> Electric(Vector2I position)
    {
        var particles = new List<Particle>();
        char[] elecChars = { '*', '~', '+' };
        Color elecColor = ASCIIColors.TechTerminal;

        for (int i = 0; i < 4; i++)
        {
            int dx = _random.Next(3) - 1;
            int dy = _random.Next(3) - 1;

            var particle = Particle.Create(
                new Vector2I(position.X + dx, position.Y + dy),
                elecChars[_random.Next(elecChars.Length)],
                elecColor,
                0.15f + (float)_random.NextDouble() * 0.1f
            );
            particle.Type = ParticleType.Arc;
            particle.FadeEnabled = true;
            particle.FadeColor = new Color(elecColor.R, elecColor.G, elecColor.B, 0);

            particles.Add(particle);
        }

        return particles;
    }

    /// <summary>
    /// Create rising smoke particles.
    /// </summary>
    public static List<Particle> Smoke(Vector2I position, int count = 3)
    {
        var particles = new List<Particle>();
        char[] smokeChars = { '.', 'o', 'O' };
        Color smokeColor = ASCIIColors.ExplosionSmoke;

        for (int i = 0; i < count; i++)
        {
            float xOffset = -0.3f + (float)_random.NextDouble() * 0.6f;
            float speed = 0.5f + (float)_random.NextDouble() * 0.5f;

            var particle = Particle.CreateMoving(
                new Vector2(position.X + xOffset, position.Y),
                new Vector2(xOffset * 0.5f, -speed),
                smokeChars[_random.Next(smokeChars.Length)],
                smokeColor,
                0.8f + (float)_random.NextDouble() * 0.4f
            );
            particle.Type = ParticleType.Smoke;
            particle.FadeEnabled = true;
            particle.FadeColor = new Color(smokeColor.R, smokeColor.G, smokeColor.B, 0);

            particles.Add(particle);
        }

        return particles;
    }

    /// <summary>
    /// Create debris particles that fall.
    /// </summary>
    public static List<Particle> Debris(Vector2I position, int count, Color color)
    {
        var particles = new List<Particle>();
        char[] debrisChars = { '.', ',', '\'', '`' };

        for (int i = 0; i < count; i++)
        {
            float angle = (float)(_random.NextDouble() * Mathf.Pi);  // Upper half
            float speed = 2f + (float)_random.NextDouble() * 3f;

            var velocity = new Vector2(
                Mathf.Cos(angle) * speed * (_random.Next(2) == 0 ? 1 : -1),
                -Mathf.Sin(angle) * speed  // Start going up
            );

            var particle = Particle.CreateMoving(
                position,
                velocity,
                debrisChars[_random.Next(debrisChars.Length)],
                color,
                0.5f + (float)_random.NextDouble() * 0.3f
            );
            particle.Type = ParticleType.Debris;
            particle.Gravity = 15f;  // Fall quickly
            particle.FadeEnabled = true;
            particle.FadeColor = new Color(color.R * 0.5f, color.G * 0.5f, color.B * 0.5f, 0);

            particles.Add(particle);
        }

        return particles;
    }

    #endregion

    #region Projectile Effects

    /// <summary>
    /// Create a bullet trail effect.
    /// </summary>
    public static ProjectileVisual BulletTrail(Vector2I from, Vector2I to, Color color)
    {
        var proj = new ProjectileVisual(from, to, ProjectileAnimStyle.Bullet, 30f)
        {
            Color = color,
            TrailColor = color * 0.4f,
            SpawnTrail = true,
            TrailDensity = 0.3f,
            TrailLifetime = 0.1f
        };
        return proj;
    }

    /// <summary>
    /// Create a laser beam effect.
    /// </summary>
    public static ProjectileVisual LaserBeam(Vector2I from, Vector2I to, Color color)
    {
        var proj = new ProjectileVisual(from, to, ProjectileAnimStyle.Beam, 100f)
        {
            Character = '=',
            Color = color,
            SpawnTrail = false
        };
        return proj;
    }

    /// <summary>
    /// Create a plasma blast effect.
    /// </summary>
    public static ProjectileVisual PlasmaBlast(Vector2I from, Vector2I to)
    {
        var proj = new ProjectileVisual(from, to, ProjectileAnimStyle.Plasma, 15f)
        {
            CharacterSequence = new[] { '*', 'O', 'o', 'O' },
            Color = ASCIIColors.ExplosionPlasma,
            TrailColor = ASCIIColors.TechTerminal * 0.5f,
            SpawnTrail = true,
            TrailDensity = 0.7f,
            TrailLifetime = 0.2f
        };
        return proj;
    }

    /// <summary>
    /// Create an arc lightning effect.
    /// </summary>
    public static ProjectileVisual ArcLightning(Vector2I from, Vector2I to)
    {
        var proj = new ProjectileVisual(from, to, ProjectileAnimStyle.Chain, 50f)
        {
            CharacterSequence = new[] { '~', '*', '=' },
            Color = ASCIIColors.TechTerminal,
            TrailColor = ASCIIColors.TechTerminal * 0.3f,
            SpawnTrail = true,
            TrailDensity = 1.0f,
            TrailLifetime = 0.15f
        };
        return proj;
    }

    /// <summary>
    /// Create a thrown/lobbed projectile effect.
    /// </summary>
    public static ProjectileVisual LobbedProjectile(Vector2I from, Vector2I to, char character, Color color)
    {
        var proj = new ProjectileVisual(from, to, ProjectileAnimStyle.Lobbed, 8f)
        {
            Character = character,
            Color = color,
            SpawnTrail = true,
            TrailDensity = 0.4f,
            ArcHeight = 2f
        };
        return proj;
    }

    #endregion

    #region AoE Effects

    /// <summary>
    /// Create a shockwave expanding ring effect.
    /// </summary>
    public static List<Particle> Shockwave(Vector2I center, int radius, Color color)
    {
        var particles = new List<Particle>();
        float duration = 0.5f;

        // Expanding ring with staggered timing
        for (int r = 1; r <= radius; r++)
        {
            float ringDelay = r * 0.06f;
            float ringDuration = duration - ringDelay;

            // Calculate points on the ring
            int circumference = (int)(2 * Mathf.Pi * r * 0.8f);
            float angleStep = Mathf.Tau / circumference;

            for (float angle = 0; angle < Mathf.Tau; angle += angleStep)
            {
                int dx = (int)Mathf.Round(Mathf.Cos(angle) * r);
                int dy = (int)Mathf.Round(Mathf.Sin(angle) * r);

                var pos = new Vector2I(center.X + dx, center.Y + dy);

                // Use ring characters
                char ringChar = GetRingChar(angle);
                float intensity = 1.0f - ((float)r / (radius + 1));

                var particle = Particle.CreateFading(
                    pos,
                    ringChar,
                    color * intensity,
                    new Color(color.R, color.G, color.B, 0),
                    ringDuration
                );
                particle.Type = ParticleType.Generic;
                particle.Age = -ringDelay;

                particles.Add(particle);
            }
        }

        return particles;
    }

    /// <summary>
    /// Get ring character based on angle for visual variety.
    /// </summary>
    private static char GetRingChar(float angle)
    {
        // Normalize to 0-8 sectors
        int sector = (int)((angle / Mathf.Tau) * 8) % 8;
        return sector switch
        {
            0 or 4 => '-',   // Horizontal
            2 or 6 => '|',   // Vertical
            1 or 5 => '/',   // Diagonal
            3 or 7 => '\\',  // Other diagonal
            _ => '*'
        };
    }

    /// <summary>
    /// Create chain lightning effect between multiple targets.
    /// </summary>
    public static List<Particle> ChainLightning(List<Vector2I> targets)
    {
        var particles = new List<Particle>();
        if (targets.Count < 2)
            return particles;

        Color arcColor = ASCIIColors.TechTerminal;
        char[] arcChars = { '~', '*', '+', '=' };

        for (int i = 0; i < targets.Count - 1; i++)
        {
            var from = targets[i];
            var to = targets[i + 1];

            // Get line between targets
            var linePoints = GetLinePoints(from, to);

            float delay = i * 0.08f;  // Stagger each arc

            foreach (var pos in linePoints)
            {
                if (pos == from || pos == to)
                    continue;

                var particle = Particle.CreateFading(
                    pos,
                    arcChars[_random.Next(arcChars.Length)],
                    arcColor,
                    new Color(arcColor.R, arcColor.G, arcColor.B, 0),
                    0.2f
                );
                particle.Type = ParticleType.Arc;
                particle.Age = -delay;

                particles.Add(particle);
            }

            // Impact flash at target
            var impactParticle = Particle.CreateFading(
                to,
                '*',
                arcColor * 1.5f,
                arcColor * 0.2f,
                0.15f
            );
            impactParticle.Type = ParticleType.Arc;
            impactParticle.Age = -delay;
            particles.Add(impactParticle);
        }

        return particles;
    }

    /// <summary>
    /// Create ground fire effect (for incendiary AoE).
    /// </summary>
    public static List<Particle> GroundFire(Vector2I center, int radius)
    {
        var particles = new List<Particle>();
        char[] fireChars = { '^', '*', '~', '+' };
        Color[] fireColors = { ASCIIColors.FireFlame, ASCIIColors.FireBlaze, ASCIIColors.FireSpark };

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                if (dist > radius)
                    continue;

                // Random fire particles
                if (_random.NextDouble() > 0.6)
                    continue;

                var pos = new Vector2I(center.X + dx, center.Y + dy);
                float intensity = 1.0f - ((float)dist / (radius + 1));

                var particle = Particle.CreateMoving(
                    pos,
                    new Vector2(0, -0.8f - (float)_random.NextDouble()),
                    fireChars[_random.Next(fireChars.Length)],
                    fireColors[_random.Next(fireColors.Length)] * intensity,
                    0.4f + (float)_random.NextDouble() * 0.3f
                );
                particle.Type = ParticleType.Smoke;
                particle.FadeEnabled = true;
                particle.FadeColor = ASCIIColors.FireDying;

                particles.Add(particle);
            }
        }

        return particles;
    }

    /// <summary>
    /// Create cone effect (for flamethrower, shotgun spread).
    /// </summary>
    public static List<Particle> ConeBlast(Vector2I origin, Vector2I target, int length, int spreadAngle, Color color)
    {
        var particles = new List<Particle>();
        char[] coneChars = { '.', '*', '+' };

        // Calculate direction angle
        var direction = target - origin;
        float baseAngle = Mathf.Atan2(direction.Y, direction.X);
        float halfSpread = Mathf.DegToRad(spreadAngle / 2f);

        // Create particles filling the cone
        for (int dist = 1; dist <= length; dist++)
        {
            // More particles at further distances
            int particlesAtDist = 2 + dist;
            float angleStep = (halfSpread * 2) / particlesAtDist;

            for (int i = 0; i <= particlesAtDist; i++)
            {
                float angle = baseAngle - halfSpread + (angleStep * i);

                // Add some randomness
                angle += (float)(_random.NextDouble() - 0.5f) * 0.2f;

                int dx = (int)Mathf.Round(Mathf.Cos(angle) * dist);
                int dy = (int)Mathf.Round(Mathf.Sin(angle) * dist);

                var pos = new Vector2I(origin.X + dx, origin.Y + dy);
                float intensity = 1.0f - ((float)dist / (length + 1));
                float delay = dist * 0.03f;

                var particle = Particle.CreateFading(
                    pos,
                    coneChars[_random.Next(coneChars.Length)],
                    color * intensity,
                    new Color(color.R, color.G, color.B, 0),
                    0.25f
                );
                particle.Type = ParticleType.Generic;
                particle.Age = -delay;

                particles.Add(particle);
            }
        }

        return particles;
    }

    /// <summary>
    /// Get line points using Bresenham's algorithm.
    /// </summary>
    private static List<Vector2I> GetLinePoints(Vector2I from, Vector2I to)
    {
        var points = new List<Vector2I>();

        int x0 = from.X, y0 = from.Y;
        int x1 = to.X, y1 = to.Y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            points.Add(new Vector2I(x0, y0));

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return points;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Get impact effect based on damage type.
    /// </summary>
    public static List<Particle> GetImpactEffect(Vector2I position, Combat.DamageType damageType, int damage)
    {
        var particles = new List<Particle>();

        switch (damageType)
        {
            case Combat.DamageType.Kinetic:
                particles.AddRange(Sparks(position, 4 + damage / 5, ASCIIColors.Common));
                break;

            case Combat.DamageType.Thermal:
                particles.AddRange(FireBurst(position));
                break;

            case Combat.DamageType.Electromagnetic:
                particles.AddRange(Electric(position));
                break;

            case Combat.DamageType.Explosive:
                particles.AddRange(Explosion(position, 1, ASCIIColors.ExplosionOuter));
                particles.AddRange(Debris(position, 4, ASCIIColors.RuinStone));
                break;

            case Combat.DamageType.Impact:
                particles.AddRange(Sparks(position, 3 + damage / 3, ASCIIColors.TechWall));
                particles.AddRange(Debris(position, 2, ASCIIColors.RuinStone));
                break;
        }

        return particles;
    }

    #endregion
}
