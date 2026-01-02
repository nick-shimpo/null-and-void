using System.Collections.Generic;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Effects;

/// <summary>
/// Manages all particles and projectiles in the game.
/// Handles spawning, updating, and rendering of visual effects.
/// </summary>
public class ParticleSystem
{
    /// <summary>
    /// Maximum concurrent particles to prevent performance issues.
    /// </summary>
    public const int MAX_PARTICLES = 500;

    /// <summary>
    /// Maximum concurrent projectiles.
    /// </summary>
    public const int MAX_PROJECTILES = 20;

    private readonly List<Particle> _particles = new();
    private readonly List<ProjectileVisual> _projectiles = new();

    /// <summary>
    /// Current particle count.
    /// </summary>
    public int ParticleCount => _particles.Count;

    /// <summary>
    /// Current projectile count.
    /// </summary>
    public int ProjectileCount => _projectiles.Count;

    /// <summary>
    /// Whether any animations are currently playing.
    /// </summary>
    public bool HasActiveEffects => _particles.Count > 0 || _projectiles.Count > 0;

    /// <summary>
    /// Whether any blocking animations are playing (projectiles that need to complete).
    /// </summary>
    public bool HasBlockingAnimation => _projectiles.Count > 0;

    /// <summary>
    /// Spawn a single particle.
    /// </summary>
    public void Spawn(Particle particle)
    {
        if (_particles.Count >= MAX_PARTICLES)
        {
            // Remove oldest particle
            _particles.RemoveAt(0);
        }
        _particles.Add(particle);
    }

    /// <summary>
    /// Spawn multiple particles.
    /// </summary>
    public void SpawnBatch(IEnumerable<Particle> particles)
    {
        foreach (var particle in particles)
        {
            Spawn(particle);
        }
    }

    /// <summary>
    /// Spawn a projectile visual.
    /// </summary>
    public void SpawnProjectile(ProjectileVisual projectile)
    {
        if (_projectiles.Count >= MAX_PROJECTILES)
        {
            // Force complete oldest projectile
            _projectiles.RemoveAt(0);
        }
        _projectiles.Add(projectile);
    }

    /// <summary>
    /// Update all particles and projectiles.
    /// </summary>
    public void Update(float delta)
    {
        // Update particles
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Update(delta);
            _particles[i] = p;

            if (!p.IsAlive)
            {
                _particles.RemoveAt(i);
            }
        }

        // Update projectiles
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var proj = _projectiles[i];
            proj.Update(delta);

            // Collect trail particles into main system
            foreach (var trailParticle in proj.TrailParticles)
            {
                // Don't double-add, let projectile manage its own trail
            }

            if (proj.IsComplete)
            {
                _projectiles.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Render all effects to the buffer.
    /// </summary>
    public void Render(ASCIIBuffer buffer, MapRenderer mapRenderer)
    {
        // Render particles first (behind projectiles)
        foreach (var particle in _particles)
        {
            RenderParticle(buffer, mapRenderer, particle);
        }

        // Render projectiles and their trails
        foreach (var projectile in _projectiles)
        {
            RenderProjectile(buffer, mapRenderer, projectile);
        }
    }

    /// <summary>
    /// Render a single particle.
    /// </summary>
    private void RenderParticle(ASCIIBuffer buffer, MapRenderer mapRenderer, Particle particle)
    {
        var bufferPos = mapRenderer.MapToBuffer(particle.GridPosition);
        if (bufferPos == null)
            return;

        int x = bufferPos.Value.x;
        int y = bufferPos.Value.y;

        switch (particle.BlendMode)
        {
            case ParticleBlendMode.Replace:
                buffer.SetCell(x, y, particle.CurrentCharacter, particle.CurrentColor);
                break;

            case ParticleBlendMode.Foreground:
                buffer.SetCell(x, y, particle.CurrentCharacter, particle.CurrentColor);
                break;

            case ParticleBlendMode.Background:
                buffer.SetCellBackground(x, y, particle.CurrentColor);
                break;

            case ParticleBlendMode.Behind:
                // Only draw if cell is relatively empty (space or period)
                var existing = buffer.GetCell(x, y);
                if (existing.Character == ' ' || existing.Character == '.')
                {
                    buffer.SetCell(x, y, particle.CurrentCharacter, particle.CurrentColor);
                }
                break;
        }

        // Handle text particles (damage numbers)
        if (particle.Type == ParticleType.DamageNumber && particle.Text != null)
        {
            RenderDamageNumber(buffer, x, y, particle);
        }
    }

    /// <summary>
    /// Render a damage number.
    /// </summary>
    private void RenderDamageNumber(ASCIIBuffer buffer, int x, int y, Particle particle)
    {
        // Offset upward based on age (floating effect)
        int yOffset = (int)(particle.Age * 3);
        y -= yOffset;

        if (y < 0 || y >= ASCIIBuffer.Height)
            return;

        // Center the text
        int textX = x - (particle.Text!.Length / 2);

        for (int i = 0; i < particle.Text.Length && textX + i < ASCIIBuffer.Width; i++)
        {
            if (textX + i >= 0)
            {
                buffer.SetCell(textX + i, y, particle.Text[i], particle.CurrentColor);
            }
        }
    }

    /// <summary>
    /// Render a projectile and its trail.
    /// </summary>
    private void RenderProjectile(ASCIIBuffer buffer, MapRenderer mapRenderer, ProjectileVisual projectile)
    {
        // Render trail particles first
        foreach (var trailParticle in projectile.TrailParticles)
        {
            RenderParticle(buffer, mapRenderer, trailParticle);
        }

        // Handle beam-style (render entire line)
        if (projectile.Style == ProjectileAnimStyle.Beam)
        {
            RenderBeam(buffer, mapRenderer, projectile);
            return;
        }

        // Skip instant projectiles
        if (projectile.Style == ProjectileAnimStyle.Instant)
            return;

        // Render main projectile
        var bufferPos = mapRenderer.MapToBuffer(projectile.GridPosition);
        if (bufferPos == null)
            return;

        buffer.SetCell(bufferPos.Value.x, bufferPos.Value.y,
                      projectile.CurrentCharacter, projectile.Color);
    }

    /// <summary>
    /// Render a beam-style projectile.
    /// </summary>
    private void RenderBeam(ASCIIBuffer buffer, MapRenderer mapRenderer, ProjectileVisual projectile)
    {
        var positions = projectile.GetBeamPositions();

        // Fade intensity based on progress
        float intensity = 1.0f - projectile.Progress;

        for (int i = 0; i < positions.Count; i++)
        {
            var pos = positions[i];
            var bufferPos = mapRenderer.MapToBuffer(pos);
            if (bufferPos == null)
                continue;

            // Vary color along beam
            float posProgress = (float)i / positions.Count;
            Color beamColor = projectile.Color * intensity;

            // Skip start position (player)
            if (i == 0)
                continue;

            buffer.SetCell(bufferPos.Value.x, bufferPos.Value.y,
                          projectile.CurrentCharacter, beamColor);
        }
    }

    /// <summary>
    /// Clear all particles and projectiles.
    /// </summary>
    public void Clear()
    {
        _particles.Clear();
        _projectiles.Clear();
    }

    /// <summary>
    /// Wait for all blocking animations to complete.
    /// Returns true when all projectiles have finished.
    /// </summary>
    public bool WaitForAnimations()
    {
        return !HasBlockingAnimation;
    }
}
