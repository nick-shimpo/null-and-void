using System.Collections.Generic;
using Godot;
using NullAndVoid.Entities;
using NullAndVoid.Systems;
using NullAndVoid.World;

namespace NullAndVoid.Rendering;

/// <summary>
/// Renders entities (player, enemies, items) to the ASCII buffer or MapViewport.
/// </summary>
public class EntityRenderer
{
    private readonly ASCIIBuffer _buffer;
    private readonly MapRenderer _mapRenderer;
    private MapViewport? _mapViewport;

    // Flash effects for damage feedback
    private readonly Dictionary<Node, float> _flashTimers = new();
    private const float FlashDuration = 0.15f;

    public EntityRenderer(ASCIIBuffer buffer, MapRenderer mapRenderer)
    {
        _buffer = buffer;
        _mapRenderer = mapRenderer;
    }

    /// <summary>
    /// Set the MapViewport for zoomed rendering.
    /// </summary>
    public void SetMapViewport(MapViewport? viewport)
    {
        _mapViewport = viewport;
    }

    /// <summary>
    /// Set a cell in the appropriate render target (MapViewport or main buffer).
    /// Coordinates come from MapToBuffer which returns:
    /// - Screen coords (0-based) when using MapViewport
    /// - Buffer coords (with offset) when not using MapViewport
    /// </summary>
    private void SetEntityCell(int x, int y, char character, Color color)
    {
        if (_mapRenderer.UseMapViewport && _mapViewport != null)
        {
            // x,y are screen coords (0-based relative to map view)
            _mapViewport.SetCell(x, y, character, color);
        }
        else
        {
            // x,y are already buffer coords (includes offset from MapToBuffer)
            _buffer.SetCell(x, y, character, color);
        }
    }

    /// <summary>
    /// Render the player entity.
    /// </summary>
    public void RenderPlayer(Player player, TileMapManager tileMap, float pulsePhase)
    {
        if (player == null)
            return;

        var screenPos = _mapRenderer.MapToBuffer(player.GridPosition);
        if (screenPos == null)
            return;

        // Check if player is flashing (damage feedback)
        Color playerColor;
        if (_flashTimers.TryGetValue(player, out float flashTime) && flashTime > 0)
        {
            float t = flashTime / FlashDuration;
            playerColor = ASCIIColors.AlertDanger.Lerp(ASCIIColors.Player, 1 - t);
        }
        else
        {
            // Normal pulsing effect
            float pulse = 0.8f + 0.2f * Mathf.Sin(pulsePhase * 3f);
            playerColor = new Color(
                ASCIIColors.Player.R * pulse,
                ASCIIColors.Player.G * pulse,
                ASCIIColors.Player.B * pulse
            );
        }

        SetEntityCell(screenPos.Value.x, screenPos.Value.y, ASCIIChars.Player, playerColor);
    }

    /// <summary>
    /// Render enemy entities.
    /// </summary>
    public void RenderEnemies(IEnumerable<Enemy> enemies, TileMapManager tileMap)
    {
        RenderEnemies(enemies, tileMap.FOV);
    }

    /// <summary>
    /// Render enemy entities with explicit FOV.
    /// </summary>
    public void RenderEnemies(IEnumerable<Enemy> enemies, FOVSystem fov)
    {
        foreach (var enemy in enemies)
        {
            RenderEnemy(enemy, fov);
        }
    }

    /// <summary>
    /// Render a single enemy.
    /// </summary>
    public void RenderEnemy(Enemy enemy, TileMapManager tileMap)
    {
        RenderEnemy(enemy, tileMap.FOV);
    }

    /// <summary>
    /// Render a single enemy with explicit FOV.
    /// </summary>
    public void RenderEnemy(Enemy enemy, FOVSystem fov)
    {
        if (enemy == null)
            return;

        // Only render if in FOV
        if (!fov.IsVisible(enemy.GridPosition))
            return;

        var screenPos = _mapRenderer.MapToBuffer(enemy.GridPosition);
        if (screenPos == null)
            return;

        // Determine enemy character based on name/type
        char enemyChar = GetEnemyChar(enemy);

        // Check for flash effect
        Color enemyColor;
        if (_flashTimers.TryGetValue(enemy, out float flashTime) && flashTime > 0)
        {
            float t = flashTime / FlashDuration;
            enemyColor = ASCIIColors.TextWhite.Lerp(ASCIIColors.Enemy, 1 - t);
        }
        else
        {
            // Normal color - dimmer if wounded
            float healthPercent = 1.0f;
            if (enemy.HealthComponent != null && enemy.MaxHealth > 0)
            {
                healthPercent = enemy.HealthComponent.CurrentHealth / (float)enemy.MaxHealth;
            }
            else if (enemy.AttributesComponent != null && enemy.MaxHealth > 0)
            {
                healthPercent = enemy.CurrentHealth / (float)enemy.MaxHealth;
            }

            if (healthPercent < 0.5f)
            {
                enemyColor = ASCIIColors.EnemyDim;
            }
            else
            {
                enemyColor = ASCIIColors.Enemy;
            }
        }

        SetEntityCell(screenPos.Value.x, screenPos.Value.y, enemyChar, enemyColor);
    }

    /// <summary>
    /// Get the ASCII character for an enemy based on its name/type.
    /// </summary>
    private static char GetEnemyChar(Enemy enemy)
    {
        var name = enemy.EntityName.ToLower();

        if (name.Contains("heavy") || name.Contains("large"))
        {
            if (name.Contains("drone"))
                return ASCIIChars.DroneHeavy;
            if (name.Contains("sentry"))
                return ASCIIChars.SentryHeavy;
            if (name.Contains("guard"))
                return ASCIIChars.GuardHeavy;
        }

        if (name.Contains("drone"))
            return ASCIIChars.Drone;
        if (name.Contains("sentry"))
            return ASCIIChars.Sentry;
        if (name.Contains("guard"))
            return ASCIIChars.Guard;
        if (name.Contains("boss"))
            return ASCIIChars.Boss;
        if (name.Contains("turret"))
            return ASCIIChars.Turret;

        // Default to drone
        return ASCIIChars.Drone;
    }

    /// <summary>
    /// Trigger a damage flash effect on an entity.
    /// </summary>
    public void TriggerFlash(Node entity)
    {
        _flashTimers[entity] = FlashDuration;
    }

    /// <summary>
    /// Update flash timers. Call each frame.
    /// </summary>
    public void Update(float delta)
    {
        var keysToRemove = new List<Node>();

        foreach (var kvp in _flashTimers)
        {
            var newTime = kvp.Value - delta;
            if (newTime <= 0)
            {
                keysToRemove.Add(kvp.Key);
            }
            else
            {
                _flashTimers[kvp.Key] = newTime;
            }
        }

        foreach (var key in keysToRemove)
        {
            _flashTimers.Remove(key);
        }
    }

    /// <summary>
    /// Clean up references to destroyed entities.
    /// </summary>
    public void RemoveEntity(Node entity)
    {
        _flashTimers.Remove(entity);
    }
}
