using System.Collections.Generic;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Destruction;

/// <summary>
/// Renders fire, smoke, explosions, and collapse effects on the ASCII buffer.
/// Integrates with the main rendering system.
/// </summary>
public class DestructionRenderer
{
    private readonly ASCIIBuffer _buffer;

    public DestructionRenderer(ASCIIBuffer buffer)
    {
        _buffer = buffer;
    }

    /// <summary>
    /// Render all fire effects on the map.
    /// Call after base terrain but before entities.
    /// </summary>
    public void RenderFire(
        FireSimulation fireSimulation,
        int mapStartX,
        int mapStartY,
        int cameraOffsetX,
        int cameraOffsetY,
        int viewWidth,
        int viewHeight,
        DestructibleTile[,] tiles)
    {
        foreach (var pos in fireSimulation.GetActiveFirePositions())
        {
            int screenX = mapStartX + pos.X - cameraOffsetX;
            int screenY = mapStartY + pos.Y - cameraOffsetY;

            // Check if on screen
            if (screenX < mapStartX || screenX >= mapStartX + viewWidth)
                continue;
            if (screenY < mapStartY || screenY >= mapStartY + viewHeight)
                continue;

            var tile = tiles[pos.X, pos.Y];
            var fire = tile.Fire;

            if (fire.IsActive)
            {
                char fireChar = fire.GetCharacter();
                Color fireColor = fire.GetColor();
                Color bgColor = fire.GetBackgroundColor();

                _buffer.SetCell(screenX, screenY, fireChar, fireColor);
                _buffer.SetCellBackground(screenX, screenY, bgColor);
            }
        }
    }

    /// <summary>
    /// Render smoke effects (drawn over terrain and fire).
    /// </summary>
    public void RenderSmoke(
        SmokeSimulation smokeSimulation,
        int mapStartX,
        int mapStartY,
        int cameraOffsetX,
        int cameraOffsetY,
        int viewWidth,
        int viewHeight)
    {
        foreach (var pos in smokeSimulation.GetActiveSmokePositions())
        {
            int screenX = mapStartX + pos.X - cameraOffsetX;
            int screenY = mapStartY + pos.Y - cameraOffsetY;

            // Check if on screen
            if (screenX < mapStartX || screenX >= mapStartX + viewWidth)
                continue;
            if (screenY < mapStartY || screenY >= mapStartY + viewHeight)
                continue;

            var smoke = smokeSimulation.GetSmoke(pos.X, pos.Y);

            if (smoke.IsActive)
            {
                char smokeChar = smoke.GetCharacter();
                Color smokeColor = smoke.GetColor();

                // Smoke blends with existing cell
                _buffer.SetCell(screenX, screenY, smokeChar, smokeColor);
            }
        }
    }

    /// <summary>
    /// Render explosion visual effects.
    /// </summary>
    public void RenderExplosions(
        List<ExplosionVisual> explosions,
        int mapStartX,
        int mapStartY,
        int cameraOffsetX,
        int cameraOffsetY)
    {
        foreach (var explosion in explosions)
        {
            float progress = explosion.Progress;
            int currentRadius = (int)(explosion.Radius * progress);

            // Draw expanding ring
            for (int dy = -currentRadius; dy <= currentRadius; dy++)
            {
                for (int dx = -currentRadius; dx <= currentRadius; dx++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Only draw the ring edge
                    if (dist > currentRadius - 1 && dist <= currentRadius)
                    {
                        int screenX = mapStartX + explosion.Center.X + dx - cameraOffsetX;
                        int screenY = mapStartY + explosion.Center.Y + dy - cameraOffsetY;

                        if (screenX >= 0 && screenX < ASCIIBuffer.Width &&
                            screenY >= 0 && screenY < ASCIIBuffer.Height)
                        {
                            char explosionChar = GetExplosionChar(progress, dist, currentRadius);
                            Color explosionColor = GetExplosionColor(explosion.Color, progress);

                            _buffer.SetCell(screenX, screenY, explosionChar, explosionColor);
                            _buffer.SetCellBackground(screenX, screenY, GetExplosionBgColor(progress));
                        }
                    }
                }
            }

            // Draw center
            if (progress < 0.5f)
            {
                int centerScreenX = mapStartX + explosion.Center.X - cameraOffsetX;
                int centerScreenY = mapStartY + explosion.Center.Y - cameraOffsetY;

                if (centerScreenX >= 0 && centerScreenX < ASCIIBuffer.Width &&
                    centerScreenY >= 0 && centerScreenY < ASCIIBuffer.Height)
                {
                    _buffer.SetCell(centerScreenX, centerScreenY, '*', Colors.White);
                    _buffer.SetCellBackground(centerScreenX, centerScreenY, explosion.Color);
                }
            }
        }
    }

    /// <summary>
    /// Render collapse visual effects.
    /// </summary>
    public void RenderCollapses(
        List<CollapseVisual> collapses,
        int mapStartX,
        int mapStartY,
        int cameraOffsetX,
        int cameraOffsetY)
    {
        foreach (var collapse in collapses)
        {
            int screenX = mapStartX + collapse.Position.X - cameraOffsetX;
            int screenY = mapStartY + collapse.Position.Y - cameraOffsetY;

            if (screenX >= 0 && screenX < ASCIIBuffer.Width &&
                screenY >= 0 && screenY < ASCIIBuffer.Height)
            {
                char collapseChar = collapse.GetAnimatedChar();
                Color color = collapse.Color;

                // Fade out as collapse completes
                float alpha = 1.0f - collapse.Progress;
                color = new Color(color.R, color.G, color.B, alpha);

                _buffer.SetCell(screenX, screenY, collapseChar, color);
            }
        }
    }

    /// <summary>
    /// Render damage numbers or effects at a position.
    /// </summary>
    public void RenderDamageIndicator(
        int worldX,
        int worldY,
        int damage,
        int mapStartX,
        int mapStartY,
        int cameraOffsetX,
        int cameraOffsetY)
    {
        int screenX = mapStartX + worldX - cameraOffsetX;
        int screenY = mapStartY + worldY - cameraOffsetY;

        if (screenX >= 0 && screenX < ASCIIBuffer.Width &&
            screenY >= 0 && screenY < ASCIIBuffer.Height)
        {
            // Show damage as a colored number or symbol
            char damageChar = damage switch
            {
                >= 30 => '!',
                >= 20 => '▓',
                >= 10 => '▒',
                _ => '·'
            };

            Color damageColor = damage switch
            {
                >= 30 => Color.Color8(255, 50, 50),
                >= 20 => Color.Color8(255, 150, 50),
                >= 10 => Color.Color8(255, 200, 100),
                _ => Color.Color8(200, 200, 200)
            };

            _buffer.SetCell(screenX, screenY, damageChar, damageColor);
        }
    }

    private static char GetExplosionChar(float progress, float distance, int radius)
    {
        if (progress < 0.3f)
            return '*';
        if (progress < 0.5f)
            return '○';
        if (progress < 0.7f)
            return '◎';
        return '·';
    }

    private static Color GetExplosionColor(Color baseColor, float progress)
    {
        // Start bright, fade to darker
        float intensity = 1.0f - (progress * 0.7f);
        return new Color(
            baseColor.R * intensity,
            baseColor.G * intensity,
            baseColor.B * intensity
        );
    }

    private static Color GetExplosionBgColor(float progress)
    {
        // Flash bright at start, then fade
        if (progress < 0.2f)
            return Color.Color8(255, 200, 100, 200);
        if (progress < 0.4f)
            return Color.Color8(200, 100, 50, 150);
        return Color.Color8(100, 50, 25, 100);
    }
}
